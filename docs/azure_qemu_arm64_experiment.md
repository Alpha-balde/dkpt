# Expérience exploratoire — Build Docker ARM64 via QEMU sur Azure DevOps

> **Statut** : 📋 En attente d'exécution
> **Branche** : `experiment-azure-qemu-arm64`
> **Pipeline** : `.azuredevops/experiment-qemu-arm64.yml`
> **Date** : 2026-07-10

---

## 1. Objectif

Tester si un runner Microsoft-hosted Ubuntu x86_64 (`vmImage: ubuntu-22.04`) peut
construire les images Docker `linux/arm64` du projet DKPT via émulation QEMU/Buildx,
et mesurer la durée de ce build.

Ce test exploratoire sert à **documenter la contrainte technique ARM64** qui justifie
le choix méthodologique du runner self-hosted ARM64 dans le protocole comparatif
du mémoire. Il ne fait pas partie du protocole de mesure principal.

---

## 2. Configuration du test

| Paramètre | Valeur |
|-----------|--------|
| **Runner** | Microsoft-hosted `ubuntu-22.04` (x86_64) |
| **Émulation** | QEMU via `multiarch/qemu-user-static` |
| **Builder** | Docker Buildx (`docker-container` driver) |
| **Plateforme cible** | `linux/arm64` |
| **Images testées** | Backend (.NET 9) + Frontend (Nuxt 4) |
| **Mode** | Build-only (aucun push vers Docker Hub) |
| **Timeout** | 60 minutes par job |
| **Tag** | `qemu-arm64-test` (local, non publié) |

### Dockerfiles utilisés

- **Backend** : `backend/Dockerfile` — Multi-stage .NET 9 (SDK → ASP.NET runtime)
- **Frontend** : `frontend/Dockerfile` — Multi-stage Node 20 Alpine (build → runtime)

### Commandes exécutées

```bash
# Setup QEMU
docker run --rm --privileged multiarch/qemu-user-static --reset -p yes

# Setup Buildx
docker buildx create --name qemu-builder --driver docker-container --use
docker buildx inspect --bootstrap

# Build backend ARM64
docker buildx build \
  --platform linux/arm64 \
  --tag dkpt-backend-qemu-test:qemu-arm64-test \
  --file ./backend/Dockerfile \
  --load \
  ./backend

# Build frontend ARM64
docker buildx build \
  --platform linux/arm64 \
  --tag dkpt-frontend-qemu-test:qemu-arm64-test \
  --file ./frontend/Dockerfile \
  --load \
  ./frontend
```

---

## 3. Résultat attendu (hypothèse)

Le build ARM64 via émulation QEMU sur x86_64 devrait être **significativement plus lent**
qu'un build natif ARM64 (facteur ×10 à ×20 estimé). La compilation .NET sous émulation
est particulièrement coûteuse. Le build pourrait atteindre le timeout de 60 minutes
sans terminer.

---

## 4. Résultats — Run #1

> ⚠️ À remplir après exécution du pipeline

### Informations du run

| Paramètre | Valeur |
|-----------|--------|
| **Date d'exécution** | |
| **Pipeline ID** | |
| **Commit** | |
| **Runner CPU** | |
| **Runner RAM** | |

### Métriques

| Image | Durée | Exit code | Résultat |
|-------|:-----:|:---------:|:--------:|
| **Backend** (.NET 9) | — | — | ⏳ |
| **Frontend** (Nuxt 4) | — | — | ⏳ |

### Comparaison avec le build natif ARM64 (self-hosted VPS)

| Image | QEMU x86_64 | Natif ARM64 (VPS) | Facteur |
|-------|:-----------:|:-----------------:|:-------:|
| **Backend** | — | ~30-40s ¹ | — |
| **Frontend** | — | ~25-35s ¹ | — |

> ¹ Valeurs de référence approximatives du protocole comparatif (Échantillon A).

### Logs pertinents

```
(à copier depuis la sortie du pipeline)
```

### Observations

-

---

## 5. Conclusion

> ⚠️ À rédiger après exécution

**Formulation attendue** (à adapter selon les résultats) :

> Cet essai exploratoire sur Azure DevOps hosted runner x86_64 indique que la
> construction Docker `linux/arm64` via QEMU est [lente / instable / impossible
> dans le timeout observé] pour le projet DKPT. Ce résultat justifie le choix
> méthodologique de fixer les phases Docker/CD sur un runner ARM64 self-hosted
> dans le protocole comparatif, sans prétendre quantifier à lui seul le
> comportement des autres plateformes.

---

## 6. Nettoyage

Après documentation des résultats :

```bash
# Revenir sur main
git checkout main

# Supprimer la branche locale
git branch -d experiment-azure-qemu-arm64

# Supprimer la branche remote (si poussée)
git push origin --delete experiment-azure-qemu-arm64

# Supprimer le pipeline dans l'UI Azure DevOps
```

---

## 7. Limites de cette expérience

1. **Plateforme unique** : seul Azure DevOps est testé. GitLab et Bitbucket pourraient
   avoir des comportements différents (DinD, shared runners avec d'autres specs).
2. **Un seul run** : pas de validation statistique (c'est exploratoire).
3. **Build-only** : le push réseau n'est pas mesuré.
4. **Hardware variable** : les hosted runners Microsoft n'ont pas de specs garanties
   (la performance peut varier entre les runs).
5. **Pas de cache** : premier build, aucun cache Buildx.
