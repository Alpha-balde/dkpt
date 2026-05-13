# GitLab CI — Analyse détaillée

> Système CI/CD intégré à GitLab, utilisant un fichier `.gitlab-ci.yml` unique
> avec le pattern parent-child pour simuler des pipelines séparés.

## Configuration dans DKPT

### Structure des fichiers

```
.gitlab-ci.yml                  ← Orchestrateur parent (racine)
.gitlab/
└── pipelines/
    ├── ci.yml                  ← Pipeline enfant : Build & Test
    ├── cd-staging.yml          ← Pipeline enfant : Deploy staging
    ├── cd-prod.yml             ← Pipeline enfant : Deploy production
    └── pr-check.yml            ← Pipeline enfant : MR check
```

### Pattern parent-child

```yaml
# Orchestrateur parent (.gitlab-ci.yml)
ci:
  stage: ci
  trigger:
    include: .gitlab/pipelines/ci.yml
    strategy: depend
  rules:
    - if: $CI_COMMIT_BRANCH == "main"
```

Le parent déclenche les enfants via `trigger: include:`. Chaque enfant est un pipeline complet avec ses propres stages et jobs.

### Pipelines

| Pipeline | Rôle | Déclencheur |
|----------|------|-------------|
| `ci.yml` | Lint, Build & Test | Push sur `main`/`develop`/toute branche |
| `cd-staging.yml` | Docker + Deploy staging | Après CI sur `develop` |
| `cd-prod.yml` | Docker + Deploy prod (manual) | Après CI sur `main` |
| `pr-check.yml` | Vérification MR | `merge_request_event` |

---

## Points forts

- **Pipeline intégré** : CI/CD, container registry, environments, tout dans un seul outil
- **Parent-child pipelines** : Permet de simuler des pipelines séparés
- **`include:`** : Import de fichiers YAML depuis d'autres repos ou chemins
- **Docker-in-Docker natif** : Service `dind` bien intégré
- **Environments visuels** : Tableau de bord des déploiements par environnement
- **`when: manual`** : Gate d'approbation simple et efficace
- **Auto DevOps** : Templates CI/CD automatiques (non utilisé ici mais notable)
- **Shared runners gratuits** : Pas besoin de configurer d'agent, les runners sont disponibles immédiatement

## Points faibles

- **1 seul fichier racine** : `.gitlab-ci.yml` obligatoire, pas de multi-fichiers natif
- **400 minutes/mois gratuites** : Le plus limité du comparatif
- **Complexité parent-child** : Plus verbeux que GitHub Actions pour le même résultat
- **5 jobs parallèles max** (free tier) : Moitié moins que GitHub Actions
- **Pull mirror réservé au plan Premium** : Le mirroring natif (GitHub → GitLab) n'est pas disponible sur le free tier. Seul le push mirror est disponible, mais il pousse FROM GitLab, ce qui est inutile quand GitLab est le miroir
- **Alternative mirroring** : Nécessite un push via GitHub Actions (`mirror.yml`) ou un `git remote add` local

## Spécificités techniques

### Services Docker-in-Docker

```yaml
docker-build:
  image: docker:27
  services:
    - docker:27-dind
  variables:
    DOCKER_TLS_CERTDIR: "/certs"
```

GitLab nécessite un service `dind` explicite pour builder des images Docker dans le pipeline. C'est plus verbeux que GitHub Actions qui a Docker préinstallé sur les runners.

### Cache par branche

```yaml
cache:
  key: dotnet-${CI_COMMIT_REF_SLUG}
  paths:
    - backend/**/bin/
    - backend/**/obj/
```

Le cache GitLab est scopé par branche via `$CI_COMMIT_REF_SLUG`, ce qui est plus fin que le cache GHA.

### Variables d'environnement

GitLab utilise des **variables CI/CD** configurées dans `Settings → CI/CD → Variables`. Pas de concept de "secrets" séparé comme GitHub — tout est dans le même mécanisme.

---

## Mirroring — Observations

### Pull mirror natif (indisponible)

Le pull mirror natif de GitLab (GitHub → GitLab automatique) est **réservé au plan Premium** ($29/mois). Sur le free tier, seul le **push mirror** est disponible (GitLab → cible), ce qui est inutile quand GitLab est le repo miroir.

> **Piège rencontré** : La direction "Push" dans le formulaire de mirroring est pré-sélectionnée et non modifiable sur le free tier. Un push mirror vers GitHub a été configuré par erreur, résultant en un repo GitLab vide.

### Solution adoptée

```bash
# Ajout de GitLab comme remote secondaire (une seule fois)
git remote add gitlab https://gitlab.com/Alpha_Balde/dkpt-mirror.git

# Push direct vers GitLab
git push gitlab feature/gitlab
```

### Automatisation future

Le workflow `mirror.yml` de GitHub Actions poussera automatiquement vers GitLab à chaque push sur `main` :

```yaml
- name: Mirror to GitLab
  uses: pixta-dev/repository-mirroring-action@v1
  with:
    target_repo_url: git@gitlab.com:Alpha_Balde/dkpt-mirror.git
    ssh_private_key: ${{ secrets.GITLAB_SSH_KEY }}
```

### Comparaison du mirroring

| Aspect | GitHub Actions | GitLab CI |
|--------|:-:|:-:|
| Pull mirror natif | ❌ Non disponible | ⚠️ Premium uniquement |
| Push mirror natif | ❌ Non disponible | ✅ Disponible (free tier) |
| Alternative | `mirror.yml` workflow | `git remote add` ou push via GitHub Actions |

---

## Journal d'implémentation

> **Branche** : `feature/gitlab`
> **Date** : 2026-05-13

### Problèmes rencontrés

| # | Problème | Type | Solution |
|:-:|----------|------|----------|
| 1 | Pull mirror indisponible sur le free tier GitLab | Limitation plateforme | Push direct via `git remote add gitlab` |
| 2 | Push mirror configuré dans la mauvaise direction (Push au lieu de Pull) | Erreur configuration | Suppression du mirror, push direct |
| 3 | ESLint crash avec Node 20 (`Object.groupBy is not a function`) | Compatibilité runtime | Image `node:22-alpine` au lieu de `node:20-alpine` |
| 4 | ESLint erreur `nuxt/nuxt-config-keys-order` bloque le pipeline | Règle opinionated auto-fixable | Ajout de `npm run lint -- --fix` avant `npm run lint` |

#### Problème n°1 — Pull mirror réservé au plan Premium

| Aspect | Détail |
|--------|--------|
| **Erreur** | Le formulaire de mirroring ne propose que la direction "Push" sur le free tier |
| **Cause** | GitLab réserve la fonctionnalité pull mirror au plan Premium ($29/mois) |
| **Impact** | Impossible de synchroniser automatiquement GitHub → GitLab via l'interface GitLab |
| **Solution** | Ajout de GitLab comme remote Git secondaire + push direct. Automatisation via `mirror.yml` (GitHub Actions) |

> **Point de comparaison pour le mémoire** : C'est la seule plateforme parmi les 4 qui restreint le mirroring entrant sur un plan payant. Bitbucket et Azure Repos ne proposent pas de pull mirror non plus, mais ne donnent pas l'impression de le faire.

#### Problème n°2 — Push mirror mal configuré

| Aspect | Détail |
|--------|--------|
| **Erreur** | Repo GitLab affiché comme vide malgré le mirror "successful" |
| **Cause** | Le push mirror pousse FROM GitLab TO la cible. Comme GitLab est vide, il n'y a rien à pousser |
| **Solution** | Suppression du mirror, utilisation d'un `git push` direct |

#### Problème n°3 — Node 20 incompatible avec ESLint

| Aspect | Détail |
|--------|--------|
| **Erreur** | `TypeError: Object.groupBy is not a function` dans `eslint-flat-config-utils` |
| **Cause** | Node.js 20 ne supporte pas `Object.groupBy` (introduit en Node.js 21+, stable en 22 LTS) |
| **Impact** | ESLint ne peut pas démarrer du tout |
| **Solution** | Mise à jour de l'image Docker de `node:20-alpine` → `node:22-alpine` |

> **Note** : Ce même problème a été rencontré sur Azure DevOps (voir [quality-gates-integration.md](quality-gates-integration.md), Problème n°1). Il est spécifique à ESLint 10 + `@nuxt/eslint` qui dépend de `eslint-flat-config-utils`.

#### Problème n°4 — ESLint `nuxt-config-keys-order`

| Aspect | Détail |
|--------|--------|
| **Erreur** | `Expected config key "modules" to come before "devtools"` dans `nuxt.config.ts` |
| **Cause** | La règle `nuxt/nuxt-config-keys-order` impose un ordre canonique des clés dans `defineNuxtConfig()` |
| **Impact** | Pipeline en échec — l'erreur est auto-fixable mais pas auto-corrigée |
| **Solution** | Ajout de `npm run lint -- --fix` dans le pipeline avant `npm run lint`, ce qui corrige automatiquement l'ordre des clés |

> **Spécificité GitLab CI** : Le `--fix` modifie les fichiers dans le workspace du runner. Comme on ne commite pas ces changements, le lint suivant vérifie que seuls des warnings subsistent. C'est une technique utile pour les règles auto-fixables qui ne méritent pas un commit dédié.

---

## Temps d'exécution observés

| Job | Durée | Notes |
|-----|:-----:|-------|
| `backend-build-test` | **53s** | dotnet restore + build + test (9 tests xUnit) |
| `frontend-lint-build` | **1m42s** | npm ci + lint --fix + lint + build Nuxt |
| **Total pipeline CI** | **~2m** | 2 jobs parallèles sur shared runners |

### Comparaison des temps avec GitHub Actions

| Métrique | GitHub Actions | GitLab CI |
|----------|:-:|:-:|
| CI total (backend + frontend) | ~1m16s | ~1m42s |
| Backend build + test | Non mesuré séparément | 53s |
| Frontend lint + build | Non mesuré séparément | 1m42s |
| Parallélisme | 20 jobs max (free) | 5 jobs max (free) |

> Les temps GitLab CI sont légèrement plus longs que GitHub Actions, probablement dû aux shared runners GitLab (moins de CPU/RAM sur le free tier) et au téléchargement des images Docker (node:22-alpine, dotnet/sdk:9.0) à chaque exécution.

---

## Comparaison avec GitHub Actions

| Aspect | GitHub Actions | GitLab CI |
|--------|:-:|:-:|
| Multi-pipelines | ✅ Natif | ⚠️ Parent-child |
| Syntaxe pour chaîner | `workflow_run` | `trigger: include:` + `needs:` |
| Docker build | Docker préinstallé | Service `dind` requis |
| Réutilisation | Reusable workflows + Actions | `include:` + parent-child |
| Minutes gratuites | 2 000 | 400 |
| Mirroring natif | ❌ (via workflow) | ⚠️ Push only (free tier) |
| Node.js | Via `setup-node` action | Via image Docker |
| Debugging | `act` local | Terminal interactif (payant) |
| Temps CI total | ~1m16s | ~1m42s |

---

## Comparaison avec les autres plateformes

→ Voir [README.md](README.md) pour le tableau synthèse

