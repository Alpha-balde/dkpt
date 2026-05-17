# Sélecteur de plateforme CI/CD — Stratégie par mot-clé commit

> **Observation pour le mémoire** : Cette stratégie résout un problème concret
> du multi-plateforme CI/CD — éviter que toutes les plateformes déclenchent
> leur pipeline à chaque push, ce qui consomme inutilement du compute et produit
> des déploiements redondants.

---

## Problème

Le projet DKPT est hébergé sur **GitHub** (source de vérité) et mirroré vers
**GitLab** et **Bitbucket** via le workflow `mirror.yml`. À chaque push sur `main` :

```
Push GitHub → mirror → GitLab → CI+CD GitLab se déclenche
                      → Bitbucket → CI+CD Bitbucket se déclenche
```

Résultat : **3 pipelines CI/CD complets** s'exécutent pour chaque commit, même
lorsque l'objectif est uniquement d'observer une plateforme spécifique.

---

## Solution : Sélecteur par mot-clé dans le message de commit

Chaque plateforme vérifie la présence d'un **mot-clé spécifique** dans le
message de commit avant d'exécuter son pipeline. Sans mot-clé, la plateforme
reste silencieuse.

### Mots-clés définis

| Mot-clé | Plateforme activée | CI | CD Staging | CD Prod |
|---------|-------------------|:--:|:----------:|:-------:|
| `[ci:github]` | GitHub Actions | ✅ | ✅ | ✅ |
| `[ci:gitlab]` | GitLab CI | ✅ | ✅ | ✅ |
| `[ci:bitbucket]` | Bitbucket Pipelines | ✅ | ✅ | ✅ |
| *(aucun)* | Aucune | ❌ | ❌ | ❌ |

### Exemples d'utilisation

```bash
# Déclencher uniquement GitHub Actions
git commit -m "fix: correction du bug d'authentification [ci:github]"
git push origin main

# Déclencher uniquement GitLab CI (pour observer les temps d'exécution)
git commit -m "perf: optimisation des migrations [ci:gitlab]"
git push origin main

# Déclencher uniquement Bitbucket (pour tester le runner self-hosted)
git commit -m "release: v1.2.0 [ci:bitbucket]"
git push origin main

# Aucune plateforme (ex : modification docs uniquement)
git commit -m "docs: mise à jour du README"
git push origin main
```

> **Note** : Le workflow `mirror.yml` (synchronisation des dépôts) s'exécute
> **indépendamment** du mot-clé — les 3 dépôts restent toujours synchronisés.

---

## Implémentation par plateforme

### GitHub Actions — Condition native sur `head_commit.message`

**Mécanisme** : Condition `if:` au niveau job, utilisant la fonction
`contains()` sur la variable `github.event.head_commit.message`.

```yaml
# .github/workflows/ci.yml
jobs:
  build-and-test:
    runs-on: ubuntu-latest
    if: |
      github.event_name == 'workflow_dispatch' ||
      contains(github.event.head_commit.message, '[ci:github]')

  docker-build-sha:
    needs: build-and-test
    if: github.event_name == 'workflow_dispatch' || (github.event_name == 'push' && contains(github.event.head_commit.message, '[ci:github]'))
```

**CD Staging et CD Production** : Ces workflows se déclenchent via
`workflow_run` depuis le CI. Si le CI ne s'exécute pas (absence de
`[ci:github]`), le CD ne démarre jamais — **la condition est transitivement
appliquée** sans duplication.

#### Limitation : pas de variable réutilisable pour les conditions `if:`

Une question naturelle est de factoriser la condition dans une variable pour
éviter la duplication entre les jobs. Cette approche **ne fonctionne pas**
dans GitHub Actions :

```yaml
# ❌ NE FONCTIONNE PAS
env:
  IS_GITHUB_TARGET: ${{ contains(github.event.head_commit.message, '[ci:github]') }}

jobs:
  build-and-test:
    if: env.IS_GITHUB_TARGET == 'true'  # ← contexte env: non disponible ici !
```

Le contexte `env:` n'est **pas accessible** dans les conditions `if:` au niveau
job. GitHub Actions restreint les contextes disponibles à ce niveau :

| Contexte | Disponible dans `if:` job |
|----------|:-:|
| `github.*` | ✅ |
| `needs.*` | ✅ |
| `inputs.*` | ✅ |
| `vars.*` | ✅ |
| `env.*` | ❌ Non disponible |

La seule solution DRY est un **job dédié avec `outputs`** :

```yaml
jobs:
  check:
    runs-on: ubuntu-latest
    outputs:
      run: ${{ github.event_name == 'workflow_dispatch' || contains(github.event.head_commit.message, '[ci:github]') }}
    steps:
      - run: echo "Platform check"

  build-and-test:
    needs: check
    if: needs.check.outputs.run == 'true'   # DRY ✅

  docker-build-sha:
    needs: [check, build-and-test]
    if: needs.check.outputs.run == 'true'   # DRY ✅
```

Ce pattern ajoute un job supplémentaire (~5-10s de latence). Pour seulement
2 jobs concernés, la **duplication de la condition reste acceptable** et
évite une complexité inutile.

> **Comparaison avec GitLab** : GitLab CI permet de définir des variables
> globales réutilisables dans les `rules:` sans job intermédiaire ni overhead.
> C'est une limitation architecturale de GitHub Actions.

#### Bug découvert : `if: |` évalué comme toujours vrai

Lors de l'implémentation initiale, la condition a été écrite avec la syntaxe
YAML multi-ligne `|` (bloc littéral) :

```yaml
# ❌ MAUVAIS — le CI s'est quand même déclenché
if: |
  github.event_name == 'workflow_dispatch' ||
  contains(github.event.head_commit.message, '[ci:github]')
```

GitHub Actions a évalué cette expression comme une **string non vide**
(et non comme une expression booléenne), qui vaut toujours `true` —
le job s'exécutait donc à chaque push sans vérifier le mot-clé.

Correction : expression sur **une seule ligne** (syntaxe correcte) :

```yaml
# ✅ CORRECT
if: github.event_name == 'workflow_dispatch' || contains(github.event.head_commit.message, '[ci:github]')
```

> Commit de correction : `c503554`

**Évaluation** :
- ✅ Support natif, syntaxe YAML déclarative
- ✅ Lisible et auto-documenté dans le fichier de configuration
- ✅ `workflow_dispatch` toujours disponible pour déclenchement manuel
- ✅ CD conditionnel sans modification des fichiers cd-staging/cd-prod
- ⚠️ Condition dupliquée sur 2 jobs (limitation GitHub Actions — pas de variable réutilisable dans `if:` job)

---

### GitLab CI — Règle `rules:` avec regex sur `CI_COMMIT_MESSAGE`

**Mécanisme** : Variable prédéfinie `$CI_COMMIT_MESSAGE` combinée à l'opérateur
`=~` (regex) dans les `rules:` du fichier orchestrateur `.gitlab-ci.yml`.

```yaml
# .gitlab-ci.yml
ci:
  stage: ci
  trigger:
    include: .gitlab/pipelines/ci.yml
    strategy: depend
  rules:
    - if: '$CI_COMMIT_BRANCH == "main" && $CI_COMMIT_MESSAGE =~ /\[ci:gitlab\]/'

cd-staging:
  stage: cd-staging
  trigger:
    include: .gitlab/pipelines/cd-staging.yml
    strategy: depend
  rules:
    - if: '$CI_COMMIT_BRANCH == "main" && $CI_COMMIT_MESSAGE =~ /\[ci:gitlab\]/'
  needs: [ci]

cd-prod:
  stage: cd-prod
  trigger:
    include: .gitlab/pipelines/cd-prod.yml
    strategy: depend
  rules:
    - if: '$CI_COMMIT_BRANCH == "main" && $CI_COMMIT_MESSAGE =~ /\[ci:gitlab\]/'
  needs: [ci]
```

**Évaluation** :
- ✅ Support natif, `$CI_COMMIT_MESSAGE` est une variable prédéfinie de GitLab
- ✅ Opérateur `=~` avec regex nativement supporté dans les `rules:`
- ✅ Un seul point de contrôle (le fichier orchestrateur parent)
- ✅ Les pipelines enfants (`ci.yml`, `cd-staging.yml`, `cd-prod.yml`) ne sont
  pas modifiés — isolation des responsabilités

---

### Bitbucket Pipelines — Guard bash via `git log`

**Mécanisme** : Bitbucket Pipelines ne fournit **pas** de variable
`$BITBUCKET_COMMIT_MESSAGE` dans ses variables prédéfinies (contrairement à
GitHub et GitLab). La vérification est implémentée en **bash** en tête de
chaque step, en lisant le message du dernier commit via `git log`.

```yaml
# bitbucket-pipelines.yml — exemple sur un step
- step: &build-test-backend
    name: 'Backend — Build & Test'
    image: mcr.microsoft.com/dotnet/sdk:9.0
    script:
      # Guard plateforme : vérification du mot-clé dans le message de commit
      - 'git log -1 --pretty=%B | grep -q "ci:bitbucket" || { echo "⏩ [ci:bitbucket] absent — step ignoré"; exit 0; }'
      # Suite normale du step...
      - cd backend && dotnet restore && dotnet build ...
```

Ce pattern est appliqué à **tous les steps** du pipeline `branches: main:` :
1. Backend Build & Test
2. Frontend Lint & Build
3. Docker Build & Push `:sha`
4. Deploy Staging + Retag `:staging`
5. Deploy Production + Retag `:latest`

**Évaluation** :
- ⚠️ Workaround bash — pas de support natif YAML
- ✅ Fonctionnel et fiable (`git log -1` est toujours disponible)
- ✅ Le step s'affiche dans l'UI Bitbucket (exit 0 = succès) mais ne fait rien
- ❌ Pas déclaratif — la logique de contrôle est mélangée avec le script métier
- ❌ Doit être répété sur chaque step (pas de point de contrôle central)

#### Bug découvert : `[[` interprété comme séquence YAML

Lors de l'implémentation initiale, le guard utilisait la syntaxe bash `[[ ]]` :

```yaml
# ❌ MAUVAIS — erreur Bitbucket : "Missing or empty command string"
- COMMIT_MSG=$(git log -1 --pretty=%B)
- if [[ "$COMMIT_MSG" != *"[ci:bitbucket]"* ]]; then echo "..."; exit 0; fi
```

Bitbucket a retourné l'erreur :
```
Configuration error at [pipelines > branches > main > 2 > step > script > 9].
Missing or empty command string.
```

Le parseur YAML de Bitbucket interprète `[[` comme le début d'une **séquence
YAML imbriquée** (flow sequence), corrompant la structure du script. La présence
de `[ci:bitbucket]` (avec `[` et `]`) aggrave le problème.

Correction : encapsuler le guard dans une **chaîne YAML single-quotée** et
utiliser `grep -q` à la place de `[[ ]]` :

```yaml
# ✅ CORRECT — YAML single-quote + grep évite toute ambiguïté
- 'git log -1 --pretty=%B | grep -q "ci:bitbucket" || { echo "⏩ absent"; exit 0; }'
```

La chaîne YAML single-quotée (`'...'`) neutralise tous les caractères spéciaux
YAML (`[`, `]`, `{`, `}`). Bash reçoit le contenu tel quel et l'exécute.

> **Observation pour le mémoire** : Cette limitation de Bitbucket illustre
> un manque de maturité par rapport à ses concurrents. L'absence d'une variable
> `$BITBUCKET_COMMIT_MESSAGE`, d'un mécanisme de conditions globales, ET
> les contraintes du parseur YAML sur les caractères `[` et `]` dans les scripts
> forcent à des contournements non triviaux, réduisant la lisibilité et la
> maintenabilité de la configuration.

---

## Comparaison des approches

| Critère | GitHub Actions | GitLab CI | Bitbucket |
|---------|:-:|:-:|:-:|
| **Variable commit message native** | ✅ `head_commit.message` | ✅ `$CI_COMMIT_MESSAGE` | ❌ Absente |
| **Condition déclarative YAML** | ✅ `if: contains(...)` | ✅ `rules: if: $VAR =~ /regex/` | ❌ |
| **Point de contrôle unique** | ✅ (niveau job) | ✅ (orchestrateur parent) | ❌ (par step) |
| **Regex native** | ❌ (fonction `contains`) | ✅ (opérateur `=~`) | ❌ |
| **Maintenance** | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| **Lisibilité config** | ⭐⭐⭐ | ⭐⭐⭐ | ⭐ |

---

## Impact sur le workflow Mirror

Le workflow `mirror.yml` (synchronisation GitHub → GitLab/Bitbucket) **n'est
pas conditionné** par le mot-clé. Il s'exécute à chaque push sur `main` pour
garantir que les 3 dépôts restent synchronisés.

```
Push GitHub (mot-clé absent)
    │
    ├── CI GitHub     → SKIP (pas de [ci:github])
    ├── mirror.yml    → ✅ TOUJOURS (sync repos)
    │       │
    │       ├── GitLab mis à jour → CI GitLab → SKIP (pas de [ci:gitlab])
    │       └── Bitbucket mis à jour → CI BB → steps ignorés (guard bash)
    └── CD GitHub     → SKIP (CI n'a pas tourné)
```

---

## Fichiers modifiés

| Fichier | Modification |
|---------|-------------|
| `.github/workflows/ci.yml` | `if: contains(...)` sur les jobs `build-and-test` et `docker-build-sha` |
| `.gitlab-ci.yml` | `rules: if:` avec regex sur les 3 triggers (ci, cd-staging, cd-prod) |
| `bitbucket-pipelines.yml` | Guard bash `git log -1` sur les 5 steps du pipeline `branches: main:` |

> Commit d'implémentation : `0fcecc8`
> Message : `feat(ci): add platform selector via commit message keyword`
