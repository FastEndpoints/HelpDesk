---
type: Reference
title: Code Map
description: Repository layout, important project locations, and edit guidance.
tags: [code-map, navigation]
---

# Code Map

## Top-level layout

| Path | Purpose |
| --- | --- |
| `README.md` | Canonical architecture and workflow overview. |
| `AGENTS.md` | Repo-local agent instructions, including OKF use/maintenance. |
| `.okf/` | Operational knowledge for agents. |
| `HelpDesk.slnx` | Solution file listing all projects. |
| `Directory.Packages.props` | Central NuGet package versions. |
| `Common/` | Reusable infrastructure/helpers only after they are truly generic. |
| `Contracts/` | Public cross-service service names/events/DTOs. |
| `Services/` | Deployable service implementations. |
| `.run/RunAll.run.xml` | JetBrains multi-launch config for all current services. |

## Projects

| Path | Type | Notes |
| --- | --- | --- |
| `Common/StorageProvider/Common.StorageProvider.csproj` | class library | MongoDB event storage provider and `EventRecord`. |
| `Common/Tools/Common.Tools.csproj` | class library | `NormalizeForLookup()` helper. |
| `Contracts/UserIdentity/Contracts.UserIdentity.csproj` | class library | UserIdentity service name and identity events. |
| `Contracts/UserProfile/Contracts.UserProfile.csproj` | class library | UserProfile service name and profile events. |
| `Contracts/Notifications/Contracts.Notifications.csproj` | class library | Notifications service name; no events currently. |
| `Services/UserIdentity/Services.UserIdentity.csproj` | web app | Identity endpoints, auth, persistence, event hubs, tests. |
| `Services/UserProfile/Services.UserProfile.csproj` | web app | Authenticated profile endpoint, profile persistence, identity subscriptions, profile event hub, tests. |
| `Services/Notifications/Services.Notifications.csproj` | web app | Notification settings/email/jobs/subscription, tests. |

## Service layout

Typical service directories:

- `Program.cs` - startup, DI, Kestrel IPC/HTTP, event hubs/subscriptions.
- `Meta.cs` - service-level global usings/metadata.
- `Configuration/` - strongly typed settings classes.
- `appsettings*.json` - service-local configuration.
- `Properties/launchSettings.json` - development launch profile.
- `Endpoints/<Area>/<Action>/` - public REST endpoints owned by the service.
- `Persistence/` - private entities, stores, database/index initialization.
- `Subscriptions/<Publisher>/<Event>/` - event handlers and colocated tests.
- `Tests/` - shared service-local fixtures and xUnit config.
- Service-specific folders such as `Email/` and `Jobs/` stay inside the owning service.

## Where to add behavior

- New public API behavior: owning service under `Services/<Service>/Endpoints/...` plus colocated `Tests/`.
- New event contract: owning `Contracts/<Service>/` project.
- New event publication: owning service after local persistence succeeds.
- New subscription: consuming service under `Subscriptions/<Publisher>/<Event>/` plus `Program.cs` `MapRemote(...)` registration.
- Reusable infrastructure: `Common/` only when generic and not domain behavior.

## Edit guidance

- Do not edit build outputs under `bin/` or `obj/` if present.
- Do not move service-local persistence/entities into contracts or common projects.
- Do not create empty service folders just to match a template.
- Release builds remove `**/Tests/**` from service web projects via csproj conditions.

## Sources

- `README.md`
- `HelpDesk.slnx`
- `*.csproj`
- `Services/*/Program.cs`
- `Services/*/Tests/Sut.cs`