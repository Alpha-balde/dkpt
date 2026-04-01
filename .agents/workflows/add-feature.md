---
description: Comment ajouter une nouvelle fonctionnalité au projet DKPT
---

# Ajouter une Fonctionnalité

## Règles impératives

> ⛔ **Ne pas écrire de code sans l'accord explicite de l'utilisateur.**
> Toujours proposer un plan, attendre la validation, puis exécuter.

## Checklist avant de coder

1. **Lire la documentation** : `README.md` (racine), `backend/README.md`, `frontend/README.md`
2. **Vérifier le schéma SQL original** : `c:\Users\Alpha\Repository\Memoire\Pratique\docs\dkpt_sql\schema_tables.sql`
3. **Vérifier l'état des phases** : voir section "Avancement" dans le README principal
4. **Comprendre l'architecture** : Clean Architecture (Domain → Application → Infrastructure → Api)

## Ajout d'une entité backend

### 1. Domain (`Dkpt.Domain`)
- Ajouter l'entité dans `Entities/`
- Ajouter l'interface repository dans `Interfaces/`
- Ajouter les enums nécessaires dans `Enums/`

### 2. Application (`Dkpt.Application`)
- Ajouter les DTOs dans `Common/DTOs/Dtos.cs`
- Ajouter les interfaces de service dans `Common/Interfaces/`

### 3. Infrastructure (`Dkpt.Infrastructure`)
- Ajouter la configuration EF dans `Data/Configurations/`
- Ajouter le DbSet dans `Data/ApplicationDbContext.cs`
- Ajouter le repository dans `Repositories/`
- Enregistrer le repository dans `DependencyInjection.cs`
- Créer la migration : `dotnet ef migrations add ...`

### 4. Api (`Dkpt.Api`)
- Ajouter le controller dans `Controllers/`
- Tester via Swagger

## Ajout d'une page frontend

### 1. Créer la page
- Ajouter le fichier `.vue` dans `app/pages/`
- Le routing est automatique (file-based)

### 2. Ajouter la navigation
- Modifier `app/layouts/default.vue` → tableau `navigationItems`

### 3. Composable (si logique réutilisable)
- Créer dans `app/composables/`
- Utiliser `useApi()` pour les appels API

### 4. Types
- Ajouter les interfaces dans `app/types/index.ts`

## Conventions de commit

Format : `type(scope): description`

Types : `feat`, `fix`, `docs`, `refactor`, `test`, `chore`  
Scopes : `backend`, `frontend`, `infra`, `cicd`

Exemples :
```
feat(backend): add dashboard stats endpoint
fix(frontend): fix pagination on members page
docs: update README with new endpoints
```

## Après chaque modification

// turbo
1. Build backend : `dotnet build` (depuis `backend/`)
2. Vérifier frontend : `npm run dev` (depuis `frontend/`)
3. Commit : `git add -A && git commit -m "..."`
