# Analyse de re-collecte — Incident Frontend CI Bitbucket

> **Statut** : Résolu — Re-collecte complétée  
> **Date de détection** : 2026-07-12  
> **Sévérité** : Critique (données invalidées sur 2 échantillons)

---

## 1. Résumé de l'incident

Le step **« Frontend — Lint & Build »** du pipeline Bitbucket Pipelines n'a **jamais exécuté** les commandes `npm ci`, `npm run lint` et `npm run build` lors de la collecte initiale. Le step se terminait avec un code de sortie 0 (succès) en 3-6 secondes, sans aucun signal d'erreur.

**Conséquence** : toutes les métriques M2 (CI Frontend), M3 (CI Total) et M9 (Pipeline Total) de Bitbucket étaient **sous-estimées et invalides** — dans les deux échantillons (A et B).

---

## 2. Chronologie

| Date | Événement | Pipelines |
|------|-----------|-----------|
| 2026-07-11 | Collecte Échantillon A (hosted) | #80-83 → M2 = 3-4s |
| 2026-07-12 | Collecte Échantillon B (self-hosted) | #98-101 → M2 = 6s |
| 2026-07-12 | Analyse croisée Claude/Codex : M2 Bitbucket flaggé comme anomalie | — |
| 2026-07-12 | Diagnostic dans les logs du pipeline #99 → `git: not found` | — |
| 2026-07-12 | Fix : `apk add --no-cache git` ajouté au step frontend | commit `9fc88ec` |
| 2026-07-12 | Re-collecte Éch. B (self-hosted) | #104-107 → M2 = **1m28-1m29s** |
| 2026-07-12 | Re-collecte Éch. A (hosted) | #108-111 → M2 = **1m16-1m17s** |

---

## 3. Cause racine

### Le mécanisme de la clause de garde

Chaque step CI du pipeline Bitbucket contient une **clause de garde** pour éviter d'exécuter les builds sur des commits non pertinents :

```bash
git log -1 --pretty=%B | grep -q "ci:bitbucket" || { echo "⏩ [ci:bitbucket] absent — step ignoré"; exit 0; }
```

**Logique attendue** :
1. `git log -1` → récupère le message du dernier commit
2. `grep -q "ci:bitbucket"` → vérifie la présence du tag
3. Si absent → affiche un message et sort avec `exit 0` (succès)
4. Si présent → continue le build

### Ce qui s'est réellement passé

```
+ git log -1 --pretty=%B | grep -q "ci:bitbucket" || { echo "⏩ [ci:bitbucket] absent — step ignoré"; exit 0; }
/tmp/.../shellScript.sh: line 4: git: not found
⏩ [ci:bitbucket] absent — step ignoré
```

**L'image `node:22-alpine` n'inclut pas `git`.** La commande `git log` échoue avec `git: not found`. Ce n'est pas une erreur fatale dans un pipe (`|`) — le côté gauche du pipe échoue silencieusement, `grep -q` ne reçoit aucune entrée et retourne un code d'erreur, ce qui active la clause `||`. Le script affiche le message de skip et sort avec `exit 0`.

### Pourquoi ça fonctionne sur le Backend

Le step Backend utilise l'image `mcr.microsoft.com/dotnet/sdk:9.0`, qui est basée sur **Debian** et inclut `git` par défaut. La même clause de garde fonctionne correctement.

### Pourquoi ça n'a pas été détecté plus tôt

1. **Le message affiché est trompeur** : « `[ci:bitbucket] absent` » suggère que le commit ne contient pas le tag — alors que le vrai problème est que `git` n'est pas installé
2. **Le pipeline est vert** : `exit 0` = succès, aucune alerte
3. **La durée courte (3-6s) ne déclenche pas d'alerte** : Bitbucket n'a pas de seuil de durée minimale
4. **Pas de comparaison immédiate** : ce n'est qu'en comparant avec les 3 autres plateformes (GitHub ~57s, GitLab ~1m34s, Azure ~1m12s) que l'anomalie devient flagrante

---

## 4. Analyse sous l'angle maintenabilité

### 4.1 Fragilité des dépendances implicites

La clause de garde repose sur une **hypothèse implicite** : `git` est disponible dans l'environnement d'exécution. Cette hypothèse est vraie pour la plupart des images Docker CI (Ubuntu, Debian, images CI officielles), mais **fausse pour les images Alpine minimalistes**.

| Image | Base | `git` inclus |
|-------|------|:---:|
| `mcr.microsoft.com/dotnet/sdk:9.0` | Debian 12 | ✅ |
| `node:22` | Debian 12 | ✅ |
| `node:22-alpine` | Alpine Linux | ❌ |
| `ubuntu:22.04` | Ubuntu | ✅ |

**Leçon** : les images Alpine sont optimisées pour la taille (< 50 MB vs > 300 MB pour Debian), mais au prix d'un outillage minimal. Le choix d'Alpine pour le frontend (léger, rapide à pull) introduit une incompatibilité silencieuse avec le script CI.

### 4.2 Danger du pattern « fail-open »

La clause de garde utilise un pattern **fail-open** :

```bash
commande || { echo "message"; exit 0; }
```

Tout échec de la commande (y compris un échec inattendu comme `git: not found`) est traité comme un cas valide de skip. C'est l'équivalent CI d'un `catch (Exception e) { return; }` — un anti-pattern qui masque les vraies erreurs.

**Alternative fail-closed** (recommandée) :

```bash
# Vérifier que git est disponible
which git || { echo "❌ git non installé"; exit 1; }

# Guard clause explicite
if git log -1 --pretty=%B | grep -q "ci:bitbucket"; then
  echo "✅ [ci:bitbucket] trouvé — build en cours"
else
  echo "⏩ [ci:bitbucket] absent — step ignoré"
  exit 0
fi
```

### 4.3 Divergence inter-images

Un même script CI produit des comportements différents selon l'image Docker du step :

```
Backend (dotnet/sdk:9.0) : git ✅ → clause OK → build .NET s'exécute
Frontend (node:22-alpine) : git ❌ → clause KO → build Nuxt skippé silencieusement
```

Cette **divergence** est un problème de maintenabilité classique : le pipeline semble fonctionner (tous les steps sont verts), mais un des steps ne fait pas son travail. Dans un contexte de production, cela signifierait que le linting et le build de validation frontend ne sont jamais exécutés — des régressions frontend passeraient en production sans détection.

### 4.4 Détection par comparaison cross-plateforme

L'anomalie a été identifiée grâce à la **comparaison inter-plateformes** dans le protocole de benchmark :

| Plateforme | M2 Frontend (Éch. A) | Signal |
|:---:|:---:|:---:|
| GitHub | 57s | — |
| GitLab | 1m34s | — |
| Azure | 1m12s | — |
| **Bitbucket** | **3.7s** | **⚠️ ×15 plus rapide** |

Sans cette comparaison, l'anomalie serait passée inaperçue indéfiniment. C'est un argument fort en faveur de la standardisation et de la comparaison régulière des métriques CI entre plateformes.

### 4.5 Coût de la non-détection

| Impact | Description |
|--------|-------------|
| **Données invalidées** | M2, M3, M9 Bitbucket — 2 échantillons × 4 runs |
| **Re-collecte** | 8 pipelines supplémentaires (#104-111) |
| **Temps perdu** | ~2h de collecte + analyse |
| **Fausse conclusion** | « Bitbucket Frontend est 15× plus rapide » → conclusion invalide qui aurait biaisé le mémoire |

---

## 5. Impact sur les données

### Avant / Après correction

| Métrique | Avant (invalide) | Après Éch. A (hosted) | Après Éch. B (self-hosted) |
|----------|:---:|:---:|:---:|
| M2 — Frontend CI | **3.7±0.6s** | **1m16s±1s** | **1m29s±1s** |
| M3 — CI Total | **29.3±0.6s** | **1m44s±4s** | **2m02s±1s** |
| M9 — Pipeline Total | **4m29s±3s** | **5m41s±4s** | **6m01s±2s** |

### Métriques non impactées

M1 (Backend CI), M6 (Docker), M7 (CD Staging), M8 (CD Prod) restent valides — ces steps utilisent des images avec `git` installé ou ne dépendent pas de la clause de garde.

---

## 6. Recommandations

### R1 — Valider les dépendances en début de step

```yaml
script:
  - apk add --no-cache git        # Installer les dépendances manquantes
  - which git || exit 1            # Vérifier la disponibilité
  - 'git log -1 ...'               # Clause de garde
```

### R2 — Préférer fail-closed pour les guard clauses

Séparer la vérification de la disponibilité de l'outil de la logique métier du skip :

```bash
# Étape 1 : s'assurer que les outils sont disponibles (fail-closed)
which git || { echo "❌ git requis mais non installé"; exit 1; }

# Étape 2 : clause de garde métier (fail-open intentionnel)
git log -1 --pretty=%B | grep -q "ci:bitbucket" || { echo "⏩ skip"; exit 0; }
```

### R3 — Surveiller les durées anormales

Établir des seuils de durée minimale pour les steps CI. Un step Frontend qui prend < 10s devrait déclencher une alerte — un `npm ci + lint + build` Nuxt ne peut pas s'exécuter en moins de 30 secondes, même avec un cache optimal.

### R4 — Standardiser les images de base

Utiliser des images avec un outillage de base cohérent, ou créer une image CI personnalisée :

```dockerfile
FROM node:22-alpine
RUN apk add --no-cache git curl
```

---

## 7. Pertinence pour le mémoire

Cet incident est un **cas d'étude concret** pour la question de recherche sur la maintenabilité (QR4) :

1. **Complexité cachée** : un pipeline « simple » de 4 steps cache des dépendances implicites entre images Docker et scripts shell
2. **Faux positif systématique** : le pipeline est vert alors qu'un step ne fait rien — pendant des semaines potentiellement
3. **Valeur de la comparaison multi-plateformes** : seule la mise en regard des 4 plateformes permet de détecter l'anomalie
4. **Coût de la dette technique CI** : un raccourci (image Alpine légère + guard clause simpliste) génère une invalidation de données et une re-collecte complète

Ce type de problème est caractéristique de la **dette technique invisible des pipelines CI/CD** — rarement testée, rarement auditée, mais avec un impact potentiel sur la qualité du produit livré.
