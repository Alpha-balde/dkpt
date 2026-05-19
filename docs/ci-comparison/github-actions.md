# GitHub Actions — Analyse détaillée

> Plateforme CI/CD native de GitHub, intégrée au dépôt source.

## Configuration dans DKPT

### Fichiers principaux

| Fichier | Rôle | Déclencheur |
|---------|------|-------------|
| `ci.yml` | Build & Test (2 jobs parallèles) + Docker push | Push/PR sur `main` |
| `cd-staging.yml` | Deploy staging (pull sha) + retag :staging | Après CI sur `main` |
| `cd-prod.yml` | Deploy production + retag :latest | Après CI sur `main` |
| `pr-check.yml` | Vérification PR (2 jobs parallèles) | Pull Request |
| `mirror.yml` | Synchronisation vers les autres plateformes | Push sur `main` |

### Variantes (expérimentations pour le mémoire)

| Fichier | Stratégie | Comparaison avec `ci.yml` |
|---------|-----------|---------------------------|
| `variante-1-parallel.yml` | Jobs parallèles | Backend ET frontend en même temps (vs séquentiel) |
| `variante-2-single-job.yml` | Job unique | Tout dans 1 seul job (single runner) |
| `variante-4-matrix.yml` | Matrix strategy | Test sur multiples OS × versions runtime |

---

## Points forts

- **1 fichier = 1 pipeline** : Organisation la plus claire et intuitive
- **Marketplace riche** : 20 000+ actions réutilisables
- **Intégration native** : Pas besoin de miroir, le CI est dans le même outil que le code
- **Cache GHA** : Cache Docker layers natif (`cache-from: type=gha`)
- **2 000 minutes/mois gratuites** : Le plus généreux du marché
- **20 jobs parallèles** : Excellente parallélisation sur le free tier
- **`workflow_dispatch`** : Déclenchement manuel facile pour les variantes

## Points faibles

- **Pas de dossiers imbriqués** : `.github/workflows/` uniquement (pas de sous-dossiers reconnus)
- **Lock-in GitHub** : Syntaxe spécifique, pas portable vers d'autres plateformes
- **Debugging limité** : Pas de terminal interactif sur les runners (sauf `act` en local)
- **Pas de pipeline DAG visuel** : La visualisation est linéaire par workflow

## Mécanisme de réutilisabilité — Reusable Workflows (QR3)

GitHub Actions permet de définir des **workflows réutilisables** via le
déclencheur `workflow_call`. Un workflow entier peut être appelé depuis
un autre workflow, avec des `inputs` et `secrets` typés.

### Implémentation dans DKPT

La logique de **retag Docker** (pull :sha → tag → push) était dupliquée
dans `cd-staging.yml` et `cd-prod.yml`. Elle est centralisée dans
`reusable-retag.yml` :

```yaml
# .github/workflows/reusable-retag.yml
on:
  workflow_call:
    inputs:
      tag: { required: true, type: string }   # "staging" ou "latest"
      sha: { required: true, type: string }   # short SHA (8 chars)
    secrets:
      DOCKERHUB_USERNAME: { required: true }
      DOCKERHUB_TOKEN:    { required: true }
```

**Appel depuis cd-staging.yml :**
```yaml
retag:
  needs: deploy
  uses: ./.github/workflows/reusable-retag.yml
  with:
    tag: staging
    sha: ${{ needs.deploy.outputs.short_sha }}
  secrets:
    DOCKERHUB_USERNAME: ${{ secrets.DOCKERHUB_USERNAME }}
    DOCKERHUB_TOKEN:    ${{ secrets.DOCKERHUB_TOKEN }}
```

**Appel depuis cd-prod.yml :** identique avec `tag: latest`.

### Comparaison avec les autres mécanismes de réutilisabilité

| Plateforme | Mécanisme | Granularité | Inputs typés | Secrets forwarding |
|-----------|-----------|:-----------:|:------------:|:-----------------:|
| **GitHub Actions** | `workflow_call` | Workflow entier | ✅ (`string`, `boolean`, `number`) | ✅ `secrets:` |
| **GitLab CI** | `include:` + `!reference` | Job / template | ✅ via `variables:` | ✅ CI/CD variables |
| **Azure DevOps** | `template:` | Steps / jobs / stages | ✅ `parameters:` | ✅ variable groups |
| **Bitbucket** | YAML anchors (`&` / `*`) | Step individuel | ❌ Pas de paramètres | ❌ |

> **Observation pour le mémoire (QR3)** : Le mécanisme `workflow_call` de GitHub
> est le plus puissant en termes de modularité au niveau workflow, mais il
> est aussi le plus verbeux — le passage de secrets doit être explicite à chaque
> appel (pas d'héritage automatique). GitLab et Azure offrent plus de flexibilité
> via leurs systèmes de variables globales.

---

## Spécificités techniques

### Chaînage de workflows (`workflow_run`)

```yaml
on:
  workflow_run:
    workflows: ["DKPT CI"]
    types: [completed]
    branches: [main]
```

GitHub Actions utilise `workflow_run` pour chaîner les pipelines. C'est un déclencheur **asynchrone** : le CD démarre après la fin du CI, pas pendant.

### Environments avec protection

```yaml
deploy:
  environment: production  # Gate d'approbation configurée dans l'UI GitHub
```

### Reusable workflows vs Composite actions

GitHub propose deux mécanismes de réutilisation :
- **Reusable workflows** : Un workflow entier réutilisable (appelé via `uses:`)
- **Composite actions** : Un ensemble de steps réutilisable

Le projet DKPT n'utilise pas encore les reusable workflows (potentielle variante-3).

---

## Optimisation : Parallélisation des jobs CI

### Contexte

La configuration initiale de `ci.yml` exécutait backend et frontend **séquentiellement**
dans un seul job `build-and-test`. Cette approche simple est courante mais sous-exploite
le potentiel de parallélisation de GitHub Actions.

### Avant — 1 job séquentiel

```
build-and-test (1 runner)
  ├── Setup .NET
  ├── Restore / Build / Test backend   → ~20s
  ├── Setup Node.js
  ├── npm ci / lint --fix / lint / build → ~60s
  └── (total : ~1m24s)
```

### Après — 2 jobs parallèles

```
backend-build-test (runner A)     frontend-build (runner B)
  ├── Setup .NET                    ├── Setup Node.js
  ├── Restore dependencies          ├── npm ci
  ├── Build backend                 ├── Lint
  └── Run tests        → 29s       └── Build frontend   → 54s
                  ↘               ↙
              docker-build-sha
               (attend les 2)
```

### Résultats mesurés

| Métrique | Séquentiel (avant) | Parallèle (après) | Gain |
|---------|:---------:|:---------:|:----:|
| **Backend** | ~20s | **29s** | *(overhead runner)* |
| **Frontend** | ~60s | **54s** | |
| **CI total effectif** | **~1m24s** | **~54s** | **−30s (~36%)** |

> Le backend parallèle affiche 29s (vs 20s séquentiel) car chaque job démarre
> un nouveau runner (checkout + setup .NET ~10s d'overhead). La performance
> globale est néanmoins meilleure car les deux s'exécutent simultanément.

### Lint simplifié

La double passe lint (`--fix` puis strict) a été supprimée — alignement avec
GitLab CI, Bitbucket et Azure DevOps qui n'exécutent qu'une seule passe.

> Commit d'optimisation : `e056145`  
> Message : `perf(ci): parallelize backend + frontend jobs in GitHub CI [ci:github]`

---

## Temps d'exécution observés

| Pipeline | Durée | Architecture | Notes |
|----------|:-----:|:------------:|-------|
| **CI backend** | **29s** | job A (runner dédié) | restore + build + test |
| **CI frontend** | **54s** | job B (runner dédié) | install + lint + build |
| **CI total effectif** | **~54s** | parallèle | job le plus lent |
| **Docker Build & Push** | **~2m40s** | runner ARM64 | backend 53s + frontend 1m21s |
| **CD Staging** | **~50s** | 2 jobs | deploy 28s + retag 22s |
| **CD Production** | **~48s** | 2 jobs | deploy 33s + retag 15s |
| **Pipeline complet** | **~4m24s** | steps actifs | CI + Docker + Staging |

---

## Comparaison avec les autres plateformes

→ Voir [README.md](README.md) pour le tableau synthèse
