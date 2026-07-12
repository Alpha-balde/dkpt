# Mesure du volume effectif des configurations CI/CD (projet DKPT)

## 1. Métadonnées

| Paramètre | Valeur |
|-----------|--------|
| **Date de mesure** | 2026-07-12 |
| **Commit mesuré** | `c4f24ecdca6d2f481f6fc3fceee93c64d2ad8698` (branche `main`, config Échantillon A) |
| **Outil** | PowerShell 5.1+ (`Get-Content` + boucle de classification ligne par ligne) |

### Commande de comptage (reproductible)

```powershell
$lines = Get-Content $fichier
$total = $lines.Count
$vides = 0; $commentaires = 0; $effectives = 0
foreach ($line in $lines) {
  $trimmed = $line.Trim()
  if ($trimmed -eq "") { $vides++ }
  elseif ($trimmed.StartsWith("#")) { $commentaires++ }
  else { $effectives++ }
}
```

### Définitions appliquées

- **Ligne vide** : ne contient que des espaces/tabulations (ou rien) → `$trimmed -eq ""`
- **Ligne de commentaire** : premier caractère non blanc est `#` → `$trimmed.StartsWith("#")`
- **Ligne effective** : toute autre ligne. Une ligne portant un commentaire en fin (`key: value  # note`) compte comme **effective**.

---

## 2. Tableau par fichier

| Plateforme | Fichier | Total | Vides | Commentaires | Effectives | Invariant |
|:-----------|:--------|------:|------:|-------------:|-----------:|:---------:|
| GitHub | `.github/workflows/ci.yml` | 159 | 24 | 39 | 96 | ✅ |
| GitHub | `.github/workflows/cd-staging.yml` | 119 | 11 | 23 | 85 | ✅ |
| GitHub | `.github/workflows/cd-prod.yml` | 120 | 11 | 24 | 85 | ✅ |
| GitHub | `.github/workflows/reusable-retag.yml` | 62 | 6 | 17 | 39 | ✅ |
| GitLab | `.gitlab-ci.yml` | 71 | 5 | 31 | 35 | ✅ |
| GitLab | `.gitlab/pipelines/ci.yml` | 84 | 5 | 28 | 51 | ✅ |
| GitLab | `.gitlab/pipelines/cd-staging.yml` | 77 | 4 | 23 | 50 | ✅ |
| GitLab | `.gitlab/pipelines/cd-prod.yml` | 80 | 4 | 24 | 52 | ✅ |
| Azure | `.azuredevops/ci.yml` | 73 | 6 | 25 | 42 | ✅ |
| Azure | `.azuredevops/cd-staging.yml` | 56 | 5 | 18 | 33 | ✅ |
| Azure | `.azuredevops/cd-prod.yml` | 54 | 5 | 16 | 33 | ✅ |
| Azure | `.azuredevops/templates/build-dotnet.yml` | 61 | 8 | 15 | 38 | ✅ |
| Azure | `.azuredevops/templates/build-nuxt.yml` | 29 | 5 | 9 | 15 | ✅ |
| Azure | `.azuredevops/templates/build-docker.yml` | 33 | 6 | 9 | 18 | ✅ |
| Azure | `.azuredevops/templates/deploy-vps.yml` | 81 | 7 | 15 | 59 | ✅ |
| Bitbucket | `bitbucket-pipelines.yml` | 200 | 12 | 71 | 117 | ✅ |

**16 fichiers mesurés, 16/16 invariants vérifiés** (Total = Vides + Commentaires + Effectives).

---

## 3. Tableau de synthèse par plateforme

| Plateforme | Fichiers | Total (`wc -l`) | Effectives | Commentaires | Vides | Part commentaires (%) |
|:-----------|:--------:|----------------:|-----------:|-------------:|------:|----------------------:|
| GitHub | 4 | 460 | 305 | 103 | 52 | 22,4 % |
| GitLab | 4 | 312 | 188 | 106 | 18 | 34,0 % |
| Azure | 7 | 387 | 238 | 107 | 42 | 27,6 % |
| Bitbucket | 1 | 200 | 117 | 71 | 12 | 35,5 % |
| **Total** | **16** | **1 359** | **848** | **387** | **124** | **28,5 %** |

---

## 4. Ratios utiles (calculés sur les lignes effectives)

| Indicateur | Valeur |
|:-----------|:-------|
| Plateforme la plus compacte | Bitbucket — 117 lignes effectives |
| Plateforme la plus étendue | GitHub — 305 lignes effectives |
| **Facteur d'amplitude max/min** | **305 / 117 = 2,61** |
| Rapport compacte / étendue | 117 / 305 = 38,4 % |
| Lignes effectives par fichier (moyenne) — GitHub | 305 / 4 = 76,2 |
| Lignes effectives par fichier (moyenne) — GitLab | 188 / 4 = 47,0 |
| Lignes effectives par fichier (moyenne) — Azure | 238 / 7 = 34,0 |
| Lignes effectives par fichier (moyenne) — Bitbucket | 117 / 1 = 117,0 |

### Classement par volume effectif

| Rang | Plateforme | Effectives | Fichiers | Moy/fichier |
|:----:|:-----------|:----------:|:--------:|:-----------:|
| 1 | GitHub | 305 | 4 | 76,2 |
| 2 | Azure | 238 | 7 | 34,0 |
| 3 | GitLab | 188 | 4 | 47,0 |
| 4 | Bitbucket | 117 | 1 | 117,0 |

### Comparaison lignes brutes vs lignes effectives

| Plateforme | Brutes | Effectives | Réduction | Facteur brut (vs min) | Facteur effectif (vs min) |
|:-----------|-------:|-----------:|----------:|----------------------:|--------------------------:|
| GitHub | 460 | 305 | −33,7 % | 2,30 | 2,61 |
| GitLab | 312 | 188 | −39,7 % | 1,56 | 1,61 |
| Azure | 387 | 238 | −38,5 % | 1,94 | 2,03 |
| Bitbucket | 200 | 117 | −41,5 % | 1,00 | 1,00 |

> Le passage aux lignes effectives **amplifie légèrement** les écarts entre plateformes
> (facteur max/min : 2,30 en brut → 2,61 en effectif), principalement parce que
> Bitbucket a la plus forte densité de commentaires (35,5 %).

---

## 5. Contrôles

### 5.1 Contrôle des totaux `wc -l`

| Plateforme | Attendu (mémoire) | Mesuré (c4f24ec) | Écart |
|:-----------|:------------------:|:----------------:|:-----:|
| GitHub | 408 | **460** | **+52** |
| GitLab | 294 | **312** | **+18** |
| Azure | 345 | **387** | **+42** |
| Bitbucket | 188 | **200** | **+12** |

> **⚠️ ÉCART DÉTECTÉ** : Les totaux `wc -l` du mémoire (188/294/345/408) ont été
> mesurés par Claude sur un état antérieur des fichiers, probablement le commit
> `bf6a266` (fix bitbucket default-image, avant la campagne de mesures).
>
> Les modifications apportées pendant la campagne de collecte des métriques v2
> (commits `9fc88ec` à `c4f24ec`) ont ajouté des lignes aux configurations :
> - `apk add --no-cache git` dans `bitbucket-pipelines.yml` (fix frontend CI)
> - `continueOnError: true` dans Azure `ci.yml` (fix ARM64)
> - Commentaires de traçabilité (`# Échantillon A/B`, `# Self-hosted ARM64 VPS`)
> - Bascule `runs-on`, `tags`, `vmImage` entre config A et B
>
> **La mesure du présent rapport porte sur le commit c4f24ec (état actuel, config A).**
> Si le mémoire doit rester cohérent avec les valeurs 188/294/345/408, il faut
> mesurer sur le commit `bf6a266`. Si le mémoire doit refléter l'état final des
> configurations (après les corrections de bugs découverts pendant la campagne),
> alors les valeurs 200/312/387/460 sont les correctes.

### 5.2 Contrôle de l'invariant

L'invariant `Total = Vides + Commentaires + Effectives` est vérifié pour les
**16 fichiers** sans exception (cf. colonne « Invariant » du tableau §2).

---

## 6. Notes

### 6.1 Séparateurs YAML `---` et `...`

Aucun fichier ne contient de ligne constituée uniquement du séparateur YAML `---`
ou du marqueur de fin de document `...`. Le compteur de séparateurs est à **zéro**
pour les 16 fichiers.

### 6.2 Densité de commentaires

Les configurations Bitbucket et GitLab présentent la plus forte densité de
commentaires (35,5 % et 34,0 % respectivement). GitHub a la plus faible (22,4 %).
Azure se situe à 27,6 %. Ces proportions expliquent pourquoi le passage aux
lignes effectives modifie les rapports entre plateformes par rapport aux lignes
brutes.

### 6.3 Commentaires en fin de ligne

Les commentaires en fin de ligne (`key: value  # note`) sont comptés comme des
lignes **effectives**, conformément à la définition. Leur nombre exact n'a pas
été isolé dans ce comptage, mais ils sont présents dans les 4 plateformes
(principalement des annotations de type runner, configuration, ou traçabilité).
