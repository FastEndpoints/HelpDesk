---
type: Database
title: Database
description: Per-service MongoDB databases, collections, indexes, and Aspire local provisioning.
tags: [data]
---

# Database

## Model

- **Engine:** MongoDB via MongoDB.Entities (`DB.InitAsync`, `DB.Default`)
- **Isolation:** one database per service (config `DatabaseName`)
- **No shared domain collections** across services
- Event storage, and Notifications job storage, are co-located in each owning service database

## Local Aspire database

`backend/AppHost/Program.cs` provisions an ephemeral authenticated standalone MongoDB container with committed development username/password parameters on `localhost:27017`. Aspire injects its connection as the `MongoDB` reference into all three services; the fixed development endpoint also lets repository tests use the same running resource.

The local resource intentionally has:

- no replica set or transaction support;
- no keyfile;
- no host volume or persistence guarantee;
- no Compose or root `.env` configuration;
- fixed development host port `27017` for direct MongoDB-backed test commands.

Read connection details from the Aspire dashboard. Container replacement/removal loses local data. These local topology constraints do not define production MongoDB deployment requirements.

## Production persistence

Production Compose runs private `mongo:8.0` with authentication and the `mongodb_data` named volume. All backend processes use the externally supplied `ConnectionStrings__MongoDB`; committed development credentials must not be reused. Profile files live at `/data/profile-pictures` on the `profile_pictures` named volume. Removing Compose volumes destroys this data.

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
| UserProfile | `UserProfileEntity` | `UserProfiles` | Unique `NormalizedEmail`; index `UserIdentityId`; display name; optional picture object key; status; EmailVerified |
| Shared pattern | `EventRecord` | type default | Compound index EventType, SubscriberID, IsComplete, ExpireOn |
| Notifications | `JobRecord` | type default | Queue/complete/execute/expire indexes; TrackingID index |

## Init

`*Database.InitializeAsync` runs after `DB.InitAsync` in each service `Program.cs`. Static constructors register BSON object/guid serializers. There is no schema migration framework; index creation is idempotent startup work.

## Sources

- `backend/AppHost/Program.cs`
- `backend/Services/*/Persistence/*Database.cs`
- `backend/Services/*/Persistence/*Entity.cs`
- `backend/Common/StorageProvider/EventRecord.cs`
- `backend/Services/Notifications/Jobs/JobRecord.cs`
