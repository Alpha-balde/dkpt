# Azure DevOps — Analyse détaillée

> Plateforme CI/CD de Microsoft, avec pipelines YAML et gestion visuelle dans l'UI.
> La plateforme la plus complète du comparatif DKPT.

## Configuration dans DKPT

### Structure des fichiers

```
.azuredevops/
├── ci.yml                  ← Pipeline CI (Microsoft-hosted) — Build, Test, SonarCloud, Docker
├── cd-staging.yml          ← Pipeline CD Staging — Deploy + Quality Gate
├── cd-prod.yml             ← Pipeline CD Production — Deploy (déclenché après cd-staging)
├── pr-check.yml            ← Pipeline PR Check (Microsoft-hosted)
└── templates/
    ├── build-dotnet.yml    ← Template : Build + Test + Coverage .NET 9
    ├── build-nuxt.yml      ← Template : Lint + TypeCheck + Build Nuxt
    ├── build-docker.yml    ← Template : Build & Push Docker (tag sha)
    ├── deploy-vps.yml      ← Template : Generate .env + Deploy + Re-tag
    └── quality-gate.yml    ← Template : k6 + OWASP ZAP + Playwright
```

> **Important** : Chaque fichier `.yml` doit être **enregistré manuellement** dans l'UI
> Azure DevOps comme un pipeline distinct (`Pipelines → New pipeline → Existing YAML`).
> C'est la seule plateforme où la configuration est côté UI et non uniquement dans le code.

### Connexion au dépôt source

Azure DevOps utilise une **connexion native via GitHub App** — pas de mirroring SSH.
Le dépôt GitHub est connecté directement lors de la création du pipeline dans l'UI.
Les autres plateformes (GitLab, Bitbucket) reçoivent le code via le workflow `mirror.yml`.

### Pipelines

| Pipeline | Rôle | Déclencheur | Agent |
|----------|------|-------------|-------|
| `ci.yml` | Build & Test + SonarCloud + Docker (sha tag) | Push sur `main` | Microsoft-hosted + Self-hosted (Docker) |
| `cd-staging.yml` | Deploy staging + Quality Gate | Pipeline resource (après CI sur `main`) | Self-hosted ARM64 (Docker) + Env Staging |
| `cd-prod.yml` | Deploy production | Pipeline resource (après cd-staging sur `main`) | Env Prod |
| `pr-check.yml` | Vérification PR (build + test, sans deploy) | PR vers `main` | Microsoft-hosted (`ubuntu-latest`) |

### Architecture des agents

```
Pool Default (Self-hosted)            Environnements Azure DevOps
┌─────────────────────────┐          ┌──────────────────────────────────────┐
│ Agent DKPT-ARM64        │          │ Staging (VPS Staging, ARM64)         │
│ (VPS Staging, ARM64)    │          │   → deploy local, pas de SSH         │
│ → Docker build & push   │          │   → k6 + ZAP + Playwright en local   │
└─────────────────────────┘          │                                      │
                                     │ Prod (VPS Production, ARM64)         │
                                     │   → deploy local, pas de SSH         │
                                     └──────────────────────────────────────┘
```

> **Différence fondamentale avec GitHub Actions** : Le déploiement ne se fait pas via SSH
> depuis un runner cloud. L'agent d'environnement tourne **directement sur le VPS** et
> exécute les commandes Docker localement.

---

## Architecture "Build Once, Deploy Many"

Azure est la seule plateforme du projet à implémenter ce pattern :

```
CI Pipeline
  └─ docker build → tag :sha-{8chars} → push Docker Hub

CD Staging
  ├─ pull :sha-{8chars} → docker compose up
  ├─ Quality Gate (k6 + ZAP + Playwright)
  └─ re-tag :staging → push Docker Hub

CD Production
  ├─ pull :sha-{8chars} → docker compose up
  └─ re-tag :latest → push Docker Hub
```

> GitHub Actions, GitLab CI et Bitbucket **rebuildent** l'image à chaque étape de CD.
> Azure évite ce gaspillage en buildant une seule fois et en propageant le même artefact.

---

## Quality Gate — Post-déploiement staging

Unique dans le comparatif : un stage de validation automatisée **après** chaque déploiement staging.

### Outils intégrés

| Outil | Type | Cible | Bloquant |
|-------|------|-------|:--------:|
| **k6** | Load testing (smoke test) | `http://localhost` | ✅ Oui |
| **OWASP ZAP Baseline** | DAST — Scan passif | `http://localhost` | ❌ (`continueOnError`) |
| **OWASP ZAP API** | DAST — Scan actif OpenAPI | `/swagger/v1/swagger.json` | ❌ (`continueOnError`) |
| **Playwright** | E2E fonctionnel (6 tests) | `http://localhost` | ❌ (`continueOnError`) |

### Profil k6 (smoke test)

- Montée en charge : 10s → 5 VUs → 20s maintien 10 VUs → 10s descente
- Seuils : `p(95) < 2000ms` et `taux d'erreur < 5%`
- 4 scénarios : page SSR Nuxt, Swagger, login JWT, endpoints authentifiés (`/Members`, `/Cotisations`)

### Tests Playwright (6 tests)

| Fichier | Test | Vérification |
|---------|------|--------------|
| `auth.spec.ts` | Page login accessible | Champs email + password visibles |
| `auth.spec.ts` | Login invalide échoue | Reste sur /login après erreur |
| `auth.spec.ts` | Redirect non-authentifié | /dashboard → /login |
| `health.spec.ts` | Page accueil accessible | Status 200 |
| `health.spec.ts` | Swagger accessible | Retourne JSON OpenAPI |
| `health.spec.ts` | API protégée | Status 401 sans token |

> Tous les artefacts (k6 JSON, ZAP HTML/JSON, Playwright HTML + JUnit) sont publiés
> dans les onglets natifs Azure DevOps (Tests, Artifacts).

---

## Analyse statique & SonarCloud (CI)

### Backend (.NET 9)

- `dotnet build` avec `/p:TreatWarningsAsErrors=true`
- `dotnet test` avec `--logger trx` + `--collect:"XPlat Code Coverage"` (coverlet)
- **9/9 tests passés** — résultats dans l'onglet **Tests** d'Azure DevOps
- Code coverage publié dans l'onglet **Code Coverage**

### Frontend (Nuxt 4 — Node.js 22)

- **ESLint** (`npm run lint`) — `continueOnError: true` (informatif)
- **TypeCheck** (`npm run typecheck`) — `continueOnError: true` (issues préexistantes)
- **Build Nuxt** (`npm run build`) — bloquant

> Node.js **22** (et non 20) est requis pour éviter le bug `Object.groupBy is not a function`
> introduit par `eslint-flat-config-utils` (voir [quality-gates-integration.md](quality-gates-integration.md), Problème n°1).

### SonarCloud

- Pipeline : `SonarCloudPrepare@3` → build backend → `SonarCloudAnalyze@3` → `SonarCloudPublish@3`
- Résultats visibles sur [sonarcloud.io](https://sonarcloud.io)
- Publish : `continueOnError: true` — erreur 403 permissions en cours de résolution

---

## Points forts

- **1 fichier = 1 pipeline** : Comme GitHub Actions, organisation claire
- **Templates YAML paramétrés** : 5 templates réutilisables avec passage de paramètres (`envTag`)
- **Build Once, Deploy Many** : Unique dans le comparatif — image buildée une seule fois
- **Pipeline resources** : Chaînage élégant CI → CD Staging → Quality Gate → CD Prod
- **Environments avancés** : Approvals, gates, checks, policies + agents locaux sans SSH
- **Quality Gate intégré** : k6 + OWASP ZAP + Playwright après chaque déploiement staging
- **SonarCloud natif** : Analyse statique + couverture de code dans le CI
- **1 800 minutes/mois gratuites** : Deuxième plus généreux après GitHub (2 000)
- **10 jobs parallèles** (free tier)
- **Service connections** : Gestion centralisée des connexions (Docker Hub, SonarCloud)
- **Variable groups** : Secrets scopés par environnement (`dkpt-secrets`, `dkpt-staging`, `dkpt-prod`)
- **GitHub App** : Connexion directe au repo GitHub sans mirroring SSH

## Points faibles

- **Enregistrement UI obligatoire** : Chaque pipeline doit être créé manuellement dans l'UI
- **Courbe d'apprentissage** : Concepts spécifiques (service connections, pipeline resources, environments, variable groups)
- **Syntaxe différente** : `task:` au lieu de `uses:`, `displayName:` au lieu de `name:`
- **$40/mois** au-delà du gratuit : Le plus cher du comparatif
- **Extensions Marketplace** : Installation manuelle par admin requise avant usage (ex: SonarCloud)

---

## Spécificités techniques

### Templates réutilisables paramétrés

```yaml
# deploy-vps.yml — template avec paramètre
parameters:
  - name: envTag
    type: string
    default: 'latest'

steps:
  - script: |
      docker tag $(dockerBackendImage):sha-${SHORT_SHA} $(dockerBackendImage):${{ parameters.envTag }}
      docker push $(dockerBackendImage):${{ parameters.envTag }}
    displayName: 'Re-tag images as :${{ parameters.envTag }}'

# Appelé avec des valeurs différentes selon l'environnement :
- template: templates/deploy-vps.yml
  parameters:
    envTag: 'staging'    # Dans cd-staging.yml
    # ou
    envTag: 'latest'     # Dans cd-prod.yml
```

Supérieur aux YAML anchors de Bitbucket (pas de paramètres) et à l'`include:` de GitLab (paramètres limités).

### Pipeline Resources (chaînage entre pipelines)

```yaml
# cd-staging.yml — déclenché après le succès de CI sur main
resources:
  pipelines:
    - pipeline: ci
      source: 'DKPT CI'
      trigger:
        branches:
          include: [main]

# cd-prod.yml — déclenché après le succès de cd-staging sur main
resources:
  pipelines:
    - pipeline: staging
      source: 'cd-staging'
      trigger:
        branches:
          include: [main]
```

Équivalent de `workflow_run` dans GitHub Actions, mais plus lisible.

### Service Connections

Azure DevOps centralise les connexions externes (Docker Hub, SonarCloud) dans des **service connections** :

```yaml
- task: Docker@2
  inputs:
    containerRegistry: 'dockerhub-connection'   # Référence à la service connection
    command: 'login'
```

Configurées dans `Project Settings → Service connections`. Révocables et gérées
indépendamment du code YAML.

### Variable Groups (secrets scopés par environnement)

```yaml
variables:
  - group: dkpt-secrets   # Commun : DOCKERHUB_*, JWT_*, SONAR_*, CORS_ORIGINS
  - group: dkpt-staging   # Staging : VPS_STAGING_*, POSTGRES_PASSWORD, TEST_USER_*
  # ou
  - group: dkpt-prod      # Prod : VPS_HOST, VPS_USER, VPS_SSH_KEY, SITE_ADDRESS
```

3 variable groups permettent de partager les secrets communs tout en isolant les valeurs
spécifiques à chaque environnement. Plus granulaire que les repository secrets de GitHub Actions.

### Deployment jobs (agents locaux sur VPS)

```yaml
- deployment: DeployVPS
  environment:
    name: 'Staging'
    resourceType: VirtualMachine   # L'agent tourne directement sur le VPS
  strategy:
    runOnce:
      deploy:
        steps:
          - checkout: self
          - template: templates/deploy-vps.yml
            parameters:
              envTag: 'staging'
```

Le déploiement s'exécute **localement sur le VPS** — pas de SSH depuis un runner cloud.
Azure DevOps propose aussi les stratégies `rolling` et `canary` (non utilisées ici).

---

## Journal d'implémentation

> **Branche** : `feature/ci-quality-gates` → merge dans `main`
> **Dates** : 2026-04-30 → 2026-05-13

### Phases de développement

| Phase | Description | Statut |
|:-----:|-------------|:------:|
| 1 | Analyse statique : ESLint, TypeCheck, .NET analyzers, couverture de code | ✅ |
| 2 | SonarCloud : intégration dans le CI, service connection, permissions | ✅ (⚠️ 403) |
| 3 | k6 : smoke test post-déploiement staging | ✅ |
| 4 | OWASP ZAP : scans DAST baseline + API | ✅ |
| 5 | Playwright : tests E2E fonctionnels (6 tests) | ✅ |

### Problèmes rencontrés

| # | Phase | Problème | Type | Solution |
|:-:|:-----:|----------|------|----------|
| 1 | 1 | `Object.groupBy` non supporté (Node 20) | Compatibilité runtime | Upgrade Node 20 → 22 |
| 2 | 1 | 377 erreurs ESLint de formatage | Dette technique | Auto-fix + règles assouplies |
| 3 | 1 | TypeCheck échoue sur code préexistant | Dette technique | `continueOnError: true` |
| 4 | 2 | Extension SonarCloud non installée | Spécificité Azure DevOps | Installation Marketplace (admin) |
| 5 | 2 | Service connection mauvais type (Generic) | Configuration UI | Recréer en type SonarCloud |
| 6 | 2 | Organisation SonarCloud en majuscules | Case sensitivity | Minuscules dans la variable |
| 7 | 2 | Confusion tasks SonarCloud vs SonarQube Server | Naming | Revenir aux tasks `SonarCloudXxx@3` |
| 8 | 2 | 403 sur API Quality Gate (permissions) | Permissions | `continueOnError: true` (temporaire) |
| 9 | 2 | Shallow clone incompatible SonarCloud | Config par défaut | `fetchDepth: 0` |
| 10 | 2 | PR merge ref introuvable (GitHub ↔ Azure) | Synchronisation | Lancer sur branche directement |

> Voir [quality-gates-integration.md](quality-gates-integration.md) pour le journal détaillé de chaque problème.

---

## Comparaison avec GitHub Actions

| Aspect | GitHub Actions | Azure DevOps |
|--------|:-:|:-:|
| Multi-pipelines | ✅ Auto-détecté | ✅ Enregistrement UI |
| Templates | Reusable workflows (complet) | **Templates paramétrés (fragments)** |
| Chaînage | `workflow_run` | Pipeline resources |
| Connexions externes | Secrets + env | **Service connections** |
| Secrets | Repository secrets | **3 variable groups scopés** |
| Docker build ARM64 | Runner cloud ARM64 | **Self-hosted agent sur VPS** |
| Déploiement | SSH depuis runner cloud | **Agent local sur VPS (pas de SSH)** |
| Build Once Deploy Many | ❌ (rebuild à chaque CD) | **✅** |
| Quality Gate post-deploy | ❌ | **✅ (k6 + ZAP + Playwright)** |
| Analyse statique | ❌ | **✅ (SonarCloud + dotnet analyzers)** |
| Connexion repo | Native | **GitHub App (native, sans mirror)** |
| Prix | $0.008/min | $40/mois |

---

## Comparaison avec les autres plateformes

→ Voir [README.md](README.md) pour le tableau synthèse
