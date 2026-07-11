---
type: Reference
title: Code Map
description: Monorepo layout for the SvelteKit frontend and .NET backend projects.
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
│   ├── Common/{StorageProvider,Tools}/
│   ├── Contracts/{UserIdentity,UserProfile,Notifications}/
│   ├── Services/{UserIdentity,UserProfile,Notifications}/
│   └── Directory.Packages.props
├── HelpDesk.slnx
├── scripts/setup-mongodb.sh
├── compose.yaml
├── package.json
└── pnpm-workspace.yaml
```

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
| `frontend/src/routes/` | SvelteKit routes; currently only the foundation landing page |
| `frontend/src/lib/server/api/` | BFF-only config, clients, errors, and session cookie convention |
| `frontend/openapi/*.json` | Normalized Identity/Profile OpenAPI snapshots |
| `frontend/src/lib/api/generated/*.d.ts` | Generated API path/schema types |

No registration, login, verification, profile-edit, or profile-picture UI currently exists.

## Entry points and ports

| Process | Entry | Local port |
| --- | --- | --- |
| Frontend | `frontend/` Vite/SvelteKit | 5173 (Playwright preview 4173) |
| UserIdentity | `backend/Services/UserIdentity/Program.cs` | 5000 |
| UserProfile | `backend/Services/UserProfile/Program.cs` | 5001 |
| Notifications | `backend/Services/Notifications/Program.cs` | no public HTTP port |
| MongoDB | `compose.yaml` | 127.0.0.1:27017 |

## Sibling library sources

Outside this monorepo (paths relative to HelpDesk root):

| Library | Path |
| --- | --- |
| FastEndpoints | `../FastEndpoints/` |
| MongoDB.Entities | `../MongoDB.Entities/` |

Use these when tracing FE messaging, endpoint generation, or MongoDB.Entities persistence behavior. HelpDesk still consumes NuGet packages via CPM; these are source trees for inspection, not in-solution project refs.

## Sources

- `package.json`
- `frontend/package.json`
- `frontend/src/`
- `HelpDesk.slnx`
- `backend/Services/*/Program.cs`
- `compose.yaml`
- `../FastEndpoints/`
- `../MongoDB.Entities/`
