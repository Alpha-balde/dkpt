# Comparaison des systèmes CI/CD — Projet DKPT

> Documentation comparative pour le mémoire de Master.
> Le même projet (DKPT) est implémenté sur 4 plateformes CI/CD différentes.

---

## Vue d'ensemble

| Plateforme | Dossier config | Fichiers pipeline |
|------------|---------------|-------------------|
| **GitHub Actions** | `.github/workflows/` | `ci.yml`, `cd-staging.yml`, `cd-prod.yml`, `pr-check.yml`, `mirror.yml` + 3 variantes |
| **GitLab CI** | `.gitlab-ci.yml` (racine) + `.gitlab/pipelines/` | Orchestrateur parent + 4 pipelines enfants |
| **Azure DevOps** | `.azuredevops/` | `ci.yml`, `cd-staging.yml`, `cd-prod.yml`, `pr-check.yml` + `templates/` |
| **Bitbucket** | `bitbucket-pipelines.yml` (racine) | 1 seul fichier avec sections |

---

## Tableau comparatif synthèse

| Critère | GitHub Actions | GitLab CI | Azure DevOps | Bitbucket |
|---------|:-:|:-:|:-:|:-:|
| **Format config** | YAML | YAML | YAML | YAML |
| **Pipelines séparés** | ✅ Natif (1 fichier = 1 pipeline) | ⚠️ Via parent-child | ✅ Natif (1 fichier = 1 pipeline UI) | ❌ Sections dans 1 fichier |
| **Déclenchement indépendant** | ✅ | ✅ (parent-child) | ✅ | ⚠️ Partiel (branches/tags) |
| **Minutes/mois (gratuit)** | 2 000 | 400 | 1 800 | 50 |
| **Jobs parallèles (gratuit)** | 20 | 5 | 10 | 5 |
| **Cache natif** | ✅ | ✅ | ✅ | ✅ |
| **Docker-in-Docker** | ✅ | ✅ (service `dind`) | ✅ | ✅ (service) |
| **Templates / Réutilisation** | Reusable workflows | `include:` + parent-child | **Templates paramétrés** | YAML anchors uniquement |
| **Runners self-hosted** | ✅ | ✅ | ✅ (agents) | ✅ |
| **Artefacts (durée)** | 90 jours | 30 jours | 30 jours | 14 jours |
| **Environnements + approvals** | ✅ | ✅ | ✅ (avancé) | ⚠️ Basique |
| **Build Once Deploy Many** | ❌ | ❌ (adapté) | ✅ | ❌ (impossible nativement) |
| **Quality Gate post-deploy** | ❌ | ❌ | ✅ (k6 + ZAP + Playwright) | ❌ |
| **Analyse statique intégrée** | ❌ | ❌ | ✅ (SonarCloud + dotnet) | ❌ |
| **Sélecteur plateforme (commit msg)** | ✅ Natif (`contains()`) | ✅ Natif (`=~ regex`) | — | ⚠️ Guard bash (`git log`) |
| **Connexion repo** | Native | Via `mirror.yml` | **GitHub App (sans mirror)** | Via `mirror.yml` |
| **Marketplace / Templates** | ✅ Très riche | ✅ | ✅ | ⚠️ Limité |
| **Prix (au-delà du gratuit)** | $0.008/min | $10/mois | $40/mois | $15/mois |

---

> **Observation clé pour le mémoire** : Le BODM est nativement impossible sur Bitbucket.
> Les types de pipelines (`default:`, `branches:`, `pull-requests:`) sont mutuellement exclusifs
> par branche — il n'existe aucun mécanisme de chaînage inter-pipelines.
> Voir [bitbucket.md → Limitation architecturale](bitbucket.md#limitation-architecturale--build-once-deploy-many-impossible)
> pour l'analyse complète et le contournement adopté.

---

## Contraintes d'organisation par plateforme

| Contrainte | GitHub Actions | GitLab CI | Azure DevOps | Bitbucket |
|------------|:-:|:-:|:-:|:-:|
| Emplacement fichier | `.github/workflows/` uniquement | Racine (`.gitlab-ci.yml`) | Libre (configuré dans l'UI) | Racine uniquement |
| Sous-dossiers reconnus | ❌ Non | ✅ Via `include:` | ✅ Via `template:` | ❌ Non |
| Enregistrement UI requis | ❌ Automatique | ❌ Automatique | ✅ Manuel | ❌ Automatique |
| Multi-fichiers natif | ✅ | ⚠️ Parent-child | ✅ | ❌ |

---

## Détails par plateforme

- [GitHub Actions](github-actions.md) — Analyse détaillée + variantes
- [GitLab CI](gitlab-ci.md) — Pattern parent-child, services DinD
- [Azure DevOps](azure-devops.md) — **Plateforme la plus complète** : templates paramétrés, Build Once Deploy Many, Quality Gate (k6 + ZAP + Playwright), SonarCloud, GitHub App
- [Bitbucket](bitbucket.md) — Fichier unique, YAML anchors, limitations
- [Quality Gates](quality-gates-integration.md) — Journal d'implémentation détaillé (5 phases, 10 problèmes)
- [Sélecteur de plateforme](platform-selector.md) — Stratégie de déclenchement ciblé par mot-clé commit (`[ci:github]`, `[ci:gitlab]`, `[ci:bitbucket]`)
- [Environnements de déploiement](environments.md) — Staging/Production sur les 3 plateformes : approbation manuelle, reviewers, comparaison des plans

---

## Stratégie de connexion au dépôt

```
GitHub (source de vérité)
    │
    ├── mirror.yml ──► GitLab     (SSH push, pull mirror natif = Premium)
    ├── mirror.yml ──► Bitbucket  (SSH push, pas de pull mirror natif)
    └── GitHub App ──► Azure DevOps (connexion native, pas de mirror)
```

> **Règle d'or** : On code et on pousse **uniquement** sur GitHub.
> GitLab et Bitbucket reçoivent les commits via le workflow `mirror.yml`.
> Azure DevOps se connecte directement au dépôt GitHub via **GitHub App**
> (configuré lors de la création du pipeline dans l'UI Azure DevOps).

| Plateforme | Méthode | Latence | Clé SSH requise |
|------------|---------|:-------:|:---------------:|
| GitLab | Push via `mirror.yml` | ~1s | ✅ `GITLAB_SSH_KEY` |
| Bitbucket | Push via `mirror.yml` | ~1s | ✅ `BITBUCKET_SSH_KEY` |
| Azure DevOps | GitHub App (native) | Instantané | ❌ |

---

## Secrets par plateforme

Chaque plateforme nécessite ses propres secrets (mêmes valeurs, mêmes noms) :

| Secret | Description |
|--------|-------------|
| `DOCKERHUB_USERNAME` | Username Docker Hub |
| `DOCKERHUB_TOKEN` | Access token Docker Hub |
| `VPS_HOST` | IP du VPS production |
| `VPS_USER` | Utilisateur SSH production |
| `VPS_SSH_KEY` | Clé privée SSH production |
| `VPS_STAGING_HOST` | IP du VPS staging |
| `VPS_STAGING_USER` | Utilisateur SSH staging |
| `VPS_STAGING_SSH_KEY` | Clé privée SSH staging |

---

## Environnements déployés

> Toutes les plateformes déploient vers les **mêmes VPS** avec les **mêmes variables**.

| Environnement | URL | Branche | Approbation |
|---------------|-----|---------|:-----------:|
| **Staging** | https://staging.dkpt.soguimod.com | `main` | Automatique |
| **Production** | https://dkpt.soguimod.com | `main` | Manuelle |
