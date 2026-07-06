---
type: Reference
title: Workflows
description: Setup, build, run, test, format, configuration, service, event, and generation workflows.
tags: [workflows, commands, configuration]
---

# Workflows

Run commands from the repository root unless noted.

## Prerequisites

- .NET SDK compatible with `net10.0`.
- MongoDB reachable at `mongodb://localhost:27017` for local runs and tests unless configuration overrides it.
- JWT private key configuration for successful UserIdentity login token generation, including UserIdentity login tests that exercise token creation.
- SMTP configuration only if real notification delivery is desired.

No `global.json`, Dockerfile, compose file, CI config, or Makefile exists at OKF initialization time.

## Restore, build, test, format

```bash
dotnet restore
dotnet build
dotnet test
dotnet format
```

The solution file is `HelpDesk.slnx`. If a tool does not support `.slnx`, run commands against specific `.csproj` files.

## Run services locally

```bash
dotnet run --project Services/UserIdentity/Services.UserIdentity.csproj
dotnet run --project Services/UserProfile/Services.UserProfile.csproj
dotnet run --project Services/Notifications/Services.Notifications.csproj
```

Local appsettings:

- UserIdentity HTTP: `http://localhost:5000`; IPC service name `USER_IDENTITY_SERVICE`.
- UserProfile HTTP: `http://localhost:5001`; IPC service name `USER_PROFILE_SERVICE`.
- Notifications listens on IPC service name `NOTIFICATIONS_SERVICE`; no configured HTTP port.

Run multiple services together when exercising the event flow through IPC.

## Configuration workflow

- Default config lives in `Services/*/appsettings.json`.
- Development files currently contain `{}` and inherit defaults.
- Testing files override database names with `_TESTING` suffixes.
- Do not commit secrets. `.env` is ignored. Use user secrets, environment variables, or deployment secret management for values such as JWT private keys and SMTP passwords.
- All current service projects share the same `UserSecretsId` (`f4709213-ea5b-483f-b487-370fc40b9db1`), so user-secrets are shared across UserIdentity, UserProfile, and Notifications.

Key config sections:

- `ConnectionStrings:MongoDB`
- `UserIdentity:HttpPort`, `UserIdentity:DatabaseName`, `UserIdentity:Jwt:*`
- `UserProfile:HttpPort`, `UserProfile:DatabaseName`
- `Notifications:DatabaseName`
- `Smtp:*`

## Add a service

1. Create `Contracts/<ServiceName>/` with stable `Service.Name` and owned event contracts.
2. Create `Services/<ServiceName>/` as a standalone FastEndpoints app.
3. Reference the service's own contract project.
4. Reference `Common/StorageProvider` if publishing/subscribing to events.
5. Reference other contract projects only for consumed events.
6. Configure IPC/remote messaging in `Program.cs`.
7. Add service-owned persistence under `Persistence/`.
8. Add public REST endpoints only if the service exposes an external API.
9. Add subscriptions under `Subscriptions/<Publisher>/<Event>/`.
10. Add colocated endpoint/subscription tests.

## Add an event

1. Add the event record to the owning service's contract project and implement `IEvent`.
2. Register the event hub in the owning service.
3. Publish the event only after local persistence succeeds.
4. In each subscriber, reference the owning contract project.
5. Add a handler under `Subscriptions/<Publisher>/<Event>/`.
6. Register the subscription with `MapRemote(...)`.
7. Add/update publication and reaction tests.

## Code generation and migrations

- FastEndpoints generator runs through build via package reference.
- No explicit code-generation command is documented.
- No migration command or migration system exists; MongoDB indexes are created at service startup.
