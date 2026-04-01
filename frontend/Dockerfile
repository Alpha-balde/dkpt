# ============================================
# DKPT Frontend — Nuxt 4 Multi-Stage Dockerfile
# ============================================

# Stage 1: Build
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files first (better cache)
COPY package.json package-lock.json ./
RUN npm ci

# Copy source and build
COPY . .
RUN npm run build

# Stage 2: Runtime
FROM node:20-alpine AS runtime
WORKDIR /app

# Copy only the built output
COPY --from=build /app/.output ./.output

ENV HOST=0.0.0.0
ENV PORT=3000
EXPOSE 3000

CMD ["node", ".output/server/index.mjs"]
