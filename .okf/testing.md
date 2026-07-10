---
type: Playbook
title: Testing
description: Colocated FastEndpoints.Testing strategy, fixtures, and commands for service boundaries.
tags: [test]
resource: README.md
---

# Testing

## Frameworks and layout

- **xunit.v3** + **Shouldly** + **FastEndpoints.Testing** (+ **Messaging.Remote.Testing** where events are asserted)
- Tests live **inside** service projects, colocated with endpoints/handlers
- Shared fixture: `Services/<Service>/Tests/Sut.cs` (`AppFixture<Program>`), `Tests/Meta.cs` with `[assembly: EnableAdvancedTesting]`
- DEBUG hosts accept `@@` args for in-proc xunit runner (`ConsoleRunner`)
- Test packages only when `Configuration != Release`; Release excludes `**/Tests/**`

Examples:

```text
Services/UserIdentity/Endpoints/Identities/Register/Tests/Cases.cs
Services/UserProfile/Subscriptions/UserIdentity/Registration/Tests/
Services/Notifications/Subscriptions/UserIdentity/VerificationIssued/Tests/
```

## Commands

```bash
dotnet test
dotnet test Services/UserIdentity/Services.UserIdentity.csproj
dotnet test Services/UserProfile/Services.UserProfile.csproj
dotnet test Services/Notifications/Services.Notifications.csproj
```

## Integration and data

- Environment: `Testing` via fixture `UseEnvironment("Testing")`
- DB names: `*_TESTING` overrides in `appsettings.Testing.json`
- Real MongoDB required for tests (connection from config/user secrets)
- Fixtures drop owned collections on dispose (`UserIdentities` / `UserProfiles` / jobs+events)
- Event assertions: `RegisterTestEventReceivers()` + `GetTestEventReceiver<TEvent>().WaitForMatchAsync(...)`
- Notifications: replaces `IEmailSender` with `TestEmailSender` capture queue

## Expectations

Prove each service at **public boundaries**:

1. REST endpoints (client-facing)
2. Subscription handlers (reactions)
3. Event publication where the service owns the contract event

Prefer not to couple correctness to full multi-process topology. E2E smoke optional later.

## Sources

- `README.md`
- `Services/*/Tests/Sut.cs`
- `Services/*/**/Tests/Cases.cs`
- `Services/*/*.csproj`
