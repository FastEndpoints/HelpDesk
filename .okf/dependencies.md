---
type: Reference
title: Dependencies
description: .NET 10 runtime, central package management, and key frameworks used by HelpDesk.
tags: [deps]
resource: backend/Directory.Packages.props
---

# Dependencies

## Runtime

- **Backend:** .NET 10 / `net10.0`; service hosts use `Microsoft.NET.Sdk.Web`
- **Frontend:** Node.js 26 or newer required by root `engines`; `.node-version` selects 26.4.0, SvelteKit 2/Svelte 5 with adapter-node
- **Package manager:** pnpm 11 or newer required by root `engines`; `packageManager` selects 11.10.0 by default; workspace package is `frontend`
- Root `.npmrc` sets `engine-strict=true`, so use a supported toolchain before a workspace install
- Bootstrap with Corepack when available (`corepack enable` + `corepack prepare pnpm@11.10.0 --activate`); otherwise `npm install --global pnpm@11.10.0`

## Packages

Central versions: `backend/Directory.Packages.props` (`ManagePackageVersionsCentrally`).

Project references only declare package **names** (no versions) when using CPM.

## Key libraries

| Package | Role |
| --- | --- |
| FastEndpoints (+ Generator, OpenApi, Security, Testing) | HTTP endpoints, DI helpers, OpenAPI |
| FastEndpoints.Messaging.Core / Remote (+ Testing) | Events, hubs, IPC/remote mesh |
| MongoDB.Entities | Persistence + indexes |
| MailKit | SMTP (Notifications) |
| Scalar.AspNetCore | API reference UI (non-prod) |
| SixLabors.ImageSharp | Profile picture decode/resize/crop/encode (UserProfile) |
| Microsoft.OpenApi | OpenAPI docs |
| xunit.v3, Shouldly, Microsoft.NET.Test.Sdk | Tests |
| ASP.NET Core Identity `PasswordHasher<T>` | Password hashing (UserIdentity) |

## Local framework sources

Sibling repos (relative to HelpDesk root) for reading/debugging library behavior—not project references inside this monorepo:

| Library | Source path |
| --- | --- |
| FastEndpoints | `../FastEndpoints/` |
| MongoDB.Entities | `../MongoDB.Entities/` |

## Constraints

- Keep FastEndpoints family versions aligned (currently `8.3.0-beta.12` in props)
- Do not add a message broker package for cross-service workflows without architecture change
- Prefer project refs: Services → Contracts/Common only
- Bump versions in `backend/Directory.Packages.props`, not scattered csproj Version attributes

## Frontend libraries

SvelteKit/Svelte/Vite, adapter-node, `openapi-fetch`, `openapi-typescript`, Vitest, Playwright, ESLint, Prettier, Tailwind CSS, and TypeScript. Versions live in `frontend/package.json`; install from the root lockfile with `pnpm install --frozen-lockfile`.

Direct frontend dependencies track their latest releases. TypeScript is held at `6.0.3`, the newest release supported by the current SvelteKit and `typescript-eslint` versions; TypeScript 7 breaks `svelte-check` and exceeds their peer ranges. `pnpm-workspace.yaml` exempts ESLint `10.7.0` from the package-manager minimum-release-age policy so frozen installs can reproduce the explicit latest-version upgrade.

## Sources

- `package.json`
- `pnpm-lock.yaml`
- `.node-version`
- `frontend/package.json`
- `backend/Directory.Packages.props`
- `backend/Services/*/*.csproj`
- `backend/Common/*/*.csproj`
- `backend/Contracts/*/*.csproj`
- `../FastEndpoints/`
- `../MongoDB.Entities/`
