# Tâches de Déploiement Manuelles (Hors CI/CD)

Dans le cadre du projet DKPT, certaines opérations critiques ou impliquant des données sensibles (PII) ne peuvent pas être entièrement automatisées par les pipelines CI/CD (GitHub Actions, GitLab CI, Azure DevOps, Bitbucket Pipelines) pour des raisons de sécurité.

Ce document recense les tâches que l'administrateur système doit effectuer **manuellement** avant ou pendant un déploiement.

---

## 1. Transfert des Données de Seed Sensibles (PII)

Les fichiers d'initialisation de la base de données (`members_rows.sql` et `payments_rows.sql`) contiennent des données réelles et confidentielles de l'association.


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
- **CORS_ORIGINS** : Les URL autorisées pour les requêtes API (ex: `https://dkpt.soguimod.com`).
- **NUXT_PUBLIC_API_BASE** : L'URL publique de l'API pour le front-end.
- **DOCKERHUB_USERNAME**, **DOCKERHUB_TOKEN** : Identifiants pour publier les images Docker sur le registre public.

---

## 3. Provisionnement Initial du VPS

Avant le tout premier lancement d'un pipeline CD, le VPS doit être préparé :

1. **Génération d'une paire de clés SSH** : La clé publique (`.pub`) doit être ajoutée au fichier `~/.ssh/authorized_keys` du VPS. La clé privée doit être ajoutée aux Secrets du CI/CD (`VPS_SSH_KEY`).
2. **Installation de Docker** : Installation de `docker` et `docker-compose-plugin`.
3. **Droits Docker** : L'utilisateur SSH doit faire partie du groupe `docker` pour exécuter des commandes sans `sudo` (ou le pipeline doit être configuré pour utiliser `sudo`).

---

## 4. Configuration Azure DevOps (Spécifique)

Azure DevOps nécessite une configuration manuelle plus importante que les autres plateformes car les pipelines, les connexions externes et les agents doivent être enregistrés via l'interface web.

> [!IMPORTANT]
> **Toutes les étapes ci-dessous doivent être effectuées AVANT le premier lancement des pipelines Azure DevOps.**
> URL du projet : `https://dev.azure.com/mpbalde2011/DKPT`

### 4.1 Service Connection Docker Hub

Permet aux pipelines de se connecter à Docker Hub pour pousser les images.

**Chemin** : `Project Settings → Service connections → New service connection → Docker Registry`

| Champ | Valeur |
|-------|--------|
| Registry type | Docker Hub |
| Docker ID | `alphab224` (username Docker Hub) |
| Docker Password | Votre Docker Hub **access token** (pas le mot de passe) |
| Service connection name | `dockerhub-connection` |
| Grant access permission to all pipelines | ✅ Coché |

> [!TIP]
> Pour créer un access token Docker Hub : `https://hub.docker.com/settings/security` → **New Access Token** → permissions **Read, Write**.

### 4.2 Variable Group (Secrets partagés)

Les secrets d'application sont centralisés dans un **variable group** partagé entre tous les pipelines Azure DevOps.

**Chemin** : `Pipelines → Library → + Variable group`

Créer **3 variable groups** :

| Variable group | Rôle | Utilisé par |
|---------------|------|-------------|
| `dkpt-secrets` | Variables communes aux deux environnements | CD Staging + CD Production |
| `dkpt-staging` | Variables spécifiques au staging | CD Staging uniquement |
| `dkpt-prod` | Variables spécifiques à la production | CD Production uniquement |

**`dkpt-secrets`** — Variables partagées :

| Variable | Secret ? | Description |
|----------|:--------:|-------------|
| `DOCKERHUB_USERNAME` | Non | `alphab224` |
| `POSTGRES_DB` | Non | Nom de la base de données |
| `POSTGRES_USER` | Non | Utilisateur PostgreSQL |
| `JWT_ISSUER` | Non | Issuer JWT |
| `JWT_AUDIENCE` | Non | Audience JWT |

**`dkpt-staging`** et **`dkpt-prod`** — Variables spécifiques par environnement :

| Variable | Secret ? | Description |
|----------|:--------:|-------------|
| `POSTGRES_PASSWORD` | ✅ | Mot de passe PostgreSQL (différent par env) |
| `JWT_SECRET_KEY` | ✅ | Clé secrète JWT (doit être différente par env) |
| `CORS_ORIGINS` | Non | URL CORS (ex: `https://staging.dkpt.com` vs `https://dkpt.com`) |
| `NUXT_PUBLIC_API_BASE` | Non | URL publique de l'API frontend |

> Les variables marquées comme **Secret** sont masquées dans les logs et ne peuvent pas être lues une fois enregistrées.

### 4.3 Agent Pool Self-Hosted (Docker Build ARM64)

Les VPS sont des machines **ARM64** (Oracle Cloud). Les images Docker doivent être construites nativement sur ARM64. Un agent pool self-hosted est installé sur le **VPS Staging** pour effectuer les builds Docker.

> [!WARNING]
> Cet agent est **distinct** de l'agent d'environnement déjà installé. Il cohabite sur le même VPS mais dans un dossier séparé et avec un rôle différent :
> - **Agent d'environnement** (`~/azagent/`) → exécute les `deployment` jobs (déploiement local)
> - **Agent pool** (`~/azure-agent-pool/`) → exécute les `job` réguliers (Docker build & push)

**Étape 1 : Créer le pool dans Azure DevOps**

`Project Settings → Agent pools → Add pool`

| Champ | Valeur |
|-------|--------|
| Pool type | Self-hosted |
| Name | `DKPT-ARM64` |
| Grant access permission to all pipelines | ✅ Coché |

**Étape 2 : Installer l'agent sur le VPS Staging**

Connectez-vous au VPS Staging et exécutez :
```bash
# Créer un dossier séparé pour l'agent pool
mkdir ~/azure-agent-pool && cd ~/azure-agent-pool

# Télécharger l'agent Azure Pipelines (ARM64)
# Vérifier la dernière version sur : https://github.com/microsoft/azure-pipelines-agent/releases
curl -O https://vstsagentpackage.azureedge.net/agent/4.x.x/vsts-agent-linux-arm64-4.x.x.tar.gz
tar xzf vsts-agent-*.tar.gz

# Configurer l'agent
./config.sh
# → URL du serveur : https://dev.azure.com/mpbalde2011
# → PAT token : votre Personal Access Token (scope: Agent Pools Read & Manage)
# → Pool name : DKPT-ARM64
# → Agent name : dkpt-staging-builder (ou un nom de votre choix)

# Installer et démarrer comme service
sudo ./svc.sh install
sudo ./svc.sh start

sudo journalctl -u 'vsts.agent.mpbalde2011.Default.DKPT\x2dARM64.service' -f

```

**Vérification** : L'agent doit apparaître comme **Online** dans `Project Settings → Agent pools → Default → Agents`.

> [!TIP]
> Pour créer un PAT token : `https://dev.azure.com/mpbalde2011/_usersSettings/tokens` → **New Token** → scope **Agent Pools: Read & manage**.

### 4.4 Environnements (Staging et Prod)

Les environnements **Staging** et **Prod** doivent exister dans Azure DevOps avec des agents VM enregistrés sur chaque VPS.

**Chemin** : `Pipelines → Environments`

| Environnement | VPS cible | Agent installé ? |
|---------------|-----------|:-:|
| **Staging** | VPS Staging (ARM64) | ✅ Déjà fait |
| **Prod** | VPS Production (ARM64) | ✅ Déjà fait |

**Optionnel — Approval gate sur Prod** :

Pour exiger une approbation manuelle avant chaque déploiement en production :

`Pipelines → Environments → Prod → ⋮ (menu) → Approvals and checks → + → Approvals`

Ajouter votre compte comme approbateur. Chaque déploiement en production attendra votre validation dans l'interface Azure DevOps.

### 4.5 Enregistrement des Pipelines

Chaque fichier YAML doit être enregistré **manuellement** comme un pipeline distinct dans l'UI Azure DevOps.

**Chemin** : `Pipelines → New pipeline → Azure Repos Git (ou GitHub) → Existing YAML file`

| Pipeline | Chemin du fichier YAML | Nom à donner dans l'UI |
|----------|----------------------|------------------------|
| CI | `.azuredevops/ci.yml` | `DKPT CI` |
| CD Staging | `.azuredevops/cd-staging.yml` | `DKPT CD Staging` |
| CD Production | `.azuredevops/cd-prod.yml` | `DKPT CD Production` |
| PR Check | `.azuredevops/pr-check.yml` | `DKPT PR Check` |

> [!CAUTION]
> Le nom du pipeline CI dans l'UI **doit être exactement** `DKPT CI`. Les pipelines CD le référencent via `source: 'DKPT CI'` dans leurs `pipeline resources`. Si le nom ne correspond pas, le chaînage automatique CI → CD ne fonctionnera pas.
