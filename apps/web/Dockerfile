# Build context MUST be the repository root: the workspace's web-kit package is
# COPYed in, and fly.toml/docker-compose declare the context explicitly.
FROM node:26-alpine AS base
RUN corepack enable
WORKDIR /app

FROM base AS deps
COPY pnpm-lock.yaml pnpm-workspace.yaml package.json ./
COPY apps/web/package.json apps/web/
COPY packages/web-kit/package.json packages/web-kit/
COPY e2e/package.json e2e/
RUN pnpm install --frozen-lockfile --filter auto-veritas-web --filter @auto-veritas/web-kit

FROM deps AS build
COPY apps/web apps/web
COPY packages/web-kit packages/web-kit
RUN pnpm --filter auto-veritas-web build

FROM node:26-alpine AS runtime
ENV NODE_ENV=production PORT=8080 HOSTNAME=0.0.0.0
RUN addgroup --system --gid 1001 nodejs && adduser --system --uid 1001 nextjs
WORKDIR /app
COPY --from=build --chown=nextjs:nodejs /app/apps/web/.next/standalone ./
COPY --from=build --chown=nextjs:nodejs /app/apps/web/.next/static ./apps/web/.next/static
USER nextjs
EXPOSE 8080
LABEL org.opencontainers.image.source=https://github.com/konradcinkusz/auto-veritas
# No env values baked in: AUTH_URL, AUTH_JWKS_URL, AUTH_ISSUER, AUTH_AUDIENCE and
# OFFERS_API_URL are supplied wherever the container starts (P12).
CMD ["node", "apps/web/server.js"]
