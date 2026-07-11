---
type: Reference
title: Dependencies
description: .NET 10 runtime, central package management, and key frameworks used by HelpDesk.
tags: [deps]
resource: Directory.Packages.props
---

# Dependencies

## Runtime

- **.NET / TFM:** `net10.0` on all projects
- **Hosts:** `Microsoft.NET.Sdk.Web` for services; class libs for Common/Contracts

## Packages

Central versions: `Directory.Packages.props` (`ManagePackageVersionsCentrally`).

Project references only declare package **names** (no versions) when using CPM.

## Key libraries

| Package | Role |
| --- | --- |
| FastEndpoints (+ Generator, OpenApi, Security, Testing) | HTTP endpoints, DI helpers, OpenAPI |
| FastEndpoints.Messaging.Core / Remote (+ Testing) | Events, hubs, IPC/remote mesh |
| MongoDB.Entities | Persistence + indexes |
| MailKit | SMTP (Notifications) |
| Scalar.AspNetCore | API reference UI (non-prod) |
| SixLabors.ImageSharp | Profile picture decode/resize/crop/encode (UserProfile) |
| Microsoft.OpenApi | OpenAPI docs |
| xunit.v3, Shouldly, Microsoft.NET.Test.Sdk | Tests |
| ASP.NET Core Identity `PasswordHasher<T>` | Password hashing (UserIdentity) |

## Constraints

- Keep FastEndpoints family versions aligned (currently `8.3.0-beta.10` in props)
- Do not add a message broker package for cross-service workflows without architecture change
- Prefer project refs: Services → Contracts/Common only
- Bump versions in `Directory.Packages.props`, not scattered csproj Version attributes

## Sources

- `Directory.Packages.props`
- `Services/*/*.csproj`
- `Common/*/*.csproj`
- `Contracts/*/*.csproj`
