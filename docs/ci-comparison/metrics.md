# Métriques de comparaison CI/CD — Projet DKPT

> Fichier de référence pour la collecte et la comparaison des métriques CI/CD
> sur les 4 plateformes étudiées dans le cadre du mémoire de Master.
>
> **Dernière mise à jour** : 2026-05-19
> **Runner** : Self-hosted ARM64 VPS (GitLab, Bitbucket, Azure) / GitHub-hosted ARM64 (GitHub)

---

## 1. Définition du cadre de mesure

### Protocole de mesure

Pour garantir la comparabilité des résultats :

| Règle | Détail |
|-------|--------|
| **Environnement** | Même code source, même commit déclenche chaque plateforme via mirroring |
| **Runner** | Self-hosted ARM64 sur VPS pour GitLab/Bitbucket/Azure, `ubuntu-24.04-arm` pour GitHub |
| **Cache** | Run "chaud" (pas le 1er run) pour les métriques de durée, sauf mention contraire |
| **Répétitions** | Minimum 3 runs par métrique, valeur médiane retenue |
| **Unité de temps** | Secondes (s) ou minutes:secondes (m:ss) |

### Catégories de métriques

| Catégorie | Nature | Objectif mémoire |
|-----------|:------:|:----------------:|
| **Performance** | Quantitative | Comparaison directe des temps d'exécution |
| **Coût** | Quantitative | Viabilité économique par plateforme |
| **Complexité de configuration** | Semi-quantitative | Effort d'adoption et de maintenance |
| **Fiabilité / Debugging** | Qualitative | Expérience développeur |
| **Fonctionnalités architecturales** | Binaire (✅/❌) | Caractérisation des capacités |

---

## 2. Métriques de performance — Temps d'exécution

> Les durées sont mesurées depuis les logs Bitbucket/GitLab/GitHub/Azure.
> Les valeurs marquées `—` n'ont pas encore été collectées.

### 2.1 CI — Build & Test

| Step | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|------|:--------------:|:---------:|:---------:|:------------:|
| **Backend** (dotnet restore + build + test) | — | **44s** | **35s** | — |
| **Frontend** (npm ci + lint + build) | — | **1m55s** | **5s** *(cache chaud)* | — |
| **CI total** (parallèle ou séquentiel) | ~1m16s | **~1m55s** *(parallèle)* | **~40s** *(cache)* | — |
| **Mode exécution** | Parallèle | Parallèle | Séquentiel | Parallèle |

> **Note** : GitHub et GitLab exécutent backend et frontend en parallèle (2 jobs simultanés).
> Bitbucket exécute les steps séquentiellement (contrainte de son modèle monolithique).
> Azure exécute en parallèle (2 jobs dans le stage BuildTest).

### 2.2 Docker Build (BUILD ONCE)

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Docker build backend** (run mesuré) | — | inclus dans 2m04s | inclus dans 2m39s | — |
| **Docker build frontend** (run mesuré) | — | inclus dans 2m04s | inclus dans 2m39s | — |
| **Docker push total** | — | inclus dans 2m04s | inclus dans 2m39s | — |
| **Docker Build total** | — | **2m04s** *(socket binding)* | **2m39s** *(DinD)* | — |
| **Cache Docker disponible** | ✅ `type=gha` | ✅ socket binding | ❌ DinD | ✅ daemon local |

### 2.3 CD Staging

| Step | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|------|:--------------:|:---------:|:---------:|:------------:|
| **Préparation répertoire VPS** | — | inclus dans 16s | inclus dans 37s | — |
| **Copie fichiers infra** (SCP/cp) | — | inclus dans 16s | inclus dans 37s | — |
| **Génération .env** | — | inclus dans 16s | inclus dans 37s | — |
| **docker compose pull + up** | — | inclus dans 16s | inclus dans 37s | — |
| **Retag :staging** | — | **13s** | inclus dans 37s | — |
| **CD Staging total** | — | **35s** (deploy 16s + retag 13s) | **37s** | — |

### 2.4 CD Production (manuel)

| Step | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|------|:--------------:|:---------:|:---------:|:------------:|
| **CD Prod total** (hors attente manuelle) | — | **32s** (deploy 17s + retag 14s) | **37s** | — |
| **Retag :latest** | — | **14s** | inclus dans 37s | — |

### 2.5 Pipeline main — Total bout en bout

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **CI total** | ~1m16s | **~1m55s** | ~40s *(cache)* | — |
| **CI + Docker build** | — | **~4m46s** | ~3m19s | — |
| **CI + Docker + CD Staging** | — | **~5m21s** | ~3m56s | — |
| **Pipeline complet** (hors attente prod) | — | **5m46s** | **4m36s** | — |

> **Gain socket binding vs DinD (GitLab)** : 7m07s *(DinD, run précédent)* → 5m46s *(socket binding)* = **−1m21s (~19%)** sur le même code source et même runner ARM64.

### 2.6 PR Check

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Durée PR check** | — | — | — | — |

---

## 3. Métriques de coût

### 3.1 Quota et tarification

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Minutes gratuites / mois** | 2 000 | 400 | 50 | 1 800 |
| **Jobs parallèles (gratuit)** | 20 | 5 | 5 | 10 |
| **Coût marginal ($/min)** | $0.008 | $0.017 | $0.015 | $0.008 |
| **Durée artefacts** | 90 jours | 30 jours | 14 jours | 30 jours |
| **Consommation par run main** | ~1m16s | ~2m35s | ~3m07s | — |
| **% quota mensuel / run** | ~0.06% | ~0.65% | ~6.2% | — |

### 3.2 Impact self-hosted runner

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Minutes consommées (self-hosted)** | N/A | 0 (illimité) | 0 (illimité) | 0 (illimité) |
| **Raison d'adoption self-hosted** | ARM64 natif | ARM64 natif + quota | 50 min/mois trop limité | ARM64 natif |
| **Impact sur quota** | Partiellement (Docker runner) | Nul | Nul | Nul |

---

## 4. Métriques de complexité de configuration

### 4.1 Structure des fichiers

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Nombre de fichiers pipeline** | 5 core (+ 3 variantes) | 5 (orchestrateur + 4 enfants) | 1 | 4 (+ 5 templates) |
| **Lignes YAML totales (core)** | **548** | **362** | **203** | **604** |
| **Lignes YAML (avec variantes/templates)** | 791 | 362 | 203 | 604 |
| **Templates / réutilisation** | Reusable workflows | Parent-child + `include:` | YAML anchors | Templates paramétrés |
| **Enregistrement UI requis** | ❌ Auto | ❌ Auto | ❌ Auto | ✅ Manuel (par pipeline) |

#### Détail par fichier

| Fichier | Lignes | Plateforme |
|---------|:------:|:-----------:|
| `.github/workflows/ci.yml` | 149 | GitHub |
| `.github/workflows/cd-staging.yml` | 133 | GitHub |
| `.github/workflows/cd-prod.yml` | 134 | GitHub |
| `.github/workflows/pr-check.yml` | 77 | GitHub |
| `.github/workflows/mirror.yml` | 55 | GitHub |
| `.github/workflows/variante-1-parallel.yml` | 74 | GitHub (variante) |
| `.github/workflows/variante-2-single-job.yml` | 77 | GitHub (variante) |
| `.github/workflows/variante-4-matrix.yml` | 92 | GitHub (variante) |
| `.gitlab-ci.yml` (orchestrateur) | 71 | GitLab |
| `.gitlab/pipelines/ci.yml` | 86 | GitLab |
| `.gitlab/pipelines/cd-staging.yml` | 77 | GitLab |
| `.gitlab/pipelines/cd-prod.yml` | 80 | GitLab |
| `.gitlab/pipelines/pr-check.yml` | 48 | GitLab |
| `bitbucket-pipelines.yml` | 203 | Bitbucket |
| `.azuredevops/ci.yml` | 99 | Azure |
| `.azuredevops/cd-staging.yml` | 73 | Azure |
| `.azuredevops/cd-prod.yml` | 54 | Azure |
| `.azuredevops/pr-check.yml` | 38 | Azure |
| `.azuredevops/templates/build-docker.yml` | 33 | Azure (template) |
| `.azuredevops/templates/build-dotnet.yml` | 60 | Azure (template) |
| `.azuredevops/templates/build-nuxt.yml` | 33 | Azure (template) |
| `.azuredevops/templates/deploy-vps.yml` | 81 | Azure (template) |
| `.azuredevops/templates/quality-gate.yml` | 133 | Azure (template) |

> **Observation** : Bitbucket a le moins de lignes (203) car tout est dans un seul fichier.
> Azure a le plus (604) mais ses 5 templates sont réutilisés entre cd-staging et cd-prod,
> réduisant la duplication. GitHub a 548 lignes core mais propose des variantes architecturales
> supplémentaires non actives en production.

### 4.2 Variables et secrets

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Nombre de variables à configurer** | ~18 | ~18 | ~16 | ~18 (via Variable Groups) |
| **Mécanisme SSH** | Secret chiffré → `echo > file` | Variable type `File` | SSH Keys natif | Service Connection |
| **Gestion des credentials Docker** | Secret (`DOCKERHUB_TOKEN`) | Secret | Secret | Service Connection |

### 4.3 Effort de mise en place

| Étape | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|-------|:--------------:|:---------:|:---------:|:------------:|
| **Connexion repo** | Native | Via `mirror.yml` | Via `mirror.yml` | GitHub App (native) |
| **Config SSH VPS** | Secrets → bash | Variable `File` | SSH Keys UI | Service Connection UI |
| **Problèmes critiques rencontrés** | 2 | 3 | 5 | 1 |
| **Temps de mise en place estimé** | — | — | — | — |

---

## 5. Métriques de fiabilité et debugging

### 5.1 Problèmes rencontrés lors de l'implémentation

| # | Problème | Plateforme | Temps résolution | Catégorie |
|:-:|----------|:----------:|:----------------:|:---------:|
| 1 | `lint --fix` absent → trailing spaces | GitHub | < 1h | Configuration |
| 2 | `workflow_run.event == 'push'` bloque CD Prod | GitHub | < 1h | Logique conditionnelle |
| 3 | `error in libcrypto` → CRLF dans clé SSH | GitLab + Bitbucket | ~2h | SSH / Secrets |
| 4 | `Name does not resolve` → DNS bridge Docker | GitLab | ~1h | Réseau runner |
| 5 | `<<:` merge YAML + `runs-on` → parsing error | Bitbucket | ~2h | YAML Syntax |
| 6 | `: ` dans `echo` → `Missing or empty command` | Bitbucket | ~1h | YAML Syntax |
| 7 | `Host key verification failed` → Known host | Bitbucket | < 1h | SSH |
| 8 | `Permission denied` → clé publique absente prod | Bitbucket | < 1h | SSH |
| 9 | Socket binding non propagé aux step containers | Bitbucket | ~1h | Docker runtime |
| 10 | CD Prod skipped → condition `workflow_run` trop stricte | GitHub | < 1h | Logique conditionnelle |

### 5.2 Clarté des logs (évaluation qualitative)

| Critère | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|---------|:--------------:|:---------:|:---------:|:------------:|
| **Message d'erreur lisible** | ✅ Excellent | ✅ Bon | ⚠️ Moyen | ✅ Bon |
| **Localisation de l'erreur** | ✅ Ligne précise | ✅ Job + stage | ⚠️ Step seulement | ✅ Task précise |
| **Logs en temps réel** | ✅ | ✅ | ✅ | ✅ |
| **Recherche dans les logs** | ✅ | ✅ | ⚠️ Limitée | ✅ |

---

## 6. Fonctionnalités architecturales

| Fonctionnalité | GitHub | GitLab | Bitbucket | Azure |
|----------------|:------:|:------:|:---------:|:-----:|
| **Pipelines CI/CD séparés** | ✅ | ✅ | ❌ (monolithique) | ✅ |
| **Chaînage inter-pipelines** | ✅ `workflow_run` | ✅ parent-child | ❌ | ✅ `resources: pipelines` |
| **Build Once Deploy Many natif** | ✅ | ✅ | ⚠️ (contournement) | ✅ |
| **Cache Docker local** | ✅ `type=gha` | ✅ socket binding | ❌ DinD | ✅ daemon local |
| **Socket binding Docker** | N/A | ✅ `config.toml` | ❌ non propagé | N/A (shell) |
| **Templates paramétrés** | ⚠️ Reusable workflows | ⚠️ `include:` | ❌ YAML anchors | ✅ Natif |
| **Quality Gate post-deploy** | ❌ | ❌ | ❌ | ✅ (k6 + ZAP + Playwright) |
| **Analyse statique (SonarCloud)** | ❌ | ❌ | ❌ | ✅ |
| **Approbation manuelle prod** | ✅ (environment) | ✅ `when: manual` | ✅ `trigger: manual` | ✅ (environment gate) |
| **Environnements nommés** | ✅ Dev + Prod | ✅ Staging + Prod | ✅ Staging + Prod | ✅ Staging + Prod |
| **Connexion repo native** | ✅ | ❌ (mirror) | ❌ (mirror) | ✅ (GitHub App) |
| **Sélecteur plateforme (commit msg)** | ✅ `contains()` | ✅ `=~ regex` | ⚠️ bash guard | — |
| **Self-hosted runner** | ✅ | ✅ | ✅ | ✅ |

---

## 7. Synthèse — Tableau de scoring

> Score de 1 à 5 par catégorie (5 = meilleur).
> Pondération indicative pour mémoire à ajuster selon les critères du jury.

| Catégorie | Poids | GitHub | GitLab | Bitbucket | Azure |
|-----------|:-----:|:------:|:------:|:---------:|:-----:|
| **Performance** (temps pipeline) | 25% | 5 | 4 | 3 | 4 |
| **Coût** (quota + tarif) | 20% | 5 | 4 | 2 | 5 |
| **Complexité configuration** | 20% | 4 | 3 | 4 | 3 |
| **Fiabilité / Debugging** | 15% | 4 | 4 | 3 | 5 |
| **Fonctionnalités architecturales** | 20% | 4 | 4 | 2 | 5 |
| **Score pondéré** | 100% | **4.4** | **3.8** | **2.8** | **4.4** |

> ⚠️ Ce scoring est indicatif et basé sur les valeurs disponibles au 2026-05-19.
> Les cases `—` doivent être complétées avec les mesures réelles.

---

## 8. Données manquantes à collecter

| Métrique | Action requise |
|----------|---------------|
| GitHub Actions — durée backend/frontend CI | Déclencher `[ci:github]` et relever les logs |
| Azure DevOps — toutes les durées | Déclencher le pipeline et relever les logs |
| Docker build — durées par plateforme | Mesurer sur 3 runs consécutifs |
| CD Staging — durées par plateforme | Mesurer sur 3 runs consécutifs |
| Temps de mise en place initial | Estimation rétrospective |
| Lignes YAML GitLab (orchestrateur inclus) | Compter avec `wc -l` |

```bash
# Compter les lignes YAML de chaque plateforme
wc -l .github/workflows/*.yml .gitlab-ci.yml .gitlab/pipelines/*.yml bitbucket-pipelines.yml .azuredevops/*.yml .azuredevops/templates/*.yml
```
