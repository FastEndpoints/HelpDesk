---
type: Reference
title: Architecture
description: Brokerless event-driven service architecture, dependency rules, persistence, and invariants.
tags: [architecture, messaging, persistence]
---

# Architecture

## Style

HelpDesk uses a brokerless event-driven microservice mesh. Each service is a self-contained FastEndpoints web project with local MongoDB persistence. Cross-service business workflows use events through `FastEndpoints.Messaging.Remote`, not direct HTTP/RPC calls and not a central broker.

```text
External clients -> UserIdentity REST API
UserIdentity -> UserIdentity events -> UserProfile
UserProfile -> UserProfile events -> Notifications
Notifications -> email job queue -> IEmailSender
```

## Components

- `Common/StorageProvider`: MongoDB-backed storage for FastEndpoints remote event hubs/subscribers (`EventRecord`, `EventStorageProvider`).
- `Common/Tools`: Generic helpers, currently lookup string normalization.
- `Contracts/UserIdentity`: `USER_IDENTITY_SERVICE`, `UserIdentityRegisteredEvent`, `UserIdentityVerifiedEvent`.
- `Contracts/UserProfile`: `USER_PROFILE_SERVICE`, `UserProfileRegisteredEvent`.
- `Contracts/Notifications`: `NOTIFICATIONS_SERVICE`; no events currently.
- `Services/UserIdentity`: identity API and identity MongoDB data.
- `Services/UserProfile`: profile MongoDB data and identity event subscriptions.
- `Services/Notifications`: profile event subscription, durable email jobs, SMTP/null email sender.

## Dependency rules

Allowed direction:

```text
Services -> Contracts
Services -> Common
Contracts -> package references only
Common -> package references only
```

Forbidden:

```text
Services/UserProfile -> Services/UserIdentity
Services/Notifications -> Services/UserProfile
service A REST/RPC call -> service B for internal workflow
Contracts -> persistence, endpoints, stores, handlers, SMTP, service-local logic
Common -> service domain behavior
```

A service may reference another service's contract project only to consume that service's public events or DTOs.

## Communication rules

- External clients may call public REST endpoints.
- Internal cross-service workflows must be represented as event facts that already happened.
- Events are published only after the owning service commits local state.
- Subscribers update only their own local state or queue their own internal work.
- Current local topology uses IPC: publisher `ListenInterProcess(...)`; subscriber `MapRemote(...)`.
- Business code should remain valid if deployment later changes IPC to separate process/container/machine remote targets.

## Persistence rules

- Each service owns its MongoDB database and private persistence model.
- Default MongoDB connection string is `mongodb://localhost:27017`.
- Default databases: `HelpDesk_UserIdentity`, `HelpDesk_UserProfile`, `HelpDesk_Notifications`.
- Testing databases use `_TESTING` suffixes from `appsettings.Testing.json`.
- UserIdentity stores `UserIdentities` with unique indexes on `NormalizedEmail` and sparse unique `VerificationCode`.
- UserProfile stores `UserProfiles` with unique `NormalizedEmail`.
- Event storage indexes `EventRecord` by event type, subscriber id, completion, and expiry.
- Notifications stores `JobRecord` queue records indexed for queue processing and tracking id.
- No migration system exists in the repo; indexes are created during service startup initialization.

## Security/auth model

- UserIdentity registration, login, and verification endpoints are anonymous.
- Login validates password hash with ASP.NET Core Identity password hashing and requires active identity status.
- Successful login creates an asymmetric RSA JWT using `UserIdentity:Jwt` settings.
- `UserIdentity:Jwt:PrivateKeyPem` is empty in committed appsettings and must be configured securely for JWT issuance.
- Do not commit secrets; use configuration/user-secrets/environment-specific secret management.

## Invariants

- Keep service internals private to `Services/<ServiceName>`.
- Keep contracts small and implementation-free.
- Preserve event facts and event publication-after-persistence behavior.
- Preserve normalized email lookup semantics (`Trim().ToUpperInvariant()`).
- Preserve service-owned tests near the endpoints/subscriptions they verify.
- Do not introduce a shared domain model or common domain package.
