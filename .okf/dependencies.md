---
type: Reference
title: Dependencies
description: Runtime, package management, key libraries, version notes, and dependency update rules.
tags: [dependencies, packages, runtime]
---

# Dependencies

## Runtime and package management

- Target framework: `net10.0` for all projects.
- No `global.json` pins a SDK version.
- NuGet versions are centrally managed in `Directory.Packages.props` via `ManagePackageVersionsCentrally=true`.
- Solution file: `HelpDesk.slnx`.

## Key packages

| Package | Use |
| --- | --- |
| `FastEndpoints` | REST endpoints and application framework. |
| `FastEndpoints.Generator` | Source generation during build. |
| `FastEndpoints.Messaging.Core` | `IEvent` contract base for contract projects. |
| `FastEndpoints.Messaging.Remote` | Brokerless remote event hubs/subscriptions and job queues. |
| `FastEndpoints.Messaging.Remote.Testing` | Test event receiver support. |
| `FastEndpoints.OpenApi` | OpenAPI for non-production UserIdentity/UserProfile services. |
| `FastEndpoints.Security` | JWT creation in UserIdentity login. |
| `FastEndpoints.Testing` | Endpoint/app fixture testing. |
| `MongoDB.Entities` | MongoDB persistence and index management. |
| `MongoDB.Driver` | Used transitively/directly in service code for Mongo client/settings and exceptions. |
| `MailKit` | SMTP email delivery in Notifications. |
| `Scalar.AspNetCore` | Scalar API reference in non-production UserIdentity/UserProfile services. |
| `Microsoft.OpenApi` | OpenAPI support. |
| `Microsoft.NET.Test.Sdk`, `xunit.v3`, `xunit.runner.visualstudio`, `Shouldly` | Test execution and assertions. |

## Version notes at initialization

- FastEndpoints packages are `8.3.0-beta.9`; keep related FastEndpoints packages aligned.
- MailKit is `4.17.0`.
- MongoDB.Entities is `25.1.0`.
- Scalar.AspNetCore is `2.16.7`.

## Dependency update rules

- Change package versions in `Directory.Packages.props`, not individual projects.
- Keep package references in `.csproj` free of explicit versions unless central management changes.
- Preserve conditional test package references in service projects unless changing Release build behavior intentionally.
- Re-run relevant build/tests after framework, FastEndpoints, MongoDB, MailKit, or test package changes.
