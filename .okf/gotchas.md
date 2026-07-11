---
type: Reference
title: Gotchas
description: Non-obvious traps for the HelpDesk mesh, tests, config, and layering rules.
tags: [gotcha]
---

# Gotchas

- **Never** reference `Services/*` from another service—only Contracts/Common. Cross-service workflow = events only, not REST callbacks.
- Events are **facts after commit**, not commands. Publish only after local persistence succeeds.
- Keep `Contracts/*/EventSubscribers` arrays in sync with real consumer `Service.Name` values when adding subscribers.
- Contracts must stay free of entities, stores, endpoints, handlers, SMTP.
- Local mesh needs **all relevant processes** running for full onboarding; unit/service tests intentionally avoid multi-process E2E.
- Tests need **MongoDB**; Testing DB names end with `_TESTING`. Fixtures drop collections—don’t point tests at shared prod data.
- JWT: empty `PrivateKeyPem` / `PublicKey` in base appsettings—configure secrets for real runs. Profile tests use Testing JWT keys in `appsettings.Testing.json`.
- Mesh authz: Identity JWT carries **role group names**, not FE permission hash codes. Resource services must register `IClaimsTransformation` (or tests must mint `permissions` codes) or endpoints with `AccessControl(..., Apply.ToThisEndpoint)` return 403.
- FE `AccessControl` group names are **syntax-only string literals**—`PermissionGroups.User` const refs are ignored by the generator; use `"User"` and keep values in sync.
- `Allow.Admin` (etc.) only exists after some endpoint in that assembly uses that group literal.
- Notifications SMTP is off unless **Production and** `Smtp:Enabled`. Dev/test use null/test senders.
- Notifications has **no public business HTTP port** in Program (IPC + jobs only).
- Email lookup always uses `NormalizeForLookup` (trim + upper). Duplicate checks depend on normalized unique indexes.
- Profile create on identity register is **idempotent on duplicate email** (handler returns); identity register is **not** (client error).
- FastEndpoints reflection helpers (`AddFromServices…`) and `Allow` ACL classes come from the generator—rebuild if missing after structural/`AccessControl` changes.
- Release builds **strip Tests**; don’t rely on test code in Release publish.
- README says Notifications may subscribe to profile registration in places historically; **code** subscribes to `UserIdentityVerificationIssuedEvent`—prefer source over stale prose if they diverge.
- Do not invent a central broker; architecture is brokerless FE remote messaging.

## Sources

- `README.md`
- `Services/*/Program.cs`
- `Services/*/Tests/Sut.cs`
- `Contracts/*/EventSubscribers.cs`
