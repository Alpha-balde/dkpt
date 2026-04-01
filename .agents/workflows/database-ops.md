---
description: Comment gérer les opérations de base de données (migrations, seed, reset)
---

# Opérations Base de Données

Toutes les commandes s'exécutent depuis `c:\Users\Alpha\Repository\Memoire\Pratique\Dkpt\backend`.

## Vérifier l'état du conteneur PostgreSQL

// turbo
1. Vérifier que le conteneur tourne :
```bash
docker ps --filter name=dkpt-db
```
Si arrêté : `docker start dkpt-db`

## Créer une nouvelle migration

2. Après modification d'une entité ou d'une configuration EF Core :
```bash
dotnet ef migrations add NomDeLaMigration -p src/Dkpt.Infrastructure -s src/Dkpt.Api
```
Conventions de nommage : `AddChampXToTable`, `CreateTableY`, `UpdatePaymentSchema`

## Appliquer les migrations

3. Appliquer les migrations en attente :
```bash
dotnet ef database update -p src/Dkpt.Infrastructure -s src/Dkpt.Api
```

## Reset complet de la DB

> ⚠️ Ceci détruit toutes les données ! Suivre ensuite le workflow dev-setup pour réimporter.

4. Drop + recréation complète :
```bash
dotnet ef database drop -p src/Dkpt.Infrastructure -s src/Dkpt.Api --force
dotnet ef migrations remove -p src/Dkpt.Infrastructure -s src/Dkpt.Api --force
dotnet ef migrations add InitialCreate -p src/Dkpt.Infrastructure -s src/Dkpt.Api
dotnet ef database update -p src/Dkpt.Infrastructure -s src/Dkpt.Api
```

## Vérifier le contenu de la DB

// turbo
5. Compter les enregistrements :
```bash
docker exec -i dkpt-db psql -U postgres -d dkpt -c "SELECT 'members' as tbl, count(*) FROM members UNION ALL SELECT 'payments', count(*) FROM payments UNION ALL SELECT 'users', count(*) FROM users UNION ALL SELECT 'contributions', count(*) FROM contribution_amounts UNION ALL SELECT 'settings', count(*) FROM settings;"
```

## Se connecter à la DB (paramètres)

- **Host** : `localhost`
- **Port** : `5432`
- **Database** : `dkpt`
- **User** : `postgres`
- **Password** : `postgres`

## Points d'attention

- Le schéma d'origine est dans `c:\Users\Alpha\Repository\Memoire\Pratique\docs\dkpt_sql\schema_tables.sql`
- Les enums sont stockés comme `string` en DB (pas comme `int`)
- La table `settings` est un singleton (CHECK: id = 1)
- La table `contribution_amounts` a des CHECK constraints (amount > 0, year >= 2022)
- Les mots de passe sont hashés avec BCrypt — **NE PAS insérer de hash manuellement en SQL** (risque d'échappement), utiliser l'endpoint `/api/Auth/register`
