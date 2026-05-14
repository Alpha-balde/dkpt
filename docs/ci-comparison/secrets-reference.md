# Référentiel centralisé des variables CI/CD

> Ce document liste **toutes** les variables/secrets nécessaires au fonctionnement
> des pipelines CI/CD sur chaque plateforme. Il sert de checklist lors de la
> configuration d'une nouvelle plateforme.

## Vue d'ensemble

### Variables par catégorie

#### 🐳 Docker Hub (Build & Push images)

| Variable | Valeur type | Sensible |
|----------|------------|:--------:|
| `DOCKERHUB_USERNAME` | `alpha-balde` | Non |
| `DOCKERHUB_TOKEN` | `dckr_pat_...` | ✅ Oui |

#### 🖥️ VPS Production (Déploiement)

| Variable | Valeur type | Sensible |
|----------|------------|:--------:|
| `VPS_HOST` | `123.45.67.89` | ⚠️ |
| `VPS_USER` | `deploy` | Non |
| `VPS_SSH_KEY` | Clé privée SSH | ✅ Oui |

#### 🧪 VPS Staging (Déploiement)

| Variable | Valeur type | Sensible |
|----------|------------|:--------:|
| `VPS_STAGING_HOST` | `123.45.67.90` | ⚠️ |
| `VPS_STAGING_USER` | `deploy` | Non |
| `VPS_STAGING_SSH_KEY` | Clé privée SSH | ✅ Oui |

#### 🔐 Application (Runtime .env)

| Variable | Valeur type | Sensible |
|----------|------------|:--------:|
| `POSTGRES_DB` | `dkpt` | Non |
| `POSTGRES_USER` | `postgres` | Non |
| `POSTGRES_PASSWORD` | `...` | ✅ Oui |
| `JWT_SECRET_KEY` | `min 32 chars` | ✅ Oui |
| `JWT_ISSUER` | `dkpt-api` | Non |
| `JWT_AUDIENCE` | `dkpt-client` | Non |
| `CORS_ORIGINS` | `https://dkpt.example.com` | Non |
| `NUXT_PUBLIC_API_BASE` | `/api` | Non |
| `SITE_ADDRESS` | `dkpt.example.com` | Non |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | `InstrumentationKey=...` | ✅ Oui |

#### 🔄 Mirroring (GitHub → plateformes)

| Variable | Où configurer | Sensible |
|----------|--------------|:--------:|
| `GITLAB_SSH_KEY` | GitHub Secrets | ✅ Oui |
| `BITBUCKET_SSH_KEY` | GitHub Secrets | ✅ Oui |

---

## Matrice par plateforme

### Quelles variables configurer sur quelle plateforme ?

| Variable | GitHub | GitLab | Bitbucket | Azure DevOps |
|----------|:------:|:------:|:---------:|:------------:|
| **Docker Hub** | | | | |
| `DOCKERHUB_USERNAME` | ✅ | ✅ | ✅ | ✅ (Variable Group) |
| `DOCKERHUB_TOKEN` | ✅ | ✅ | ✅ | ✅ (Variable Group) |
| **VPS Production** | | | | |
| `VPS_HOST` | ✅ | ✅ | ✅ | ✅ (Variable Group) |
| `VPS_USER` | ✅ | ✅ | ✅ | ✅ (Variable Group) |
| `VPS_SSH_KEY` | ✅ | ✅ | ✅ | ✅ (Variable Group) |
| **VPS Staging** | | | | |
| `VPS_STAGING_HOST` | ✅ | ✅ | ✅ | ✅ |
| `VPS_STAGING_USER` | ✅ | ✅ | ✅ | ✅ |
| `VPS_STAGING_SSH_KEY` | ✅ | ✅ | ✅ | ✅ |
| **Application** | | | | |
| `POSTGRES_DB` | ✅ | ✅ | ✅ | ✅ |
| `POSTGRES_USER` | ✅ | ✅ | ✅ | ✅ |
| `POSTGRES_PASSWORD` | ✅ | ✅ | ✅ | ✅ |
| `JWT_SECRET_KEY` | ✅ | ✅ | ✅ | ✅ |
| `JWT_ISSUER` | ✅ | ✅ | ✅ | ✅ |
| `JWT_AUDIENCE` | ✅ | ✅ | ✅ | ✅ |
| `CORS_ORIGINS` | ✅ | ✅ | ✅ | ✅ |
| `NUXT_PUBLIC_API_BASE` | ✅ | ✅ | ✅ | ✅ |
| `SITE_ADDRESS` | ✅ | ✅ | ✅ | ✅ |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | ✅ | ✅ | ✅ | ✅ |
| **Mirroring** | | | | |
| `GITLAB_SSH_KEY` | ✅ | — | — | — |
| `BITBUCKET_SSH_KEY` | ✅ | — | — | — |

> **Total** : ~18 variables par plateforme pour un déploiement complet (CI + CD).

---

## Où configurer sur chaque plateforme

### GitHub Actions
**Chemin** : `Settings → Secrets and variables → Actions → Repository secrets`
- Toutes les variables sont des **Secrets** (masquées dans les logs)
- Pas de distinction secret/variable

### GitLab CI
**Chemin** : `Settings → CI/CD → Variables`
- Cocher **"Mask variable"** pour les valeurs sensibles
- Cocher **"Protect variable"** pour limiter aux branches protégées
- Option **"Expand variable reference"** pour les valeurs contenant des `$`

### Bitbucket Pipelines
**Chemin** : `Repository Settings → Pipelines → Repository variables`
- Cocher **"Secured"** pour masquer dans les logs
- Pas de groupes de variables natifs

### Azure DevOps
**Chemin** : `Pipelines → Library → Variable Groups`
- Utiliser un **Variable Group** nommé `dkpt-secrets`
- Cocher le 🔒 pour les valeurs sensibles
- Le groupe est lié aux pipelines via `variables: - group: dkpt-secrets`

---

## Comparaison de la gestion des secrets

| Aspect | GitHub | GitLab | Bitbucket | Azure DevOps |
|--------|:------:|:------:|:---------:|:------------:|
| Interface | Secrets | Variables CI/CD | Repository variables | Variable Groups |
| Masquage logs | ✅ Auto | ✅ Option | ✅ Option | ✅ Option |
| Scope environnement | ✅ Environment secrets | ✅ Protected + scope | ❌ | ✅ Variable Groups |
| Groupes | ❌ | ❌ | ❌ | ✅ Variable Groups |
| Limitation branches | ❌ | ✅ Protected variables | ❌ | ✅ Via permissions |
| Vault externe | ✅ (OIDC) | ✅ (HashiCorp) | ❌ | ✅ (Key Vault) |

---

## Recommandations

### Pour le mémoire (environnement de test)

1. **GitHub est la source de vérité** → Configurer les 18 variables sur GitHub en premier
2. **GitLab et Bitbucket** → Copier les mêmes valeurs manuellement
3. **Azure DevOps** → Utiliser un Variable Group `dkpt-secrets`

### Pour un environnement de production réel

1. **Utiliser un coffre-fort externe** : HashiCorp Vault ou Azure Key Vault
2. **OIDC (OpenID Connect)** : Évite de stocker des credentials longue durée
3. **Rotation automatique** : Renouveler les clés SSH et tokens régulièrement
4. **Principe du moindre privilège** : Chaque plateforme n'a accès qu'à ce dont elle a besoin

> **Observation pour le mémoire** : La duplication manuelle des secrets sur 4 plateformes est le point faible majeur du multi-plateforme CI/CD. En production, un coffre-fort centralisé (Vault, Key Vault) avec injection dynamique serait indispensable.
