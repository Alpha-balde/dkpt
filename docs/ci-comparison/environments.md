# Environnements de déploiement — Staging & Production

> Documentation comparative pour le mémoire de Master.
> Les 3 plateformes (GitHub Actions, GitLab CI, Bitbucket Pipelines) supportent
> les environnements nommés, mais avec des niveaux de maturité différents.

---

## Concept

Un **environnement de déploiement** est une cible nommée (staging, production)
associée à un déploiement. Il permet de :
- Tracer **qui a déployé quoi et quand** par environnement
- Scopier des **variables/secrets** à un environnement spécifique
- Définir des **règles de protection** (approbation manuelle, reviewers)
- Visualiser l'**historique des déploiements** dans l'UI

---

## Implémentation dans DKPT

### GitHub Actions

```yaml
# .github/workflows/cd-staging.yml
jobs:
  deploy:
    environment: Dev        # ← nom de l'environnement (configuré dans l'UI GitHub)

# .github/workflows/cd-prod.yml
jobs:
  deploy:
    environment: Prod       # ← protection manuelle configurée dans Settings → Environments
```

**Configuration UI** : Settings → Environments → New environment
- Définir les **Required reviewers** (membres désignés qui doivent approuver)
- Optionnellement : **Wait timer** (délai avant déploiement)
- Variables et secrets **scopés** à cet environnement uniquement

---

### GitLab CI

```yaml
# .gitlab/pipelines/cd-staging.yml
deploy-staging:
  environment:
    name: staging
    url: https://staging.dkpt.soguimod.com   # ← lien cliquable dans l'UI

# .gitlab/pipelines/cd-prod.yml
deploy-production:
  environment:
    name: production
    url: https://dkpt.soguimod.com
  when: manual    # ← approbation manuelle (n'importe quel membre autorisé)
```

**Configuration UI** : Operate → Environments
- Variables scopées par environnement dans Settings → CI/CD → Variables
  (cocher "Protect variable" + nom d'environnement)

---

### Bitbucket Pipelines

```yaml
# bitbucket-pipelines.yml
- step: &deploy-staging
    deployment: staging     # ← mot-clé Bitbucket (3 valeurs : test, staging, production)

- step: &deploy-prod
    deployment: production
    trigger: manual         # ← bloque jusqu'à validation manuelle dans l'UI
```

**Configuration UI** : Repository Settings → Deployments
- Variables scopées par environnement (ajoutées dans l'UI)

---

## Comparaison des mécanismes d'approbation

| Critère | GitHub Actions | GitLab CI | Bitbucket |
|---------|:-:|:-:|:-:|
| **Mot-clé YAML** | `environment: Name` | `environment: name: x` | `deployment: x` |
| **URL de déploiement** | ✅ (dans l'UI) | ✅ `url:` dans le YAML | ✅ (dans l'UI) |
| **Historique déploiements** | ✅ | ✅ | ✅ |
| **Variables scopées** | ✅ | ✅ | ✅ |
| **Approbation manuelle** | ✅ `environment:` + protection UI | ✅ `when: manual` | ✅ `trigger: manual` |
| **Reviewers désignés** | ✅ **Free** | ❌ **Premium uniquement** | ❌ Non disponible |
| **Wait timer** | ✅ Free | ❌ Premium | ❌ |
| **Blocage sur branch protégée** | ✅ | ✅ | ⚠️ Basique |

---

## Analyse détaillée : approbation manuelle

### GitHub Actions — Le plus complet sur le Free tier ✅

L'approbation est configurée dans **Settings → Environments → Protection rules** :

```
Environment: Prod
├── Required reviewers: @alpha-balde (désigné nominalement)
├── Wait timer: 0 minutes
└── Deployment branches: main only
```

Quand un pipeline atteint un job avec `environment: Prod`, GitHub **bloque**
et envoie une notification aux reviewers désignés. Le déploiement ne peut
démarrer que si l'un d'eux approuve dans l'UI.

**Avantage clé** : Disponible sur le plan **Free** — c'est une fonctionnalité
Enterprise chez GitLab, gratuite chez GitHub.

---

### GitLab CI — `when: manual` sans contrôle des reviewers ⚠️

```yaml
deploy-production:
  when: manual    # Bloque jusqu'à déclenchement manuel
```

Le déploiement est bloqué et nécessite un clic dans l'UI GitLab pour démarrer.
**Mais** : n'importe quel membre ayant accès au projet peut déclencher le job —
il n'y a pas de liste de reviewers désignés sur le plan Free.

Pour avoir des **reviewers désignés** (`required_approval_count`), il faut :
- **GitLab Premium** (~$29/utilisateur/mois) ou GitLab.com Ultimate
- Configurer un **Protected environment** dans Settings → CI/CD → Protected environments

```yaml
# GitLab Premium uniquement
deploy-production:
  environment:
    name: production
    # La protection avec reviewers est configurée dans l'UI, pas dans le YAML
```

**Limitation** : Sur GitLab Free, `when: manual` est un simple "bouton play"
sans contrôle d'accès granulaire.

---

### Bitbucket Pipelines — `trigger: manual` équivalent à GitLab Free ⚠️

```yaml
- step: &deploy-prod
    trigger: manual   # Bloque jusqu'à clic dans l'UI Bitbucket
```

Même comportement que GitLab avec `when: manual` : n'importe quel membre
du repository peut valider. Pas de reviewers désignés sur aucun plan.

---

## Synthèse pour le mémoire

> **Observation clé** : La fonctionnalité d'approbation avec reviewers désignés
> est la plus différenciatrice entre les plateformes. GitHub Actions la propose
> **gratuitement**, GitLab la réserve au plan payant, et Bitbucket ne la propose
> pas du tout. Pour un projet académique ou une startup, GitHub Actions offre
> le meilleur niveau de contrôle des déploiements sans coût additionnel.

| Plateforme | Approbation libre | Reviewers désignés | Coût |
|------------|:-:|:-:|:-:|
| GitHub Actions | ✅ | ✅ | Gratuit |
| GitLab CI | ✅ | ❌ (Free) / ✅ (Premium) | $0 / $29/user/mois |
| Bitbucket | ✅ | ❌ | Gratuit |

---

## Fichiers de configuration DKPT

| Fichier | Environnement | Mécanisme d'approbation |
|---------|--------------|------------------------|
| `.github/workflows/cd-staging.yml` | `environment: Dev` | Automatique |
| `.github/workflows/cd-prod.yml` | `environment: Prod` | Reviewers désignés (UI) |
| `.gitlab/pipelines/cd-staging.yml` | `environment: staging` | Automatique |
| `.gitlab/pipelines/cd-prod.yml` | `environment: production` | `when: manual` |
| `bitbucket-pipelines.yml` | `deployment: staging` | Automatique |
| `bitbucket-pipelines.yml` | `deployment: production` | `trigger: manual` |
