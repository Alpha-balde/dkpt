# Azure DevOps — Analyse détaillée

> Plateforme CI/CD de Microsoft, avec pipelines YAML et gestion visuelle dans l'UI.

## Configuration dans DKPT

### Structure des fichiers

```
.azuredevops/
├── ci.yml                  ← Pipeline CI
├── cd-staging.yml          ← Pipeline CD staging
├── cd-prod.yml             ← Pipeline CD production
├── pr-check.yml            ← Pipeline PR check
└── templates/
    ├── build-dotnet.yml    ← Template réutilisable .NET
    └── build-nuxt.yml      ← Template réutilisable Nuxt
```

> **Important** : Chaque fichier `.yml` doit être **enregistré manuellement** dans l'UI
> Azure DevOps comme un pipeline distinct (`Pipelines → New pipeline → Existing YAML`).
> C'est la seule plateforme où la configuration est côté UI et non uniquement dans le code.

### Pipelines

| Pipeline | Rôle | Déclencheur |
|----------|------|-------------|
| `ci.yml` | Build & Test | Push sur `main`/`develop` |
| `cd-staging.yml` | Docker + Deploy staging | Pipeline resource (après CI sur `develop`) |
| `cd-prod.yml` | Docker + Deploy prod | Pipeline resource (après CI sur `main`) |
| `pr-check.yml` | Vérification PR | PR vers `main`/`develop` |

---

## Points forts

- **1 fichier = 1 pipeline** : Comme GitHub Actions, organisation claire
- **Templates YAML** : Factorisation native des steps communes
- **Pipeline resources** : Chaînage élégant entre pipelines
- **Environments avancés** : Approvals, gates, checks, policies
- **1 800 minutes/mois gratuites** : Deuxième plus généreux après GitHub
- **10 jobs parallèles** (free tier)
- **Service connections** : Gestion centralisée des connexions (Docker Hub, SSH, etc.)
- **Intégration Azure** : Déploiement natif vers Azure services

## Points faibles

- **Enregistrement UI obligatoire** : Chaque pipeline doit être créé manuellement dans l'UI
- **Courbe d'apprentissage** : Concepts spécifiques (service connections, pipeline resources)
- **Syntaxe différente** : `task:` au lieu de `uses:`, `displayName:` au lieu de `name:`
- **$40/mois** au-delà du gratuit : Le plus cher du comparatif
- **Marketplace moins riche** : Moins de tasks que les GitHub Actions

## Spécificités techniques

### Templates réutilisables

```yaml
# ci.yml
steps:
  - template: templates/build-dotnet.yml
```

```yaml
# templates/build-dotnet.yml
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

### Deployment jobs

```yaml
- deployment: DeployVPS
  environment: 'production'
  strategy:
    runOnce:
      deploy:
        steps: ...
```

Azure DevOps a un type de job spécifique pour les déploiements (`deployment`) avec des stratégies (`runOnce`, `rolling`, `canary`).

---

## Comparaison avec GitHub Actions

| Aspect | GitHub Actions | Azure DevOps |
|--------|:-:|:-:|
| Multi-pipelines | ✅ Auto-détecté | ✅ Enregistrement UI |
| Templates | Reusable workflows (workflow complet) | Templates (fragments de steps) |
| Chaînage | `workflow_run` | Pipeline resources |
| Connexions externes | Secrets + env | Service connections |
| Deployment strategy | Basique (environment) | Avancé (runOnce, rolling, canary) |
| Prix | $0.008/min | $40/mois |

---

## Comparaison avec les autres plateformes

→ Voir [README.md](README.md) pour le tableau synthèse
