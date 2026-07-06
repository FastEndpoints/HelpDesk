---
type: Reference
title: Gotchas
description: Practical traps, common failure modes, and non-obvious repository constraints.
tags: [gotchas, constraints, troubleshooting]
---

# Gotchas

- `HelpDesk.slnx` is the solution file. Some tools may not support `.slnx`; use specific `.csproj` paths when needed.
- The repo targets `net10.0` and has no `global.json`; local SDK compatibility matters.
- MongoDB must be running for app startup and tests that use fixtures.
- `UserIdentity:Jwt:PrivateKeyPem` is empty in committed config; successful login runtime behavior and UserIdentity login tests need a real RSA private key configured securely.
- Do not introduce direct service project references or internal REST calls for business workflows.
- Contracts must not contain persistence, endpoints, handlers, stores, SMTP, or service-local rules.
- Events must be facts published after local persistence succeeds, not commands.
- Email lookup normalization is `Trim().ToUpperInvariant()`; preserve it for duplicate and activation behavior.
- Release builds remove `**/Tests/**`; do not put runtime code under a `Tests` folder.
- Notifications uses `NullEmailSender` unless Production and `Smtp:Enabled=true`, so local runs will not send real email by default.
- Notifications job storage has distributed job processing disabled; do not assume multiple service instances safely compete for the same email jobs.
- UserProfile and Notifications currently have no public business APIs even though app projects may include dummy endpoints.
- FastEndpoints generated/build artifacts should not be edited manually.
- There are no migration files; schema/index changes happen in startup database initialization code.
- All current service projects share one user-secrets store; avoid accidental cross-service config key collisions.
- Debug builds support an in-process xUnit runner when args contain `@@`; normal workflows should still use `dotnet test` unless intentionally invoking that path.
