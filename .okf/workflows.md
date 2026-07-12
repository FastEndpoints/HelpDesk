---
type: Playbook
title: Workflows
description: Aspire full-stack startup plus monorepo build, test, format, and OpenAPI commands.
tags: [build]
resource: README.md
---

# Workflows

## Bootstrap

Required: .NET 10, Node 26 or newer (`.node-version` selects 26.4.0), pnpm 11 or newer (`packageManager` selects 11.10.0), and a running Aspire-compatible container runtime. Root `.npmrc` enables strict engine enforcement.

```bash
# Preferred when Corepack is available:
corepack enable
corepack prepare pnpm@11.10.0 --activate
# Otherwise:
npm install --global pnpm@11.10.0

pnpm install --frozen-lockfile
```

No root `.env`, MongoDB setup script, keyfile, or generated JWT files are part of bootstrap.

## Full-stack development

```bash
pnpm stack:dev
```

This runs `backend/AppHost/HelpDesk.AppHost.csproj`. `backend/AppHost/Program.cs` is the sole supported local full-stack orchestrator. Aspire starts ephemeral authenticated standalone MongoDB on development port `27017`, Identity, Profile, Notifications, and Vite; assigns application HTTP ports dynamically; injects MongoDB/API environment; and exposes current application endpoints in the dashboard. Stop with Ctrl+C.

In Rider, run the `HelpDesk.AppHost` launch profile from `backend/AppHost/Properties/launchSettings.json`. It launches the AppHost project in Development with the dashboard on `http://localhost:15235`; the profile enables `ASPIRE_ALLOW_UNSECURED_TRANSPORT` for this development-only HTTP endpoint. Aspire continues to assign application ports dynamically.

Do not document running service projects in separate terminals as a supported full-stack alternative. `pnpm frontend:dev` remains useful for frontend-only work.

MongoDB-backed backend test commands require `pnpm stack:dev` to be running; they use the Aspire-managed instance on `localhost:27017`.

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

`pnpm` remains the frontend package manager and repository validation command surface. Direct backend restore/build/test/format commands may target `HelpDesk.slnx`; they are validation workflows, not replacement orchestration.

## Frontend and Playwright

From `frontend/`: `pnpm check`, `pnpm lint`, `pnpm format:check`, `pnpm test:unit`, `pnpm build`. Before the first E2E run install browsers with `pnpm exec playwright install`, then run `pnpm test:e2e`. Playwright builds/previews on fixed test port 4173 and configures that origin as `baseURL`.

## OpenAPI

Start `pnpm stack:dev`. Copy Identity and Profile HTTP URLs from the Aspire dashboard, append `/openapi/v1.json`, and export the complete document URLs:

```bash
cd frontend
export IDENTITY_OPENAPI_URL='<identity-http-url>/openapi/v1.json'
export PROFILE_OPENAPI_URL='<profile-http-url>/openapi/v1.json'

pnpm api:refresh
pnpm api:generate
pnpm api:check
pnpm api:check:live
```

`api:refresh` and `api:check:live` require both variables; there are no default fixed ports. `api:refresh` fetches and normalizes every service spec before writing snapshots, removing the runtime-specific top-level `servers` entry so dynamic Aspire ports do not cause drift. `api:generate` regenerates declarations. `api:check` is offline and compares declarations to committed snapshots. `api:check:live` additionally compares live specs.

## Sources

- `package.json`
- `backend/AppHost/Program.cs`
- `backend/AppHost/HelpDesk.AppHost.csproj`
- `frontend/package.json`
- `frontend/scripts/openapi.mjs`
- `frontend/playwright.config.ts`
