---
description: Comment installer et lancer l'environnement de développement DKPT
---

# Setup Environnement de Dev

## Prérequis vérifiés
// turbo
1. Vérifier les versions installées :
```bash
dotnet --version   # >= 9.0
node --version     # >= 20
docker --version   # Docker Desktop
```

## Démarrage de la base de données
2. Lancer PostgreSQL via Docker (si le conteneur n'existe pas déjà) :
```bash
docker run -d --name dkpt-db -e POSTGRES_DB=dkpt -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16-alpine
```
Si le conteneur existe mais est arrêté : `docker start dkpt-db`

## Démarrage du Backend
// turbo
3. Restaurer les dépendances .NET :
```bash
cd c:\Users\Alpha\Repository\Memoire\Pratique\Dkpt\backend
dotnet restore
```

4. Appliquer les migrations EF Core :
```bash
dotnet ef database update -p src/Dkpt.Infrastructure -s src/Dkpt.Api
```

5. Lancer l'API :
```bash
dotnet run --project src/Dkpt.Api
```
L'API sera disponible sur `http://localhost:5244/swagger`

## Import des données (si DB vide)
6. Importer les données SQL existantes :
```powershell
Get-Content "c:\Users\Alpha\Repository\Memoire\Pratique\docs\dkpt_sql\contribution_amounts_rows.sql" -Raw | docker exec -i dkpt-db psql -U postgres -d dkpt
Get-Content "c:\Users\Alpha\Repository\Memoire\Pratique\docs\dkpt_sql\members_rows.sql" -Raw | docker exec -i dkpt-db psql -U postgres -d dkpt
Get-Content "c:\Users\Alpha\Repository\Memoire\Pratique\docs\dkpt_sql\payments_rows.sql" -Raw | docker exec -i dkpt-db psql -U postgres -d dkpt
Get-Content "c:\Users\Alpha\Repository\Memoire\Pratique\docs\dkpt_sql\settings_rows.sql" -Raw | docker exec -i dkpt-db psql -U postgres -d dkpt
```

7. Créer les utilisateurs via l'API (Register endpoint) :
```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5244/api/Auth/register" -ContentType "application/json" -Body '{"email":"admin@dkpt.com","password":"Dkpt@2026","role":"Admin"}'
Invoke-RestMethod -Method Post -Uri "http://localhost:5244/api/Auth/register" -ContentType "application/json" -Body '{"email":"sg@dkpt.com","password":"Dkpt@2026","role":"Secretaire"}'
Invoke-RestMethod -Method Post -Uri "http://localhost:5244/api/Auth/register" -ContentType "application/json" -Body '{"email":"tr@dkpt.com","password":"Dkpt@2026","role":"Tresorier"}'
Invoke-RestMethod -Method Post -Uri "http://localhost:5244/api/Auth/register" -ContentType "application/json" -Body '{"email":"membre@dkpt.com","password":"Dkpt@2026"}'
```

## Démarrage du Frontend
// turbo
8. Installer les dépendances npm :
```bash
cd c:\Users\Alpha\Repository\Memoire\Pratique\Dkpt\frontend
npm install
```

9. Lancer le serveur de développement Nuxt :
```bash
npm run dev
```
L'application sera disponible sur `http://localhost:3000`

## Vérification
// turbo
10. Vérifier que tout fonctionne :
- Backend : ouvrir `http://localhost:5244/swagger`
- Frontend : ouvrir `http://localhost:3000`
- Se connecter avec `admin@dkpt.com` / `Dkpt@2026`
