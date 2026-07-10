---
type: Event
title: Events
description: Contract events, hubs, and subscribers for the onboarding mesh.
tags: [api]
resource: Contracts/UserIdentity/Events.cs
---

# Events

All business events implement `IEvent` and live in Contracts of the **owning** service.

## UserIdentity (`Contracts.UserIdentity`)

| Event | Payload (fields) | Hub subscribers (`EventSubscribers`) |
| --- | --- | --- |
| `UserIdentityRegisteredEvent` | `UserIdentityId`, `Email`, `RegisteredAt` | `USER_PROFILE_SERVICE` |
| `UserIdentityVerificationIssuedEvent` | `UserIdentityId`, `Email`, `VerificationCode`, `BaseUrl`, `IssuedAt` | `NOTIFICATIONS_SERVICE` |
| `UserIdentityVerifiedEvent` | `UserIdentityId`, `Email`, `VerifiedAt` | `USER_PROFILE_SERVICE` |

Published from register/verify endpoints after persistence (register also issues verification event).

## UserProfile (`Contracts.UserProfile`)

| Event | Payload | Hub subscribers |
| --- | --- | --- |
| `UserProfileRegisteredEvent` | `UserProfileId`, `Email`, `DisplayName`, `RegisteredAt` | `[]` (none yet) |

Published from `UserIdentityRegisteredEventHandler` after profile create.

## Notifications

No owned events currently (`Contracts.Notifications` has `Service.Name` only).

## Handler map

| Handler | Listens | Side effect |
| --- | --- | --- |
| `UserIdentityRegisteredEventHandler` | Registered | Create deactivated profile; broadcast profile registered |
| `UserIdentityVerifiedEventHandler` | Verified | Activate profile by normalized email; set EmailVerified |
| `UserIdentityVerificationIssuedEventHandler` | VerificationIssued | Queue welcome email with verify link |

## Adding an event

1. Add record to owner contract → `IEvent`
2. Extend `EventSubscribers` with consumer service names
3. `RegisterEventHub` on publisher
4. Publish only after local commit
5. Consumer: contract ref, handler, `MapRemote` subscribe, tests

## Sources

- `Contracts/UserIdentity/Events.cs`
- `Contracts/UserIdentity/EventSubscribers.cs`
- `Contracts/UserProfile/Events.cs`
- `Services/*/Program.cs`
- `Services/*/Subscriptions/**`
