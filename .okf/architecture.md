---
type: Architecture
title: Architecture
description: Brokerless FastEndpoints mesh with isolated services, a SvelteKit BFF, and Aspire local orchestration.
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
- **AppHost** = development orchestration, not a business service
- **Broker** = none; **business RPC** = none

Current service topology is host-local and hardcoded in startup: `ListenInterProcess(Service.Name)` + `MapRemote(publisherName, …)`, with Identity/Profile HTTP listeners bound through `ListenLocalhost`. No deployment manifest implements a network transport or multi-host mesh.

## Local resource graph

`backend/AppHost/Program.cs` is the sole supported local full-stack orchestrator. Aspire 13.4.6 starts an ephemeral authenticated standalone MongoDB resource, then Identity, Profile, Notifications, and Vite. Dependencies enforce MongoDB-before-services and Identity/Profile-before-frontend ordering.

Aspire assigns application HTTP ports dynamically. It injects the MongoDB connection into each service and the Identity/Profile HTTP endpoints into Vite as private `IDENTITY_API_BASE_URL` / `PROFILE_API_BASE_URL` values. The AppHost is local tooling; backend services remain independently deployable.

## Monorepo and external-client boundary

`frontend/` is the SvelteKit adapter-node application; `backend/` contains the AppHost, .NET solution, and Common/Contracts/Services layers. Browsers use SvelteKit as a BFF. Only server modules under `frontend/src/lib/server/api/` know the private Identity/Profile origins and attach server-held bearer tokens. Their shared client middleware converts every backend non-success response to `ApiError`, preserving RFC problem details and the HTTP status.

## Components

```text
External client
  │ REST
  ▼
SvelteKit BFF
  │ private REST
  ├──────────────► UserIdentity ──events──► UserProfile
  │                     │                       │
  └──────────────► UserProfile                  └─ own persistence
                        └──events──────────► Notifications

Aspire AppHost: local lifecycle, endpoint discovery, and configuration injection
```

| Layer | Projects | Role |
| --- | --- | --- |
| AppHost | `HelpDesk.AppHost` | Local resource graph and orchestration |
| Common | `StorageProvider`, `Tools` | Event storage provider; helpers and permission group names |
| Contracts | `UserIdentity`, `UserProfile`, `Notifications` | `Service.Name`, events, `EventSubscribers` |
| Services | same three names | Endpoints, persistence, hubs, subscriptions |

## Dependency rules

**Allowed:**

```text
AppHost → service host projects
Services → Contracts
Services → Common
Contracts/Common → package refs only
```

**Forbidden:**

```text
backend/Services/A → backend/Services/B
Contracts → Services or domain logic
Common → domain / service-specific behavior
```

Cross-service business flow is events only. A service may expose REST for external clients; it must not call another service’s REST to finish an internal workflow. Events are facts after local commit, not commands.

## Communication and persistence

- Publisher: persist locally → `event.Broadcast()`; hubs registered through `MapHandlers` + `RegisterEventHub<TEvent>(subscriberIds)`.
- Subscriber: `MapRemote(publisherServiceName, …)` with stable service-name subscriber IDs.
- Durable event records: `Common.StorageProvider.EventRecord` + `EventStorageProvider` (MongoDB.Entities).
- Notifications internal work: FastEndpoints job queue for `SendEmailCommand`.
- Each service owns its MongoDB database; no shared domain collections.

## Security / auth boundaries

- UserIdentity issues RSA JWTs; UserProfile validates them with the matching public value.
- Matching development-only private/public values are committed in base appsettings so the Aspire stack runs directly; environment variables may override both.
- The BFF holds JWTs in an HttpOnly session cookie and never exposes backend origins publicly.
- Notifications has no public business API / JWT surface today.
- Details: [security.md](security.md).

## Invariants

1. Own state committed before publishing related events.
2. Subscribers mutate only own state / queue own jobs; no callback to publisher.
3. Contracts stay free of endpoints, entities, stores, SMTP, handlers.
4. Service names and `EventSubscribers` arrays stay aligned with real consumer `Service.Name` values.
5. Tests prove REST boundaries and subscription reactions; topology-agnostic.
6. Local full-stack startup flows through the AppHost; do not reintroduce parallel orchestration paths.

## Sources

- `README.md`
- `backend/AppHost/Program.cs`
- `backend/Services/*/Program.cs`
- `backend/Contracts/*/`
- `backend/Common/StorageProvider/`
