---
type: Reference
title: Dependencies
description: Runtime, package management, key libraries, and compatibility notes.
tags: [dependencies, dotnet]
---

# Dependencies

## Runtime and language

- Target framework: `net10.0` for all projects.
- C# nullable reference types enabled.
- Implicit usings enabled.
- Web service projects use `Microsoft.NET.Sdk.Web`; common/contracts use `Microsoft.NET.Sdk`.

## Package management

- NuGet versions are centrally managed in `Directory.Packages.props` with `ManagePackageVersionsCentrally=true`.
- Project files reference packages without versions.
- Update package versions in `Directory.Packages.props`, not individual csproj files.

## Key packages

| Package | Version | Use |
| --- | --- | --- |
| `FastEndpoints` | `8.3.0-beta.10` | Endpoint framework and event abstractions. |
| `FastEndpoints.Generator` | `8.3.0-beta.10` | FastEndpoints source generation/analyzers. |
| `FastEndpoints.Messaging.Core` | `8.3.0-beta.10` | Contract event interfaces. |
| `FastEndpoints.Messaging.Remote` | `8.3.0-beta.10` | Remote event hubs/subscriptions and handler server. |
| `FastEndpoints.Messaging.Remote.Testing` | `8.3.0-beta.10` | Test event receivers. |
| `FastEndpoints.OpenApi` | `8.3.0-beta.10` | OpenAPI docs for non-production services. |
| `FastEndpoints.Security` | `8.3.0-beta.10` | JWT bearer token creation in UserIdentity and JWT auth dependencies for UserProfile. |
| `FastEndpoints.Testing` | `8.3.0-beta.10` | App fixtures and endpoint test helpers. |
| `MongoDB.Entities` | `25.1.0` | MongoDB persistence abstraction. |
| `MailKit` | `4.17.0` | SMTP delivery in Notifications. |
| `Scalar.AspNetCore` | `2.16.7` | Scalar OpenAPI UI for non-production identity/profile services. |
| `Shouldly` | `4.3.0` | Assertions. |
| `xunit.v3` | `3.2.2` | Test framework. |
| `xunit.runner.visualstudio` | `3.1.5` | Visual Studio/.NET test runner integration. |
| `Microsoft.NET.Test.Sdk` | `18.7.0` | Test SDK for service test projects. |
| `Microsoft.OpenApi` | `2.9.0` | OpenAPI model support. |

## Project references

- `Services/UserIdentity` references `Contracts/UserIdentity`, `Common/StorageProvider`, and `Common/Tools`.
- `Services/UserProfile` references `Contracts/UserProfile`, `Contracts/UserIdentity`, `Common/StorageProvider`, and `Common/Tools`.
- `Services/Notifications` references `Contracts/Notifications`, `Contracts/UserProfile`, and `Common/StorageProvider`.
- Known subscriber IDs for event hubs live as data-only `string[]` on the publisher contract (`EventSubscribers`); publishers do not reference subscriber contract projects solely for hub registration.
- Contracts do not reference common, other contracts, or service implementation projects.

## Compatibility notes

- The repository uses `HelpDesk.slnx`; if a tool cannot load `.slnx`, load individual `.csproj` files.
- FastEndpoints packages are beta versions; verify API changes before dependency upgrades.
- Service tests are part of service web projects in non-Release builds; Release excludes `**/Tests/**`.

## Local library docs

- FastEndpoints library docs are available at `../FE-Docs/src/content/docs/`.
- MongoDB.Entities docs are available at `../MongoDB.Entities/Documentation/wiki/`.

## Sources

- `Directory.Packages.props`
- `HelpDesk.slnx`
- `Common/*/*.csproj`
- `Contracts/*/*.csproj`
- `Services/*/*.csproj`
