# DKPT — Diiwal Koïn Préfecture Tougué

Application de gestion des cotisations et des membres de l'association DKPT.

## Stack Technique

| Couche | Technologie |
|--------|------------|
| **Backend** | .NET 9 (ASP.NET Core) — Clean Architecture |
| **Frontend** | Nuxt 4 + Nuxt UI v4 + TailwindCSS v4 |
| **Base de données** | PostgreSQL 16 (Docker) |
| **Auth** | JWT custom (BCrypt) |
| **API Docs** | Swagger / OpenAPI |

## Prérequis

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Un éditeur (VS Code recommandé)

## Démarrage rapide

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

## Comptes de test

| Email | Mot de passe | Rôle |
|-------|-------------|------|
| `admin@dkpt.com` | `Dkpt@2026` | Admin |
| `sg@dkpt.com` | `Dkpt@2026` | Secrétaire |
| `tr@dkpt.com` | `Dkpt@2026` | Trésorier |
| `membre@dkpt.com` | `Dkpt@2026` | Lecteur |

> ⚠️ Ces comptes doivent être recréés après chaque reset de la DB via `/api/Auth/register` avec le paramètre `role`.

## Structure du projet

```
Dkpt/
├── README.md               ← Ce fichier
├── backend/                ← API .NET 9 (Clean Architecture)
│   ├── Dkpt.sln
│   ├── src/
│   │   ├── Dkpt.Domain/         → Entités, Enums, Interfaces
│   │   ├── Dkpt.Application/    → DTOs, Interfaces services
│   │   ├── Dkpt.Infrastructure/ → EF Core, Repositories, Services
│   │   └── Dkpt.Api/            → Controllers, Program.cs, Swagger
│   └── tests/                   → (à venir)
│
├── frontend/               ← Application Nuxt 4
│   ├── nuxt.config.ts
│   ├── app/
│   │   ├── pages/               → Routes (file-based)
│   │   ├── composables/         → useAuth, useApi
│   │   ├── layouts/             → default (sidebar), auth (login)
│   │   ├── middleware/          → auth.global.ts
│   │   └── types/               → Interfaces TypeScript
│   └── package.json
│
├── docs/                   ← Documentation technique
│   ├── architecture.md
│   ├── database-schema.md
│   └── api-endpoints.md
│
└── .agents/workflows/      ← Workflows pour agents IA
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
| 2 — Frontend | ✅ Base posée | Nuxt 4, 10 pages, auth, layout responsive |
| 3 — Infrastructure | ⬜ À faire | Docker Compose, Dockerfiles multi-stage, Nginx |
| 4 — CI/CD | ⬜ À faire | GitHub Actions, GitLab CI, Bitbucket, Gitea |
