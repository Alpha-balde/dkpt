# Notes de rédaction — Contrainte ARM64 et protocole de mesure

## Observation 1 — Docker build ARM64 sur hosted runners

Les hosted runners (runners cloud managés par chaque plateforme) ne disposent pas tous d'une architecture ARM64 native :

| Plateforme | Hosted runner | Architecture | Docker ARM64 natif |
|:----------:|:------------:|:------------:|:------------------:|
| **GitHub Actions** | `ubuntu-24.04-arm` | ARM64 natif | ✅ Oui (~2m40s mesuré) |
| **GitLab CI** | Shared runner (SaaS) | x86_64 | ❌ Non — émulation QEMU requise |
| **Bitbucket Pipelines** | Cloud runner (Atlassian) | x86_64 | ❌ Non — émulation QEMU requise |
| **Azure DevOps** | `ubuntu-22.04` (Microsoft-hosted) | x86_64 | ❌ Non — émulation QEMU requise |

Le build Docker d'images `linux/arm64` sur un runner x86_64 nécessite une couche d'émulation QEMU. En pratique, cette émulation rend le build **extrêmement lent** (facteur ×10 à ×20) voire **impossible** dans les délais de timeout imposés par les plateformes (typiquement 60 à 120 minutes).

Seul GitHub Actions propose à ce jour des hosted runners ARM64 natifs (`ubuntu-24.04-arm`), les trois autres plateformes ne disposant que de runners x86_64 dans leur offre cloud gratuite.

## Observation 2 — Conséquence sur le protocole de mesure

Cette contrainte matérielle impose que le **Docker build et le déploiement (CD)** soient exécutés sur un **runner self-hosted ARM64** commun aux quatre plateformes. Ce n'est pas un choix méthodologique mais une **nécessité technique** : sans runner ARM64 natif, trois plateformes sur quatre ne peuvent pas produire les images Docker du projet.

En conséquence, la seule variable pouvant varier entre les échantillons est le **runner CI** (compilation, tests, lint). Les phases Docker build et CD sont invariablement exécutées sur le même VPS ARM64 self-hosted, garantissant une comparaison à infrastructure constante pour ces phases.

Cette contrainte renforce paradoxalement la rigueur de la comparaison : en fixant le matériel Docker/CD, on isole mieux la **performance intrinsèque de chaque plateforme** sur la phase CI.
