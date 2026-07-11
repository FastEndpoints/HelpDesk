---
type: Architecture
title: Architecture
description: Brokerless FastEndpoints remote mesh with backend/Contracts/backend/Common/Services layering and event-only cross-service workflows.
tags: [architecture]
resource: README.md
---

# Architecture

## Style

Brokerless microservice mesh:

- **Contracts** = public event language
- **Common** = reusable infrastructure/helpers only
- **Services** = isolated deployable FastEndpoints hosts
- **Mesh** = FastEndpoints.Messaging.Remote event queues between nodes
- **Broker** = none; **business RPC** = none

Current topology is host-local and hardcoded in service startup: `ListenInterProcess(Service.Name)` + `MapRemote(publisherName, …)`, with public HTTP listeners bound through `ListenLocalhost`. No current configuration or deployment manifest implements a network transport or multi-host mesh; adding one is an architecture/deployment change even if handlers and contracts remain stable.

## Monorepo and external-client boundary

`frontend/` is the SvelteKit adapter-node application; `backend/` contains the .NET solution and Common/Contracts/Services layers. Browsers use SvelteKit as a BFF. Only server modules under `frontend/src/lib/server/api/` know the private Identity/Profile origins and attach server-held bearer tokens. Their shared client middleware converts every backend non-success response to `ApiError`, preserving RFC problem details and the HTTP status. The existing frontend is a foundation page and API helpers, not completed auth/profile UI.

## Components

```text
External client
  │ REST
  ▼
UserIdentity ──events──► UserProfile
     │                        │
     └──events──────────────► Notifications (email jobs)
```

| Layer | Projects | Role |
| --- | --- | --- |
| Common | `StorageProvider`, `Tools` | Event storage provider; `NormalizeForLookup`; `PermissionGroups` (permission group names only) |
| Contracts | `UserIdentity`, `UserProfile`, `Notifications` | `Service.Name`, events, `EventSubscribers` |
| Services | same three names | Endpoints, persistence, hubs, subscriptions |

## Dependency rules

**Allowed:**

```text
Services → Contracts
Services → Common
Contracts → package refs only (e.g. FastEndpoints.Messaging.Core)
Common → package refs only
```

**Forbidden:**

```text
backend/Services/A → backend/Services/B
Contracts → Services or domain logic
Common → domain / service-specific behavior
```

Cross-service business flow: **events only**. A service may expose REST for external clients; must not call another service’s REST to finish an internal workflow. Events are facts after local commit, not commands.

## Communication

- Publisher: persist locally → `event.Broadcast()`; hubs registered in `Program.cs` via `MapHandlers` + `RegisterEventHub<TEvent>(subscriberIds)`.
- Subscriber: `MapRemote(publisherServiceName, c => { c.SubscriberID = ownName; c.Subscribe<TEvent, THandler>(); })`.
- Durable event records: `Common.StorageProvider.EventRecord` + `EventStorageProvider` (MongoDB.Entities).
- Notifications internal work: FastEndpoints job queue (`JobRecord` / `JobStorageProvider`) for `SendEmailCommand`.

## Persistence

Each service owns its MongoDB database (config `*Settings` / `DatabaseName`). No shared domain collections. Event storage indexes created per service DB init. Notifications also stores job records.

## Security / auth (boundaries)

- UserIdentity issues RSA JWT (`sub` + role **group names** from identity `Groups`); signs with private PEM. Never references another service’s `Allow`.
- UserProfile validates JWT with public key (asymmetric); expands roles → local permission codes via `IClaimsTransformation`; `GET /profiles/me` requires `Profiles_Read_Own`, `PUT /profiles/me` requires `Profiles_Update_Own` (both group `User`).
- Shared group names live in `Common.Tools.PermissionGroups` (constants only—not a full RBAC engine).
- Notifications has no public business API / no JWT surface today.
- Details: [security.md](security.md).

## Invariants

1. Own state committed before publishing related events.
2. Subscribers mutate only own state / queue own jobs; no callback to publisher.
3. Contracts stay free of endpoints, entities, stores, SMTP, handlers.
4. Service names and `EventSubscribers` arrays stay aligned with real consumer `Service.Name` values.
5. Tests prove REST boundaries and subscription reactions; topology-agnostic.

## Sources

- `README.md`
- `backend/Services/*/Program.cs`
- `backend/Contracts/*/`
- `backend/Common/StorageProvider/`
