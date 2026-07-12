# Plan de recollecte des échantillons de performance CI/CD — Projet DKPT

## Contexte

Mémoire de Master comparant 4 plateformes CI/CD : **GitHub Actions**, **GitLab CI**, **Bitbucket Pipelines**, **Azure DevOps**.
Le projet DKPT est une application .NET 9 + Nuxt 4 déployée sur un VPS ARM64 (Oracle Cloud, Ubuntu 22.04).

---

## 1. Diagnostic de l'existant — Pourquoi refaire les mesures

Les 3 échantillons actuels ne sont pas convaincants :

| Échantillon actuel | Problème |
|:------------------:|----------|
| **§2 — Référence** | Configuration hétérogène : GitHub sur hosted dédié ARM64, GitLab/Bitbucket/Azure sur self-hosted VPS. On compare des matériels différents, pas des plateformes |
| **§9 — Self-hosted** | Seul GitHub est mesuré. Ce n'est pas un échantillon à 4 plateformes mais une exploration intra-GitHub |
| **§10 — Hybrid Optimal** | Le seul vrai échantillon comparable (4 plateformes, infra Docker/CD fixe), mais N=1 run — aucune validation statistique |

**Conclusion** : on a 1 vrai échantillon comparable (N=1) + 2 explorations partielles. Il faut recolleter avec un protocole rigoureux.

---

## 2. Contrainte matérielle — ARM64 et hosted runners

Les hosted runners des plateformes ne disposent pas tous d'une architecture ARM64 native :

| Plateforme | Hosted runner | Architecture | Docker ARM64 natif |
|:----------:|:------------:|:------------:|:------------------:|
| **GitHub Actions** | `ubuntu-24.04-arm` | ✅ ARM64 natif | ✅ Oui |
| **GitLab CI** | Shared runner (SaaS) | ❌ x86_64 | ❌ Émulation QEMU → timeout |
| **Bitbucket Pipelines** | Cloud runner | ❌ x86_64 | ❌ Émulation QEMU → timeout |
| **Azure DevOps** | `ubuntu-22.04` | ❌ x86_64 | ❌ Émulation QEMU → timeout |

**Conséquence** : le Docker build et le CD **doivent** être exécutés sur le VPS self-hosted ARM64 pour les 4 plateformes. Ce n'est pas un choix méthodologique mais une **nécessité technique**. La seule variable pouvant varier entre les échantillons est le **runner CI** (compilation, tests, lint).

---

## 3. Définition des 2 échantillons

### Échantillon A — "CI Hosted" (plateforme telle quelle)

> **Objectif** : Mesurer la performance CI de chaque plateforme avec ses runners cloud natifs, Docker/CD sur infrastructure commune.

| Phase | GitHub | GitLab | Bitbucket | Azure |
|-------|--------|--------|-----------|-------|
| **CI** (build + test + lint) | `ubuntu-22.04` hosted | Shared runner SaaS | Cloud runner | `ubuntu-22.04` hosted |
| **Docker build + push** | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS |
| **CD Staging** | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS |
| **CD Production** | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS |

**Ce que ça mesure** : la qualité/vitesse intrinsèque des runners hosted de chaque plateforme pour le CI, à infrastructure Docker/CD constante.

---

### Échantillon B — "Full Self-Hosted" (même matériel partout)

> **Objectif** : Isoler l'overhead pur de chaque plateforme (scheduling, orchestration, communication agent) en éliminant toute variable matérielle.

| Phase | GitHub | GitLab | Bitbucket | Azure |
|-------|--------|--------|-----------|-------|
| **CI** (build + test + lint) | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS |
| **Docker build + push** | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS |
| **CD Staging** | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS |
| **CD Production** | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS |

**Ce que ça mesure** : l'overhead plateforme pur — même CPU, même RAM, même réseau. Les écarts = différences d'orchestration et de scheduling.

---

## 4. Protocole de collecte

### 4.1 Conditions pré-run

- [ ] Aucun autre pipeline en cours sur le VPS (éviter contention CPU/mémoire)
- [ ] Docker cache purgé OU état de cache identique entre les runs ¹
- [ ] Même commit pour les 4 plateformes (utiliser le mirror pipeline)
- [ ] Runner self-hosted actif et idle sur les 4 plateformes
- [ ] Node modules et NuGet packages non cachés (cold start) OU état de cache identique

> ¹ **Choix à faire** : cold start (purge cache avant chaque run) ou warm start (3 runs consécutifs, garder le 2ème et 3ème). Le warm start est plus réaliste (utilisation quotidienne) mais nécessite de documenter l'état du cache.

### 4.2 Métriques à collecter par run

Pour chaque run, noter depuis les logs de la plateforme :

| Métrique | Source | Exemple |
|----------|--------|---------|
| **CI Backend** (restore + build + test) | Log du job/step | `38s` |
| **CI Frontend** (npm ci + lint + build) | Log du job/step | `1m36s` |
| **CI Total** | Durée du pipeline CI (incluant overhead scheduling) | `1m45s` |
| **Docker Backend** (build + push) | Log du step | `1m18s` |
| **Docker Frontend** (build + push) | Log du step | `52s` |
| **Docker Total** | Somme ou durée job Docker | `2m10s` |
| **CD Staging** (deploy SSH + retag) | Durée du pipeline/job CD | `32s` |
| **CD Production** (deploy SSH + retag) | Durée du pipeline/job CD | `28s` |
| **Pipeline Total** | De la 1ère action CI au dernier step CD Prod | `5m15s` |
| **Overhead scheduling** | Temps entre le push et le 1er step réel | `8s` |

### 4.3 Séquence de collecte recommandée

```
Pour chaque Échantillon (A puis B) :
  1. Configurer les pipelines (hosted ou self-hosted pour CI)
  2. Pousser le commit via mirror
  3. Attendre que les 4 plateformes terminent
  4. Collecter les métriques depuis les logs
  5. Répéter 2× de plus (total 3 runs)
  6. Calculer moyenne et écart-type par métrique
```

---

## 5. Matrice de collecte — Template

### Échantillon A — CI Hosted (3 runs)

| Métrique | GitHub R1 | R2 | R3 | μ | GitLab R1 | R2 | R3 | μ | Bitbucket R1 | R2 | R3 | μ | Azure R1 | R2 | R3 | μ |
|----------|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| CI Backend | | | | | | | | | | | | | | | | |
| CI Frontend | | | | | | | | | | | | | | | | |
| CI Total | | | | | | | | | | | | | | | | |
| Docker Backend | | | | | | | | | | | | | | | | |
| Docker Frontend | | | | | | | | | | | | | | | | |
| Docker Total | | | | | | | | | | | | | | | | |
| CD Staging | | | | | | | | | | | | | | | | |
| CD Prod | | | | | | | | | | | | | | | | |
| Pipeline Total | | | | | | | | | | | | | | | | |

### Échantillon B — Full Self-Hosted (3 runs)

*(Même tableau)*

---

## 6. Modifications pipeline nécessaires

### Pour l'Échantillon A (CI Hosted)

C'est la configuration actuelle des pipelines (Hybrid Optimal). **Pas de modification nécessaire** si les pipelines sont déjà configurés avec CI sur hosted et Docker/CD sur self-hosted.

Vérifier :
- [ ] GitHub : CI jobs sur `ubuntu-22.04` (ou `ubuntu-24.04`), Docker job sur `self-hosted`
- [ ] GitLab : CI jobs sans `tags:` (shared runner), Docker/CD jobs avec `tags: [ubuntu_arm64]`
- [ ] Bitbucket : CI steps sans `runs-on`, Docker/CD steps avec `runs-on: self.hosted`
- [ ] Azure : CI jobs sur `vmImage: ubuntu-22.04`, Docker/CD jobs sur `pool: Default`

### Pour l'Échantillon B (Full Self-Hosted)

Passer **tous les jobs CI** sur le runner self-hosted ARM64 :

| Plateforme | Modification CI |
|:----------:|----------------|
| **GitHub** | `runs-on: self-hosted` (au lieu de `ubuntu-22.04`) |
| **GitLab** | Ajouter `tags: [ubuntu_arm64]` aux jobs CI (backend, frontend) |
| **Bitbucket** | Ajouter `runs-on: self.hosted` aux steps CI |
| **Azure** | Changer `pool: vmImage: ubuntu-22.04` → `pool: name: Default` |

> **Important** : ces modifications doivent être faites dans une branche dédiée ou via des variables, pour pouvoir basculer facilement entre Échantillon A et B.

---

## 7. Résumé

| | Échantillon A | Échantillon B |
|:-:|:------------:|:-------------:|
| **Nom** | CI Hosted | Full Self-Hosted |
| **CI runner** | Hosted natif (chaque plateforme) | Self-hosted ARM64 VPS (commun) |
| **Docker + CD** | Self-hosted ARM64 VPS | Self-hosted ARM64 VPS |
| **Variable mesurée** | Performance CI intrinsèque | Overhead plateforme pur |
| **N runs** | 3 | 3 |
| **Total runs** | 12 | 12 |
| **Modifications pipeline** | Aucune (config actuelle) | CI → self-hosted |

**Total global : 24 runs** (12 par échantillon).

**Option réduite (12 runs)** : ne faire que l'Échantillon A (CI Hosted), qui est le cas d'usage le plus représentatif de la réalité d'un développeur. L'Échantillon B peut être présenté comme exploration complémentaire (N=1) ou perspective future.
