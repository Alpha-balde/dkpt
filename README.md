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
| **Infrastructure** | Docker Compose, Nginx reverse proxy |

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
    :80             │    Nginx     │
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
├── README.md               ← Ce fichier
├── docker-compose.yml      ← Orchestration des services
├── .env.example            ← Template des variables d'environnement
├── nginx/
│   └── nginx.conf          ← Configuration du reverse proxy
├── docker/
│   └── seed/
│       ├── seed-data.sql       ← Données de référence (settings, cotisations)
│       ├── run-seed.sh         ← Script de seed automatique
│       └── import-all.ps1     ← Script d'import manuel (PowerShell)
│
├── backend/                ← API .NET 9 (Clean Architecture)
│   ├── Dockerfile
│   ├── Dkpt.sln
│   └── src/
│       ├── Dkpt.Domain/         → Entités, Enums, Interfaces
│       ├── Dkpt.Application/    → DTOs, Interfaces services
│       ├── Dkpt.Infrastructure/ → EF Core, Repositories, Services
│       └── Dkpt.Api/            → Controllers, Program.cs, Swagger
│
├── frontend/               ← Application Nuxt 4
│   ├── Dockerfile
│   ├── nuxt.config.ts
│   └── app/
│       ├── pages/               → Routes (file-based)
│       ├── composables/         → useAuth, useApi
│       ├── layouts/             → default (sidebar), auth (login)
│       ├── middleware/          → auth.global.ts
│       └── types/               → Interfaces TypeScript
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
| 2 — Frontend | ✅ Terminé | Nuxt 4, 10 pages, auth, layout responsive |
| 3 — Infrastructure | ✅ Terminé | Docker Compose, Dockerfiles multi-stage, Nginx, seed auto |
| 4 — CI/CD | ⬜ À faire | GitHub Actions, GitLab CI, Bitbucket, Gitea |
