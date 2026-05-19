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

## Cache Docker — Analyse comparative

### Pourquoi le cache Docker est important

Un build Docker sans cache repart de zéro à chaque run : téléchargement des layers de base, installation des dépendances, compilation. Avec le cache, seules les couches modifiées sont reconstruites. Sur un projet .NET + Nuxt, la différence peut être de **3-5 minutes par build**.

### La distinction fondamentale : DinD vs Shell executor

Le bénéfice du cache local dépend de **comment le runner exécute Docker**, pas de s'il est self-hosted.

| Mode | Daemon Docker | Cache entre runs |
|------|:-------------:|:----------------:|
| **DinD** (Docker-in-Docker) | Nouveau daemon à chaque job | ❌ Détruit en fin de job |
| **Shell executor** (host direct) | Daemon du VPS host, persistant | ✅ Automatique sur disque |

### Comparaison par plateforme

| Plateforme | Executor | Cache Docker | Mécanisme | Config requise |
|------------|:--------:|:------------:|-----------|:--------------:|
| **GitHub Actions** | Shared (GitHub) | ✅ | `type=gha` (cache GitHub Actions, ~10 Go) | `cache-from/to` dans `build-push-action` |
| **Azure DevOps** | Shell (self-hosted sur VPS) | ✅ Auto | Layers locaux sur disque VPS — daemon host persistant | ❌ Aucune |
| **GitLab CI** | Docker + **socket binding** | ✅ | Host daemon via `/var/run/docker.sock` — persistant | `config.toml` volumes |
| **Bitbucket** | Docker + **DinD** | ❌ | DinD crée un daemon éphémère par job | Socket non propagé aux step containers |

> **Point contre-intuitif** : GitLab CI et Bitbucket utilisent des runners **self-hosted sur le même VPS**,
> mais obtiennent des résultats différents. GitLab peut bénéficier du cache local via le socket binding
> (configurable dans `config.toml`). Bitbucket en revanche est contraint par son runtime `linux-docker` :
> le runner a le socket monté, mais **ne le propage pas automatiquement** aux step containers.

### Socket binding sur GitLab CI

GitLab CI a été migré de DinD vers Docker socket binding, permettant d'utiliser le daemon Docker
du host directement depuis les job containers :

```toml
# /etc/gitlab-runner/config.toml
[[runners]]
  [runners.docker]
    volumes = ["/var/run/docker.sock:/var/run/docker.sock", "/cache"]
```

```yaml
# .gitlab/pipelines/ci.yml
docker-build-sha:
  image: docker:27
  variables:
    DOCKER_HOST: "unix:///var/run/docker.sock"
    DOCKER_TLS_CERTDIR: ""
  # Plus de services: docker:27-dind
```

### Tentative sur Bitbucket et limitation

La même approche a été tentée sur Bitbucket (suppression de `services: docker`, ajout de `DOCKER_HOST`).
Elle a échoué car le runner Bitbucket (`RUNTIME=linux-docker`) ne transmet pas le socket
mounté dans le runner container vers les step containers.

**Option disponible mais non retenue** : relancer le runner avec `EXTRA_DOCKER_ARGS` :

```bash
docker run -d \
  -e EXTRA_DOCKER_ARGS='-v /var/run/docker.sock:/var/run/docker.sock' \
  # ... autres variables du runner
  docker-public.packages.atlassian.com/sox/atlassian/bitbucket-pipelines-runner
```

Cette option aurait propagé le socket aux step containers. Elle n'a pas été retenue car :
- Elle nécessite de reconfigurer le runner (arrêt + relance avec le nouvel argument)
- La configuration Bitbucket standard ne l'expose pas nativement
- Les métriques de performance mesurées en DinD restent valides et représentatives

**Choix final** : Bitbucket conserve DinD. Les temps de build mesurés reflètent le comportement
réel de la plateforme dans sa **configuration standard self-hosted**.

### Solution pour GitLab CI et Bitbucket : Registry cache

Si on voulait activer le cache pour Bitbucket (sans modifier le runner), il faudrait utiliser le **registry cache BuildKit** :

```bash
# Activer BuildKit
export DOCKER_BUILDKIT=1

# Build avec cache depuis/vers Docker Hub
docker build \
  --cache-from type=registry,ref=$IMAGE:cache \
  --cache-to   type=registry,ref=$IMAGE:cache,mode=max \
  -t $IMAGE:sha-$SHORT_SHA ./backend
```

Le cache est stocké sur **Docker Hub** sous un tag dédié (ex: `:cache`). C'est un contournement fonctionnel mais qui consomme de l'espace sur le registry.

### Alternative : Shell executor sur GitLab

GitLab CI permet de configurer un runner en **Shell executor** au lieu de Docker executor. Cela permettrait d'utiliser le daemon Docker du host directement (comme Azure DevOps) et bénéficier du cache local. Inconvénient : perte d'isolation entre les jobs. Le socket binding est un meilleur compromis.

### Conclusion pour le mémoire

> Azure DevOps est structurellement avantagé sur le cache Docker grâce à son modèle Shell executor.
> GitHub compense via son cache natif `type=gha`. GitLab CI a pu être migré vers le socket binding
> (cache local via `config.toml`). Bitbucket reste sur DinD car son runtime `linux-docker` ne propage
> pas le socket aux step containers sans reconfiguration manuelle avancée du runner.

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
- [GitLab CI](gitlab-ci.md) — Pattern parent-child, socket binding Docker
- [Azure DevOps](azure-devops.md) — **Plateforme la plus complète** : templates paramétrés, Build Once Deploy Many, Quality Gate (k6 + ZAP + Playwright), SonarCloud, GitHub App
- [Bitbucket](bitbucket.md) — Fichier unique, YAML anchors, limitations (DinD, socket binding, EXTRA_DOCKER_ARGS)
- [**Métriques comparatives**](metrics.md) — Cadre de mesure, valeurs collectées, scoring des 4 plateformes
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
