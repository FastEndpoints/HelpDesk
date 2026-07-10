---
type: Playbook
title: Workflows
description: Restore, build, run, test, and format commands for the HelpDesk solution.
tags: [build]
resource: README.md
---

# Workflows

## Setup

- Install .NET 10 SDK
- MongoDB reachable at connection string (default `mongodb://localhost:27017`)
- JWT keys for non-test runs: set `UserIdentity:Jwt:PrivateKeyPem` and matching `UserProfile:Jwt:PublicKey` (user secrets or env)—empty defaults in base `appsettings.json`

```bash
dotnet restore
```

## Build and run

```bash
dotnet build
dotnet run --project Services/UserIdentity/Services.UserIdentity.csproj
dotnet run --project Services/UserProfile/Services.UserProfile.csproj
dotnet run --project Services/Notifications/Services.Notifications.csproj
```

Local HTTP: UserIdentity `:5000`, UserProfile `:5001` (from appsettings / launchSettings). Mesh IPC uses contract `Service.Name` values—run subscribers with publishers as needed for full onboarding.

IDE: `.run/RunAll.run.xml` multi-launch configuration.

OpenAPI/Scalar (non-Production): UserIdentity and UserProfile map OpenAPI + Scalar (`/scalar` launchUrl).

## Lint and format

```bash
dotnet format
dotnet test
```

## Codegen and migrations

- No EF migrations. Mongo indexes created at startup in `*Database.InitializeAsync`.
- FastEndpoints generators run on build (`FastEndpoints.Generator`).

## Adding a service (checklist)

1. `Contracts/<Name>/` with `Service.Name` + owned events
2. `Services/<Name>/` FastEndpoints host; refs own contract + Common as needed
3. Ref other contracts only for consumed events
4. IPC listen / MapRemote / hubs in `Program.cs`
5. Private persistence + colocated tests
6. Update solution + OKF

## Sources

- `README.md`
- `Services/*/Properties/launchSettings.json`
- `Services/*/appsettings.json`
