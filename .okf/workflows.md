---
type: Playbook
title: Workflows
description: Setup, build, run, format, and local development commands.
tags: [workflow, commands]
---

# Workflows

Run commands from the repository root unless noted.

## Restore, build, test, format

```bash
dotnet restore
dotnet build
dotnet test
dotnet format
```

Targeted examples:

```bash
dotnet build HelpDesk.slnx
dotnet test Services/UserIdentity/Services.UserIdentity.csproj
dotnet test Services/UserProfile/Services.UserProfile.csproj
dotnet test Services/Notifications/Services.Notifications.csproj
```

Notes:

- Projects target `net10.0`; use an SDK that supports .NET 10.
- Package versions are centrally managed by `Directory.Packages.props`.
- Service projects include tests only when configuration is not `Release`.
- The repository uses `.slnx`; some tools may need a specific `.csproj` instead of the solution file.

## Run services locally

```bash
dotnet run --project Services/UserIdentity/Services.UserIdentity.csproj
dotnet run --project Services/UserProfile/Services.UserProfile.csproj
dotnet run --project Services/Notifications/Services.Notifications.csproj
```

Local ports/topology:

- `UserIdentity`: IPC service name from `Contracts.UserIdentity.Service.Name`, HTTP `http://localhost:5000`.
- `UserProfile`: IPC service name from `Contracts.UserProfile.Service.Name`, HTTP `http://localhost:5001`.
- `Notifications`: IPC service name from `Contracts.Notifications.Service.Name`; no public HTTP port in settings.
- JetBrains users can use `.run/RunAll.run.xml` to start all three current services.

## Local dependencies

- MongoDB must be reachable at the configured `ConnectionStrings:MongoDB`; defaults use `mongodb://localhost:27017`.
- Default database names are `HelpDesk_UserIdentity`, `HelpDesk_UserProfile`, and `HelpDesk_Notifications`.
- Testing database names add `_TESTING` via `appsettings.Testing.json`.

## Configuration workflow

- Edit non-secret defaults in each service's `appsettings.json`.
- Put secrets in user secrets, environment variables, or another secret manager; do not commit real secrets.
- `UserIdentity` login requires `UserIdentity:Jwt:PrivateKeyPem` to contain a valid RSA private key for JWT signing.
- `Notifications` SMTP delivery requires Production environment plus `Smtp:Enabled=true`; otherwise email is logged/suppressed by `NullEmailSender`.

## Adding a service

1. Create `Contracts/<ServiceName>/` and add a stable `Service.Name`.
2. Add owned event records that implement `IEvent`.
3. Create standalone `Services/<ServiceName>/` FastEndpoints app.
4. Reference own contract project and `Common/StorageProvider` if publishing/subscribing.
5. Reference other contract projects only for consumed events.
6. Configure IPC/remote messaging in `Program.cs`.
7. Add service-local persistence, endpoints/subscriptions, and colocated tests.

## Adding an event

1. Add the event record to the owning contract project.
2. Register the event hub in the owner.
3. Publish only after local persistence succeeds.
4. In each subscriber, reference the owner contract project.
5. Add handler and `MapRemote(...).Subscribe<...>()` registration.
6. Add/update tests for publication and reaction behavior.

## Sources

- `README.md`
- `Directory.Packages.props`
- `HelpDesk.slnx`
- `Services/*/appsettings*.json`
- `Services/*/Properties/launchSettings.json`
- `.run/RunAll.run.xml`
