---
type: Reference
title: Gotchas
description: Non-obvious traps for the HelpDesk mesh, tests, config, and layering rules.
tags: [gotcha]
---

# Gotchas


- Frontend is a SvelteKit BFF: backend origins are private `IDENTITY_API_BASE_URL` / `PROFILE_API_BASE_URL`, never `PUBLIC_*`; JWT stays in the secure HttpOnly session cookie.
- Root installs enforce Node 26 or newer and pnpm 11 or newer. `.node-version` selects Node 26.4.0 and `packageManager` selects pnpm 11.10.0 by default, but newer versions are accepted.
- Generated OpenAPI snapshots/declarations are committed but excluded from ESLint/Prettier. Validate them with `pnpm frontend:api:check`; `api:refresh` fetches all specs before writing any snapshot.
- Current frontend is foundation only. Do not claim registration/login/verification/profile/picture UI exists.
- Verification-link destination and profile-picture deployment/public URL are unresolved and block shipping those UI flows.
- Podman MongoDB requires a Compose provider, both `.env` credentials, `scripts/setup-mongodb.sh`, authentication, and `authSource=admin&replicaSet=rs0&directConnection=true`. Compose rejects absent credentials. Compose launches MongoDB only—not the application services.
- `scripts/setup-mongodb.sh` preserves an existing non-empty regular replica-set keyfile and rejects directories, symlinks, or empty files with recovery instructions; `--rotate` explicitly replaces a valid keyfile and should be used only with MongoDB stopped.
- **Never** reference `backend/Services/*` from another service—only projects under `backend/Contracts/*` or `backend/Common/*`. Cross-service workflow = events only, not REST callbacks.
- Events are **facts after commit**, not commands. Publish only after local persistence succeeds.
- Keep `backend/Contracts/*/EventSubscribers` arrays in sync with real consumer `Service.Name` values when adding subscribers.
- Contracts must stay free of entities, stores, endpoints, handlers, SMTP.
- Local mesh needs **all relevant processes** running for full onboarding; unit/service tests intentionally avoid multi-process E2E.
- Tests need **MongoDB**; Testing DB names end with `_TESTING`. Fixtures drop collections—don’t point tests at shared prod data.
- JWT: empty `PrivateKeyPem` / `PublicKey` in base appsettings—Identity needs a private PEM and Profile its matching public PEM via user secrets or multiline-preserving env vars. Profile tests use Testing JWT keys in `appsettings.Testing.json`.
- Mesh authz: Identity JWT carries **role group names**, not FE permission hash codes. Resource services must register `IClaimsTransformation` (or tests must mint `permissions` codes) or endpoints with `AccessControl(..., Apply.ToThisEndpoint)` return 403.
- `AccessControl` group args: always `PermissionGroups.*` (never `"User"` / `"Admin"` string literals).
- `Allow.Admin` (etc.) only exists after some endpoint in that assembly uses that group.
- Notifications SMTP is off unless **Production and** `Smtp:Enabled`. Dev/test use null/test senders.
- Notifications has **no public business HTTP port** in Program (IPC + jobs only).
- Email lookup always uses `NormalizeForLookup` (trim + upper). Duplicate checks depend on normalized unique indexes.
- Profile create on identity register is **idempotent on duplicate email** (handler returns); identity register is **not** (client error).
- FastEndpoints reflection helpers (`AddFromServices…`) and `Allow` ACL classes come from the generator—rebuild if missing after structural/`AccessControl` changes. **AccessControl `keyName` values must be unique** in the assembly (duplicate keys fail generation with CS0102).
- Profile pictures: store **object key only** on `UserProfileEntity` (never image bytes in Mongo). Files under `UserProfile:ProfilePictures:StorageRoot`. Leave `PublicBaseUrl` empty so URLs follow the current host; set it only for CDN/proxy overrides. Static files at `/profile-pictures` are unauthenticated by design.
- Release builds **strip Tests**; don’t rely on test code in Release publish.
- Current service startup supports host-local IPC/loopback only. Do not describe network or multi-host mesh transport as configured; implementing it requires an architecture/deployment change.
- Verification codes currently have no expiry and are not cleared after activation; do not assume one-time or time-limited verification links.
- Identity verification links and default profile-picture URLs use the raw request scheme/host. No forwarded-header middleware is configured, so proxies need an explicit public URL/header strategy.
- Do not invent a central broker; architecture is brokerless FE remote messaging.

## Sources

- `README.md`
- `backend/Services/*/Program.cs`
- `backend/Services/*/Tests/Sut.cs`
- `backend/Contracts/*/EventSubscribers.cs`
