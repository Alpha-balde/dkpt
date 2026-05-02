#!/bin/bash
# ============================================
# DKPT — Génération du fichier .env
# ============================================
# Ce script génère le fichier .env pour docker-compose
# à partir des variables d'environnement passées par le CI/CD.
#
# Usage (appelé par le pipeline CI/CD) :
#   Les variables sont injectées via le paramètre 'envs'
#   de appleboy/ssh-action (GitHub) ou équivalent.
# ============================================

set -e

ENV_FILE="/opt/dkpt/.env"

# Construire la connection string à partir des variables Postgres
CONNECTION_STRING="Host=dkpt-db-prod;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"

echo "📝 Generating $ENV_FILE..."

cat > "$ENV_FILE" << EOF
POSTGRES_DB=${POSTGRES_DB}
POSTGRES_USER=${POSTGRES_USER}
POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
ConnectionStrings__DefaultConnection=${CONNECTION_STRING}
Jwt__SecretKey=${JWT_SECRET_KEY}
Jwt__Issuer=${JWT_ISSUER}
Jwt__Audience=${JWT_AUDIENCE}
Cors__Origins=${CORS_ORIGINS}
ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Production}
NUXT_PUBLIC_API_BASE=${NUXT_PUBLIC_API_BASE}
NUXT_API_BASE_INTERNAL=${NUXT_API_BASE_INTERNAL:-http://dkpt-backend-prod:8080}
ApplicationInsights__ConnectionString=${APPLICATIONINSIGHTS_CONNECTION_STRING}
IMAGE_TAG=${IMAGE_TAG:-latest}
EOF

echo "✅ .env generated with $(wc -l < "$ENV_FILE") variables"
