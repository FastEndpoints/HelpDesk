---
type: Reference
title: Code Map
description: Repository layout, project locations, important modules, generated files, and edit guidance.
tags: [code-map, layout, navigation]
---

# Code Map

## Top-level layout

| Path | Purpose |
| --- | --- |
| `README.md` | Canonical architecture and workflow documentation. |
| `HelpDesk.slnx` | Solution file listing Common, Contracts, and Services projects. |
| `Directory.Packages.props` | Central NuGet package versions. |
| `Common/StorageProvider/` | Shared MongoDB storage provider for FastEndpoints remote event infrastructure. |
| `Common/Tools/` | Generic helpers, currently string lookup normalization. |
| `Contracts/` | Public contract projects by service. |
| `Services/` | Independently deployable service implementations. |

## Projects

| Project | Purpose |
| --- | --- |
| `Common/StorageProvider/Common.StorageProvider.csproj` | Event storage over MongoDB for remote messaging. |
| `Common/Tools/Common.Tools.csproj` | Generic helper library. |
| `Contracts/UserIdentity/Contracts.UserIdentity.csproj` | UserIdentity service name and identity events. |
| `Contracts/UserProfile/Contracts.UserProfile.csproj` | UserProfile service name and profile events. |
| `Contracts/Notifications/Contracts.Notifications.csproj` | Notifications service name. |
| `Services/UserIdentity/Services.UserIdentity.csproj` | FastEndpoints identity service. |
| `Services/UserProfile/Services.UserProfile.csproj` | FastEndpoints profile service. |
| `Services/Notifications/Services.Notifications.csproj` | FastEndpoints notification/job service. |

## Service layout

Typical folders under `Services/<Service>`:

| Path | Purpose |
| --- | --- |
| `Program.cs` | Startup, DI, Kestrel listeners, event hub/subscription mapping. |
| `Meta.cs` | Service-local global usings. |
| `Configuration/` | Strongly typed appsettings classes. |
| `Endpoints/` | Public REST endpoints owned by the service. UserIdentity has identity endpoints. |
| `Persistence/` | Private MongoDB entities, stores, database/index initialization. |
| `Subscriptions/<Publisher>/<Event>/` | Event handlers for consumed contract events. |
| `Jobs/` | Notifications durable job records/storage. |
| `Email/` | Notifications email abstractions, templates, SMTP/null sender. |
| `Tests/` | Shared service-local test fixture/config; behavior tests are colocated under endpoint/subscription folders. |
| `appsettings*.json` | Service runtime/test configuration. |
| `Properties/launchSettings.json` | Local launch profiles. |

Do not force empty folders into every service; match the service responsibility.

## Important locations

- Identity endpoints: `Services/UserIdentity/Endpoints/Identities/{Register,Login,Verify}/`.
- Identity subscription consumers: `Services/UserProfile/Subscriptions/UserIdentity/{Registration,Verification}/`.
- Profile registration consumer: `Services/Notifications/Subscriptions/UserProfile/Registration/`.
- Event contracts: `Contracts/UserIdentity/Events.cs`, `Contracts/UserProfile/Events.cs`.
- Service names: `Contracts/*/Service.cs`.
- MongoDB indexes: `Services/*/Persistence/*Database.cs`.
- Local config: `Services/*/appsettings.json` and `appsettings.Testing.json`.

## Generated and ignored files

- `bin/`, `obj/`, build outputs, coverage outputs, user files, `.env`, and `Generated Files/` are ignored by `.gitignore`.
- FastEndpoints generator outputs are build artifacts; edit source files, not generated outputs.
- Release builds remove `**/Tests/**` from service projects.

## Edit guidance

- Add behavior inside the owning service project.
- Add cross-service facts to the owning contract project.
- Add reusable infrastructure to `Common/` only when it is genuinely generic and domain-free.
- Keep tests beside the endpoint/subscription behavior being changed.
