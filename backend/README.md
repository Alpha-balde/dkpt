# DKPT Backend — API .NET 9

API REST pour la gestion de l'association DKPT, structurée en Clean Architecture.

## Architecture

```
src/
├── Dkpt.Domain/            → Couche domaine (aucune dépendance)
│   ├── Entities/            → Member, Payment, User, ContributionAmount, Setting
│   ├── Enums/               → UserRole, PaymentMethod
│   └── Interfaces/          → IRepository<T>, IMemberRepository, etc.
│
├── Dkpt.Application/       → Couche application
│   └── Common/
│       ├── DTOs/Dtos.cs     → Tous les DTOs (request/response)
│       └── Interfaces/      → IAuthService, ITokenService
│
├── Dkpt.Infrastructure/    → Couche infrastructure (EF Core, services)
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/  → MemberConfiguration, PaymentConfiguration, etc.
│   │   └── Migrations/
│   ├── Repositories/        → Implémentations des repositories
│   ├── Services/            → AuthService, JwtTokenService
│   └── DependencyInjection.cs → Extension AddInfrastructure()
│
└── Dkpt.Api/               → Couche présentation
    ├── Controllers/         → AuthController, MembersController, etc.
    ├── Program.cs           → DI, JWT, Swagger, CORS, Pipeline
    └── appsettings.Development.json → Config locale
```

## Entités (schéma DB)

| Table | Clé primaire | Colonnes principales |
|-------|-------------|---------------------|
| `members` | `id` (UUID) | numero_membre, prenom, nom, telephone, whatsapp, residence, village, sous_prefecture_origine, nom_complet_raw, annee_debut, actif |
| `payments` | `id` (UUID) | member_id (FK), annee, date_paiement, montant, frais_paiement, moyen_paiement, reference, note, created_by_user_id (FK) |
| `users` | `id` (UUID) | email, password_hash, role (string enum) |
| `contribution_amounts` | `year` (int) | amount — CHECK: amount > 0, year >= 2022 |
| `settings` | `id` (int) | montant_cotisation_annuelle_par_defaut — CHECK: id = 1 (singleton) |

## Endpoints API

| Méthode | Route | Description | Rôle requis |
|---------|-------|-------------|-------------|
| POST | `/api/Auth/login` | Connexion | Public |
| POST | `/api/Auth/register` | Inscription (avec rôle optionnel) | Public* |
| GET | `/api/Members?page=&pageSize=&search=` | Liste paginée | Authentifié |
| GET | `/api/Members/{id}` | Détail membre | Authentifié |
| POST | `/api/Members` | Créer membre | Admin, Secrétaire |
| PUT | `/api/Members/{id}` | Modifier membre | Admin, Secrétaire |
| DELETE | `/api/Members/{id}` | Supprimer membre | Admin |
| GET | `/api/Payments?page=&pageSize=&memberId=` | Liste paiements | Authentifié |
| POST | `/api/Payments` | Créer paiement | Admin, Trésorier |
| PUT | `/api/Payments/{id}` | Modifier paiement | Admin, Trésorier |
| DELETE | `/api/Payments/{id}` | Supprimer paiement | Admin |
| GET | `/api/Users?page=&pageSize=` | Liste utilisateurs | Admin |
| PUT | `/api/Users/{id}` | Modifier utilisateur | Admin |
| DELETE | `/api/Users/{id}` | Supprimer utilisateur | Admin |
| GET | `/api/ContributionAmounts` | Montants par année | Authentifié |
| POST | `/api/ContributionAmounts` | Ajouter année | Admin |
| PUT | `/api/ContributionAmounts/{year}` | Modifier montant | Admin |
| DELETE | `/api/ContributionAmounts/{year}` | Supprimer | Admin |
| GET | `/api/Settings` | Paramètres | Authentifié |
| PUT | `/api/Settings` | Modifier paramètres | Admin |

> *Le register est actuellement public pour le développement. À sécuriser en production.

## Configuration (`appsettings.Development.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=dkpt;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "SecretKey": "...",
    "Issuer": "dkpt-api",
    "Audience": "dkpt-client",
    "ExpirationMinutes": 1440
  }
}
```

## Commandes courantes

```bash
# Build
dotnet build

# Lancer l'API
dotnet run --project src/Dkpt.Api

# Créer une migration
dotnet ef migrations add NomMigration -p src/Dkpt.Infrastructure -s src/Dkpt.Api

# Appliquer les migrations
dotnet ef database update -p src/Dkpt.Infrastructure -s src/Dkpt.Api

# Reset complet (drop + recreate)
dotnet ef database drop -p src/Dkpt.Infrastructure -s src/Dkpt.Api --force
dotnet ef migrations remove -p src/Dkpt.Infrastructure -s src/Dkpt.Api --force
dotnet ef migrations add InitialCreate -p src/Dkpt.Infrastructure -s src/Dkpt.Api
dotnet ef database update -p src/Dkpt.Infrastructure -s src/Dkpt.Api
```

## Conventions

- **Nommage DB** : `snake_case` (PostgreSQL) — ex: `created_by_user_id`
- **Nommage C#** : `PascalCase` — ex: `CreatedByUserId`
- **Enums** : Stockés comme `string` en DB via `HasConversion<string>()`
- **JSON** : `JsonStringEnumConverter` actif — les enums sont sérialisés en string
- **Auth** : JWT dans le header `Authorization: Bearer {token}`
- **Passwords** : BCrypt hash (lib `BCrypt.Net-Next`)
