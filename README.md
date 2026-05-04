# DKPT — Diiwal Koïn Préfecture Tougué

[![DKPT CI/CD](https://github.com/Alpha-balde/dkpt/actions/workflows/ci.yml/badge.svg)](https://github.com/Alpha-balde/dkpt/actions/workflows/ci.yml)

Application de gestion des cotisations et des membres de l'association DKPT.

## Stack Technique

| Couche | Technologie |
|--------|------------|
| **Backend** | .NET 9 (ASP.NET Core) — Clean Architecture |
| **Frontend** | Nuxt 4 + Nuxt UI v4 + TailwindCSS v4 |
| **Base de données** | PostgreSQL 16 (Docker) |
| **Auth** | JWT custom (BCrypt) |
| **API Docs** | Swagger / OpenAPI |
| **Infrastructure** | Docker Compose, Caddy reverse proxy |
| **CI/CD** | GitHub Actions, GitLab CI, Azure DevOps, Bitbucket Pipelines |
| **Production** | Oracle Cloud VPS (Ubuntu 22.04) |

## Prérequis

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

> Pour le développement local (hors Docker), installer aussi :
> - [.NET 9 SDK](https://dotnet.microsoft.com/download)
> - [Node.js 20+](https://nodejs.org/)

---

## Démarrage rapide (Docker Compose)

### Premier lancement (avec seed des données)

```powershell
# Copier le fichier d'environnement
cp .env.example .env

# Lancer tous les services + importer les données automatiquement
docker compose --profile seed up --build -d
```

L'application est accessible sur **http://localhost** :

| URL | Description |
|-----|-------------|
| `http://localhost` | Application frontend |
| `http://localhost/swagger` | Swagger UI (API docs) |
| `http://localhost/api/swagger` | Redirection vers Swagger |

### Lancements suivants

```powershell
# Les données sont persistées dans le volume Docker
docker compose up -d
```

### Commandes utiles

```powershell
# Voir les logs de tous les services
docker compose logs -f

# Voir les logs du seed uniquement
docker compose logs seed

# État des conteneurs
docker compose ps

# Arrêter
docker compose down

# Reset complet (supprime la base de données)
docker compose down -v
# puis relancer avec --profile seed
```

### Architecture Docker

```
                    ┌──────────────┐
    :80/:443        │    Caddy     │
  ──────────────────┤ reverse proxy│
                    └──────┬───────┘
                     /api  │  /*
                ┌──────────┴─────────┐
                │                    │
         ┌──────┴──────┐     ┌──────┴──────┐
         │   Backend   │     │  Frontend   │
         │  .NET 9     │     │ Nuxt 4 SSR  │
         │   :8080     │     │   :3000     │
         └──────┬──────┘     └─────────────┘
                │                    │
                │  SSR: http://backend:8080/api
                │◄───────────────────┘
         ┌──────┴──────┐
         │ PostgreSQL  │
         │  16-alpine  │
         │   :5432     │
         └─────────────┘
```

## Stratégie de Branching

Le projet suit un modèle **GitHub Flow simplifié** avec les branches suivantes :

```
main         ← Production (branche protégée)
develop      ← Staging / intégration
feature/*    ← Développement de fonctionnalités
```

| Branche | Rôle | Protection |
|---------|------|-----------|
| `main` | Code en production | ✅ PR obligatoire, CI doit passer |
| `develop` | Intégration, déploiement staging | PR recommandée |
| `feature/*` | Développement | Merge vers `develop` ou `main` via PR |

**Workflow** :
1. Créer une branche `feature/nom` depuis `main`
2. Développer et pousser
3. Ouvrir une PR → le pipeline `pr-check.yml` vérifie le code
4. Merge dans `main` après review → CI + CD se déclenchent

---

## CI/CD Multi-Plateformes

Le projet implémente le même pipeline CI/CD sur **4 plateformes** pour comparaison dans le mémoire :

```
GitHub (source de vérité)
    ├── miroir → GitLab      (.gitlab-ci.yml + .gitlab/pipelines/)
    ├── miroir → Bitbucket   (bitbucket-pipelines.yml)
    └── miroir → Azure Repos (.azuredevops/)
```

### Pipelines par plateforme

| Pipeline | GitHub Actions | GitLab CI | Azure DevOps | Bitbucket |
|----------|:-:|:-:|:-:|:-:|
| **CI** | `ci.yml` | `pipelines/ci.yml` | `ci.yml` | section `default` |
| **CD Staging** | `cd-staging.yml` | `pipelines/cd-staging.yml` | `cd-staging.yml` | section `branches: develop` |
| **CD Production** | `cd-prod.yml` | `pipelines/cd-prod.yml` | `cd-prod.yml` | section `branches: main` |
| **PR Check** | `pr-check.yml` | `pipelines/pr-check.yml` | `pr-check.yml` | section `pull-requests` |
| **Mirror** | `mirror.yml` | — | — | — |

### Variantes GitHub Actions (comparaison intra-plateforme)

| Variante | Fichier | Description |
|----------|---------|-------------|
| Parallel | `variante-1-parallel.yml` | Backend et frontend en 2 jobs parallèles |
| Single Job | `variante-2-single-job.yml` | Tout dans 1 seul job séquentiel |
| Matrix | `variante-4-matrix.yml` | Test sur multiples OS × versions runtime |

### Secrets requis (par plateforme)

| Secret | Description |
|--------|------------|
| `DOCKERHUB_USERNAME` | Username Docker Hub |
| `DOCKERHUB_TOKEN` | Access token Docker Hub |
| `VPS_HOST` | IP du VPS production |
| `VPS_USER` | Utilisateur SSH production |
| `VPS_SSH_KEY` | Clé privée SSH production |
| `VPS_STAGING_HOST` | IP du VPS staging |
| `VPS_STAGING_USER` | Utilisateur SSH staging |
| `VPS_STAGING_SSH_KEY` | Clé privée SSH staging |

> 📊 Documentation comparative détaillée : voir `docs/ci-comparison/`

### Tests unitaires

9 tests xUnit couvrant :

| Fichier | Tests | Couverture |
|---------|-------|-----------|
| `AuthServiceTests` | 3 | Hash BCrypt, vérification mot de passe |
| `JwtTokenServiceTests` | 2 | Génération JWT, validation des claims |
| `EntityTests` | 4 | Valeurs par défaut entités, PagedResult |

```bash
cd backend
dotnet test --verbosity minimal
```

---

## Développement local (sans Docker)

### 1. Base de données (PostgreSQL via Docker)

```bash
docker run -d --name dkpt-db \
  -e POSTGRES_DB=dkpt \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:16-alpine
```

### 2. Backend (.NET API)

```bash
cd backend
dotnet restore
dotnet ef database update -p src/Dkpt.Infrastructure -s src/Dkpt.Api
dotnet run --project src/Dkpt.Api
```

API disponible sur : `http://localhost:5244`  
Swagger UI : `http://localhost:5244/swagger`

### 3. Frontend (Nuxt 4)

```bash
cd frontend
npm install
npm run dev
```

Application disponible sur : `http://localhost:3000`

### 4. Import des données existantes

Les fichiers SQL se trouvent dans `../docs/dkpt_sql/` :

```bash
# Depuis la racine du projet
cat ../docs/dkpt_sql/contribution_amounts_rows.sql | docker exec -i dkpt-db psql -U postgres -d dkpt
cat ../docs/dkpt_sql/members_rows.sql | docker exec -i dkpt-db psql -U postgres -d dkpt
cat ../docs/dkpt_sql/payments_rows.sql | docker exec -i dkpt-db psql -U postgres -d dkpt
cat ../docs/dkpt_sql/settings_rows.sql | docker exec -i dkpt-db psql -U postgres -d dkpt
```

Les utilisateurs doivent être créés via l'API (endpoint `/api/Auth/register`).

---

## Comptes de test

| Email | Mot de passe | Rôle |
|-------|-------------|------|
| `admin@dkpt.com` | `Dkpt@2026` | Admin |
| `sg@dkpt.com` | `Dkpt@2026` | Secrétaire |
| `tr@dkpt.com` | `Dkpt@2026` | Trésorier |
| `membre@dkpt.com` | `Dkpt@2026` | Lecteur |

> ⚠️ Ces comptes doivent être créés via `/api/Auth/register` avec le paramètre `role`.
> Le seed automatique importe les données SQL mais ne crée pas les utilisateurs.

## Structure du projet

```
Dkpt/
├── README.md                   ← Ce fichier
├── docker-compose.yml          ← Orchestration des services (dev)
├── docker-compose.prod.yml     ← Orchestration production (images Docker Hub)
├── .env.example                ← Template des variables d'environnement
│
├── .github/workflows/          ← GitHub Actions (1 fichier = 1 pipeline)
│   ├── ci.yml                     CI
│   ├── cd-staging.yml             CD Staging
│   ├── cd-prod.yml                CD Production
│   ├── pr-check.yml               Vérification PR
│   ├── mirror.yml                 Sync repos miroirs
│   ├── variante-1-parallel.yml    Variante parallèle
│   ├── variante-2-single-job.yml  Variante single job
│   └── variante-4-matrix.yml     Variante matrix
│
├── .gitlab-ci.yml              ← GitLab CI orchestrateur parent
├── .gitlab/pipelines/          ← GitLab CI pipelines enfants
│   ├── ci.yml
│   ├── cd-staging.yml
│   ├── cd-prod.yml
│   └── pr-check.yml
│
├── .azuredevops/               ← Azure DevOps
│   ├── ci.yml
│   ├── cd-staging.yml
│   ├── cd-prod.yml
│   ├── pr-check.yml
│   └── templates/
│       ├── build-dotnet.yml
│       └── build-nuxt.yml
│
├── bitbucket-pipelines.yml     ← Bitbucket (tout dans 1 fichier)
│
├── docs/ci-comparison/         ← Documentation comparative (mémoire)
│   ├── README.md
│   ├── github-actions.md
│   ├── gitlab-ci.md
│   ├── azure-devops.md
│   └── bitbucket.md
│
├── caddy/
│   └── Caddyfile                ← Configuration du reverse proxy
├── docker/
│   └── seed/
│       ├── seed-data.sql
│       ├── run-seed.sh
│       └── import-all.ps1
│
├── backend/                    ← API .NET 9 (Clean Architecture)
│   ├── Dockerfile
│   ├── Dkpt.sln
│   ├── src/
│   │   ├── Dkpt.Domain/
│   │   ├── Dkpt.Application/
│   │   ├── Dkpt.Infrastructure/
│   │   └── Dkpt.Api/
│   └── tests/
│       └── Dkpt.Tests/
│
├── frontend/                   ← Application Nuxt 4
│   ├── Dockerfile
│   ├── nuxt.config.ts
│   └── app/
│       ├── pages/
│       ├── composables/
│       ├── layouts/
│       ├── middleware/
│       └── types/
│
└── .agents/workflows/          ← Workflows pour agents IA
```

## Rôles et permissions (RBAC)

| Action | Admin | Secrétaire | Trésorier | Lecteur |
|--------|:-----:|:----------:|:---------:|:-------:|
| Voir membres/paiements | ✅ | ✅ | ✅ | ✅ |
| Créer/modifier membres | ✅ | ✅ | ❌ | ❌ |
| Créer/modifier paiements | ✅ | ❌ | ✅ | ❌ |
| Supprimer (tout) | ✅ | ❌ | ❌ | ❌ |
| Gérer utilisateurs | ✅ | ❌ | ❌ | ❌ |
| Paramètres | ✅ | ❌ | ❌ | ❌ |

## Avancement des phases

| Phase | Statut | Description |
|-------|--------|-------------|
| 1 — Backend API | ✅ Terminé | .NET 9, Clean Architecture, JWT, 5 tables, Swagger |
| 2 — Frontend | ✅ Terminé | Nuxt 4, 10 pages, auth, layout responsive |
| 3 — Infrastructure | ✅ Terminé | Docker Compose, Dockerfiles multi-stage, Caddy, seed auto |
| 4 — CI/CD (GitHub Actions) | ✅ Terminé | CI + CD staging/prod + PR check + variantes + mirroring |
| 4 — CI/CD (GitLab CI) | 📝 Configuré | Pipelines parent-child créés, à tester après mirroring |
| 4 — CI/CD (Azure DevOps) | 📝 Configuré | Pipelines + templates créés, enregistrement UI à faire |
| 4 — CI/CD (Bitbucket) | 📝 Configuré | Fichier pipeline créé, à tester après mirroring |
