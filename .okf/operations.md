---
type: Reference
title: Operations
description: Local runtime processes, ports, configuration, storage, messaging, jobs, and observability.
tags: [operations, runtime]
---

# Operations

## Runtime processes

Current deployable services:

| Service | Project | Local HTTP | IPC service name source |
| --- | --- | --- | --- |
| UserIdentity | `Services/UserIdentity/Services.UserIdentity.csproj` | `http://localhost:5000` | `Contracts.UserIdentity.Service.Name` |
| UserProfile | `Services/UserProfile/Services.UserProfile.csproj` | `http://localhost:5001` | `Contracts.UserProfile.Service.Name` |
| Notifications | `Services/Notifications/Services.Notifications.csproj` | none configured | `Contracts.Notifications.Service.Name` |

`UserProfile` exposes `GET /profiles/me` on its local HTTP port and may include a dummy root endpoint in `Program.cs`. `Notifications` includes a dummy root endpoint but does not expose public business APIs.

## Messaging topology

- Each service listens over FastEndpoints IPC via `ListenInterProcess(...)`.
- `UserIdentity` registers hubs for `UserIdentityRegisteredEvent`, `UserIdentityVerificationIssuedEvent`, and `UserIdentityVerifiedEvent`.
- `UserProfile` maps remote `UserIdentity` and subscribes to registered/verified identity events; it registers a hub for `UserProfileRegisteredEvent` (no known subscribers currently).
- `Notifications` maps remote `UserIdentity` and subscribes to `UserIdentityVerificationIssuedEvent` only, so verification secrets never enter Profile event storage.
- Remote event storage uses MongoDB-backed `EventRecord` through `Common/StorageProvider`.

## Databases

Default local MongoDB connection string: `mongodb://localhost:27017`.

| Service | Default database | Testing database | Main collections/indexes |
| --- | --- | --- | --- |
| UserIdentity | `HelpDesk_UserIdentity` | `HelpDesk_UserIdentity_TESTING` | `UserIdentities`; unique normalized email; sparse unique verification code; event records. |
| UserProfile | `HelpDesk_UserProfile` | `HelpDesk_UserProfile_TESTING` | `UserProfiles`; unique normalized email; user identity id index; event records. |
| Notifications | `HelpDesk_Notifications` | `HelpDesk_Notifications_TESTING` | event records; job records by queue/completion/schedule/expiry and tracking ID. |

## Configuration

Config files live under each service:

- `appsettings.json` - non-secret defaults.
- `appsettings.Development.json` - currently empty overrides.
- `appsettings.Testing.json` - testing database names.
- `Properties/launchSettings.json` - Development launch profile.

Important config keys:

- `ConnectionStrings:MongoDB`
- `UserIdentity:HttpPort`, `UserIdentity:DatabaseName`, `UserIdentity:Jwt:*`
- `UserProfile:HttpPort`, `UserProfile:DatabaseName`, `UserProfile:Jwt:PublicKey`, `UserProfile:Jwt:PrivateKey` (test JWT minting only)
- `Notifications:DatabaseName`
- `Smtp:*`
- `Logging:LogLevel:*`

Do not copy secret values into source or OKF.

## Email/jobs

- Notifications queues `SendEmailCommand` jobs when `UserIdentityVerificationIssuedEvent` arrives.
- Welcome email verification links use `UserIdentityVerificationIssuedEvent.BaseUrl` plus `/identities/verify/{escapedCode}`; `BaseUrl` originates from the incoming registration request's scheme/host/path base.
- Public/proxy host configuration for registration therefore affects emailed verification links.
- Job queue limits `SendEmailCommand` to max concurrency 1 and 2 minute time limit.
- SMTP delivery uses `SmtpService` only when environment is Production and `Smtp.Enabled` is true.
- Non-production or disabled SMTP uses `NullEmailSender`, which logs suppressed email delivery.
- SMTP failures log a warning, reconnect, retry once, then log an error and rethrow.
- Failed jobs are rescheduled one minute later by `JobStorageProvider`.

## Observability

- ASP.NET logging defaults are `Default=Information`, `Microsoft.AspNetCore=Warning`.
- Non-production `UserIdentity` and `UserProfile` map OpenAPI and Scalar UI.
- Notifications does not map OpenAPI/Scalar in current startup.

## Sources

- `Services/*/Program.cs`
- `Services/*/appsettings*.json`
- `Services/*/Properties/launchSettings.json`
- `Services/*/Persistence/*Database.cs`
- `Services/UserIdentity/Endpoints/Identities/Register/Endpoint.cs`
- `Services/Notifications/Subscriptions/UserIdentity/VerificationIssued/UserIdentityVerificationIssuedEventHandler.cs`
- `Services/Notifications/Email/*.cs`
- `Services/Notifications/Jobs/JobStorageProvider.cs`
