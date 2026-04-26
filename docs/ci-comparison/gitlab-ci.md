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
| `ci.yml` | Build & Test | Push sur `main`/`develop` |
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

## Points faibles

- **1 seul fichier racine** : `.gitlab-ci.yml` obligatoire, pas de multi-fichiers natif
- **400 minutes/mois gratuites** : Le plus limité du comparatif
- **Complexité parent-child** : Plus verbeux que GitHub Actions pour le même résultat
- **5 jobs parallèles max** (free tier) : Moitié moins que GitHub Actions
- **Pull mirror délai** : Jusqu'à 5 min de latence si pas de webhook

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

## Comparaison avec GitHub Actions

| Aspect | GitHub Actions | GitLab CI |
|--------|:-:|:-:|
| Multi-pipelines | ✅ Natif | ⚠️ Parent-child |
| Syntaxe pour chaîner | `workflow_run` | `trigger: include:` + `needs:` |
| Docker build | Docker préinstallé | Service `dind` requis |
| Réutilisation | Reusable workflows + Actions | `include:` + parent-child |
| Minutes gratuites | 2 000 | 400 |
| Debugging | `act` local | Terminal interactif (payant) |

---

## Comparaison avec les autres plateformes

→ Voir [README.md](README.md) pour le tableau synthèse
