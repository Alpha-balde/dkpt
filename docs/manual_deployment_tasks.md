# Tâches de Déploiement Manuelles (Hors CI/CD)

Dans le cadre du projet DKPT, certaines opérations critiques ou impliquant des données sensibles (PII) ne peuvent pas être entièrement automatisées par les pipelines CI/CD (GitHub Actions, GitLab CI, Azure DevOps, Bitbucket Pipelines) pour des raisons de sécurité.

Ce document recense les tâches que l'administrateur système doit effectuer **manuellement** avant ou pendant un déploiement.

---

## 1. Transfert des Données de Seed Sensibles (PII)

Les fichiers d'initialisation de la base de données (`members_rows.sql` et `payments_rows.sql`) contiennent des données réelles et confidentielles de l'association.
> [!CAUTION]
> **Ne jamais commiter ces fichiers dans le dépôt Git.** Les pipelines CI/CD n'y ayant pas accès, il faut les copier manuellement sur le serveur cible (Staging ou Production).

### Étape 1 : Copier les fichiers sur le VPS
Depuis votre machine locale, uploadez le dossier `dkpt_sql` vers votre dossier personnel sur le VPS via SCP :
```powershell
scp -i "C:\Chemin\Vers\Votre\cle-ssh.key" -r "C:\Chemin\Local\Vers\docs\dkpt_sql" user@ip_du_vps:~/
```

### Étape 2 : Déplacer le dossier à son emplacement définitif
Connectez-vous au VPS et déplacez le dossier vers `/opt/docs/` (le dossier parent attendu par `docker-compose.prod.yml`) en appliquant les bonnes permissions :
```powershell
ssh -i "C:\Chemin\Vers\Votre\cle-ssh.key" user@ip_du_vps "sudo mkdir -p /opt/docs && sudo rm -rf /opt/docs/dkpt_sql && sudo mv ~/dkpt_sql /opt/docs/ && sudo chown -R 1000:1000 /opt/docs/"
```

*(Cette opération n'est à réaliser qu'une seule fois, ou à chaque fois que vous souhaitez mettre à jour le jeu de données d'initialisation.)*

---

## 2. Configuration des Variables Secrètes (CI/CD Secrets)

Pour que les pipelines puissent se connecter au VPS et générer le fichier `.env` indispensable au démarrage des conteneurs, les variables secrètes doivent être configurées manuellement dans l'interface de la plateforme CI/CD utilisée.

> [!WARNING]
> Si le fichier `.env` n'est pas généré correctement (variables vides), le *healthcheck* de la base de données Postgres échouera et le déploiement plantera.

Vous devez définir **obligatoirement** les clés suivantes dans les "Settings > Secrets / Variables" de GitHub / GitLab / Azure / Bitbucket :

- **VPS_HOST** : L'adresse IP du serveur de production.
- **VPS_USER** : L'utilisateur SSH (ex: `ubuntu`, `root`).
- **VPS_SSH_KEY** : La clé privée SSH complète.
- **VPS_STAGING_HOST**, **VPS_STAGING_USER**, **VPS_STAGING_SSH_KEY** : Identifiants pour le serveur de Staging.
- **POSTGRES_USER**, **POSTGRES_PASSWORD**, **POSTGRES_DB** : Identifiants pour la base de données.
- **JWT_SECRET_KEY**, **JWT_ISSUER**, **JWT_AUDIENCE** : Configuration des tokens d'authentification.
- **CORS_ORIGINS** : Les URL autorisées pour les requêtes API (ex: `https://dkpt.example.com`).
- **NUXT_PUBLIC_API_BASE** : L'URL publique de l'API pour le front-end.
- **DOCKERHUB_USERNAME**, **DOCKERHUB_TOKEN** : Identifiants pour publier les images Docker sur le registre public.

---

## 3. Provisionnement Initial du VPS

Avant le tout premier lancement d'un pipeline CD, le VPS doit être préparé :

1. **Génération d'une paire de clés SSH** : La clé publique (`.pub`) doit être ajoutée au fichier `~/.ssh/authorized_keys` du VPS. La clé privée doit être ajoutée aux Secrets du CI/CD (`VPS_SSH_KEY`).
2. **Installation de Docker** : Installation de `docker` et `docker-compose-plugin`.
3. **Droits Docker** : L'utilisateur SSH doit faire partie du groupe `docker` pour exécuter des commandes sans `sudo` (ou le pipeline doit être configuré pour utiliser `sudo`).
