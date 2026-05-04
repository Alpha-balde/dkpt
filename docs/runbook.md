# Runbook Opérationnel — DKPT

> Procédures courantes pour l'exploitation de l'infrastructure DKPT.
> Destiné à tout opérateur ayant accès aux VPS et au portail Azure DevOps.

---

## 1. Déployer une nouvelle version

### Déploiement automatique (recommandé)

Le pipeline CI/CD gère le déploiement de bout en bout :

1. **Pousser le code** sur la branche `main` (via PR merge)
2. **CI Pipeline** se déclenche automatiquement :
   - Build & test backend (.NET 9)
   - Build & test frontend (Nuxt 4)
   - Analyse SonarCloud
   - Build & push images Docker (tag `sha-xxxxxxxx`)
3. **CD Staging** se déclenche après succès du CI :
   - Deploy sur VPS Staging
   - Quality Gate (k6, OWASP ZAP, Playwright)
4. **CD Production** se déclenche après succès du CD Staging :
   - ⚠️ **Approbation manuelle requise** dans Azure DevOps
   - Deploy sur VPS Production

**Suivre l'avancement** : `Azure DevOps → Pipelines → Runs`

### Déploiement manuel (urgence uniquement)

```bash
# Se connecter au VPS
ssh -i ~/.ssh/dkpt-key user@<VPS_IP>

# Aller dans le répertoire de déploiement
cd /opt/dkpt

# Mettre à jour l'image (remplacer le tag)
sed -i 's/IMAGE_TAG=.*/IMAGE_TAG=sha-xxxxxxxx/' .env

# Pull et redémarrer
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d --remove-orphans

# Vérifier que tout est up
docker compose -f docker-compose.prod.yml ps
```

---

## 2. Rollback

### Méthode 1 — Rollback via tag Docker (rapide, < 2 min)

Chaque déploiement crée une image Docker taggée avec le SHA du commit.

```bash
ssh -i ~/.ssh/dkpt-key user@<VPS_IP>
cd /opt/dkpt

# Voir les tags disponibles localement
docker images alphab224/dkpt-backend --format "{{.Tag}}"
docker images alphab224/dkpt-frontend --format "{{.Tag}}"

# Revenir au tag précédent
sed -i 's/IMAGE_TAG=.*/IMAGE_TAG=sha-PREVIOUS/' .env

# Redéployer
docker compose -f docker-compose.prod.yml up -d --remove-orphans

# Vérifier
docker compose -f docker-compose.prod.yml ps
```

### Méthode 2 — Rollback via pipeline (recommandé)

1. Aller dans `Azure DevOps → Pipelines → DKPT CD Production`
2. Cliquer sur **Run pipeline**
3. Sélectionner le **commit précédent** (branche `main`)
4. Approuver le déploiement

### Méthode 3 — Revert Git (si le code est la cause)

```bash
git revert HEAD
git push origin main
# Le pipeline CI/CD se relance automatiquement
```

---

## 3. Consulter les logs

### Logs applicatifs — Azure Application Insights

1. Aller sur le **portail Azure** → `Application Insights` → ressource DKPT
2. **Failures** : voir les requêtes échouées (5xx, 4xx)
3. **Performance** : temps de réponse, dépendances lentes
4. **Logs (Log Analytics)** :
   - Cliquer sur `Logs` dans le menu
   - Requête KQL pour les erreurs récentes :
     ```kusto
     traces
     | where severityLevel >= 3
     | order by timestamp desc
     | take 50
     ```
   - Requête KQL pour les requêtes échouées :
     ```kusto
     requests
     | where success == false
     | order by timestamp desc
     | take 20
     ```

### Logs Docker — directement sur le VPS

```bash
ssh -i ~/.ssh/dkpt-key user@<VPS_IP>
cd /opt/dkpt

# Tous les services
docker compose -f docker-compose.prod.yml logs -f

# Un service spécifique
docker compose -f docker-compose.prod.yml logs -f backend
docker compose -f docker-compose.prod.yml logs -f frontend
docker compose -f docker-compose.prod.yml logs -f db

# Les 100 dernières lignes
docker compose -f docker-compose.prod.yml logs --tail 100 backend
```

### Logs du pipeline CI/CD

`Azure DevOps → Pipelines → Runs → sélectionner le run → cliquer sur le job/step`

---

## 4. Répondre à une alerte

### Alerte : `Failed requests > 5`

**Source** : Azure Application Insights → Alert Rules

**Procédure** :

1. **Identifier** : Aller dans Application Insights → Failures → analyser les requêtes échouées
2. **Diagnostiquer** :
   - Vérifier les logs backend : `docker compose logs --tail 200 backend`
   - Vérifier que la base de données est accessible : `docker compose exec db pg_isready`
   - Vérifier l'état des conteneurs : `docker compose ps`
3. **Résoudre** :
   - **Si un conteneur est down** : `docker compose -f docker-compose.prod.yml up -d`
   - **Si la DB est inaccessible** : `docker compose -f docker-compose.prod.yml restart db`
   - **Si le backend crash en boucle** : consulter les logs, puis rollback si nécessaire
4. **Clôturer** : Vérifier que l'alerte se résout dans Azure Monitor

### Alerte : Temps de réponse élevé

**Procédure** :

1. Vérifier la charge CPU/RAM du VPS : `htop` ou `top`
2. Vérifier les requêtes lentes dans Application Insights → Performance
3. Si surcharge : `docker compose restart backend`
4. Si problème persistant : vérifier les connexions DB, la mémoire PostgreSQL

### Alerte : Conteneur en échec (healthcheck)

**Procédure** :

1. `docker compose -f docker-compose.prod.yml ps` → identifier le service unhealthy
2. `docker compose -f docker-compose.prod.yml logs <service>` → analyser l'erreur
3. `docker compose -f docker-compose.prod.yml restart <service>`
4. Si le problème persiste → rollback via tag Docker

---

## 5. Commandes utiles

### État de l'infrastructure

```bash
# État des conteneurs
docker compose -f docker-compose.prod.yml ps

# Utilisation des ressources
docker stats --no-stream

# Espace disque
df -h

# Images Docker stockées
docker images | grep dkpt
```

### Maintenance

```bash
# Nettoyer les images Docker inutilisées
docker image prune -f

# Nettoyer tout (images, volumes orphelins, networks)
docker system prune -f

# Vérifier les certificats TLS (Caddy)
docker compose -f docker-compose.prod.yml exec caddy caddy list-certificates
```

### Base de données

```bash
# Se connecter à PostgreSQL
docker compose -f docker-compose.prod.yml exec db psql -U $POSTGRES_USER -d $POSTGRES_DB

# Backup de la base
docker compose -f docker-compose.prod.yml exec db pg_dump -U $POSTGRES_USER $POSTGRES_DB > backup_$(date +%Y%m%d).sql

# Vérifier la connectivité
docker compose -f docker-compose.prod.yml exec db pg_isready
```

---

## 6. Contacts et accès

| Ressource | URL |
|-----------|-----|
| **Azure DevOps** | `https://dev.azure.com/mpbalde2011/DKPT` |
| **GitHub** | `https://github.com/Alpha-balde/dkpt` |
| **Application Insights** | Portail Azure → Application Insights → DKPT |
| **Docker Hub** | `https://hub.docker.com/u/alphab224` |
| **VPS Staging** | SSH via clé privée (voir variable `VPS_STAGING_SSH_KEY`) |
| **VPS Production** | SSH via clé privée (voir variable `VPS_SSH_KEY`) |
