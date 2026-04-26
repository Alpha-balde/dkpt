# GitHub Actions — Analyse détaillée

> Plateforme CI/CD native de GitHub, intégrée au dépôt source.

## Configuration dans DKPT

### Fichiers principaux

| Fichier | Rôle | Déclencheur |
|---------|------|-------------|
| `ci.yml` | Build & Test (backend + frontend) | Push/PR sur `main` |
| `cd-staging.yml` | Build Docker + Deploy staging | Après CI sur `develop` |
| `cd-prod.yml` | Build Docker + Deploy production | Après CI sur `main` |
| `pr-check.yml` | Vérification PR (2 jobs parallèles) | Pull Request |
| `mirror.yml` | Synchronisation vers les autres plateformes | Push sur `main`/`develop` |

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

## Temps d'exécution observés

| Pipeline | Durée moyenne | Notes |
|----------|:---:|-------|
| CI (build & test) | ~1m16s | Backend + Frontend séquentiel |
| CD Docker | ~2m42s | Build multi-stage + push |
| CD Deploy | ~9s | SSH simple |
| **Total CI+CD** | **~4m07s** | Push to deploy |

---

## Comparaison avec les autres plateformes

→ Voir [README.md](README.md) pour le tableau synthèse
