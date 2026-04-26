#!/bin/bash
# ============================================
# DKPT — Script de déploiement
# ============================================
# Pull les images Docker et relance les containers.
# Appelé par les pipelines CI/CD après generate-env.sh.
#
# Usage :
#   ./scripts/deploy.sh [compose-file]
#   Par défaut : docker-compose.prod.yml
# ============================================

set -e

COMPOSE_FILE="${1:-docker-compose.prod.yml}"
DEPLOY_DIR="/opt/dkpt"

echo "🚀 Deploying DKPT..."
cd "$DEPLOY_DIR"

echo "📦 Pulling images..."
docker compose -f "$COMPOSE_FILE" pull

echo "🔄 Starting containers..."
docker compose -f "$COMPOSE_FILE" up -d --remove-orphans

echo "🧹 Cleaning old images..."
docker image prune -f

echo "✅ Deploy complete!"
