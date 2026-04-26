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
| **Templates / Réutilisation** | Reusable workflows | `include:` + parent-child | Templates YAML | YAML anchors uniquement |
| **Runners self-hosted** | ✅ | ✅ | ✅ (agents) | ✅ |
| **Artefacts (durée)** | 90 jours | 30 jours | 30 jours | 14 jours |
| **Environnements + approvals** | ✅ | ✅ | ✅ (avancé) | ⚠️ Basique |
| **Intégration Kubernetes** | ✅ | ✅ Natif | ✅ Natif | ⚠️ |
| **Marketplace / Templates** | ✅ Très riche | ✅ | ✅ | ⚠️ Limité |
| **Prix (au-delà du gratuit)** | $0.008/min | $10/mois | $40/mois | $15/mois |

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
- [Azure DevOps](azure-devops.md) — Templates, enregistrement UI, pipelines resources
- [Bitbucket](bitbucket.md) — Fichier unique, YAML anchors, limitations

---

## Stratégie de mirroring

```
GitHub (source de vérité)
    │
    ├── mirror.yml ──► GitLab     (pull mirror natif possible aussi)
    ├── mirror.yml ──► Bitbucket  (via SSH push)
    └── mirror.yml ──► Azure Repos (via SSH push)
```

> **Règle d'or** : On code et on pousse **uniquement** sur GitHub.
> Les autres plateformes reçoivent les commits via mirroring
> et déclenchent leurs propres pipelines automatiquement.

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
