---
type: Playbook
title: Testing
description: Colocated FastEndpoints.Testing strategy, fixtures, and commands for service boundaries.
tags: [test]
resource: README.md
---

# Testing


## Frontend

From `frontend/`: `pnpm test:unit` runs Vitest and `pnpm test:e2e` runs Playwright. Install browser binaries once with `pnpm exec playwright install`. Playwright builds and previews the app on port 4173, with that origin configured as `baseURL`. Root aliases are `pnpm frontend:test:unit` and `pnpm frontend:test:e2e`; `pnpm check:quick` includes unit tests and `pnpm check:full` includes self-contained E2E without a separate prebuild.

Current frontend unit coverage verifies server API error middleware/client behavior; the landing-page browser smoke is Playwright coverage. Do not infer that registration, login, verification, profile, or picture UI exists.

## Frameworks and layout

- **xunit.v3** + **Shouldly** + **FastEndpoints.Testing** (+ **Messaging.Remote.Testing** where events are asserted)
- Tests live **inside** service projects, colocated with endpoints/handlers
- Shared fixture: `backend/Services/<Service>/Tests/Sut.cs` (`AppFixture<Program>`), `Tests/Meta.cs` with `[assembly: EnableAdvancedTesting]`
- DEBUG hosts accept `@@` args for in-proc xunit runner (`ConsoleRunner`)
- Test packages only when `Configuration != Release`; Release excludes `**/Tests/**`

Examples:

```text
backend/Services/UserIdentity/Endpoints/Identities/Register/Tests/Cases.cs
backend/Services/UserProfile/Subscriptions/UserIdentity/Registration/Tests/
backend/Services/Notifications/Subscriptions/UserIdentity/VerificationIssued/Tests/
```

## Commands

```bash
dotnet test HelpDesk.slnx -c Debug
dotnet test backend/Services/UserIdentity/Services.UserIdentity.csproj
dotnet test backend/Services/UserProfile/Services.UserProfile.csproj
dotnet test backend/Services/Notifications/Services.Notifications.csproj
```

## Integration and data

- Environment: `Testing` via fixture `UseEnvironment("Testing")`
- DB names: `*_TESTING` overrides in `appsettings.Testing.json`
- Real MongoDB required for tests; start `pnpm stack:dev` and use the committed development connection from base appsettings (environment variables may override it)
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
- `backend/Services/*/Tests/Sut.cs`
- `backend/Services/*/**/Tests/Cases.cs`
- `backend/Services/*/*.csproj`
