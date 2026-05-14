# Bitbucket Pipelines — Analyse détaillée

> Système CI/CD intégré à Bitbucket, avec un fichier unique à la racine
> et des sections pour organiser les différents pipelines.

## Configuration dans DKPT

### Structure

```
bitbucket-pipelines.yml         ← Tout dans un seul fichier (racine obligatoire)
```

### Organisation interne du fichier

```yaml
definitions:
  steps:           # Steps réutilisables via YAML anchors (&nom / *nom)

pipelines:
  default:         # CI — Toutes les branches
  branches:
    develop:       # CD Staging
    main:          # CD Production
  pull-requests:
    '**':          # PR Check
```

### Sections du fichier

| Section | Rôle | Équivalent GitHub Actions |
|---------|------|--------------------------|
| `default:` | CI sur toutes les branches | `ci.yml` |
| `branches: develop:` | CD staging | `cd-staging.yml` |
| `branches: main:` | CD production | `cd-prod.yml` |
| `pull-requests: '**':` | PR check | `pr-check.yml` |

---

## Points forts

- **Simplicité** : Un seul fichier, tout est visible d'un coup
- **YAML anchors** : Mécanisme natif YAML pour réutiliser des steps (`&anchor` / `*reference`)
- **Pipes** : Intégrations pré-packagées (ex: `atlassian/ssh-run`)
- **Deployments** : Support natif des environnements (`deployment: staging`)
- **Intégration Jira** : Lien automatique entre commits, PRs et tickets Jira
- **Docker natif** : Chaque step s'exécute dans un container Docker
- **Rapidité des runners** : Les runners Bitbucket sont performants (backend build en 29s vs 53s sur GitLab)

## Points faibles

- **50 minutes/mois gratuites** : De loin le plus limité du comparatif
- **1 seul fichier** : Pas de pipelines séparés, tout dans `bitbucket-pipelines.yml`
- **Pas de multi-fichiers** : Impossible de séparer les pipelines en fichiers distincts
- **Déclenchement partiel** : Pas de déclenchement vraiment indépendant comme `workflow_run`
- **5 jobs parallèles max** (free tier)
- **YAML anchors seulement** : Pas de vrai système de templates comme Azure DevOps
- **Marketplace limité** : Moins de pipes que d'actions GitHub ou de tasks Azure
- **Artefacts 14 jours** : La durée la plus courte
- **Steps séquentiels** : Sur le pipeline `branches: main:`, les steps s'exécutent les uns après les autres, pas en parallèle

## Spécificités techniques

### YAML anchors pour la réutilisation

```yaml
definitions:
  steps:
    - step: &build-test-backend
        name: 'Backend — Build & Test'
        image: mcr.microsoft.com/dotnet/sdk:9.0
        script:
          - cd backend && dotnet restore && dotnet build
          - dotnet test --verbosity minimal

pipelines:
  default:
    - step: *build-test-backend    # Réutilisation par référence

  branches:
    main:
      - step: *build-test-backend  # Même step, réutilisé
      - step: *docker-build-push
```

C'est du YAML standard, pas une fonctionnalité Bitbucket. L'avantage est la portabilité ; l'inconvénient est que c'est limité (pas de paramètres, pas de composition).

### Deployment environments

```yaml
- step:
    name: 'Deploy to Production'
    deployment: production    # Déclare l'environnement
    trigger: manual           # Gate d'approbation manuelle
```

Bitbucket supporte les environnements de déploiement, mais de façon plus basique que GitHub Actions ou Azure DevOps (pas de gates automatiques, pas de checks).

### Services

```yaml
definitions:
  services:
    docker:
      memory: 2048    # Allouer de la mémoire au service Docker
```

Les services Bitbucket sont des containers auxiliaires (Docker, Redis, PostgreSQL, etc.) qui tournent à côté du step principal.

### Pipes vs Actions

Les **pipes** Bitbucket sont l'équivalent des **actions** GitHub. Mais l'écosystème est nettement plus restreint :

```yaml
# Bitbucket pipe
- pipe: atlassian/ssh-run:0.8.1
  variables:
    SSH_USER: $VPS_USER
    SERVER: $VPS_HOST

# Équivalent GitHub Action
- uses: appleboy/ssh-action@v1
  with:
    host: ${{ secrets.VPS_HOST }}
    username: ${{ secrets.VPS_USER }}
```

---

## Mirroring — Observations

### Méthode utilisée

Bitbucket ne propose pas de pull mirror natif. Le mirroring est effectué via le workflow `mirror.yml` de GitHub Actions avec une paire de clés SSH :

```yaml
- name: Mirror to Bitbucket
  uses: pixta-dev/repository-mirroring-action@v1
  with:
    target_repo_url: git@bitbucket.org:alpha-balde/dkpt-mirror.git
    ssh_private_key: ${{ secrets.BITBUCKET_SSH_KEY }}
```

### Configuration SSH

1. Génération d'une paire de clés SSH dédiée au mirroring
2. Clé publique ajoutée dans Bitbucket → `Repository Settings → Access keys`
3. Clé privée ajoutée dans GitHub → `Settings → Secrets → BITBUCKET_SSH_KEY`

> **Comparaison avec GitLab** : Bitbucket et GitLab utilisent la même méthode de mirroring (SSH via GitHub Actions). La différence est que GitLab **propose** un pull mirror dans l'UI (mais réservé au Premium), tandis que Bitbucket ne propose rien — c'est plus honnête.

---

## Journal d'implémentation

> **Branche** : `feature/gitlab` (merge dans `main`)
> **Date** : 2026-05-13

### Mise en place

| # | Étape | Résultat |
|:-:|-------|---------|
| 1 | Création du repo `dkpt-mirror` sur Bitbucket | ✅ |
| 2 | Génération paire de clés SSH | ✅ |
| 3 | Ajout clé publique dans Bitbucket (Access keys) | ✅ |
| 4 | Ajout `BITBUCKET_SSH_KEY` dans GitHub Secrets | ✅ |
| 5 | Mise à jour URL dans `mirror.yml` | ✅ |
| 6 | Push via `mirror.yml` → Pipeline CI déclenché | ✅ |

### Résultat du premier pipeline

Le pipeline Bitbucket s'est déclenché automatiquement après le mirroring. La section `branches: main:` a été exécutée :

| Step | Durée | Statut |
|------|:-----:|:------:|
| Backend — Build & Test | **29s** | ✅ |
| Frontend — Build | **1m38s** | ✅ |
| Docker Build & Push (Production) | 9s | ❌ (secrets Docker Hub non configurés) |
| Deploy to Production | — | ⏭️ Non exécuté |
| **Total pipeline** | **2m17s** | — |

> L'échec du Docker build est attendu : les secrets `DOCKERHUB_USERNAME` et `DOCKERHUB_TOKEN` ne sont pas encore configurés dans Bitbucket. Le message d'erreur `Must provide --username with --password-stdin` confirme que les variables sont vides.

---

## Temps d'exécution observés

| Step | Durée | Notes |
|------|:-----:|-------|
| Backend — Build & Test | **29s** | dotnet restore + build + test (9 tests xUnit) |
| Frontend — Build | **1m38s** | npm ci + build Nuxt (pas de lint) |
| **Total CI (build & test)** | **~2m07s** | Steps séquentiels |

### Comparaison des temps — 3 plateformes

| Métrique | GitHub Actions | GitLab CI | Bitbucket |
|----------|:-:|:-:|:-:|
| Backend build + test | N/A | 53s | **29s** |
| Frontend build | N/A | 1m42s | **1m38s** |
| CI total | **~1m16s** | ~2m | ~2m17s |
| Parallélisme (free) | 20 jobs | 5 jobs | 5 jobs |
| Minutes gratuites | 2 000 | 400 | **50** |

> **Observation** : Bitbucket a les runners les plus rapides par job (backend 29s vs 53s sur GitLab, soit **45% plus rapide**). Cependant, le total est plus élevé car le pipeline `branches: main:` exécute les steps **séquentiellement** (backend → frontend → docker → deploy), tandis que GitHub Actions et GitLab CI exécutent backend et frontend **en parallèle**.

> **Attention** : Avec seulement **50 minutes/mois** gratuites, chaque exécution de la section `main` (~2m17s) consomme environ **4.5%** du quota mensuel. C'est un facteur limitant majeur pour un usage en production.

### Self-hosted runner vs Shared runners

Un self-hosted runner Linux a été configuré pour contourner la limitation des 50 minutes/mois.

#### Configuration du runner

```yaml
# Utilisation dans bitbucket-pipelines.yml
- step:
    <<: *build-test-backend
    runs-on:
      - self.hosted
      - linux
```

#### Mise en place

| Étape | Détail |
|-------|--------|
| **Type** | Runner: Linux Container |
| **Labels** | `self.hosted`, `linux` |
| **Version** | V5 |
| **Installation** | Via Docker sur machine locale |

#### Comparaison des performances

| Métrique | Shared runners | Self-hosted runner |
|----------|:-:|:-:|
| Pipeline CI total | **3m11s** | **3m02s** |
| Consomme des minutes | ✅ Oui (50/mois) | ❌ Non (illimité) |
| Maintenance | Aucune | Hébergement requis |
| Disponibilité | 24/7 | Dépend de la machine hôte |

> **Observation** : Le self-hosted runner est légèrement plus rapide (~3%) mais l'avantage principal est de **ne pas consommer le quota de 50 minutes/mois**. C'est une solution pragmatique pour un projet de test/mémoire.

> **Comparaison avec Azure DevOps** : Azure DevOps utilise aussi un self-hosted agent (ARM64 sur le VPS de production). La différence est que l'agent Azure est permanent et tourne en tant que service, tandis que le runner Bitbucket tourne dans un container Docker local.

---

## Comparaison avec GitHub Actions

| Aspect | GitHub Actions | Bitbucket |
|--------|:-:|:-:|
| Multi-pipelines | ✅ Multi-fichiers | ❌ Fichier unique |
| Réutilisation | Reusable workflows + Actions | YAML anchors + Pipes |
| Minutes gratuites | 2 000 | 50 |
| Déclenchement indépendant | ✅ `workflow_run` | ⚠️ Branches/tags seulement |
| Marketplace | 20 000+ actions | ~200 pipes |
| Artefacts | 90 jours | 14 jours |
| Mirroring | Via `mirror.yml` workflow | Pas de pull mirror |
| Rapidité runners | Standard | Plus rapide par job |
| Self-hosted runner | ✅ (mais rarement nécessaire) | ✅ (quasi obligatoire vu les 50 min) |
| Temps CI total | ~1m16s | ~3m (séquentiel) |

---

## Comparaison avec les autres plateformes

→ Voir [README.md](README.md) pour le tableau synthèse


