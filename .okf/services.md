---
type: Service
title: Services
description: Responsibilities, publish/subscribe map, and project refs for UserIdentity, UserProfile, and Notifications.
tags: [architecture]
---

# Services

## UserIdentity (`USER_IDENTITY_SERVICE`)

- **Project:** `backend/Services/UserIdentity`
- **Owns:** identity credentials, status, verification codes, JWT issuance, identity events
- **Refs:** `Contracts.UserIdentity`, `Common.StorageProvider`, `Common.Tools`
- **Publishes:** `UserIdentityRegisteredEvent`, `UserIdentityVerificationIssuedEvent`, `UserIdentityVerifiedEvent`
- **Subscribes:** none currently
- **REST:** register, login, resend-verification, verify

## UserProfile (`USER_PROFILE_SERVICE`)

- **Project:** `backend/Services/UserProfile`
- **Owns:** profile entity lifecycle, display name, and profile pictures (local filesystem + static URL; no Media service)
- **Refs:** `Contracts.UserProfile`, `Contracts.UserIdentity`, `Common.StorageProvider`, `Common.Tools`
- **Packages:** SixLabors.ImageSharp (300×300 center-crop encode)
- **Publishes:** `UserProfileRegisteredEvent` (hub registered; subscriber list empty today)
- **Subscribes:** `UserIdentityRegisteredEvent` → create deactivated profile; `UserIdentityVerifiedEvent` → activate by `UserIdentityId` + `EmailVerified`
- **REST:** authenticated current profile read/update + picture upload/delete; public static `/profile-pictures`

## Notifications (`NOTIFICATIONS_SERVICE`)

- **Project:** `backend/Services/Notifications`
- **Owns:** email templates, SMTP/null sender, durable email jobs
- **Job limits:** non-distributed processing; `SendEmailCommand` concurrency 1 per process with a 2-minute execution limit; multiple instances are not coordinated; handler failures reschedule the job one minute later
- **Job idempotency:** `JobRecord` implements `IHasIdempotencyKey`; unique partial index on `(QueueID, IdempotencyKey)` while the row exists (including completed); `StoreJobAsync` maps `MongoWriteException` duplicate-key to `DuplicateJobException` with the existing `TrackingID` (null/whitespace keys rethrow). `IdempotencyKeyFor<SendEmailCommand>` uses `SendEmailCommand.IdempotencyKey`. Verification emails use `UserIdentityVerificationIssuedEventHandler.JobIdempotencyKey` → `user-identity-verification:{UserIdentityId}:{VerificationCode}` so duplicate deliveries of the same issued event enqueue once, while a resend that rotates the code can queue another email.
- **Refs:** `Contracts.Notifications`, `Contracts.UserIdentity`, `Common.StorageProvider`
- **Publishes:** none
- **Subscribes:** `UserIdentityVerificationIssuedEvent` → queue `SendEmailCommand`
- **REST:** no business API (dummy root endpoint present)

## Production process topology

Production Compose publishes the three service projects into one Ubuntu Chiseled .NET 10 backend image. `backend/Deployment/BackendLauncher` is PID 1: it starts all service DLLs, forwards SIGINT/SIGTERM, terminates siblings when one exits, and reports unexpected child exits as failure. Identity/Profile bind private container ports 8080/8081 in Production; Notifications remains IPC-only. This co-location preserves host-local FastEndpoints IPC and does not alter service project-reference boundaries.

## Mesh edges

```text
UserIdentity  --UserIdentityRegisteredEvent-->  UserProfile
UserIdentity  --UserIdentityVerificationIssuedEvent-->  Notifications
UserIdentity  --UserIdentityVerifiedEvent-->  UserProfile
UserProfile   --UserProfileRegisteredEvent-->  (no subscribers yet)
```

Subscriber ID arrays: `backend/Contracts/*/EventSubscribers.cs` must match consumer `Service.Name`.

## Sources

- `backend/Services/*/Program.cs`
- `backend/Contracts/*/`
- `README.md`
