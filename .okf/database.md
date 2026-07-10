---
type: Database
title: Database
description: Per-service MongoDB databases, collections, and startup indexes.
tags: [data]
---

# Database

## Model

- **Engine:** MongoDB via MongoDB.Entities (`DB.InitAsync`, `DB.Default`)
- **Isolation:** one database per service (config `DatabaseName`)
- **No shared domain collections** across services
- Event (and Notifications job) storage co-located in each service’s DB

## Databases

| Service | Production default | Testing default |
| --- | --- | --- |
| UserIdentity | `HelpDesk_UserIdentity` | `HelpDesk_UserIdentity_TESTING` |
| UserProfile | `HelpDesk_UserProfile` | `HelpDesk_UserProfile_TESTING` |
| Notifications | `HelpDesk_Notifications` | `HelpDesk_Notifications_TESTING` |

## Collections / entities

| Service | Entity | Collection | Notable fields / indexes |
| --- | --- | --- | --- |
| UserIdentity | `UserIdentityEntity` | `UserIdentities` | Unique `NormalizedEmail`; unique sparse `VerificationCode`; password hash; status |
| UserProfile | `UserProfileEntity` | `UserProfiles` | Unique `NormalizedEmail`; index `UserIdentityId`; status; EmailVerified |
| Shared pattern | `EventRecord` | (MongoDB.Entities default for type) | Compound index EventType, SubscriberID, IsComplete, ExpireOn |
| Notifications | `JobRecord` | (type default) | Queue/complete/execute/expire indexes; TrackingID index |

## Init

`*Database.InitializeAsync` runs after `DB.InitAsync` in each `Program.cs`. Static ctors register BSON object/guid serializers.

No schema migration framework—index create is idempotent startup work.

## Sources

- `Services/*/Persistence/*Database.cs`
- `Services/*/Persistence/*Entity.cs`
- `Common/StorageProvider/EventRecord.cs`
- `Services/Notifications/Jobs/JobRecord.cs`
