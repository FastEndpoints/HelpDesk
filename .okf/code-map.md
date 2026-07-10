---
type: Reference
title: Code Map
description: Top-level and per-service layout for Common, Contracts, and Services projects.
tags: [layout]
resource: HelpDesk.slnx
---

# Code Map

## Layout

```text
HelpDesk/
├── Common/
│   ├── StorageProvider/     # EventRecord, EventStorageProvider
│   └── Tools/               # StringExtensions, AuthGroups (permission group names)
├── Contracts/
│   ├── UserIdentity/        # Service, Events, EventSubscribers
│   ├── UserProfile/
│   └── Notifications/       # Service name only (no events yet)
├── Services/
│   ├── UserIdentity/
│   ├── UserProfile/
│   └── Notifications/
├── Directory.Packages.props # central package versions
├── HelpDesk.slnx
└── README.md
```

## Modules

| Path | Contents |
| --- | --- |
| `Common/StorageProvider` | Mongo-backed FE remote event hub/subscriber storage |
| `Common/Tools` | Generic helpers; `AuthGroups` (mesh permission group name constants) |
| `Services/UserProfile/Auth/` | `PermissionClaimsTransformation` (role groups → local Allow codes) |
| `Contracts/<Name>` | `Service.cs`, `Events.cs`, `EventSubscribers.cs` as needed |
| `Services/<Name>/Program.cs` | Host, Kestrel IPC/HTTP, DI, hubs, MapRemote |
| `Services/<Name>/Meta.cs` | Global usings |
| `Services/<Name>/Configuration/` | Typed settings bound from configuration |
| `Services/<Name>/Endpoints/` | Public REST (area/action folders) |
| `Services/<Name>/Persistence/` | Entities, stores, DB init |
| `Services/<Name>/Subscriptions/<Publisher>/<Event>/` | Handlers + Tests |
| `Services/<Name>/Tests/` | `Sut` fixture, assembly Meta, xunit.runner.json |
| `Services/Notifications/Email/`, `Jobs/` | SMTP/null sender, job storage |

## Entry points

| Service | Host entry | HTTP (local) | IPC name |
| --- | --- | --- | --- |
| UserIdentity | `Services/UserIdentity/Program.cs` | port from `UserIdentity:HttpPort` (5000) | `USER_IDENTITY_SERVICE` |
| UserProfile | `Services/UserProfile/Program.cs` | `UserProfile:HttpPort` (5001) | `USER_PROFILE_SERVICE` |
| Notifications | `Services/Notifications/Program.cs` | IPC only (no public HTTP listen in Program) | `NOTIFICATIONS_SERVICE` |

IDE multi-run: `.run/RunAll.run.xml` launches all three profiles.

## Generated code

- FastEndpoints.Generator source generators (reflection cache e.g. `AddFromServicesUserIdentity()`). Do not hand-edit generated outputs; regenerate via build.
- Tests excluded from Release builds (`Compile Remove="**\Tests\**"`).

## Sources

- `HelpDesk.slnx`
- `Services/*/Program.cs`
- `Services/*/*.csproj`
