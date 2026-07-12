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

Frontend unit coverage:
- server API client → `ApiError` middleware; bearer `Authorization` header only when token passed
- session cookie helpers (`lib/server/api/session.spec.ts`): `helpdesk_session` name, HttpOnly/SameSite/Path, maxAge default + 7-day cap, Secure in production, clear attributes
- problem-details field mapping (`mapProblemFieldErrors`)
- register BFF action (`routes/register/page.server.spec.ts`): local validation, email trim, Identity POST body shape, success message fallback, field/form `ApiError` mapping, unreachable-service 500
- login BFF action (`routes/login/page.server.spec.ts`): local validation, email trim, Identity POST body shape, session cookie maxAge from string/Date `expiresAt` (unparseable → default; past → 0), missing-token 502, redirect home or safe `redirectTo`, open-redirect rejection, field/form `ApiError` mapping (email/password), title/generic fallbacks, out-of-range status clamp, unreachable-service 500
- profile BFF load/actions (`routes/settings/profile/page.server.spec.ts`): no session → login redirect with return URL; load maps profile + null picture; incomplete payload 502; 401/403/404 clear session + redirect; unreachable 503; update validation/trim/PUT body; upload local type/size gates + multipart FormData body; delete success; field/form `ApiError` mapping; unreachable action 500
- root layout load (`routes/layout.server.spec.ts`): no cookie → anonymous; session → Profile `GET /profiles/me` maps `displayName`/`pictureUrl` (null/undefined → null); empty/missing displayName or empty body → anonymous without clear; 401/403/404 clears session; other errors keep cookie and stay anonymous
- verify BFF load/action (`routes/verify/[code]/page.server.spec.ts`): code trim/`hasCode`, missing/whitespace submit, path param to Identity GET, success message fallback, `ApiError` detail/title/status clamp, unreachable-service 500

Playwright (`register.e2e.ts`):
- form + shell smoke
- password mismatch keeps form and repopulates email
- short password after `novalidate` hits server validation
- Identity-unavailable path shows form-level error (preview has no backend URLs)

Playwright (`login.e2e.ts`):
- form + anonymous shell smoke (Sign in / Create account visible; no `shell-profile`)
- empty fields after `novalidate` hit server validation
- invalid email after `novalidate` keeps form and repopulates email
- Identity-unavailable path shows form-level error (preview has no backend URLs)

Playwright (`page.svelte.e2e.ts`):
- landing content smoke
- anonymous shell chrome (banner Sign in / Create account; no `shell-profile`)

Playwright (`verify.e2e.ts`):
- code present → verify prompt + button (no success state until submit)
- whitespace/missing code → invalid-link UI, no verify button
- Identity-unavailable after click → form error, stays on prompt
- `/login` form smoke (post-verify CTA target)

Register/login/verify/profile success against live Identity/Profile services is not covered by Playwright without the Aspire stack (preview has no backend URLs; browser never calls backends). Signed-in shell chrome and `/settings/profile` depend on Profile `GET /profiles/me`; live success paths need Aspire.

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
