# Expérience exploratoire — Build Docker ARM64 via QEMU sur Azure DevOps

> **Statut** : ✅ Complété — Backend ❌ (crash QEMU) / Frontend ✅ (9m19s, ×16)
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
| Paramètre | Valeur |
|-----------|--------|
| **Date d'exécution** | 2026-07-10 ~19:20–19:33 UTC |
| **Pipeline ID** | experiment-qemu-arm64 (branche `experiment-azure-qemu-arm64`) |
| **Commit** | `803a927` |
| **Runner** | Microsoft-hosted Ubuntu 22.04 x86_64 |

### Métriques

| Image | Durée | Exit code réel | Résultat |
|-------|:-----:|:--------------:|:--------:|
| **Backend** (.NET 9) | **23s** | 1 (échec) | ❌ Crash — `dotnet restore` échoue sous QEMU |
| **Frontend** (Nuxt 4) | **9m19s** (559s) | 0 | ✅ Succès |

> **Note** : Le script rapporte `Exit code: 0` pour le backend à cause d'un bug de pipeline
> (`docker buildx build 2>&1 | tee` capture l'exit code de `tee`, pas de `docker`).
> Le build a bien échoué — l'erreur Buildx est explicite dans les logs.

### Comparaison avec le build natif ARM64 (self-hosted VPS)

| Image | QEMU x86_64 | Natif ARM64 (VPS) | Facteur |
|-------|:-----------:|:-----------------:|:-------:|
| **Backend** | ❌ Échec | ~30-40s ¹ | ∞ (impossible) |
| **Frontend** | **9m19s** | ~25-35s ¹ | **~16× à 22×** |

> ¹ Valeurs de référence approximatives du protocole comparatif (Échantillon A).

### Logs pertinents

**Backend — Crash SDK .NET sous émulation QEMU :**
```
#15 [build  8/10] RUN dotnet restore src/Dkpt.Api/Dkpt.Api.csproj
#15 9.892   Determining projects to restore...
#15 10.68 error MSB4018: The "ConvertToAbsolutePath" task failed unexpectedly.
       [/src/src/Dkpt.Infrastructure/Dkpt.Infrastructure.csproj]
#15 10.68 System.NullReferenceException: Object reference not set to an instance of an object.
#15 10.68    at InvokeStub_ConvertToAbsolutePath.get_AbsolutePaths(Object, Object, IntPtr*)
#15 10.68    at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
#15 10.69 error MSB4028: The "ConvertToAbsolutePath" task's outputs could not be retrieved
       from the "AbsolutePaths" parameter. Object reference not set to an instance of an object.
#15 ERROR: process "/bin/sh -c dotnet restore src/Dkpt.Api/Dkpt.Api.csproj"
       did not complete successfully: exit code: 1
```

**Frontend — Succès en 9m19s :**
```
Exit code : 0
Durée     : 559s (9m19s)
```

### Analyse de l'erreur backend

L'erreur est un `NullReferenceException` dans le stub de réflexion généré par le JIT .NET
(`InvokeStub_ConvertToAbsolutePath.get_AbsolutePaths`). Ce n'est **pas** un problème réseau,
de timeout ou de mémoire :

| Hypothèse | Verdict | Justification |
|-----------|:-------:|---------------|
| Problème réseau / DNS | ❌ | Aucune erreur réseau dans les logs |
| Timeout NuGet | ❌ | Crash après 10s, pas de timeout |
| Mémoire insuffisante | ❌ | Pas d'OOM killer |
| **Incompatibilité JIT .NET sous QEMU** | ✅ | `NullReferenceException` dans `System.Reflection.MethodBaseInvoker` |

Le SDK .NET utilise RyuJIT pour générer du code natif ARM64 à la volée. Sous émulation
QEMU, ce code natif généré (les `InvokeStub_*`) ne se comporte pas correctement —
le stub de réflexion pour `ConvertToAbsolutePath` retourne `null` au lieu du résultat
attendu. C'est une **incompatibilité systématique**, pas un problème transitoire.

### Observations

1. **Le SDK .NET 9 est incompatible avec l'émulation QEMU ARM64** : le crash se produit
   dans le JIT/reflection du SDK (MSBuild task `ConvertToAbsolutePath`), pas dans le code
   applicatif. C'est un problème fondamental de la couche d'émulation.

2. **Node.js fonctionne mais avec un facteur ~16×** : le frontend (npm ci + npm run build)
   termine en 9m19s sous émulation, contre ~25-35s en natif. L'émulation est fonctionnelle
   mais impraticable en CI quotidien.

3. **Asymétrie .NET vs Node.js** : le runtime .NET est significativement plus sensible
   à l'émulation QEMU que Node.js. Node.js utilise V8 (interprété/JIT plus tolérant),
   tandis que .NET s'appuie sur RyuJIT et des stubs de réflexion natifs qui ne tolèrent
   pas la couche d'émulation QEMU.


---

## 5. Conclusion

Cet essai exploratoire sur Azure DevOps hosted runner x86_64 démontre que la
construction Docker `linux/arm64` via QEMU présente **deux niveaux de contrainte**
pour le projet DKPT :

1. **Impossibilité** pour le backend .NET 9 : le SDK crash dès la phase `dotnet restore`
   sous émulation, rendant le build ARM64 sur runner x86_64 **techniquement impossible**
   sans intervention sur le Dockerfile (multi-arch, cross-compilation).

2. **Dégradation critique** pour le frontend Node.js : le build aboutit mais en
   **9m19s** (facteur ~16× vs natif ARM64), ce qui est impraticable pour un
   pipeline CI/CD quotidien.

Ces résultats justifient le choix méthodologique de fixer les phases Docker/CD sur un
runner ARM64 self-hosted dans le protocole comparatif du mémoire. Cette contrainte
n'est pas spécifique à Azure DevOps — elle s'applique à toute plateforme dont les
hosted runners sont exclusivement x86_64 (GitLab CI, Bitbucket Pipelines), sans
prétendre quantifier à elle seule leur comportement exact.


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
