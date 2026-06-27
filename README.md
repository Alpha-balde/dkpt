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
> - [Node.js 22](https://nodejs.org/)

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

### Lancements suivants

```powershell
docker compose up -d
```

### Commandes utiles

```powershell
docker compose logs -f          # Logs de tous les services
docker compose ps               # État des conteneurs
docker compose down             # Arrêter
docker compose down -v          # Reset complet (supprime la base de données)
```

---

## Architecture Docker

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

---

## Comptes de test

| Email | Rôle |
|-------| :---:|
| `admin@dkpt.com` | Admin |
| `sg@dkpt.com` |  Secrétaire |
| `tr@dkpt.com` |  Trésorier |
| `membre@dkpt.com` |  Lecteur |

> ⚠️ Ces comptes doivent être créés via `/api/Auth/register` avec le paramètre `role`.
> Le seed automatique importe les données SQL mais ne crée pas les utilisateurs.

## Rôles et permissions (RBAC)

| Action | Admin | Secrétaire | Trésorier | Lecteur |
|--------|:-----:|:----------:|:---------:|:-------:|
| Voir membres/paiements | ✅ | ✅ | ✅ | ✅ |
| Créer/modifier membres | ✅ | ✅ | ❌ | ❌ |
| Créer/modifier paiements | ✅ | ❌ | ✅ | ❌ |
| Supprimer (tout) | ✅ | ❌ | ❌ | ❌ |
| Gérer utilisateurs | ✅ | ❌ | ❌ | ❌ |
| Paramètres | ✅ | ❌ | ❌ | ❌ |