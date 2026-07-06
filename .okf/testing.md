---
type: Reference
title: Testing
description: Test frameworks, layout, commands, environment assumptions, fixtures, and validation strategy.
tags: [testing, validation, fixtures]
---

# Testing

## Frameworks

- xUnit v3 (`xunit.v3`, `xunit.runner.visualstudio`).
- FastEndpoints.Testing for app fixtures and endpoint test helpers.
- FastEndpoints.Messaging.Remote.Testing where event receiver testing is needed.
- Shouldly for assertions.
- Microsoft.NET.Test.Sdk.
Test package references are included only when `$(Configuration) != Release`; Release builds remove `**/Tests/**` from service projects.

## Layout

Tests are colocated with owned behavior:

```text
Services/UserIdentity/Endpoints/Identities/Register/Tests/
Services/UserIdentity/Endpoints/Identities/Login/Tests/
Services/UserIdentity/Endpoints/Identities/Verify/Tests/
Services/UserProfile/Subscriptions/UserIdentity/Registration/Tests/
Services/UserProfile/Subscriptions/UserIdentity/Verification/Tests/
Services/Notifications/Subscriptions/UserProfile/Registration/Tests/
```

Shared service fixtures:

```text
Services/UserIdentity/Tests/Sut.cs
Services/UserProfile/Tests/Sut.cs
Services/Notifications/Tests/Sut.cs
Services/*/Tests/xunit.runner.json
```

`xunit.runner.json` enables parallel assembly and test collection execution.

## Commands

Run all tests:

```bash
dotnet test
```

Run one service's tests:

```bash
dotnet test Services/UserIdentity/Services.UserIdentity.csproj
dotnet test Services/UserProfile/Services.UserProfile.csproj
dotnet test Services/Notifications/Services.Notifications.csproj
```

Run targeted tests with standard .NET filters, for example:

```bash
dotnet test Services/UserIdentity/Services.UserIdentity.csproj --filter FullyQualifiedName~Register
```

## Test environment and data

- Fixtures set `ASPNETCORE_ENVIRONMENT` to `Testing`.
- Testing config uses MongoDB databases `HelpDesk_UserIdentity_TESTING`, `HelpDesk_UserProfile_TESTING`, and `HelpDesk_Notifications_TESTING`.
- Testing startup loads user-secrets; UserIdentity login tests need `UserIdentity:Jwt:PrivateKeyPem` configured via user-secrets or environment/config override.
- UserIdentity and UserProfile fixtures register test event receivers for event publication assertions.
- Notifications tests replace `IEmailSender` with `TestEmailSender`.
- Cached fixture disposal drops these collections: UserIdentity drops `UserIdentityEntity`; UserProfile drops `UserProfileEntity`; Notifications drops `JobRecord` and `EventRecord`.
- Tests still require a reachable MongoDB instance unless a test-specific replacement is added.

## Debug in-process test runner

Each service `Program.cs` supports an in-process xUnit runner in Debug builds: when process args contain `@@`, startup delegates to `Xunit.Runner.InProc.SystemConsole.ConsoleRunner`. Normal `dotnet test` remains the primary workflow.

## What to test

- Endpoint behavior at public REST boundaries: validation, status codes, response bodies, persistence, and event publication.
- Event handler behavior at subscription boundaries: local state changes, idempotence/duplicate handling, and downstream event publication or queued jobs.
- Contract changes: owning service publication tests and all subscriber reaction tests.
- Persistence changes: indexes, duplicate behavior, normalization, and service-local store methods.
- Notification changes: job queuing, email sender selection, template merge fields, and SMTP/null sender behavior where relevant.

## Baseline strategy

The repo favors service-boundary tests instead of broad cross-service end-to-end tests. If each service verifies its external REST endpoints and event reactions/publications, baseline confidence should not depend on deployment topology.
