# Azure DevOps — Analyse détaillée

> Plateforme CI/CD de Microsoft, avec pipelines YAML et gestion visuelle dans l'UI.

## Configuration dans DKPT

### Structure des fichiers

```
.azuredevops/
├── ci.yml                  ← Pipeline CI (Microsoft-hosted)
├── cd-staging.yml          ← Pipeline CD staging (self-hosted + env agent)
├── cd-prod.yml             ← Pipeline CD production (self-hosted + env agent)
├── pr-check.yml            ← Pipeline PR check (Microsoft-hosted)
└── templates/
    ├── build-dotnet.yml    ← Template réutilisable .NET
    ├── build-nuxt.yml      ← Template réutilisable Nuxt
    └── deploy-vps.yml      ← Template réutilisable déploiement VPS
```

> **Important** : Chaque fichier `.yml` doit être **enregistré manuellement** dans l'UI
> Azure DevOps comme un pipeline distinct (`Pipelines → New pipeline → Existing YAML`).
> C'est la seule plateforme où la configuration est côté UI et non uniquement dans le code.

### Pipelines

| Pipeline | Rôle | Déclencheur | Agent |
|----------|------|-------------|-------|
| `ci.yml` | Build & Test | Push sur `main`/`develop` | Microsoft-hosted (`ubuntu-latest`) |
| `cd-staging.yml` | Docker + Deploy staging | Pipeline resource (après CI sur `develop`) | Self-hosted (Docker) + Env Staging (Deploy) |
| `cd-prod.yml` | Docker + Deploy prod | Pipeline resource (après CI sur `main`) | Self-hosted (Docker) + Env Prod (Deploy) |
| `pr-check.yml` | Vérification PR | PR vers `main`/`develop` | Microsoft-hosted (`ubuntu-latest`) |

### Architecture des agents

```
Pool Default                          Environnements Azure DevOps
┌─────────────────────────┐          ┌──────────────────────────────┐
│ Agent DKPT-ARM64        │          │ Staging (VPS Staging)        │
│ (VPS Staging, ARM64)    │          │   → deploy local, pas de SSH │
│ → Docker build & push   │          │                              │
└─────────────────────────┘          │ Prod (VPS Production)        │
                                     │   → deploy local, pas de SSH │
                                     └──────────────────────────────┘
```

> **Différence fondamentale avec GitHub Actions** : Le déploiement ne se fait pas via SSH
> depuis un runner cloud. L'agent d'environnement tourne **directement sur le VPS** et
> exécute les commandes Docker localement.

---

## Points forts

- **1 fichier = 1 pipeline** : Comme GitHub Actions, organisation claire
- **Templates YAML** : Factorisation native des steps communes (3 templates dans DKPT)
- **Pipeline resources** : Chaînage élégant entre pipelines
- **Environments avancés** : Approvals, gates, checks, policies + agents locaux
- **1 800 minutes/mois gratuites** : Deuxième plus généreux après GitHub
- **10 jobs parallèles** (free tier)
- **Service connections** : Gestion centralisée des connexions (Docker Hub, SSH, etc.)
- **Variable groups** : Secrets partagés entre pipelines, scopés par environnement
- **Self-hosted agents** : Build natif ARM64 sans cross-compilation
- **Intégration Azure** : Déploiement natif vers Azure services

## Points faibles

- **Enregistrement UI obligatoire** : Chaque pipeline doit être créé manuellement dans l'UI
- **Courbe d'apprentissage** : Concepts spécifiques (service connections, pipeline resources, environments)
- **Syntaxe différente** : `task:` au lieu de `uses:`, `displayName:` au lieu de `name:`
- **$40/mois** au-delà du gratuit : Le plus cher du comparatif
- **Marketplace moins riche** : Moins de tasks que les GitHub Actions

## Spécificités techniques

### Templates réutilisables

```yaml
# ci.yml — référence un template
steps:
  - template: templates/build-dotnet.yml
```

```yaml
# templates/build-dotnet.yml — fragment de steps
steps:
  - task: UseDotNet@2
    inputs:
      version: '9.0.x'
  - script: dotnet build
```

Les templates Azure DevOps sont des **fragments de steps** injectés directement dans le job appelant. C'est plus simple que les reusable workflows de GitHub Actions (qui sont des workflows complets).

### Pipeline Resources (chaînage)

```yaml
resources:
  pipelines:
    - pipeline: ci
      source: 'DKPT CI'
      trigger:
        branches:
          include:
            - main
```

C'est l'équivalent de `workflow_run` dans GitHub Actions. Le CD se déclenche après le succès du CI.

### Service Connections

Azure DevOps centralise les connexions externes (Docker Hub, SSH, Azure) dans des **service connections** configurées dans `Project Settings`. Les pipelines y font référence par nom :

```yaml
- task: Docker@2
  inputs:
    containerRegistry: 'dockerhub-connection'  # Référence à la service connection
```

### Variable Groups (secrets par environnement)

```yaml
variables:
  - group: dkpt-secrets   # Variables communes (DOCKERHUB_USERNAME, JWT_ISSUER, etc.)
  - group: dkpt-staging   # Variables spécifiques (POSTGRES_PASSWORD, CORS_ORIGINS, etc.)
```

3 variable groups permettent de partager les secrets communs tout en ayant des valeurs différentes par environnement. C'est plus granulaire que les repository secrets de GitHub Actions.

### Deployment jobs (agents locaux)

```yaml
- deployment: DeployVPS
  environment: 'Staging'    # L'agent tourne sur le VPS !
  strategy:
    runOnce:
      deploy:
        steps:
          - checkout: self
          - template: templates/deploy-vps.yml  # Exécuté localement
```

Azure DevOps a un type de job spécifique pour les déploiements (`deployment`) avec des stratégies (`runOnce`, `rolling`, `canary`). L'agent d'environnement exécute les steps **directement sur le VPS** — pas de SSH.

---

## Comparaison avec GitHub Actions

| Aspect | GitHub Actions | Azure DevOps |
|--------|:-:|:-:|
| Multi-pipelines | ✅ Auto-détecté | ✅ Enregistrement UI |
| Templates | Reusable workflows (workflow complet) | Templates (fragments de steps) |
| Chaînage | `workflow_run` | Pipeline resources |
| Connexions externes | Secrets + env | Service connections |
| Secrets | Repository secrets | 3 variable groups (secrets + staging + prod) |
| Docker build ARM64 | Runner cloud ARM64 | Self-hosted agent sur VPS |
| Déploiement | SSH depuis runner cloud | Agent local sur VPS (pas de SSH) |
| Deployment strategy | Basique (environment) | Avancé (runOnce, rolling, canary) |
| Prix | $0.008/min | $40/mois |

---

## Comparaison avec les autres plateformes

→ Voir [README.md](README.md) pour le tableau synthèse
