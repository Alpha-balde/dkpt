# Bitbucket Pipelines — Analyse détaillée

> Système CI/CD intégré à Bitbucket, avec un fichier unique à la racine
> et des sections pour organiser les différents pipelines.

## Configuration dans DKPT

### Structure

```
bitbucket-pipelines.yml         ← Tout dans un seul fichier (racine obligatoire)
```

### Organisation interne du fichier

```yaml
definitions:
  steps:           # Steps réutilisables via YAML anchors (&nom / *nom)

pipelines:
  default:         # CI — Toutes les branches (sauf main)
  branches:
    main:          # CI + Docker build sha + CD Staging + CD Prod (manual)
  pull-requests:
    '**':          # PR Check
```

### Sections du fichier

| Section | Rôle | Équivalent GitHub Actions |
|---------|------|--------------------------|
| `default:` | CI sur les branches de feature | `ci.yml` |
| `branches: main:` | CI + Docker build sha + CD staging + CD prod (manual) | `ci.yml` + `cd-staging.yml` + `cd-prod.yml` |
| `pull-requests: '**':` | PR check | `pr-check.yml` |

---

## Points forts

- **Simplicité** : Un seul fichier, tout est visible d'un coup
- **YAML anchors** : Mécanisme natif YAML pour réutiliser des steps (`&anchor` / `*reference`)
- **Pipes** : Intégrations pré-packagées (ex: `atlassian/ssh-run`)
- **Deployments** : Support natif des environnements (`deployment: staging`)
- **Intégration Jira** : Lien automatique entre commits, PRs et tickets Jira
- **Docker natif** : Chaque step s'exécute dans un container Docker
- **Rapidité des runners** : Les runners Bitbucket sont performants (backend build en 29s vs 53s sur GitLab)

## Points faibles

- **50 minutes/mois gratuites** : De loin le plus limité du comparatif
- **1 seul fichier** : Pas de pipelines séparés, tout dans `bitbucket-pipelines.yml`
- **Pas de multi-fichiers** : Impossible de séparer les pipelines en fichiers distincts
- **Déclenchement partiel** : Pas de déclenchement vraiment indépendant comme `workflow_run`
- **5 jobs parallèles max** (free tier)
- **YAML anchors seulement** : Pas de vrai système de templates comme Azure DevOps
- **Marketplace limité** : Moins de pipes que d'actions GitHub ou de tasks Azure
- **Artefacts 14 jours** : La durée la plus courte
- **Steps séquentiels** : Sur le pipeline `branches: main:`, les steps s'exécutent les uns après les autres, pas en parallèle
- **Build Once Deploy Many impossible nativement** : Les types de pipelines (`default:`, `branches:`, `pull-requests:`) sont mutuellement exclusifs par branche — aucun mécanisme de chaînage inter-pipelines n'existe

## Spécificités techniques

### YAML anchors pour la réutilisation

```yaml
definitions:
  steps:
    - step: &build-test-backend
        name: 'Backend — Build & Test'
        image: mcr.microsoft.com/dotnet/sdk:9.0
        script:
          - cd backend && dotnet restore && dotnet build
          - dotnet test --verbosity minimal

pipelines:
  default:
    - step: *build-test-backend    # Réutilisation par référence

  branches:
    main:
      - step: *build-test-backend  # Même step, réutilisé
      - step: *docker-build-push
```

C'est du YAML standard, pas une fonctionnalité Bitbucket. L'avantage est la portabilité ; l'inconvénient est que c'est limité (pas de paramètres, pas de composition).

### Deployment environments

```yaml
- step:
    name: 'Deploy to Production'
    deployment: production    # Déclare l'environnement
    trigger: manual           # Gate d'approbation manuelle
```

Bitbucket supporte les environnements de déploiement, mais de façon plus basique que GitHub Actions ou Azure DevOps (pas de gates automatiques, pas de checks).

### Services

```yaml
definitions:
  services:
    docker:
      memory: 2048    # Allouer de la mémoire au service Docker
```

Les services Bitbucket sont des containers auxiliaires (Docker, Redis, PostgreSQL, etc.) qui tournent à côté du step principal.

### Pipes vs Actions

Les **pipes** Bitbucket sont l'équivalent des **actions** GitHub. Mais l'écosystème est nettement plus restreint :

```yaml
# Bitbucket pipe
- pipe: atlassian/ssh-run:0.8.1
  variables:
    SSH_USER: $VPS_USER
    SERVER: $VPS_HOST

# Équivalent GitHub Action
- uses: appleboy/ssh-action@v1
  with:
    host: ${{ secrets.VPS_HOST }}
    username: ${{ secrets.VPS_USER }}
```

---

## Mirroring — Observations

### Méthode utilisée

Bitbucket ne propose pas de pull mirror natif. Le mirroring est effectué via le workflow `mirror.yml` de GitHub Actions avec une paire de clés SSH :

```yaml
- name: Mirror to Bitbucket
  uses: pixta-dev/repository-mirroring-action@v1
  with:
    target_repo_url: git@bitbucket.org:alpha-balde/dkpt-mirror.git
    ssh_private_key: ${{ secrets.BITBUCKET_SSH_KEY }}
```

### Configuration SSH

1. Génération d'une paire de clés SSH dédiée au mirroring
2. Clé publique ajoutée dans Bitbucket → `Repository Settings → Access keys`
3. Clé privée ajoutée dans GitHub → `Settings → Secrets → BITBUCKET_SSH_KEY`

> **Comparaison avec GitLab** : Bitbucket et GitLab utilisent la même méthode de mirroring (SSH via GitHub Actions). La différence est que GitLab **propose** un pull mirror dans l'UI (mais réservé au Premium), tandis que Bitbucket ne propose rien — c'est plus honnête.

---

## Journal d'implémentation

> **Branche** : `feature/gitlab` (merge dans `main`)
> **Date** : 2026-05-13

### Mise en place

| # | Étape | Résultat |
|:-:|-------|---------|
| 1 | Création du repo `dkpt-mirror` sur Bitbucket | ✅ |
| 2 | Génération paire de clés SSH | ✅ |
| 3 | Ajout clé publique dans Bitbucket (Access keys) | ✅ |
| 4 | Ajout `BITBUCKET_SSH_KEY` dans GitHub Secrets | ✅ |
| 5 | Mise à jour URL dans `mirror.yml` | ✅ |
| 6 | Push via `mirror.yml` → Pipeline CI déclenché | ✅ |

### Résultat du premier pipeline

Le pipeline Bitbucket s'est déclenché automatiquement après le mirroring. La section `branches: main:` a été exécutée :

| Step | Durée | Statut |
|------|:-----:|:------:|
| Backend — Build & Test | **29s** | ✅ |
| Frontend — Build | **1m38s** | ✅ |
| Docker Build & Push (Production) | 9s | ❌ (secrets Docker Hub non configurés) |
| Deploy to Production | — | ⏭️ Non exécuté |
| **Total pipeline** | **2m17s** | — |

> L'échec du Docker build est attendu : les secrets `DOCKERHUB_USERNAME` et `DOCKERHUB_TOKEN` ne sont pas encore configurés dans Bitbucket. Le message d'erreur `Must provide --username with --password-stdin` confirme que les variables sont vides.

---

### Problèmes rencontrés et résolus

#### Problème n°1 — Parser YAML : commande vide (`Missing or empty command string`)

| Aspect | Détail |
|--------|--------|
| **Erreur** | `There is an error in your bitbucket-pipelines.yml at [pipelines > branches > main > 2 > step > script > 8]. Missing or empty command string` |
| **Cause** | Une commande `echo` contenant `: ` (deux-points + espace) sans guillemets YAML extérieurs. Le parser YAML interprète `: ` comme un séparateur clé-valeur, produisant un item de script vide |
| **Impact** | Le pipeline est rejeté avant exécution, aucun step ne tourne |
| **Solution** | Encapsuler toute commande contenant `: `, `\|`, `&`, `*` entre guillemets simples YAML |

```yaml
# ❌ Problématique : le parseur YAML voit "sha-$SHORT_SHA" comme valeur d'une clé
- echo "✅ Images pushed: sha-$SHORT_SHA"

# ✅ Correct : guillemets simples YAML protègent le contenu
- 'echo "Images pushed: sha-$SHORT_SHA"'
```

> **Règle générale Bitbucket** : toute commande shell contenant des caractères YAML
> significatifs (`:` `, `\|`, `&`, `*`, `#`, `!`) doit être encapsulée
> en `'...'` ou `"..."` au niveau YAML, indépendamment des guillemets bash.

#### Problème n°2 — `error in libcrypto` avec les variables SSH

| Aspect | Détail |
|--------|--------|
| **Erreur** | `Error loading key "(stdin)": error in libcrypto` |
| **Cause** | Les clés SSH privées multi-lignes copiées depuis Windows contiennent des `\r\n` au lieu de `\n`. OpenSSH exige un format PEM strict avec uniquement des `\n`. Le passage par `tr -d '\r'` ne suffit pas si Bitbucket stocke les newlines comme des caractères littéraux `\n` |
| **Impact** | `ssh-add` échoue, aucune connexion SSH possible depuis le pipeline |
| **Solution** | Utiliser le **SSH Keys natif Bitbucket** (Repository Settings → Pipelines → SSH Keys) |

#### Problème n°3 — `Host key verification failed` (Known hosts manquant)

| Aspect | Détail |
|--------|--------|
| **Erreur** | `Host key verification failed` + `ssh_askpass: exec(/usr/bin/ssh-askpass): No such file or directory` |
| **Cause** | L'IP du VPS prod (`145.241.164.58`) n'était pas dans les Known hosts de Bitbucket SSH Keys |
| **Impact** | SSH refuse la connexion au VPS prod, le step de déploiement échoue |
| **Solution** | Bitbucket → SSH Keys → Known hosts → entrer l'IP → Fetch → Add host |

#### Problème n°4 — `Permission denied (publickey)` sur le VPS prod

| Aspect | Détail |
|--------|--------|
| **Erreur** | `ubuntu@145.241.164.58: Permission denied (publickey)` |
| **Cause** | La clé publique Bitbucket n'était pas dans `~/.ssh/authorized_keys` du VPS prod |
| **Impact** | Connexion SSH impossible même avec le Known host configuré |
| **Solution** | Copier la clé publique depuis Bitbucket SSH Keys et l'ajouter sur le VPS prod |

```bash
# Sur le VPS prod
echo "ssh-rsa AAAA..." >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

> **Piège** : Le VPS staging s'appelle `dev` dans le prompt bash (`ubuntu@dev`).
> Il est facile de confondre staging et prod et d'ajouter la clé sur le mauvais serveur.
> Vérifier le hostname avant toute opération : `hostname` ou `curl ifconfig.me`.

---

## SSH Keys natif Bitbucket

Bitbucket propose un mécanisme dédié pour les clés SSH dans **Repository Settings → Pipelines → SSH Keys**, distinct des variables de dépôt.

### Fonctionnement

| Élément | Détail |
|---------|--------|
| **Clé privée** | Collée directement dans l'UI — Bitbucket gère le format PEM |
| **Clé publique** | Générée automatiquement par Bitbucket — à ajouter dans `~/.ssh/authorized_keys` de chaque VPS cible |
| **Known hosts** | Configurés manuellement par IP ou hostname avec vérification automatique de l'empreinte |
| **Chargement** | Automatique avant chaque step — aucune commande `ssh-agent` / `ssh-add` nécessaire |

### Avantages vs variables SSH

| Aspect | Variables (`$VPS_SSH_KEY`) | SSH Keys natif |
|--------|:------------------------:|:--------------:|
| Format PEM préservé | ⚠️ Dépend de l'encodage | ✅ Garanti |
| CRLF Windows | ❌ Problématique | ✅ Géré automatiquement |
| Commandes `ssh-agent` | ❌ Requises | ✅ Inutiles |
| Known hosts | ❌ Manuel dans le script | ✅ Configurés en UI |
| Sécurité | Variables masquées | Dédié SSH |

### Comparaison inter-plateformes

| Plateforme | Mécanisme SSH | Particularité |
|------------|:-------------:|---------------|
| **GitHub Actions** | Secrets chiffrés | `echo "$KEY" >> key && chmod 600 key` |
| **GitLab CI** | Variable type `File` | GitLab écrit le fichier, `$VAR` = chemin |
| **Bitbucket** | SSH Keys natif | Chargé automatiquement, zéro configuration dans le YAML |

### Configuration adoptée dans DKPT

Après échec des approches par variable, le projet utilise SSH Keys natif :

```yaml
# bitbucket-pipelines.yml — deploy-staging
script:
  # SSH chargé automatiquement via Bitbucket SSH Keys (Repository Settings)
  - ssh $VPS_STAGING_USER@$VPS_STAGING_HOST "bash /opt/dkpt/scripts/deploy.sh"
```

Aucune ligne `ssh-agent`, `ssh-add`, `chmod`, ou gestion de clé. Bitbucket injecte la clé avant chaque step qui en a besoin.

---

## Temps d'exécution observés

| Step | Durée | Notes |
|------|:-----:|-------|
| Backend — Build & Test | **29s** | dotnet restore + build + test (9 tests xUnit) |
| Frontend — Build | **1m38s** | npm ci + build Nuxt (pas de lint) |
| **Total CI (build & test)** | **~2m07s** | Steps séquentiels |

### Comparaison des temps — 3 plateformes

| Métrique | GitHub Actions | GitLab CI | Bitbucket |
|----------|:-:|:-:|:-:|
| Backend build + test | N/A | 53s | **29s** |
| Frontend build | N/A | 1m42s | **1m38s** |
| CI total | **~1m16s** | ~2m | ~2m17s |
| Parallélisme (free) | 20 jobs | 5 jobs | 5 jobs |
| Minutes gratuites | 2 000 | 400 | **50** |

> **Observation** : Bitbucket a les runners les plus rapides par job (backend 29s vs 53s sur GitLab, soit **45% plus rapide**). Cependant, le total est plus élevé car le pipeline `branches: main:` exécute les steps **séquentiellement** (backend → frontend → docker → deploy), tandis que GitHub Actions et GitLab CI exécutent backend et frontend **en parallèle**.

> **Attention** : Avec seulement **50 minutes/mois** gratuites, chaque exécution de la section `main` (~2m17s) consomme environ **4.5%** du quota mensuel. C'est un facteur limitant majeur pour un usage en production.

### Self-hosted runner vs Shared runners

Un self-hosted runner Linux a été configuré pour contourner la limitation des 50 minutes/mois.

#### Configuration du runner

```yaml
# Utilisation dans bitbucket-pipelines.yml
- step:
    <<: *build-test-backend
    runs-on:
      - self.hosted
      - linux
```

#### Mise en place

| Étape | Détail |
|-------|--------|
| **Type** | Runner: Linux Container |
| **Labels** | `self.hosted`, `linux` |
| **Version** | V5 |
| **Installation** | Via Docker sur machine locale |

#### Comparaison des performances

| Métrique | Shared runners | Self-hosted runner |
|----------|:-:|:-:|
| Pipeline CI total | **3m11s** | **3m02s** |
| Consomme des minutes | ✅ Oui (50/mois) | ❌ Non (illimité) |
| Maintenance | Aucune | Hébergement requis |
| Disponibilité | 24/7 | Dépend de la machine hôte |

> **Observation** : Le self-hosted runner est légèrement plus rapide (~3%) mais l'avantage principal est de **ne pas consommer le quota de 50 minutes/mois**. C'est une solution pragmatique pour un projet de test/mémoire.

> **Comparaison avec Azure DevOps** : Azure DevOps utilise aussi un self-hosted agent (ARM64 sur le VPS de production). La différence est que l'agent Azure est permanent et tourne en tant que service, tandis que le runner Bitbucket tourne dans un container Docker local.

#### Problème de syntaxe YAML : `<<:` merge + `runs-on`

Lors de l'ajout du `runs-on` au pipeline `branches: main:`, la première tentative utilisait le merge YAML `<<:` :

```yaml
# ❌ Problématique : <<: merge avec runs-on cause des erreurs de parsing
branches:
  main:
    - step:
        <<: *build-test-backend
        runs-on:
          - self.hosted
          - linux
```

Bitbucket rejette cette syntaxe avec `Missing or empty command string`. La solution correcte est d'inclure `runs-on` **directement dans l'anchor** :

```yaml
# ✅ Correct : runs-on déclaré dans l'anchor
definitions:
  steps:
    - step: &build-test-backend
        name: 'Backend — Build & Test'
        runs-on:
          - self.hosted
          - linux
        script:
          - cd backend && dotnet build

branches:
  main:
    - step: *build-test-backend   # runs-on hérité automatiquement
```

> **Conclusion** : Le YAML merge `<<:` est supporté en YAML standard mais son interaction
> avec le parser Bitbucket est instable quand le bloc fusionné contient des listes (`script:`).
> Préférer l'inclusion directe des propriétés dans l'anchor.

---

## Comparaison avec GitHub Actions

| Aspect | GitHub Actions | Bitbucket |
|--------|:-:|:-:|
| Multi-pipelines | ✅ Multi-fichiers | ❌ Fichier unique |
| Réutilisation | Reusable workflows + Actions | YAML anchors + Pipes |
| Minutes gratuites | 2 000 | 50 |
| Déclenchement indépendant | ✅ `workflow_run` | ⚠️ Branches/tags seulement |
| Marketplace | 20 000+ actions | ~200 pipes |
| Artefacts | 90 jours | 14 jours |
| Mirroring | Via `mirror.yml` workflow | Pas de pull mirror |
| Rapidité runners | Standard | Plus rapide par job |
| Self-hosted runner | ✅ (mais rarement nécessaire) | ✅ (quasi obligatoire vu les 50 min) |
| Temps CI total | ~1m16s | ~3m (séquentiel) |

---

## Limitation architecturale : Build Once, Deploy Many impossible

Le pattern **Build Once, Deploy Many (BODM)** consiste à construire l'image Docker **une seule fois** dans le CI (taguée avec le SHA du commit), puis à la **promouvoir** à travers les environnements (staging → production) sans jamais la reconstruire.

Cette stratégie est **fondamentalement incompatible** avec le modèle de Bitbucket Pipelines.

### Pourquoi ?

Les types de pipelines dans Bitbucket (`default:`, `branches:`, `pull-requests:`) sont **mutuellement exclusifs par branche** :

```
Push sur main
    ├── branches: main:   ✅ se déclenche
    └── default:          ⛔ ignoré (ne tourne PAS si une règle branches: existe)
```

Si l'on place le Docker build dans `default:` (CI), l'image `:sha-xxxxx` est construite et poussée sur Docker Hub. Mais quand `branches: main:` se déclenche pour déployer en production, `default:` **n'a pas tourné** — l'image `:sha` n'existe pas sur Docker Hub.

À l'inverse des autres plateformes :

| Plateforme | Mécanisme de chaînage CI → CD | Garantie d'ordre |
|------------|-------------------------------|:----------------:|
| **Azure DevOps** | `pipeline resources` + `trigger` | ✅ Explicite |
| **GitLab CI** | Parent-child + `needs:` | ✅ Explicite |
| **GitHub Actions** | `workflow_run` + `head_sha` | ✅ Explicite |
| **Bitbucket** | ❌ Aucun mécanisme inter-pipelines | ⛔ Impossible |

### Contournement adopté

Dans le projet DKPT, **seule la branche `main`** est utilisée comme branche de déploiement (staging et production sont déclenchés depuis `main`, comme sur Azure DevOps). Ce choix rend le contournement plus satisfaisant :

Tout le pipeline de déploiement est placé dans `branches: main:` avec des steps séquentiels :

```
branches: main:
  1. Backend Build & Test
  2. Frontend Build
  3. Docker build → :sha-{8chars} → push   ← BUILD ONCE
  4. Deploy staging (pull :sha)            ← DEPLOY 1
  5. Retag :staging → push
  6. Deploy prod (manual, pull :sha)       ← DEPLOY 2 (même image)
  7. Retag :latest → push
```

Puisque les steps s'exécutent dans le même contexte de pipeline, l'image `:sha` est construite **une seule fois** et réutilisée pour les deux environnements — ce qui constitue bien du BODM.

Ceci garantit que l'image déployée en production est **exactement la même** que celle validée en staging, traçable via son tag `:sha-{8chars}`.

> **Ce qui reste différent d'Azure/GitLab/GitHub** : il n'y a pas de pipelines CI et CD
> **séparés et indépendants**. Le CI (build & test) et le CD (docker + deploy) vivent dans
> le même bloc `branches: main:`, ce qui empile toutes les étapes de manière monolithique.
> Sur les autres plateformes, le CI peut échouer sans déclencher le CD, et ils sont
> observables indépendamment dans l'UI. Sur Bitbucket, c'est une seule exécution.

> **Point de comparaison pour le mémoire** : Cette limitation est directement liée au choix
> architectural de Bitbucket d'imposer un fichier unique avec des sections indépendantes.
> Ce modèle simplifie la configuration initiale mais sacrifie la flexibilité d'orchestration
> avancée. C'est l'illustration concrète du compromis entre **simplicité de mise en place**
> et **expressivité du pipeline**.

---

## Validation finale — Pipeline BODM complet

### Résultat après configuration complète

| Step | Statut | Runner | Notes |
|------|:------:|--------:|-------|
| Backend — Build & Test | ✅ | Self-hosted ARM64 | dotnet restore + build + test |
| Frontend — Lint & Build | ✅ | Self-hosted ARM64 | npm ci + lint + build Nuxt |
| Docker — Build & Push `:sha` | ✅ | Self-hosted ARM64 | Build natif, image pousse sur Docker Hub |
| Deploy Staging (automatique) | ✅ | Self-hosted ARM64 | SSH natif, VPS staging `141.253.105.181` |
| Deploy Prod (manuel ▶️) | ✅ | Self-hosted ARM64 | SSH natif, VPS prod `145.241.164.58` |

### Flux de déploiement validé

```
GitHub push → mirror Bitbucket → [ci:bitbucket] détecté
  ↓
CI Backend + Frontend (tests + lint)           ✅  ARM64 natif
  ↓
Docker build :sha-{8chars} → Docker Hub       ✅  Build Once
  ↓
Deploy Staging (pull :sha, retag :staging)     ✅  Deploy Many (1/2)
  ↓
Deploy Prod (manuel, pull :sha, retag :latest) ✅  Deploy Many (2/2)
```

### Variables de déploiement requises

| Variable | Environnement | Valeur |
|----------|:-------------:|--------|
| `VPS_STAGING_HOST` | Staging | IP directe (`141.253.105.181`) |
| `VPS_STAGING_USER` | Staging | `ubuntu` |
| `VPS_HOST` | Production | IP directe (`145.241.164.58`) |
| `VPS_USER` | Production | `ubuntu` |

> **Note** : Les variables `VPS_STAGING_SSH_KEY` et `VPS_SSH_KEY` ne sont plus utilisées
> depuis l'adoption des SSH Keys natifs Bitbucket.

---

## Comparaison avec les autres plateformes

→ Voir [README.md](README.md) pour le tableau synthèse
