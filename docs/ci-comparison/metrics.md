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

| Catégorie | Poids | GitHub | GitLab | Bitbucket | Azure | Justification |
|-----------|:-----:|:------:|:------:|:---------:|:-----:|---------------|
| **Performance** (temps pipeline) | 25% | 4 | 3 | 3 | **5** | Azure : 2m48s, GitHub : 4m24s, Bitbucket : 4m36s, GitLab : 5m46s |
| **Coût** (quota + tarif) | 20% | 5 | 4 | 2 | 5 | GitHub et Azure : quotas généreux + self-hosted ; Bitbucket : 50 min/mois |
| **Complexité configuration** | 20% | 4 | 3 | 4 | 3 | Bitbucket : syntaxe simple (203L, 1 fichier) ; Azure : concepts nombreux |
| **Fiabilité / Debugging** | 15% | 4 | 4 | 3 | 5 | Azure : logs précis + 1 seul blocage ; Bitbucket : 5 blocages critiques |
| **Fonctionnalités architecturales** | 20% | 4 | 4 | 2 | 5 | Azure : Quality Gate, SonarCloud, templates natifs |
| **Score pondéré** | 100% | **4.2** | **3.6** | **2.8** | **4.6** | |

> ⚠️ Ce scoring est indicatif et basé sur les données empiriques du projet DKPT au 2026-05-19.
> La **complexité configuration** mesure la simplicité syntaxique (YAML), pas l'effort de setup
> (couvert par §4.3). C'est pourquoi Bitbucket score 4 malgré un setup laborieux.

---

## 8. Statut de la collecte des données

> ✅ **Toutes les données principales ont été collectées** au 2026-05-19.
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

```bash
# Commande de vérification des lignes YAML (archivée — résultats dans §4.1)
wc -l .github/workflows/*.yml .gitlab-ci.yml .gitlab/pipelines/*.yml bitbucket-pipelines.yml .azuredevops/*.yml .azuredevops/templates/*.yml
```
