---
type: Reference
title: Operations
description: Runtime processes, databases, messaging, API docs, notification delivery, configuration, and observability.
tags: [operations, configuration, runtime]
---

# Operations

## Runtime processes

Each service is a separate .NET web application:

| Service | Project | Transport/listeners |
| --- | --- | --- |
| UserIdentity | `Services/UserIdentity/Services.UserIdentity.csproj` | IPC `USER_IDENTITY_SERVICE`; HTTP localhost port from `UserIdentity:HttpPort` default `5000`. |
| UserProfile | `Services/UserProfile/Services.UserProfile.csproj` | IPC `USER_PROFILE_SERVICE`; HTTP localhost port from `UserProfile:HttpPort` default `5001`. |
| Notifications | `Services/Notifications/Services.Notifications.csproj` | IPC `NOTIFICATIONS_SERVICE`; no configured HTTP port. |

UserProfile and Notifications include dummy root endpoints to satisfy app shape, but they have no public business APIs currently.

## Databases

MongoDB is required for service state, event records, and notification jobs.

Default connection:

```text
mongodb://localhost:27017
```

Default database names:

- `HelpDesk_UserIdentity`
- `HelpDesk_UserProfile`
- `HelpDesk_Notifications`

Testing database names add `_TESTING` suffixes in `appsettings.Testing.json`.

Indexes are created on service startup by `*Database.InitializeAsync(...)`; there are no migration files.

## Messaging

- Current topology uses local IPC via `ListenInterProcess(...)` and `MapRemote(...)`.
- Event durability uses MongoDB-backed `EventRecord` through `Common/StorageProvider`.
- There is no central message broker.
- Deployment may change remote endpoints/topology, but business workflows should remain contract/event-based.

## HTTP/API docs

- UserIdentity exposes identity REST endpoints on port `5000` by default.
- UserProfile listens on HTTP port `5001` by default but currently has no public business API.
- UserIdentity and UserProfile map OpenAPI and Scalar when not Production.
- Notifications does not configure OpenAPI/Scalar.

## Notification delivery

- Notifications queues `SendEmailCommand` jobs in MongoDB-backed job storage.
- Job queue limit for `SendEmailCommand`: max concurrency `1`, time limit `2 minutes`.
- `JobStorageProvider.DistributedJobProcessingEnabled` is currently `false`; notification job processing is not configured for distributed competing workers.
- Production with `Smtp:Enabled=true` uses `SmtpService`.
- Non-production or disabled SMTP uses `NullEmailSender`.
- SMTP settings live under `Smtp:*` and must be supplied securely for real email delivery.

## Configuration and secrets

Important sections:

- `ConnectionStrings:MongoDB`
- `UserIdentity:Jwt:Issuer`, `Audience`, `AccessTokenDays`, `PrivateKeyPem`
- `Smtp:Enabled`, `Host`, `Port`, `UseSsl`, `Username`, `Password`, sender fields
- `Smtp:AdminName` and `Smtp:AdminEmail` exist in committed config but are currently unused/reserved by delivery code.

Do not commit secrets. The repo ignores `.env`; use environment variables, user secrets, or deployment secret storage.

## Observability

Only standard ASP.NET Core logging configuration is present in appsettings. No metrics, tracing, external logging sinks, health checks, Docker, Kubernetes, or CI deployment config exists in the repo at OKF initialization time.
