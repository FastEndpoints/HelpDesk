---
type: Architecture
title: Architecture
description: Brokerless FastEndpoints remote mesh with Contracts/Common/Services layering and event-only cross-service workflows.
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

Local: `ListenInterProcess(Service.Name)` + `MapRemote(publisherName, …)`. Topology (IPC vs network) is config/deployment; handlers and contracts stay the same.

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
Services/A → Services/B
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
- `Services/*/Program.cs`
- `Contracts/*/`
- `Common/StorageProvider/`
