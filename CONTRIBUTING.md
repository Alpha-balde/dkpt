# Contribuer au projet DKPT

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

## Tests unitaires

9 tests xUnit couvrant :

| Fichier | Tests | Couverture |
|---------|-------|-----------:|
| `AuthServiceTests` | 3 | Hash BCrypt, vérification mot de passe |
| `JwtTokenServiceTests` | 2 | Génération JWT, validation des claims |
| `EntityTests` | 4 | Valeurs par défaut entités, PagedResult |

```bash
cd backend
dotnet test --verbosity minimal
```

---

## CI/CD Multi-Plateformes

Le projet implémente le même pipeline CI/CD sur **4 plateformes** :

```
GitHub (source de vérité)
    ├── miroir → GitLab      (.gitlab-ci.yml + .gitlab/pipelines/)
    ├── miroir → Bitbucket   (bitbucket-pipelines.yml)
    └── GitHub App → Azure DevOps (.azuredevops/)
```

### Pipelines par plateforme

| Pipeline | GitHub Actions | GitLab CI | Azure DevOps | Bitbucket |
|----------|:-:|:-:|:-:|:-:|
| **CI** | `ci.yml` | `pipelines/ci.yml` | `ci.yml` | section `default` |
| **CD Staging** | `cd-staging.yml` | `pipelines/cd-staging.yml` | `cd-staging.yml` | section `branches: main` |
| **CD Production** | `cd-prod.yml` | `pipelines/cd-prod.yml` | `cd-prod.yml` | section `branches: main` |
| **PR Check** | `pr-check.yml` | `pipelines/pr-check.yml` | `pr-check.yml` | section `pull-requests` |
| **Mirror** | `mirror.yml` | — | — | — |

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

---

## Structure du projet

```
Dkpt/
├── README.md
├── CONTRIBUTING.md             ← Ce fichier
├── docker-compose.yml          ← Orchestration des services (dev)
├── docker-compose.prod.yml     ← Orchestration production
├── .env.example
│
├── .github/workflows/          ← GitHub Actions
│   ├── ci.yml
│   ├── cd-staging.yml
│   ├── cd-prod.yml
│   ├── pr-check.yml
│   └── mirror.yml
│
├── .gitlab-ci.yml              ← GitLab CI orchestrateur
├── .gitlab/pipelines/          ← GitLab CI pipelines enfants
│
├── .azuredevops/               ← Azure DevOps
│   ├── ci.yml
│   ├── cd-staging.yml
│   ├── cd-prod.yml
│   └── templates/
│
├── bitbucket-pipelines.yml     ← Bitbucket (tout dans 1 fichier)
│
├── caddy/Caddyfile             ← Reverse proxy
├── docker/seed/                ← Données initiales
│
├── backend/                    ← API .NET 9 (Clean Architecture)
│   ├── Dockerfile
│   ├── src/
│   │   ├── Dkpt.Domain/
│   │   ├── Dkpt.Application/
│   │   ├── Dkpt.Infrastructure/
│   │   └── Dkpt.Api/
│   └── tests/Dkpt.Tests/
│
└── frontend/                   ← Application Nuxt 4
    ├── Dockerfile
    ├── nuxt.config.ts
    └── app/
```
