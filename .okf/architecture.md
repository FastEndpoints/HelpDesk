---
type: Architecture
title: Architecture
description: Service boundaries, event mesh communication, persistence, security, and invariants.
tags: [architecture, boundaries, events]
---

# Architecture

## Style

Brokerless, event-driven microservice mesh. Services are independently deployable FastEndpoints apps. Cross-service business communication uses FastEndpoints remote events over local IPC in development; deployment can change topology without changing business/event code.

```text
External clients -> service-owned HTTP endpoints
Services         -> contract events over FastEndpoints remote messaging
Storage          -> service-owned MongoDB databases + event/job records
Broker           -> none
Internal RPC     -> forbidden for business workflows
```

## Components

| Area | Projects | Responsibility |
| --- | --- | --- |
| `Contracts/` | `Contracts.UserIdentity`, `Contracts.UserProfile`, `Contracts.Notifications` | Stable service names, public event contracts, and data-only `EventSubscribers` known-subscriber arrays. |
| `Common/StorageProvider` | class library | MongoDB-backed `IEventHubStorageProvider`/`IEventSubscriberStorageProvider` using `EventRecord`. |
| `Common/Tools` | class library | Generic helpers, currently lookup string normalization. |
| `Services/UserIdentity` | web app | Identity HTTP API, credentials/status persistence, JWTs, identity events. |
| `Services/UserProfile` | web app | Profile persistence and reactions to identity events. |
| `Services/Notifications` | web app | Identity verification-issued reaction, durable email jobs, SMTP/null email sending. |

## Dependency rules

Allowed reference direction:

```text
Services -> Contracts
Services -> Common
Contracts -> package references only
Common -> package references only
```

Forbidden:

- `Services/<A>` referencing `Services/<B>`.
- Contract projects containing endpoints, handlers, entities, stores, persistence, SMTP/email implementation, or service-local business logic.
- Shared domain models in `Common/` or `Contracts/`.

## Communication rules

- Cross-service workflows use events only.
- Events are facts that already happened, not commands.
- Publish events only after the owning service commits local state.
- Subscribers update only their own local state or queue internal work.
- Current local topology uses `ListenInterProcess(Service.Name)` and `MapRemote(Service.Name, ...)`.
- Subscribers set `c.SubscriberID = SubscriberService.Name` inside `MapRemote(...)`, then call `c.Subscribe<TEvent, THandler>()`, so subscriber IDs are stable and match contract service names.
- Known subscriber IDs for durable startup/offline delivery live as data-only `string[]` fields on the publisher contract's `EventSubscribers` type (string literals matching subscriber `Service.Name`; no contract→contract references).
- Publisher services register hubs with those arrays: `RegisterEventHub<TEvent>(EventSubscribers.SomeEvent)`.
- When adding a new subscriber, update the publisher contract `EventSubscribers` array and the subscriber's process-local `MapRemote(...)` registration (`SubscriberID` + `Subscribe`).

## Persistence rules

- Each service initializes and owns its MongoDB database name from its settings.
- `UserIdentity` stores `UserIdentities`, with unique normalized email and sparse unique verification code indexes.
- `UserProfile` stores `UserProfiles`, with unique normalized email and user identity id indexes.
- Notifications stores FastEndpoints job records and event records.
- Every service initializes an `EventRecord` index for remote event subscriber storage.
- Persistence classes/entities/stores stay private to the owning service.

## Security/auth model

- `UserIdentity` hashes passwords with ASP.NET Core Identity `IPasswordHasher<UserIdentityEntity>`.
- Login issues asymmetric JWT bearer tokens using `UserIdentity.Jwt.PrivateKeyPem`, issuer, audience, and expiry settings.
- `UserProfile` validates JWT bearer tokens with `UserProfile.Jwt.PublicKey` and uses the token subject to load local profile state.
- Registration/login/verification endpoints allow anonymous access; `GET /profiles/me` requires authentication.
- Do not commit real JWT private keys, SMTP credentials, or MongoDB credentials.

## Invariants agents must preserve

- Keep service implementation projects isolated; use contract projects for cross-service language.
- Keep internal workflows event-driven and brokerless unless architecture is intentionally changed and OKF/README are updated.
- Keep events small, public, and owned by the publishing service's contract project.
- Keep service-local state changes before event publication.
- Keep tests colocated with the endpoint/subscription behavior they verify.

## Sources

- `README.md`
- `HelpDesk.slnx`
- `Services/*/Program.cs`
- `Services/*/Persistence/*Database.cs`
- `Services/UserIdentity/Endpoints/Identities/*/Endpoint.cs`
- `Contracts/*/Events.cs`