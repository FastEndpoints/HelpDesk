---
type: Reference
title: Testing
description: Test strategy, layout, commands, fixtures, dependencies, and expectations.
tags: [testing, validation]
---

# Testing

## Strategy

Tests are service-local and colocated with the behavior they verify. The baseline confidence model is to test every service at its public boundary:

1. public REST endpoints for client-facing API behavior;
2. event subscriptions/reactions for cross-service behavior;
3. event publication when a service owns and broadcasts a contract event.

Full end-to-end tests may be useful later for deployed smoke testing, but they are not the primary correctness strategy.

## Frameworks

- xUnit v3 (`xunit.v3`, `xunit.runner.visualstudio`).
- FastEndpoints.Testing and `AppFixture<Program>`.
- FastEndpoints.Messaging.Remote.Testing for event receivers where present.
- Shouldly assertions.
- Microsoft.NET.Test.Sdk.

Test package references are included only for non-Release configurations in each service csproj.

## Layout

| Path | Purpose |
| --- | --- |
| `Services/UserIdentity/Endpoints/Identities/Register/Tests/` | Registration endpoint tests, including event publication. |
| `Services/UserIdentity/Endpoints/Identities/Login/Tests/` | Login endpoint tests. |
| `Services/UserIdentity/Endpoints/Identities/Verify/Tests/` | Verification endpoint tests, including event publication. |
| `Services/UserProfile/Subscriptions/UserIdentity/Registration/Tests/` | Reaction to identity registration and profile event publication. |
| `Services/UserProfile/Subscriptions/UserIdentity/Verification/Tests/` | Reaction to identity verification. |
| `Services/Notifications/Subscriptions/UserProfile/Registration/Tests/` | Reaction to profile registration and email/job behavior. |
| `Services/<Service>/Tests/Sut.cs` | Shared service fixture. |
| `Services/<Service>/Tests/xunit.runner.json` | xUnit parallelization settings. |

## Commands

```bash
dotnet test
```

Targeted service tests:

```bash
dotnet test Services/UserIdentity/Services.UserIdentity.csproj
dotnet test Services/UserProfile/Services.UserProfile.csproj
dotnet test Services/Notifications/Services.Notifications.csproj
```

## Test fixtures and data

- Fixtures set `ASPNETCORE_ENVIRONMENT` to `Testing` through `UseEnvironment("Testing")`.
- Testing settings use service-specific `_TESTING` database names.
- Fixture disposal drops relevant MongoDB collections, not whole MongoDB servers.
- `UserIdentity` tests register test event receivers and inspect stored identities.
- `UserProfile` tests register test event receivers for published profile events.
- `Notifications` tests replace `IEmailSender` with `TestEmailSender` and drop job/event collections.

## Integration dependencies

- A reachable MongoDB instance is required unless a specific test replaces storage.
- Tests may read optional user secrets in the `Testing` environment.
- Avoid using production database names or credentials in tests.

## Expectations for new behavior

- Add tests in the owning service, near the endpoint or subscription being changed.
- For new event publishers, assert event payloads with FastEndpoints test event receivers.
- For new subscribers, assert service-local state/work queued by the handler.
- For duplicate/validation/error paths, assert HTTP status/problem details or idempotent handler behavior.

## Sources

- `README.md`
- `Services/*/*.csproj`
- `Services/*/Tests/Sut.cs`
- `Services/*/Tests/xunit.runner.json`
- `Services/*/**/Tests/Cases.cs`
