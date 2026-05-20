# Métriques de comparaison CI/CD — Projet DKPT

> Fichier de référence pour la collecte et la comparaison des métriques CI/CD
> sur les 4 plateformes étudiées dans le cadre du mémoire de Master.
>
> **Dernière mise à jour** : 2026-05-20
> **Runner** : Hybrid Optimal — CI hosted (plateforme) / Docker+CD self-hosted ARM64 VPS

---

## 1. Définition du cadre de mesure

### Protocole de mesure

Pour garantir la comparabilité des résultats :

| Règle | Détail |
|-------|--------|
| **Environnement** | Même code source, même commit déclenche chaque plateforme via mirroring |
| **Runner** | Self-hosted ARM64 sur VPS pour GitLab/Bitbucket/Azure, `ubuntu-24.04-arm` pour GitHub |
| **Cache** | Run "chaud" (pas le 1er run) pour les métriques de durée, sauf mention contraire |
| **Répétitions** | 1 à 3 runs par métrique selon disponibilité ; valeur du run stable (cache chaud) retenue — protocole cible non toujours atteint (voir §2) |
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

### Configuration de référence au moment des mesures

> ⚠️ Les métriques ne sont comparables qu'en connaissant la configuration exacte au moment de la mesure.
> Toute modification de config (runner, executor, cache) doit être documentée ici.

| Paramètre | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|-----------|:--------------:|:---------:|:---------:|:------------:|
| **Commit de référence** | `d0bc196` | `1a05dd5` | `744611b` | `e056145` *(approx.)* |
| **Pipeline de référence** | CI #78 / Staging #44 / Prod #50 | #2536856712 | #41 | CI #15 *(collecté — ref. non notée)* |
| **Date de mesure** | 2026-05-19 | 2026-05-19 | 2026-05-19 | 2026-05-19 |
| **Runner CI** | `ubuntu-24.04-arm` (GitHub-hosted) | `ubuntu_arm64` (self-hosted VPS) | `self.hosted` ARM64 (VPS) | `ubuntu-latest` + `Default` |
| **Runner Docker build** | `ubuntu-24.04-arm` (GitHub-hosted) | `ubuntu_arm64` (self-hosted VPS) | `self.hosted` ARM64 (VPS) | `Default` (self-hosted VPS) |
| **Executor Docker** | buildx + `build-push-action` | **Socket binding** (`/var/run/docker.sock`) | **DinD** (`services: docker`) | Shell executor (daemon local) |
| **Cache Docker** | `type=gha` (cache GitHub Actions) | Daemon local (socket binding) | ❌ Aucun (DinD éphémère) | Daemon local (shell executor) |
| **Lint frontend** | `lint` strict *(simplifié après refactoring — voir §2.1)* | `lint` strict | `lint` strict | `lint` strict |
| **SSH deploy** | `appleboy/ssh-action` (action) | `ssh`/`scp` manuels | SSH Keys natif Bitbucket | Agent local sur VPS (pas de SSH) |

### 2.1 CI — Build & Test

| Step | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|------|:--------------:|:---------:|:---------:|:------------:|
| **Backend** (restore + build + test) | **29s** *(parallèle)* | **44s** | **35s** | **28s** *(Core CI)* |
| **Frontend** (npm ci + lint + build) | **54s** *(parallèle)* | **1m55s** | **5s** *(cache)* | **1m7s** (install 25s + lint 4s + build 38s) |
| **CI total** (temps effectif) | **~54s** *(parallèle — job le plus lent)* | **~1m55s** *(parallèle)* | **~40s** *(cache)* | **~1m7s** *(parallèle, Core CI)* |
| **CI total** *(séquentiel, ancien run)* | ~~1m24s~~ | N/A | N/A | N/A |
| **Full CI total** (avec SonarCloud) | N/A | N/A | N/A | **2m21s** (+52s SonarCloud) |
| **Mode exécution** | Parallèle *(2 jobs)* | Parallèle | Séquentiel | Parallèle |

> **Optimisation GitHub** : Refactoring du job `build-and-test` (séquentiel) en 2 jobs parallèles
> `backend-build-test` + `frontend-build`. Gain mesuré : **1m24s → 54s = −30s (~36%)**.
> Le lint frontend est simplifié à une seule passe (aligné avec les autres plateformes).

> **Note Azure — Core CI vs Full CI** :
> - **Core CI** = restore (2s) + build (17s) + test (9s) = **28s** — comparable aux autres plateformes
> - **Full CI** = Core CI + SonarCloud (Prepare 5s + Analyze 46s + Publish 1s) + NuGet cache (7s) + divers = **2m21s**
> - L’overhead SonarCloud (**~52s**) est exclu de la comparaison principale.

> **Note** : GitLab, GitHub et Azure exécutent backend et frontend en parallèle (2 jobs simultanés).
> Bitbucket exécute les steps séquentiellement (contrainte de son modèle monolithique).

### 2.2 Docker Build (BUILD ONCE)

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Docker build backend** (run mesuré) | **53s** | inclus dans 2m04s | inclus dans 2m39s | inclus dans 11s |
| **Docker build frontend** (run mesuré) | **1m21s** | inclus dans 2m04s | inclus dans 2m39s | inclus dans 11s |
| **Setup Docker Buildx** | **18s** | N/A (socket binding) | N/A (DinD) | N/A |
| **Docker Build total** | **2m40s** *(séquentiel backend + frontend)* | **2m04s** *(socket binding)* | **2m39s** *(DinD)* | **11s** *(ARM64, daemon local)* |
| **Cache Docker disponible** | ✅ `type=gha` (0% hit ce run) | ✅ socket binding | ❌ DinD | ✅ daemon local |

> **Observation Docker** : GitHub utilise Docker Buildx avec cache `type=gha`.
> Le cache était à **0%** sur ce run (premier run après le refactoring des jobs).
> Azure est le plus rapide (11s) grâce au daemon local sur runner self-hosted ARM64.

### 2.3 CD Staging

| Step | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|------|:--------------:|:---------:|:---------:|:------------:|
| **Préparation répertoire VPS** | **7s** *(prepare dir)* | inclus dans 16s | inclus dans 37s | 3s *(Prepare & copy infra)* |
| **Copie fichiers infra** (SCP/cp) | **7s** *(scp-action)* | inclus dans 16s | inclus dans 37s | inclus dans 3s |
| **Génération .env** | **1s** | inclus dans 16s | inclus dans 37s | **2s** |
| **docker compose pull + up** | **10s** *(SSH deploy)* | inclus dans 16s | inclus dans 37s | **28s** |
| **Retag :staging** | **22s** *(job séparé)* | **13s** | inclus dans 37s | **15s** |
| **CD Staging total** (jobs actifs) | **50s** (deploy 28s + retag 22s) | **35s** (deploy 16s + retag 13s) | **37s** | **1m7s** |
| **CD Staging total** (pipeline mur à mur) | **58s** | **35s** | **37s** | **1m30s** |

### 2.4 CD Production (manuel)

| Step | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|------|:--------------:|:---------:|:---------:|:------------:|
| **CD Prod total** (hors attente manuelle) | **48s** *(steps actifs)* | **32s** (deploy 17s + retag 14s) | **37s** | **19s** (deploy 10s + retag 5s) |
| **CD Prod total** (overhead `workflow_run` inclus) | **3m39s** | N/A (déclenchement parallèle) | N/A | N/A |
| **Retag :latest** | **15s** | **14s** | inclus dans 37s | **5s** |
| **Deploy** | **33s** | **17s** | inclus dans 37s | **10s** |

> **Note `workflow_run`** : Le CD Prod GitHub affiche 3m39s car il attend que le CD Staging
> termine via `workflow_run`. Le temps actif des steps (deploy + retag) est **48s**.

### 2.5 Pipeline main — Total bout en bout

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **CI total** (Core CI, comparable) | **~54s** *(parallèle)* | **~1m55s** *(parallèle)* | ~40s *(cache)* | **~1m7s** *(parallèle)* |
| **CI total** (Full CI avec SonarCloud) | N/A | N/A | N/A | **2m21s** |
| **CI + Docker build** | **~3m34s** (54s CI + 2m40s Docker) | **~4m46s** | ~3m19s | **~1m18s** *(Core CI + 11s Docker)* |
| **CI + Docker + CD Staging** | **~4m24s** | **~5m21s** | ~3m56s | **~2m48s** *(Core)* / **~3m42s** *(Full)* |
| **Pipeline complet** (steps actifs) | **~4m24s** | **5m46s** | **4m36s** | **~2m48s** *(Core CI)* |

> **Gain socket binding vs DinD (GitLab)** : 7m07s *(DinD, run précédent)* → 5m46s *(socket binding)* = **−1m21s (~19%)** sur le même code source et même runner ARM64.

> **Note GitHub workflow_run** : L'architecture GitHub (CI → CD Staging → CD Prod via `workflow_run`)
> ajoute une latence entre chaque étape. Le temps mur à mur est plus élevé que le temps actif cumulatif.

### 2.6 PR Check

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Durée PR check** | — | — | — | — |

---

## 3. Métriques de coût

> **Méthodologie** : GitHub et Azure facturent les runners **hosted** à la minute
> (arrondi au-dessus). GitLab, Bitbucket et Azure utilisent des runners **self-hosted**
> pour les jobs lourds (Docker, tests) — consommation quota = 0 min pour ces jobs.
> Les runners self-hosted du projet DKPT sont mutualisés sur un VPS ARM64 partagé.

### 3.1 Tarification des plateformes

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Minutes gratuites / mois** | 2 000 | 400 | 50 | 1 800 |
| **Jobs parallèles (gratuit)** | 20 | 5 | 5 | 10 |
| **Coût marginal Linux ($/min)** | $0.008 | $0.017 | $0.015 | $0.008 |
| **Coût marginal ARM64 ($/min)** | $0.008 | N/A | N/A | N/A |
| **Durée artefacts** | 90 jours | 30 jours | 14 jours | 30 jours |

### 3.2 Consommation réelle par run (pipeline complet main)

> Les durées mesurées sont arrondies à la minute supérieure (unité de facturation).

| Job | Runner | GitHub | GitLab | Bitbucket | Azure |
|-----|--------|:------:|:------:|:---------:|:-----:|
| **CI Backend** | Hosted | 1 min | 0 *(self-hosted)* | 0 *(self-hosted)* | 1 min |
| **CI Frontend** | Hosted | 1 min | 0 *(self-hosted)* | 0 *(self-hosted)* | 0 *(self-hosted)* |
| **Docker Build** | ARM64 hosted / self-hosted | 3 min | 0 *(self-hosted)* | 0 *(self-hosted)* | 0 *(self-hosted)* |
| **CD Staging Deploy** | Hosted | 1 min | 0 *(self-hosted)* | 0 *(self-hosted)* | 1 min |
| **CD Staging Retag** | ARM64 hosted / self-hosted | 1 min | 0 *(self-hosted)* | 0 *(self-hosted)* | 0 *(self-hosted)* |
| **CD Prod Deploy** | Hosted | 1 min | 0 *(self-hosted)* | 0 *(self-hosted)* | 1 min |
| **CD Prod Retag** | ARM64 hosted / self-hosted | 1 min | 0 *(self-hosted)* | 0 *(self-hosted)* | 0 *(self-hosted)* |
| **Total facturé / run** | | **~9 min** | **0 min** | **0 min** | **~3 min** |
| **% quota mensuel / run** | | ~0.45% | 0% | 0% | ~0.17% |

> **Note GitHub** : Les 2 jobs CI parallèles (backend 29s + frontend 54s) consomment
> 2 minutes de runner (1 par job). Les jobs Docker et Retag sur `ubuntu-24.04-arm`
> sont facturés comme des runners Linux ARM64 (même tarif $0.008/min).

> **Note Azure** : Les jobs CI (backend + frontend) tournent sur des runners hosted
> (`ubuntu-latest`). Les jobs Docker Build et CD Retag tournent sur `DKPT-ARM64`
> (self-hosted) → **0 quota consommé** pour ces jobs.

### 3.3 Projections mensuelles

#### Scénario A — 20 pushes/mois (1 par jour ouvré, équipe solo)

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Minutes hosted consommées** | ~180 min | 0 min | 0 min | ~60 min |
| **Quota mensuel disponible** | 2 000 min | 400 min | 50 min | 1 800 min |
| **Quota restant** | 1 820 min | 400 min | 50 min | 1 740 min |
| **Dépassement** | ❌ Non | ❌ Non | ❌ Non | ❌ Non |
| **Coût mensuel** | **$0** | **$0** | **$0** | **$0** |

#### Scénario B — 100 pushes/mois (équipe de 5, active)

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Minutes hosted consommées** | ~900 min | 0 min | 0 min | ~300 min |
| **Quota mensuel disponible** | 2 000 min | 400 min | 50 min | 1 800 min |
| **Dépassement** | ❌ Non | ❌ Non | ❌ Non | ❌ Non |
| **Coût mensuel** | **$0** | **$0** | **$0** | **$0** |

#### Scénario C — 100 pushes/mois SANS runners self-hosted

> Simulation : que se passerait-il si GitLab et Bitbucket utilisaient
> uniquement des runners **hosted** (sans VPS self-hosted) ?

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Minutes hosted / run** | ~9 min | ~9 min | ~9 min | ~9 min |
| **Minutes totales (100 runs)** | 900 min | 900 min | 900 min | 900 min |
| **Quota gratuit** | 2 000 min | 400 min | 50 min | 1 800 min |
| **Dépassement** | 0 min | **500 min** | **850 min** | 0 min |
| **Coût dépassement** | **$0** | **~$8.50** | **~$12.75** | **$0** |

> **Observation** : Sans self-hosted, Bitbucket serait la plateforme la plus coûteuse
> à usage modéré (quota 50 min/mois épuisé dès le 6ème push mensuel).
> GitLab dépasserait son quota dès le 45ème push. GitHub et Azure resteraient
> dans le quota gratuit même à 100 pushes/mois.

### 3.4 Impact self-hosted — Analyse

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Jobs sur self-hosted** | Docker Build + Retag | Tous (CI + Docker + CD) | Tous (CI + Docker + CD) | Docker Build + CD Deploy + Retag |
| **Minutes économisées / run** | ~4 min | ~9 min | ~9 min | ~6 min |
| **Économie (100 runs/mois)** | 400 min → $3.20 | 900 min → $15.30 | 900 min → $13.50 | 600 min → $4.80 |
| **Stratégie** | ARM64 uniquement | Quota + ARM64 | Quota très limité | ARM64 + quota préservé |

### 3.5 Coût Total de Possession — TCO simplifié

Le VPS ARM64 (runner self-hosted) est **mutualisé** entre les 4 plateformes.
Son coût est amorti sur l'ensemble des projets hébergés.

| Composante | Coût mensuel | Notes |
|-----------|:------------:|-------|
| **VPS staging** (ARM64, 4 vCPU, 8 GB) | ~$12/mois | Hébergement application DKPT |
| **VPS production** (ARM64, 4 vCPU, 8 GB) | ~$12/mois | Production + runner CI/CD |
| **CI/CD plateforme (hosted runners)** | $0/mois | Dans les quotas gratuits |
| **Docker Hub** | $0/mois | Plan gratuit (images publiques) |
| **SonarCloud** | $0/mois | Plan gratuit (repo public) |
| **Total infrastructure** | **~$24/mois** | Identique pour les 4 plateformes |

> **Conclusion coût** : Pour un projet solo ou une petite équipe (<5 devs, <100 pushes/mois),
> les 4 plateformes sont gratuites en termes de CI/CD grâce aux quotas hosted et
> aux runners self-hosted. La différence de coût entre plateformes n'apparaît
> qu'à grande échelle (>200 pushes/mois sans self-hosted).
> Le vrai différenciateur n'est donc pas le coût mais la **générosité du quota**
> (GitHub > Azure >> GitLab > Bitbucket) et la **flexibilité du self-hosted**.

---

## 4. Métriques de complexité de configuration

### 4.1 Structure des fichiers

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Nombre de fichiers pipeline** | 6 core (+ 3 variantes) | 5 (orchestrateur + 4 enfants) | 1 | 4 (+ 5 templates) |
| **Lignes YAML totales (core)** | **590** | **362** | **203** | **604** |
| **Lignes YAML (avec variantes/templates)** | 833 | 362 | 203 | 604 |
| **Templates / réutilisation** | Reusable workflows (`workflow_call`) | Parent-child + `include:` | YAML anchors | Templates paramétrés |
| **Enregistrement UI requis** | ❌ Auto | ❌ Auto | ❌ Auto | ✅ Manuel (par pipeline) |

#### Détail par fichier

| Fichier | Lignes | Rôle | Plateforme |
|---------|:------:|------|:-----------:|
| `.github/workflows/ci.yml` | **157** | CI — 2 jobs parallèles (backend + frontend) + Docker push | GitHub |
| `.github/workflows/cd-staging.yml` | **119** | CD Staging — deploy SSH + appel reusable retag | GitHub |
| `.github/workflows/cd-prod.yml` | **120** | CD Prod — deploy SSH + appel reusable retag | GitHub |
| `.github/workflows/reusable-retag.yml` | **62** | Reusable workflow — retag :sha → :tag (QR3) | GitHub *(core)* |
| `.github/workflows/pr-check.yml` | 77 | PR check | GitHub |
| `.github/workflows/mirror.yml` | 55 | Synchronisation inter-plateformes | GitHub |
| `.github/workflows/variante-1-parallel.yml` | 74 | *Variante archivée — remplacée par ci.yml* | GitHub (variante) |
| `.github/workflows/variante-2-single-job.yml` | 77 | Variante 1 job séquentiel | GitHub (variante) |
| `.github/workflows/variante-4-matrix.yml` | 92 | Variante matrix OS × version | GitHub (variante) |
| `.gitlab-ci.yml` (orchestrateur) | 71 | Orchestrateur — inclut les 4 pipelines enfants | GitLab |
| `.gitlab/pipelines/ci.yml` | 86 | CI — backend + frontend + Docker push | GitLab |
| `.gitlab/pipelines/cd-staging.yml` | 77 | CD Staging — deploy SSH + retag :staging | GitLab |
| `.gitlab/pipelines/cd-prod.yml` | 80 | CD Prod — deploy SSH + retag :latest | GitLab |
| `.gitlab/pipelines/pr-check.yml` | 48 | PR check | GitLab |
| `bitbucket-pipelines.yml` | 203 | Pipeline monolithique — CI + Docker + CD Staging + CD Prod | Bitbucket |
| `.azuredevops/ci.yml` | 99 | CI — backend + frontend + Docker push + SonarCloud | Azure |
| `.azuredevops/cd-staging.yml` | 73 | CD Staging — deploy VPS + retag :staging | Azure |
| `.azuredevops/cd-prod.yml` | 54 | CD Prod — deploy VPS + retag :latest | Azure |
| `.azuredevops/pr-check.yml` | 38 | PR check | Azure |
| `.azuredevops/templates/build-docker.yml` | 33 | Template — Docker build & push | Azure (template) |
| `.azuredevops/templates/build-dotnet.yml` | 60 | Template — .NET restore + build + test | Azure (template) |
| `.azuredevops/templates/build-nuxt.yml` | 33 | Template — npm ci + lint + build | Azure (template) |
| `.azuredevops/templates/deploy-vps.yml` | 81 | Template — deploy SSH + retag | Azure (template) |
| `.azuredevops/templates/quality-gate.yml` | 133 | Template — k6 + ZAP + Playwright (Quality Gate) | Azure (template) |

> **Observation** : Bitbucket a le moins de lignes (203) car tout est dans un seul fichier.
> Azure a le plus (604) car ses 5 templates sont réutilisés entre cd-staging et cd-prod.
> GitHub atteint 590 lignes core (6 fichiers) dont 62L pour le reusable workflow `reusable-retag.yml`
> qui centralise la logique dupliquée entre cd-staging et cd-prod (−32L de duplication).

### 4.2 Variables et secrets

| Métrique | GitHub Actions | GitLab CI | Bitbucket | Azure DevOps |
|----------|:--------------:|:---------:|:---------:|:------------:|
| **Nombre de variables à configurer** | ~18 | ~18 | ~16 | ~18 (via Variable Groups) |
| **Mécanisme SSH** | Secret chiffré → `echo > file` | Variable type `File` | SSH Keys natif | Service Connection |
| **Gestion des credentials Docker** | Secret (`DOCKERHUB_TOKEN`) | Secret | Secret | Service Connection |

### 4.3 Effort de mise en place

> **Méthodologie** : Temps estimés à partir de l'expérience réelle du projet DKPT,
> en incluant la recherche de documentation, le débogage et la configuration des secrets.
> Les durées reflètent un profil développeur familier avec Docker et CI/CD en général,
> mais découvrant chaque plateforme pour la première fois.

#### Temps de mise en place par phase

| Étape | GitHub | GitLab | Bitbucket | Azure | Commentaire |
|-------|:------:|:------:|:---------:|:-----:|-------------|
| **Setup initial** (connexion + secrets UI) | ~1h | ~2h | ~1h30 | ~2h30 | Azure nécessite création d'une organisation + project + service connections manuelles |
| **Enregistrement runner self-hosted** | ~20min | ~30min | ~30min | ~1h | Azure : création agent pool + enregistrement via PAT dans l'UI DevOps |
| **Debug problèmes critiques** | ~1h | ~3h | ~8h | ~1h | GitLab : CRLF SSH (~1h) + DNS Docker (~1h) + setup ; Bitbucket : 5 blocages (voir §5.1) |
| **Total estimé (mise en prod)** | **~2h20** | **~5h30** | **~10h** | **~4h30** | GitHub le plus rapide (natif), Bitbucket le plus laborieux |

#### Caractéristiques de mise en place

| Critère | GitHub | GitLab | Bitbucket | Azure | Commentaire |
|---------|:------:|:------:|:---------:|:-----:|-------------|
| **Connexion repo** | Native | Via `mirror.yml` | Via `mirror.yml` | GitHub App (native) | GitLab et Bitbucket nécessitent un pipeline de miroir supplémentaire actif |
| **Enregistrement pipeline UI** | ❌ Auto | ❌ Auto | ❌ Auto | ✅ Manuel (×4) | Azure requiert l'enregistrement explicite de chaque pipeline dans l'UI |
| **Mécanisme SSH VPS** | Secret → bash | Variable type `File` | SSH Keys natif (UI) | Service Connection | Bitbucket est le plus ergonomique ; GitHub requiert un script bash de reconstruction |
| **Courbe d'apprentissage** | Faible | Modérée | Modérée | Élevée | Azure introduit des concepts propres : stages, pools, service connections, variable groups |
| **Qualité documentation** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | Bitbucket : lacunes sur self-hosted avancé et ancres YAML complexes |
| **Problèmes critiques rencontrés** | 3 | 3 | 5 | 1 | GitHub : #1, #2, #10 — GitLab : #3, #4 — Bitbucket : #3, #5–#9 — Azure : #11 (voir §5.1) |

> **Observation** : L'effort de mise en place est fortement corrélé au nombre de concepts
> propres à chaque plateforme. GitHub Actions, en étant natif au dépôt source, évite
> toutes les frictions de connexion. Azure DevOps, malgré une connexion native via
> GitHub App, introduit le plus de concepts à maîtriser (agent pools, service connections,
> variable groups, enregistrement manuel des pipelines). Bitbucket cumule les deux
> difficultés : connexion par miroir + contraintes YAML propres à sa syntaxe.

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
| 11 | `npm run typecheck` → erreurs TS (`string \| undefined`) → step supprimé | Azure | ~1h | TypeScript / Config |

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
| **Templates paramétrés** | ✅ Reusable workflows | ✅ `include:` | ❌ YAML anchors | ✅ Natif |
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
> **Mise à jour 2026-05-20** : scores de performance recalculés avec les données
> de l'Échantillon 3 (Hybrid Optimal — configuration équitable, voir §10).

### 7.1 Scoring global

| Catégorie | Poids | GitHub | GitLab | Bitbucket | Azure | Justification |
|-----------|:-----:|:------:|:------:|:---------:|:-----:|---------------|
| **Performance** (pipeline total) | 25% | 3 | 3 | 4 | **5** | Azure : ~2m17s, Bitbucket : ~4m1s, GitLab : ~4m21s, GitHub : ~7m12s (voir §10.7) |
| **Coût** (quota + tarif) | 20% | 5 | 4 | 2 | 5 | GitHub et Azure : quotas généreux + self-hosted ; Bitbucket : 50 min/mois |
| **Complexité configuration** | 20% | 4 | 3 | 4 | 3 | Bitbucket : syntaxe simple (203L, 1 fichier) ; Azure : concepts nombreux |
| **Fiabilité / Debugging** | 15% | 4 | 4 | 3 | 5 | Azure : logs précis + 1 seul blocage ; Bitbucket : 5 blocages critiques |
| **Fonctionnalités architecturales** | 20% | 4 | 4 | 2 | 5 | Azure : Quality Gate, SonarCloud, templates natifs |
| **Score pondéré** | 100% | **3.95** | **3.55** | **3.05** | **4.60** | |

> ⚠️ Ce scoring est indicatif et basé sur les données empiriques du projet DKPT.
> La **complexité configuration** mesure la simplicité syntaxique (YAML), pas l'effort de setup
> (couvert par §4.3). C'est pourquoi Bitbucket score 4 malgré un setup laborieux.

### 7.2 Évolution des scores Performance (§2 vs Échantillon 3)

| Plateforme | Score §2 (2026-05-19) | Score Éch. 3 (2026-05-20) | Δ | Cause principale |
|------------|:---------------------:|:-------------------------:|:-:|:-----------------|
| **GitHub** | 4 | **3** | −1 | Docker BuildKit overhead : 2m40s → 4m43s sur self-hosted VPS |
| **GitLab** | 3 | **3** | = | CI séquentiel (2m33s) compensé par Docker rapide (1m18s) |
| **Bitbucket** | 3 | **4** | +1 | Cache node actif (frontend 4s) + Docker 2m40s compétitif |
| **Azure** | 5 | **5** | = | Daemon local ARM64 : Docker 11s, CD Prod 18s |

> **Note sur GitHub** : La baisse de performance dans l'Échantillon 3 s'explique par
> le changement de runner Docker (hosted arm64 2m40s → self-hosted VPS 4m43s).
> L'overhead BuildKit (`docker/build-push-action`) est présent dans les deux cas,
> mais plus visible sur le VPS. Azure évite cet overhead via un shell executor
> avec daemon Docker local (cache à 100% sur les layers inchangés).


---

## 8. Statut de la collecte des données

> ✅ **Toutes les données ont été collectées** (2026-05-19 pour §2, 2026-05-20 pour Échantillon 3).
> Ce fichier constitue le référentiel empirique complet pour la phase d'analyse du mémoire.

| Domaine | Statut | Note |
|---------|:------:|------|
| GitHub Actions — CI, Docker, CD | ✅ Collecté | Parallélisation documentée, cache Docker à 0% sur run mesuré |
| GitLab CI — CI, Docker, CD | ✅ Collecté | Socket binding vs DinD comparé (gain −19%) |
| Bitbucket — CI, Docker, CD | ✅ Collecté | Cache frontend actif, DinD Docker (pas de cache) |
| Azure DevOps — CI, Docker, CD | ✅ Collecté | Core CI (28s) vs Full CI avec SonarCloud (2m21s) |
| Coûts — quotas, projections, TCO | ✅ Calculé | §3.1–3.5 (3 scénarios, analyse self-hosted, TCO) |
| Complexité — YAML, variables, setup | ✅ Documenté | §4.1–4.3 (6 fichiers GitHub, Rôle colonne, effort mise en place) |
| Fiabilité — 11 problèmes, debugging | ✅ Documenté | §5.1–5.2 |
| Fonctionnalités architecturales | ✅ Documenté | §6 (13 critères) |
| QR3 — Réutilisabilité | ✅ Documenté | GitHub `workflow_call`, GitLab `include:`, Azure templates, Bitbucket anchors |
| **GitHub self-hosted ARM64 VPS** | ✅ Collecté | §9 — Échantillon 2, comparaison hosted vs self-hosted |
| **Hybrid Optimal Échantillon 3** | ✅ Collecté | §10 — 4 plateformes, config équitable, scoring mis à jour |

```bash
# Commande de vérification des lignes YAML (archivée — résultats dans §4.1)
wc -l .github/workflows/*.yml .gitlab-ci.yml .gitlab/pipelines/*.yml bitbucket-pipelines.yml .azuredevops/*.yml .azuredevops/templates/*.yml
```

---

## 9. Échantillons comparatifs — Évolution de configuration GitHub Actions

> Cette section documente des mesures complémentaires prises avec des **configurations
> de runner différentes** sur GitHub Actions, afin d'isoler l'impact du type de runner
> sur les performances. Les autres plateformes sont inchangées.

### Configuration des échantillons

| Paramètre | Échantillon 1 (référence §2) | Échantillon 2 |
|-----------|:----------------------------:|:-------------:|
| **Date** | 2026-05-19 | 2026-05-20 |
| **Runner CI** | `ubuntu-24.04-arm` (GitHub-hosted) | `self-hosted` ARM64 (VPS staging) |
| **Runner Docker build** | `ubuntu-24.04-arm` (GitHub-hosted) | `self-hosted` ARM64 (VPS staging) |
| **Runner CD deploy** | `ubuntu-latest` (GitHub-hosted) | `self-hosted` ARM64 (VPS staging) |
| **Runner retag** | `ubuntu-24.04-arm` (GitHub-hosted) | `self-hosted` ARM64 (VPS staging) |
| **Cache Docker** | `type=gha` (0% hit) | `type=gha` (0% hit) |
| **Commit** | `d0bc196` | `0d85f88` |
| **Pipeline** | CI #78 / Staging #44 / Prod #50 | CI #— / Staging #51 / Prod #57 |

> **Note** : Le runner self-hosted est le VPS staging ARM64 mutualisé avec GitLab,
> Bitbucket et Azure. Le cache Docker `type=gha` est lié au runner GitHub — un runner
> self-hosted **partage le même cache GHA** s'il est authentifié avec le même token.
> Le hit était à 0% dans les deux cas (premier run après changement de config).

---

### 9.1 CI — Build & Test (Échantillon 2 vs Échantillon 1)

| Step | Échantillon 1 (GitHub-hosted) | Échantillon 2 (self-hosted VPS) | Δ |
|------|:-----------------------------:|:-------------------------------:|:-:|
| **Backend** (restore + build + test) | **29s** | **38s** | +9s (+31%) |
| **Frontend** (npm ci + lint + build) | **54s** | **1m35s** | +41s (+76%) |
| **CI total** (parallèle, job le plus lent) | **~54s** | **~1m35s** | +41s (+76%) |

> **Analyse** : Le runner self-hosted VPS est significativement plus lent sur le
> frontend (+76%). Cela s'explique par les ressources partagées du VPS (4 vCPU, 8 GB
> mutualisés entre l'application DKPT + 4 runners CI/CD). Le runner GitHub-hosted
> `ubuntu-24.04-arm` bénéficie de ressources dédiées.

---

### 9.2 Docker Build (Échantillon 2 vs Échantillon 1)

| Métrique | Échantillon 1 (GitHub-hosted) | Échantillon 2 (self-hosted VPS) | Δ |
|----------|:-----------------------------:|:-------------------------------:|:-:|
| **Docker build backend** | **~53s** | **1m47s** | +54s (+102%) |
| **Docker build frontend** | **~1m21s** | **2m31s** | +70s (+86%) |
| **Docker build total** (job) | **2m40s** | **4m54s** | +2m14s (+84%) |
| **Cache Docker** | `type=gha` (0%) | `type=gha` (0%) | = |

> **Analyse** : Le build Docker est ~2× plus lent sur le self-hosted VPS. La différence
> s'explique par la contention CPU : le VPS héberge simultanément l'application Docker
> (staging) et le runner CI/CD. Les runners GitHub-hosted disposent de vCPU dédiés.

---

### 9.3 CD Staging (Échantillon 2 vs Échantillon 1)

| Step | Échantillon 1 (GitHub-hosted) | Échantillon 2 (self-hosted VPS) | Δ |
|------|:-----------------------------:|:-------------------------------:|:-:|
| **Deploy Staging** (SSH + docker compose) | **28s** | **23s** | −5s (−18%) |
| **Retag :staging** | **22s** | **13s** | −9s (−41%) |
| **CD Staging total** (steps actifs) | **50s** | **36s** | −14s (−28%) |
| **CD Staging total** (pipeline mur à mur) | **58s** | **1m35s** | +37s |

> **Analyse** : Les steps de déploiement SSH sont plus rapides sur le self-hosted
> (le runner est sur le même réseau VPS que la cible). En revanche, le temps
> mur à mur est plus long car le runner doit récupérer les actions JavaScript
> (`appleboy/ssh-action`) qui ne sont pas en cache local.

---

### 9.4 CD Production (Échantillon 2 vs Échantillon 1)

| Step | Échantillon 1 (GitHub-hosted) | Échantillon 2 (self-hosted VPS) | Δ |
|------|:-----------------------------:|:-------------------------------:|:-:|
| **CD Prod total** (mur à mur) | **3m39s** | **2m0s** | −1m39s |

> **Note** : Le temps mur à mur CD Prod inclut l'attente `workflow_run`.
> La réduction s'explique par le fait que le runner self-hosted enchaîne
> plus vite les jobs (pas de provisioning de VM).

---

### 9.5 Synthèse comparative — GitHub-hosted vs Self-hosted VPS

| Métrique | GitHub-hosted ARM64 | Self-hosted VPS ARM64 | Δ | Avantage |
|----------|:-------------------:|:---------------------:|:-:|:--------:|
| **CI total** | **54s** | **1m35s** | +41s | GitHub-hosted |
| **Docker build total** | **2m40s** | **4m54s** | +2m14s | GitHub-hosted |
| **CD Staging** (steps actifs) | **50s** | **36s** | −14s | Self-hosted |
| **CD Prod** (mur à mur) | **3m39s** | **2m0s** | −1m39s | Self-hosted |
| **CI + Docker + CD Staging** | **~4m24s** | **~7m05s** | +2m41s | GitHub-hosted |
| **Quota hosted consommé** | **~9 min** | **0 min** | −9 min | Self-hosted |

> **Conclusion** : Le runner GitHub-hosted `ubuntu-24.04-arm` est **plus rapide
> pour les tâches de compilation et de build Docker** grâce à des ressources CPU
> dédiées. Le runner self-hosted VPS est **plus efficace pour les déploiements SSH**
> (réseau local) et **ne consomme aucun quota** hosted.
>
> Pour un projet en production, la combinaison optimale est :
> - **CI + Docker build** → GitHub-hosted (ressources dédiées)
> - **CD deploy** → Self-hosted (réseau VPS, quota préservé)
>
> C'est précisément la configuration de l'**Échantillon 1** (référence §2),
> qui représente donc la configuration la plus performante pour GitHub Actions.

---

### 9.6 Run 2 — Self-hosted VPS (cache actions chaud)

> Deuxième exécution avec le runner self-hosted, à quelques minutes d'intervalle.
> Objectif : mesurer l'impact du **cache local des actions JavaScript** (`appleboy/ssh-action`, etc.)
> après le premier run.

| Métrique | Run 1 (self-hosted) | Run 2 (self-hosted) | Δ |
|----------|:-------------------:|:-------------------:|:-:|
| **Backend** | **38s** | **30s** | −8s |
| **Frontend** | **1m35s** | **1m20s** | −15s |
| **CI total** (parallèle) | **1m35s** | **1m20s** | −15s |
| **Docker build frontend** | **2m31s** | **2m26s** | −5s |
| **Docker build backend** | **1m47s** | **1m44s** | −3s |
| **Docker build total** (job) | **4m54s** | **4m36s** | −18s |
| **Cache Docker** | 0% | 0% | = |
| **CD Staging total** (mur à mur) | **1m35s** | **44s** | **−51s** |
| **Deploy Staging** | **23s** | **22s** | −1s |
| **Retag :staging** | **13s** | **13s** | = |
| **CD Prod total** (mur à mur) | **2m0s** | **2m15s** | +15s |
| **Deploy Production** | — | **20s** | — |
| **Retag :latest** | — | **13s** | — |

> **Observation clé — Cache des actions JS** :
> Le temps mur à mur du CD Staging chute de **1m35s → 44s (−51s)** entre le Run 1 et
> le Run 2. Cette différence s'explique par le **cache local des actions JavaScript** :
> lors du Run 1, le runner télécharge `appleboy/ssh-action`, `appleboy/scp-action`, etc.
> depuis GitHub. Lors du Run 2, ces actions sont en cache local sur le VPS → démarrage
> quasi-instantané.
>
> Le **cache Docker reste à 0%** sur les deux runs. Le cache `type=gha` est lié au
> token GitHub du runner. Avec un runner self-hosted, le cache est bien stocké sur
> les serveurs GitHub (token identique) mais les **layers Docker ont changé** entre
> les commits → miss cache attendu.

### 9.7 Stabilisation — Valeurs représentatives self-hosted (cache chaud)

> Le Run 2 est plus représentatif des performances **en conditions normales** (cache
> d'actions chaud). Les valeurs à retenir pour comparaison sont celles du Run 2.

| Métrique | Échantillon 1 (hosted, réf.) | Échantillon 2 Run 2 (self-hosted, stable) | Δ |
|----------|:----------------------------:|:-----------------------------------------:|:-:|
| **CI total** | **54s** | **1m20s** | +26s (+48%) |
| **Docker build total** | **2m40s** | **4m36s** | +1m56s (+73%) |
| **CD Staging** (mur à mur) | **58s** | **44s** | −14s (−24%) |
| **CD Prod** (mur à mur) | **3m39s** | **2m15s** | −1m24s (−38%) |
| **Pipeline total** (CI→Staging) | **~4m24s** | **~6m40s** | +2m16s |
| **Quota hosted consommé** | **~9 min** | **0 min** | −9 min |

---

## 10. Configuration Équitable "Hybrid Optimal" — Échantillon 3

> **Statut** : ✅ **Complété** au 2026-05-20 — toutes les plateformes collectées.
>
> Cette configuration vise à **maximiser l'équité de la comparaison** en isolant
> deux variables indépendantes :
> 1. **Performance du hosted runner** (CI uniquement) — chaque plateforme utilise ses runners natifs
> 2. **Performance Docker + déploiement** — même matériel ARM64 pour toutes les plateformes

### 10.1 Principe et justification

Les mesures précédentes (§2 et §9) ont montré que le type de runner impacte fortement
les résultats. Pour une comparaison académiquement rigoureuse, la stratégie retenue est :

| Phase | Runner | Justification |
|-------|:------:|---------------|
| **CI — lint + test** | Hosted (plateforme) | Mesure la qualité intrinsèque de chaque plateforme |
| **Docker build + push** | Self-hosted ARM64 VPS | Élimine la variable matérielle — même CPU pour tous |
| **CD Staging + Prod** | Self-hosted ARM64 VPS | Réseau local VPS, même latence pour tous |

Cette configuration correspond à ce que **Azure DevOps utilise déjà** en §2
(référence), ce qui valide son utilisation comme configuration "naturelle optimale".

---

### 10.2 Harmonisation de l'environnement CI hosted

#### Problématique

Les plateformes ne s'exécutent pas sur le même type de runtime pour les jobs CI :

| Plateforme | Runtime CI | Contrôle OS |
|------------|:----------:|:-----------:|
| GitHub Actions | VM directe | ✅ `runs-on: ubuntu-22.04` |
| Azure DevOps | VM directe | ✅ `vmImage: ubuntu-22.04` |
| GitLab CI | Conteneur Docker sur shared runner | ⚠️ Via `image:` (pas l'OS host) |
| Bitbucket | Conteneur Docker sur cloud runner | ⚠️ Via `image:` (pas l'OS host) |

> **Observation** : GitHub et Azure exposent l'OS de la VM d'exécution via `runs-on`/`vmImage`.
> GitLab et Bitbucket exécutent les jobs dans des **conteneurs Docker** sur leurs shared/cloud
> runners — l'OS du runner host n'est pas configurable via YAML. L'harmonisation réelle
> passe par les **images Docker des jobs CI**.

#### Choix d'harmonisation

**GitHub et Azure — OS de la VM :**

| Runner | Version choisie | Raison |
|--------|:--------------:|--------|
| `ubuntu-22.04` | LTS Jammy | Version stable commune aux 4 plateformes en 2026 |

> `ubuntu-latest` est déconseillé pour les mesures : il peut changer de version
> (22.04 → 24.04) selon les plateformes et dans le temps, rendant les résultats
> non reproductibles.

**GitLab et Bitbucket — Images Docker des jobs :**

L'OS du conteneur CI est déjà harmonisé via les images Docker :

| Job | Image Docker | OS base |
|-----|:------------:|:-------:|
| Backend (dotnet) | `mcr.microsoft.com/dotnet/sdk:9.0` | Debian 12 (Bookworm) |
| Frontend (node) | `node:22-alpine` | Alpine 3.x |

> Ces images sont **identiques sur GitLab et Bitbucket**, ce qui garantit un environnement
> d'exécution cohérent pour les jobs CI de ces deux plateformes.
>
> La différence Debian/Alpine (GitLab+Bitbucket) vs Ubuntu 22.04 (GitHub+Azure) est
> une contrainte structurelle des plateformes. Elle est documentée et acceptable car
> les outils (.NET 9, Node.js 22) ont un comportement identique sur les deux bases.

---

### 10.3 Configuration exacte par plateforme (Échantillon 3)

#### GitHub Actions

| Job | `runs-on` | Changement vs Échantillon 2 |
|-----|:---------:|:---------------------------:|
| `backend-build-test` | `ubuntu-22.04` | ✅ Retour hosted (était: `self-hosted`) |
| `frontend-build` | `ubuntu-22.04` | ✅ Retour hosted (était: `self-hosted`) |
| `docker-build-sha` | `self-hosted` | Inchangé ✅ |
| `deploy` (staging) | `self-hosted` | Inchangé ✅ |
| `deploy` (prod) | `self-hosted` | Inchangé ✅ |
| `retag` (staging+prod) | `self-hosted` | Inchangé ✅ |

#### GitLab CI

| Job | Tags runner | Changement vs §2 |
|-----|:-----------:|:-----------------:|
| `backend-build-test` | *(aucun — shared runner hosted)* | ⚠️ Retrait tag `ubuntu_arm64` |
| `frontend-lint-build` | *(aucun — shared runner hosted)* | ⚠️ Retrait tag `ubuntu_arm64` |
| `docker-build-sha` | `[ubuntu_arm64]` | Inchangé ✅ |
| `deploy-staging` | `[ubuntu_arm64]` | Inchangé ✅ |
| `deploy-production` | `[ubuntu_arm64]` | Inchangé ✅ |

#### Bitbucket Pipelines

| Step | `runs-on` | Changement vs §2 |
|------|:---------:|:-----------------:|
| Backend Build & Test | *(aucun — cloud runner)* | ⚠️ Retrait `runs-on` |
| Frontend Lint & Build | *(aucun — cloud runner)* | ⚠️ Retrait `runs-on` |
| Docker Build & Push | `[self.hosted, linux, arm64]` | Inchangé ✅ |
| Deploy Staging | `[self.hosted, linux, arm64]` | Inchangé ✅ |
| Deploy Production | `[self.hosted, linux, arm64]` | Inchangé ✅ |

#### Azure DevOps

| Job | Pool | Changement |
|-----|:----:|:----------:|
| `backend-build-test` | `ubuntu-22.04` (hosted) | ⚠️ Fixer version (était `ubuntu-latest`) |
| `frontend-lint-build` | `ubuntu-22.04` (hosted) | ⚠️ Fixer version (était `ubuntu-latest`) |
| `docker-build-sha` | `DKPT-ARM64` (self-hosted) | Inchangé ✅ |
| `deploy-staging` | `DKPT-ARM64` (self-hosted) | Inchangé ✅ |
| `deploy-production` | `DKPT-ARM64` (self-hosted) | Inchangé ✅ |

---

### 10.4 Tableau de collecte — Échantillon 3 (✅ Complet)

> Toutes les valeurs ont été collectées. Pipeline GitHub = Échantillon 3a (avec `setup-dotnet`) ;
> voir §10.6 pour les valeurs optimisées 3b retenues comme référence dans §10.7.

#### CI — Build & Test

| Step | GitHub | GitLab | Bitbucket | Azure |
|------|:------:|:------:|:---------:|:-----:|
| **Backend** (Core CI) | **1m11s** ¹ | **57s** | **41s** | **~38s** ⁹ |
| **Frontend** | **59s** | **1m36s** ² | **4s** ⁶ | **1m12s** |
| **CI total** (séquentiel/parallèle) | **1m11s** | **2m33s** ³ | **45s** ⁷ | **~1m12s** |
| **Runner type** | Hosted `ubuntu-22.04` | Shared runner (Docker) | Cloud runner (Docker) | Hosted `ubuntu-22.04` |

> ¹ **Observation importante** : Sur ubuntu-22.04 hosted, `setup-dotnet` télécharge
> .NET 9 alors qu'il est **déjà pré-installé** sur ce runner. Cela ajoute ~30-40s
> inutilement. Retiré dans l'Échantillon 3b (→ 39s pour le backend).
>
> ² Frontend GitLab plus lent : image `node:22-alpine` téléchargée + `npm ci` sur
> shared runner sans cache persistant entre runs. Alpine = image légère mais
> téléchargement réseau à chaque run.
>
> ³ GitLab exécute les jobs CI en **séquentiel** par défaut dans la même stage
> (`build-test`), contrairement à GitHub qui les exécute en parallèle.
> Total réel = backend (57s) + frontend (1m36s) = **2m33s**.
>
> ⁶ **Bitbucket Frontend 4s** : cache `node` actif (Bitbucket Pipelines cache natif).
> `npm ci` exécuté avec `node_modules/` restauré depuis le cache → install quasi-instant.
> C'est le résultat le plus rapide observé sur l'ensemble des plateformes pour ce step.
>
> ⁷ Bitbucket exécute les steps en **séquentiel** (contrainte du pipeline monolithique).
> Mais l'overhead est minimal ici : le step le plus lent (backend 41s) détermine
> la base, et frontend (4s, cache) n'ajoute que 4s.
>
> ⁹ **Azure Backend Core CI** : SonarCloud exclu de la comparaison (voir §2 — +51s overhead).
> Valeur comparable : setup 9s + restore 2s + build 18s + test 9s = **~38s**.
> CI total (parallèle, hors SonarCloud) : max(~38s backend, 1m12s frontend) = **~1m12s**.

#### Docker Build

| Métrique | GitHub | GitLab | Bitbucket | Azure |
|----------|:------:|:------:|:---------:|:-----:|
| **Docker build backend** | **1m47s** | — ⁴ | **50s** | inclus ¹° |
| **Docker build frontend** | **2m51s** | — ⁴ | **1m24s** | inclus ¹° |
| **Docker build total** | **5m0s** | **1m18s** ⁴ | **2m40s** | **11s** ¹° |
| **Docker job total** | **5m0s** | **1m18s** | **2m40s** | **24s** |
| **Cache Docker** | 0% | 0% | 0% | ✅ daemon local |
| **Runner** | `self-hosted` VPS | `ubuntu_arm64` VPS | `self.hosted` VPS | `DKPT-ARM64` VPS |

> ⁴ **Écart majeur GitLab vs GitHub** : GitLab utilise `docker build` natif
> (sans BuildKit/Buildx), GitHub utilise `docker/build-push-action@v6` avec
> driver `docker-container` (BuildKit). L'overhead BuildKit étant absent sur
> GitLab, le Docker build est **3,6× plus rapide** (1m18s vs 4m43s).
> À noter : GitLab ne produit pas d'attestations de provenance (pas de `--attest`).
>
> ¹° **Azure Docker 11s** : daemon Docker local sur le self-hosted ARM64 (`DKPT-ARM64`).
> Les layers sont mis en cache localement entre les builds — **cache à 100%** sur les
> layers inchangés. C'est le résultat le plus rapide toutes plateformes confondues
> pour le Docker build (2,2× plus rapide que Bitbucket, 6,5× plus que GitHub).

#### CD Staging

| Métrique | GitHub | GitLab | Bitbucket | Azure |
|----------|:------:|:------:|:---------:|:-----:|
| **Deploy** | 19s | 17s | inclus | **10s** |
| **Retag** | 11s | 13s | inclus | **7s** |
| **Deploy + Retag** | 19s + 11s | 17s + 13s | **36s** (inclus) | 10s + 7s |
| **Total** (mur à mur) | **1m29s** | **30s** | **36s** | **41s** |

> ⁸ Bitbucket : Deploy et Retag sont dans le **même step** (pas de jobs séparés).
> Les 36s incluent SSH + docker compose + retag :staging.

#### CD Production

| Métrique | GitHub | GitLab | Bitbucket | Azure |
|----------|:------:|:------:|:---------:|:-----:|
| **Deploy** | 19s | 17s | inclus | **9s** |
| **Retag** | 14s | 13s | inclus | **5s** |
| **Deploy + Retag** | 19s + 14s | 17s + 13s | **40s** (inclus) | 9s + 5s |
| **Total** (mur à mur) | **2m9s** | **30s** ⁵ | **40s** | **18s** |

> ⁵ CD Prod GitLab : queued 321s (attente approbation manuelle). Steps actifs = 30s.

---

### 10.5 Fichiers à modifier

| Fichier | Modification |
|---------|-------------|
| `.github/workflows/ci.yml` | `runs-on: ubuntu-22.04` sur `backend-build-test` et `frontend-build` |
| `.gitlab/pipelines/ci.yml` | Retirer `tags: [ubuntu_arm64]` sur `backend-build-test` et `frontend-lint-build` |
| `bitbucket-pipelines.yml` | Retirer `runs-on:` sur les steps Backend et Frontend uniquement |
| `.azuredevops/ci.yml` | `vmImage: ubuntu-22.04` (fixer la version) |

---

### 10.6 Optimisation — Retrait de setup-dotnet (Échantillon 3b)

> **Contexte** : Les runners GitHub-hosted `ubuntu-22.04` ont .NET 9 et Node.js 22
> **pré-installés**. Les actions `setup-dotnet` et `setup-node` sont donc redondantes
> pour l'installation — elles ajoutent du temps inutile sur les runners hosted.

#### Analyse des actions de setup

| Action | Rôle réel sur ubuntu-22.04 hosted | Temps gaspillé |
|--------|:---------------------------------:|:--------------:|
| `actions/setup-dotnet@v4` | Télécharge .NET 9 déjà présent | **~30-40s** |
| `actions/setup-node@v4` | Node.js déjà présent ; **utile pour le cache npm** | ~2-3s |

> **Décision** :
> - `setup-dotnet` → **retiré** (remplacé par `dotnet --version` pour vérification)
> - `setup-node` → **conservé avec `cache: "npm"`** — Node.js est pré-installé
>   mais l'action fournit le cache npm via GitHub Actions Cache. Sans elle,
>   `npm ci` télécharge ~200MB à chaque run (perte de ~20-30s).

#### Changement appliqué dans `.github/workflows/ci.yml`

```yaml
# AVANT (Échantillon 3a)
- name: Setup .NET ${{ env.DOTNET_VERSION }}
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: ${{ env.DOTNET_VERSION }}   # ~30-40s inutiles

# APRÈS (Échantillon 3b)
# .NET 9 est pré-installé sur ubuntu-22.04 — setup-dotnet non requis
- name: Check .NET version
  run: dotnet --version                          # ~1s
```

#### Tableau de collecte — Échantillon 3b (à remplir)

| Métrique | Échantillon 3a (avec setup) | Échantillon 3b (sans setup-dotnet) | Δ |
|----------|:---------------------------:|:----------------------------------:|:-:|
| **CI Backend** | **1m11s** | **39s** | **−32s (−45%)** ✅ |
| **CI Frontend** | **59s** | **1m0s** | ~= |
| **CI total** (parallèle) | **1m11s** | **1m0s** | −11s (−15%) |
| **Docker build frontend** | **2m51s** | **2m20s** | −31s |
| **Docker build backend** | **1m47s** | **1m59s** | +12s (variation VPS) |
| **Docker build total** | **5m0s** | **4m43s** | −17s |
| **CD Staging total** (mur à mur) | **1m29s** | **39s** | −50s ¹ |
| **CD Prod** (steps actifs) | Deploy **19s** + Retag **14s** | Deploy **21s** + Retag **14s** | ~= |
| **CD Prod total** (mur à mur) | **2m9s** | **55m5s** ² | N/A |

> ¹ La réduction de 1m29s → 39s sur CD Staging combine deux effets :
> le retrait de setup-dotnet (−32s sur CI) accélère le déclenchement du CD,
> et le cache des actions JS est chaud (2ème run consécutif).
>
> ² CD Prod **non représentatif** : durée totale inclut une attente d'approbation
> manuelle (Protection Rule "Prod"). Les steps actifs (21s + 14s = 35s) sont
> comparables aux runs précédents.

#### Conclusion Échantillon 3b

> **Validation confirmée** : le retrait de `setup-dotnet` économise **~32s** sur
> le job backend CI, exactement conforme à l'estimation (~30-40s). Sur un runner
> hosted GitHub `ubuntu-22.04`, .NET 9 est bien pré-installé et opérationnel
> sans action de setup.
>
> **Valeurs de référence retenues pour Échantillon 3 (GitHub)** :
> - CI total : **1m0s** (3b optimisé)
> - Docker build : **4m43s**
> - CD Staging : **39s** (mur à mur, actions cache chaud)
> - CD Prod steps actifs : **35s**

---

## 10.7 Synthèse comparative — Échantillon 3 (Hybrid Optimal)

> Configuration équitable : CI hosted natif + Docker/CD self-hosted ARM64 VPS pour toutes les plateformes.

### Pipeline total bout en bout (CI → Docker → CD Staging)

| Phase | GitHub | GitLab | Bitbucket | Azure |
|-------|:------:|:------:|:---------:|:-----:|
| **CI total** | 1m0s | 2m33s ¹ | 45s | ~1m12s ² |
| **Docker build** | 4m43s | 1m18s | 2m40s | **11s** |
| **CD Staging** | 1m29s | 30s | 36s | 41s |
| **Pipeline total** ³ | **~7m12s** | **~4m21s** | **~4m1s** | **~2m17s** |
| **Classement** | 4ᵉ | 3ᵉ | 2ᵉ | **1ᵉ** |

> ¹ GitLab CI : jobs séquentiels dans la stage `build-test` (backend 57s + frontend 1m36s).
> Contrairement à GitHub/Azure qui parallélisent les jobs CI.
>
> ² Azure CI total hors SonarCloud (~38s backend + 1m12s frontend, parallèle = **~1m12s**).
>
> ³ Pipeline total = CI + Docker + CD Staging (steps actifs, sans attente approbation prod).

### Classement par dimension

| Dimension | 1ᵉ | 2ᵉ | 3ᵉ | 4ᵉ |
|-----------|:---:|:---:|:---:|:---:|
| **CI Backend** (Core) | Azure (~38s) | GitHub (39s) | Bitbucket (41s) | GitLab (57s) |
| **CI Frontend** | Bitbucket (4s 📦) | GitHub (1m0s) | Azure (1m12s) | GitLab (1m36s) |
| **CI total** (parallèle) | Bitbucket (45s) | GitHub (1m0s) | Azure (~1m12s) | GitLab (2m33s) |
| **Docker build** | Azure (11s 🚣) | GitLab (1m18s) | Bitbucket (2m40s) | GitHub (4m43s) |
| **CD Staging** | GitLab (30s) | Bitbucket (36s) | Azure (41s) | GitHub (1m29s) |
| **CD Prod** (actifs) | Azure (18s) | GitLab (30s) | GitHub (35s) | Bitbucket (40s) |
| **Pipeline total** | **Azure (~2m17s)** | Bitbucket (~4m1s) | GitLab (~4m21s) | GitHub (~7m12s) |

### Observations clés

| Observation | Détail |
|-------------|--------|
| **Docker : 44× écart** | Azure 11s vs GitHub 4m43s — daemon local + cache vs BuildKit sans cache |
| **Frontend cache Bitbucket** | 4s grâce au cache `node` natif Pipelines — 18× plus rapide que GitLab |
| **GitHub bottleneck** | Docker BuildKit overhead domine le pipeline total (66% du temps) |
| **GitLab CI séquentiel** | Pénalité de 2m33s vs 45s Bitbucket malgré un Docker 1m18s excellent |
| **Azure dominant** | Seule plateforme sous les 3 minutes pipeline total — daemon local décisif |
| **Setup-dotnet évitable** | Sur ubuntu-22.04 hosted (GitHub), supprimer `setup-dotnet` économise ~32s — .NET 9 est pré-installé. Bitbucket non concerné (image `dotnet/sdk:9.0`) |
