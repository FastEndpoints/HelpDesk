---
type: Reference
title: Dependencies
description: .NET 10, Aspire 13.4.6, FastEndpoints 8.3 beta, central package management, and frontend toolchain dependencies.
tags: [deps]
resource: backend/Directory.Packages.props
---

# Dependencies

## Runtime

- **Backend:** .NET 10 / `net10.0`; service hosts use `Microsoft.NET.Sdk.Web`
- **Local orchestration:** Aspire AppHost SDK and hosting packages 13.4.6; a running compatible container runtime supplies MongoDB
- **Frontend:** Node.js 26 or newer; `.node-version` selects 26.4.0; SvelteKit 2/Svelte 5 with adapter-node
- **Package manager:** pnpm 11 or newer; `packageManager` selects 11.10.0 by default; workspace package is `frontend`
- **Production host tools:** Docker Compose (Podman Compose is a script fallback on hosts configured for privileged ports and SELinux bind mounts), OpenSSL for bootstrap, and curl for the public smoke test. Podman-only boot integration additionally needs systemd; `scripts/install-host-service.sh` installs a host unit from `deploy/helpdesk.service.in` (rootless also needs `loginctl` linger and unprivileged bind of ports 80/443).
- Root `.npmrc` sets `engine-strict=true`

Bootstrap with Corepack when available (`corepack enable` + `corepack prepare pnpm@11.10.0 --activate`); otherwise use `npm install --global pnpm@11.10.0`.

## Production container runtime

Production builds the frontend with Node 26 Alpine and runs the generated adapter-node HTTP server behind `caddy:2.10-alpine`, which provides automatic HTTPS. Backend service processes and the dependency-free `net10.0` PID 1 launcher run on `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`; builds use the matching Ubuntu Noble SDK. MongoDB uses the pinned `mongo:8.2` image (Aspire AppHost and production Compose).

## Central package management

Versions live in `backend/Directory.Packages.props` (`ManagePackageVersionsCentrally`). Project references declare package names without versions.

| Package family | Role |
| --- | --- |
| `Aspire.Hosting.AppHost`, `.JavaScript`, `.MongoDB` | Local application model, Vite, and MongoDB resources |
| FastEndpoints (+ Generator, OpenApi, Security, Testing) | HTTP endpoints, DI helpers, OpenAPI |
| FastEndpoints.Messaging.Core / Remote (+ Testing) | Events, hubs, IPC/remote mesh |
| MongoDB.Entities | Persistence + indexes |
| MailKit | SMTP (Notifications) |
| Scalar.AspNetCore | API reference UI (non-production) |
| SixLabors.ImageSharp | Profile picture processing |
| Microsoft.OpenApi | OpenAPI documents |
| xunit.v3, Shouldly, Microsoft.NET.Test.Sdk | Tests |
| ASP.NET Core Identity `PasswordHasher<T>` | Password hashing |

## Constraints

- Keep all Aspire references at 13.4.6 unless intentionally upgrading the AppHost SDK and hosting packages together.
- Keep FastEndpoints family versions aligned (currently `8.3.0-beta.14`).
- Keep `Microsoft.OpenApi` on 2.x while `Microsoft.AspNetCore.OpenApi` 10 depends on OpenAPI 2; do not jump to 3.x without a stack migration.
- Keep `SixLabors.ImageSharp` on 3.x; 4.0 requires a Six Labors commercial license key at build time.
- Prefer stable xunit.v3 / xunit.runner.visualstudio / Shouldly until previews are intentionally adopted.
- Do not add a message broker package without an architecture change.
- Service project refs remain Services → Contracts/Common only; the AppHost may reference service hosts for orchestration.
- Bump package versions centrally, not in service csproj files.

## Frontend libraries

SvelteKit/Svelte/Vite, adapter-node, `openapi-fetch`, `openapi-typescript`, Vitest, Playwright, ESLint, Prettier, Tailwind CSS, and TypeScript. Versions live in `frontend/package.json`; install from the root lockfile with `pnpm install --frozen-lockfile`.

Direct frontend dependencies track their latest releases. TypeScript is held at `6.0.3`, the newest release supported by the current SvelteKit and `typescript-eslint` versions; TypeScript 7 breaks `svelte-check` and exceeds their peer ranges. `pnpm-workspace.yaml` exempts ESLint `10.7.0` from the package-manager minimum-release-age policy.

## Local framework sources

Sibling repos `../FastEndpoints/` and `../MongoDB.Entities/` may be used to inspect library behavior. HelpDesk consumes NuGet packages; these are not project references.

## Sources

- `package.json`
- `pnpm-lock.yaml`
- `.node-version`
- `frontend/package.json`
- `backend/AppHost/HelpDesk.AppHost.csproj`
- `backend/Directory.Packages.props`
- `backend/Services/*/*.csproj`
