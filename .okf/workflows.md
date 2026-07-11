---
type: Playbook
title: Workflows
description: Monorepo bootstrap, build, run, test, format, and OpenAPI commands.
tags: [build]
resource: README.md
---

# Workflows

## Bootstrap

Required: .NET 10, Node 26 or newer (root `engines`; `.node-version` selects 26.4.0), pnpm 11 or newer (root `engines`; `packageManager` selects 11.10.0 by default), Podman plus a Compose provider (for example, `podman-compose`), OpenSSL. Root `.npmrc` enables strict engine enforcement.

```bash
# Preferred when Corepack is available:
corepack enable
corepack prepare pnpm@11.10.0 --activate
# Otherwise:
npm install --global pnpm@11.10.0
pnpm install --frozen-lockfile
```

## Infrastructure

```bash
cp .env.example .env                 # replace local Mongo credentials
./scripts/setup-mongodb.sh           # creates keyfile or preserves the existing one
# ./scripts/setup-mongodb.sh --rotate  # explicit rotation only; stop MongoDB first
podman compose up -d
podman compose ps
podman compose logs -f mongodb
podman compose down
podman compose down -v               # destructive: removes Mongo data volume
```

Authenticated local backend connection string (substitute `.env` values):

```text
mongodb://helpdesk_local_admin:<password>@localhost:27017/?authSource=admin&replicaSet=rs0&directConnection=true
```

Set it through `ConnectionStrings__MongoDB` or user secrets. Before authenticated local API use, generate matching Identity private/Profile public RSA PEM keys and configure them through user secrets or multiline-preserving environment variables; use the root README commands.

## Root commands

```bash
pnpm backend:restore
pnpm backend:build
pnpm backend:build:release
pnpm backend:test
pnpm backend:format:check
pnpm stack:dev
pnpm frontend:dev
pnpm frontend:check
pnpm frontend:lint
pnpm frontend:format:check
pnpm frontend:test:unit
pnpm frontend:test:e2e
pnpm frontend:build
pnpm frontend:api:check
pnpm check:quick
pnpm check:full
```

## Full-stack run

After configuring root `.env`, run `pnpm stack:dev`. It creates `frontend/.env` and local JWT keys when absent, starts MongoDB plus all backend and frontend processes in the foreground, and tears the complete stack down on Ctrl+C or when any application process exits. Ctrl+C is a successful shutdown; application failures retain their nonzero exit status.

## Direct run

```bash
cp frontend/.env.example frontend/.env
dotnet run --project backend/Services/UserIdentity/Services.UserIdentity.csproj
dotnet run --project backend/Services/UserProfile/Services.UserProfile.csproj
dotnet run --project backend/Services/Notifications/Services.Notifications.csproj
pnpm --dir frontend dev
```

Frontend 5173; Identity 5000; Profile 5001; MongoDB 27017; Notifications has no public HTTP port.

## Frontend and Playwright

From `frontend/`: `pnpm check`, `pnpm lint`, `pnpm format:check`, `pnpm test:unit`, `pnpm build`. Before first E2E run install browsers with `pnpm exec playwright install`, then run `pnpm test:e2e`. Playwright builds/previews on 4173 and configures that origin as `baseURL`; its command is self-contained, so `check:full` does not prebuild separately.

## OpenAPI

From `frontend/`, with Identity/Profile live for refresh/live-check:

```bash
pnpm api:refresh
pnpm api:generate
pnpm api:check
pnpm api:check:live
```

`api:refresh` fetches and normalizes every service spec before writing any snapshot; `api:generate` regenerates declarations; `api:check` compares declarations to committed snapshots offline; `api:check:live` additionally compares live specs. Override live URLs with `IDENTITY_OPENAPI_URL` / `PROFILE_OPENAPI_URL`.

## Sources

- `package.json`
- `frontend/package.json`
- `frontend/scripts/openapi.mjs`
- `frontend/playwright.config.ts`
- `compose.yaml`
- `.env.example`
