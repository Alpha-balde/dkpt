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
  default:         # CI — Toutes les branches
  branches:
    develop:       # CD Staging
    main:          # CD Production
  pull-requests:
    '**':          # PR Check
```

### Sections du fichier

| Section | Rôle | Équivalent GitHub Actions |
|---------|------|--------------------------|
| `default:` | CI sur toutes les branches | `ci.yml` |
| `branches: develop:` | CD staging | `cd-staging.yml` |
| `branches: main:` | CD production | `cd-prod.yml` |
| `pull-requests: '**':` | PR check | `pr-check.yml` |

---

## Points forts

- **Simplicité** : Un seul fichier, tout est visible d'un coup
- **YAML anchors** : Mécanisme natif YAML pour réutiliser des steps (`&anchor` / `*reference`)
- **Pipes** : Intégrations pré-packagées (ex: `atlassian/ssh-run`)
- **Deployments** : Support natif des environnements (`deployment: staging`)
- **Intégration Jira** : Lien automatique entre commits, PRs et tickets Jira
- **Docker natif** : Chaque step s'exécute dans un container Docker

## Points faibles

- **50 minutes/mois gratuites** : De loin le plus limité du comparatif
- **1 seul fichier** : Pas de pipelines séparés, tout dans `bitbucket-pipelines.yml`
- **Pas de multi-fichiers** : Impossible de séparer les pipelines en fichiers distincts
- **Déclenchement partiel** : Pas de déclenchement vraiment indépendant comme `workflow_run`
- **5 jobs parallèles max** (free tier)
- **YAML anchors seulement** : Pas de vrai système de templates comme Azure DevOps
- **Marketplace limité** : Moins de pipes que d'actions GitHub ou de tasks Azure
- **Artefacts 14 jours** : La durée la plus courte

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

## Comparaison avec GitHub Actions

| Aspect | GitHub Actions | Bitbucket |
|--------|:-:|:-:|
| Multi-pipelines | ✅ Multi-fichiers | ❌ Fichier unique |
| Réutilisation | Reusable workflows + Actions | YAML anchors + Pipes |
| Minutes gratuites | 2 000 | 50 |
| Déclenchement indépendant | ✅ `workflow_run` | ⚠️ Branches/tags seulement |
| Marketplace | 20 000+ actions | ~200 pipes |
| Artefacts | 90 jours | 14 jours |

---

## Comparaison avec les autres plateformes

→ Voir [README.md](README.md) pour le tableau synthèse
