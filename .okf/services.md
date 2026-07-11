---
type: Service
title: Services
description: Responsibilities, publish/subscribe map, and project refs for UserIdentity, UserProfile, and Notifications.
tags: [architecture]
---

# Services

## UserIdentity (`USER_IDENTITY_SERVICE`)

- **Project:** `Services/UserIdentity`
- **Owns:** identity credentials, status, verification codes, JWT issuance, identity events
- **Refs:** `Contracts.UserIdentity`, `Common.StorageProvider`, `Common.Tools`
- **Publishes:** `UserIdentityRegisteredEvent`, `UserIdentityVerificationIssuedEvent`, `UserIdentityVerifiedEvent`
- **Subscribes:** none currently
- **REST:** register, login, verify

## UserProfile (`USER_PROFILE_SERVICE`)

- **Project:** `Services/UserProfile`
- **Owns:** profile entity lifecycle, display name, and profile pictures (local filesystem + static URL; no Media service)
- **Refs:** `Contracts.UserProfile`, `Contracts.UserIdentity`, `Common.StorageProvider`, `Common.Tools`
- **Packages:** SixLabors.ImageSharp (300×300 center-crop encode)
- **Publishes:** `UserProfileRegisteredEvent` (hub registered; subscriber list empty today)
- **Subscribes:** `UserIdentityRegisteredEvent` → create deactivated profile; `UserIdentityVerifiedEvent` → activate by email + `EmailVerified`
- **REST:** authenticated current profile read/update + picture upload/delete; public static `/profile-pictures`

## Notifications (`NOTIFICATIONS_SERVICE`)

- **Project:** `Services/Notifications`
- **Owns:** email templates, SMTP/null sender, durable email jobs
- **Refs:** `Contracts.Notifications`, `Contracts.UserIdentity`, `Common.StorageProvider`
- **Publishes:** none
- **Subscribes:** `UserIdentityVerificationIssuedEvent` → queue `SendEmailCommand`
- **REST:** no business API (dummy root endpoint present)

## Mesh edges

```text
UserIdentity  --UserIdentityRegisteredEvent-->  UserProfile
UserIdentity  --UserIdentityVerificationIssuedEvent-->  Notifications
UserIdentity  --UserIdentityVerifiedEvent-->  UserProfile
UserProfile   --UserProfileRegisteredEvent-->  (no subscribers yet)
```

Subscriber ID arrays: `Contracts/*/EventSubscribers.cs` must match consumer `Service.Name`.

## Sources

- `Services/*/Program.cs`
- `Contracts/*/`
- `README.md`
