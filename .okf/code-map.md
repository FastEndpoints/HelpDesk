---
type: Reference
title: Code Map
description: Monorepo layout for Aspire orchestration, the SvelteKit frontend, and .NET backend projects.
tags: [layout]
resource: HelpDesk.slnx
---

# Code Map

## Layout

```text
HelpDesk/
├── frontend/
│   ├── src/lib/server/api/       # server-only typed backend clients and JWT cookie helper
│   ├── src/lib/api/generated/    # generated OpenAPI TypeScript declarations
│   ├── openapi/                  # committed backend API snapshots
│   └── scripts/openapi.mjs
├── backend/
│   ├── AppHost/                  # Aspire local full-stack orchestrator
│   ├── Common/{StorageProvider,Tools}/
│   ├── Contracts/{UserIdentity,UserProfile,Notifications}/
│   ├── Services/{UserIdentity,UserProfile,Notifications}/
│   └── Directory.Packages.props
├── HelpDesk.slnx
├── package.json
└── pnpm-workspace.yaml
```

Removed migration-era paths must not be documented or recreated as active workflows: `compose.yaml`, root `.env.example`, `scripts/`, service `Properties/launchSettings.json`, and `frontend/.env.example`. The AppHost launch settings are active IDE configuration, not a service-local workflow.

## Orchestration

| Path | Contents |
| --- | --- |
| `backend/AppHost/HelpDesk.AppHost.csproj` | Aspire 13.4.6 executable and resource package references |
| `backend/AppHost/Program.cs` | Sole supported local full-stack graph: MongoDB, three services, Vite, references, waits, and injected API origins |
| `backend/AppHost/Properties/launchSettings.json` | Rider/.NET `HelpDesk.AppHost` Development launch profile |
| `package.json` | Root commands; `stack:dev` runs the AppHost |

## Backend modules

| Path | Contents |
| --- | --- |
| `backend/Common/StorageProvider` | Mongo-backed remote event storage |
| `backend/Common/Tools` | Generic helpers and permission group names |
| `backend/Contracts/<Name>` | Service name, events, subscriber IDs as needed |
| `backend/Services/<Name>/Program.cs` | Host, IPC/HTTP, DI, hubs, subscriptions |
| `backend/Services/<Name>/Endpoints/` | Service-owned REST endpoints |
| `backend/Services/<Name>/Persistence/` | Private entities, stores, database init |
| `backend/Services/<Name>/Subscriptions/` | Event handlers and colocated tests |
| `backend/Services/<Name>/Tests/` | Shared service test fixtures |

## Frontend modules

| Path | Contents |
| --- | --- |
| `frontend/src/routes/` | SvelteKit routes: landing (`/`), registration (`/register`), verify (`/verify/[code]`), login (`/login`), profile (`/settings/profile`), shared shell layout |
| `frontend/src/routes/+layout.server.ts` | Root layout load: session cookie → Profile `GET /profiles/me` for shell user chrome |
| `frontend/src/routes/register/` | Registration form + server action BFF to Identity `POST /identities/register` |
| `frontend/src/routes/verify/[code]/` | Email verification page; button posts to BFF action → Identity `GET /identities/verify/{code}` |
| `frontend/src/routes/login/` | Sign-in form + server action BFF to Identity `POST /identities/login`; sets `helpdesk_session` cookie; optional safe `redirectTo` |
| `frontend/src/routes/settings/profile/` | Auth-gated profile view/edit BFF: load + update/upload/delete picture actions against Profile |
| `frontend/src/app.css` | Global styles / Tailwind v4 entry; FE-Docs navy/cyan theme tokens (`fe-*`) |
| `frontend/src/lib/server/api/` | BFF-only config, clients, `ApiError`/problem mapping, and session cookie convention |
| `frontend/openapi/*.json` | Normalized Identity/Profile OpenAPI snapshots |
| `frontend/src/lib/api/generated/*.d.ts` | Generated API path/schema types |
| `frontend/scripts/openapi.mjs` | Snapshot/type workflow; live commands require explicit Aspire-derived URLs |

Registration, email-verification, login, profile view/edit (including picture upload/delete), and signed-in shell chrome (name/avatar link to `/settings/profile`) exist. Logout UI does not yet.

## Entry points and endpoints

| Resource | Entry | Local endpoint behavior |
| --- | --- | --- |
| AppHost | `backend/AppHost/Program.cs` | Aspire dashboard and resource lifecycle |
| Frontend | `frontend/` Vite/SvelteKit | Aspire-assigned during full-stack run; Playwright preview remains 4173 |
| UserIdentity | `backend/Services/UserIdentity/Program.cs` | Aspire-assigned HTTP endpoint |
| UserProfile | `backend/Services/UserProfile/Program.cs` | Aspire-assigned HTTP endpoint |
| Notifications | `backend/Services/Notifications/Program.cs` | no public HTTP endpoint |
| MongoDB | Aspire `mongodb` resource | ephemeral authenticated standalone container |

Read dynamic application endpoints from the Aspire dashboard. MongoDB uses fixed development port `27017` so direct test commands can share the Aspire-managed resource.

## Sibling library sources

Outside this monorepo (paths relative to HelpDesk root):

| Library | Path |
| --- | --- |
| FastEndpoints | `../FastEndpoints/` |
| MongoDB.Entities | `../MongoDB.Entities/` |

These are inspection sources, not in-solution project references.

## Sources

- `package.json`
- `frontend/package.json`
- `frontend/src/`
- `HelpDesk.slnx`
- `backend/AppHost/`
- `backend/Services/*/Program.cs`
- `../FastEndpoints/`
- `../MongoDB.Entities/`
