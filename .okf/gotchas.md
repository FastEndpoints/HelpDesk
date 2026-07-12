---
type: Reference
title: Gotchas
description: Non-obvious traps for Aspire orchestration, the HelpDesk mesh, tests, config, and layering.
tags: [gotcha]
---

# Gotchas

- `backend/AppHost/Program.cs` is the **sole supported local full-stack orchestrator**. Run it with `pnpm stack:dev`; do not restore obsolete parallel startup workflows.
- Aspire assigns application HTTP ports dynamically. Read Identity, Profile, and frontend endpoints from the dashboard; do not document fixed development ports.
- Aspire local MongoDB is ephemeral, authenticated, and standalone on fixed development port `27017` with committed development credentials. It has no replica set, transactions, keyfile, host volume, Compose configuration, or root `.env`; container replacement/removal loses data. Deployments must override the connection string.
- `compose.yaml`, root `.env.example`, `scripts/`, service launch settings, and `frontend/.env.example` were removed by the Aspire migration. References to them are stale.
- Aspire injects `ConnectionStrings__MongoDB` into backend resources and private `IDENTITY_API_BASE_URL` / `PROFILE_API_BASE_URL` values into Vite. Never rename the frontend values to `PUBLIC_*`.
- Live OpenAPI commands have no fixed-port defaults. For `api:refresh` and `api:check:live`, copy Identity/Profile HTTP URLs from the Aspire dashboard, append `/openapi/v1.json`, and set both `IDENTITY_OPENAPI_URL` and `PROFILE_OPENAPI_URL`. Normalization removes runtime-specific top-level `servers`; offline `api:check` needs no live URLs.
- Matching development JWT private/public values are committed in base Identity/Profile appsettings. Do not regenerate keys or require user-secrets for normal local startup. Environment variables may override both; keep overrides paired and never reuse repository development material in production.
- `pnpm` still owns frontend package management and validation. `pnpm frontend:dev` is frontend-only, not an alternative full-stack orchestrator.
- Frontend is a SvelteKit BFF: JWT stays in the HttpOnly session cookie and backend origins remain private.
- Current frontend is foundation only. Do not claim registration/login/verification/profile/picture UI exists.
- Verification-link destination and profile-picture deployment/public URL are unresolved and block shipping those UI flows.
- **Never** reference `backend/Services/*` from another service—only projects under `backend/Contracts/*` or `backend/Common/*`. Cross-service workflow = events only, not REST callbacks. The AppHost may reference service host projects solely for orchestration.
- Events are facts after commit, not commands. Publish only after local persistence succeeds.
- Keep `backend/Contracts/*/EventSubscribers` arrays aligned with real consumer `Service.Name` values.
- Contracts must stay free of entities, stores, endpoints, handlers, and SMTP.
- Tests need MongoDB; start `pnpm stack:dev` first so the Aspire-managed instance is available on `localhost:27017`. Testing database names end with `_TESTING`; fixtures drop collections—do not point tests at shared production data.
- Identity JWT carries role group names, not FE permission hash codes. Resource services must register `IClaimsTransformation` or protected endpoints return 403.
- `AccessControl` group args must use `PermissionGroups.*`; key names must be unique. Generated `Allow` classes appear only for groups used by endpoints.
- Notifications SMTP is off unless Production and `Smtp:Enabled`; Notifications has no public business HTTP endpoint.
- Email lookup always uses `NormalizeForLookup` (trim + uppercase). Duplicate checks depend on normalized unique indexes.
- Profile create on identity registration is idempotent on duplicate email; identity registration is not.
- Profile pictures store object keys, never image bytes, in MongoDB. Static `/profile-pictures` files are unauthenticated by design.
- Release builds strip Tests; do not rely on test code in Release publish.
- Current service communication supports host-local IPC only. Do not claim network or multi-host mesh transport is configured.
- Verification codes have no expiry and are not cleared after activation; do not assume one-time or time-limited links.
- Verification links and default profile-picture URLs use the raw request scheme/host. No forwarded-header middleware is configured.
- Do not invent a central broker; architecture is brokerless FastEndpoints remote messaging.

## Sources

- `README.md`
- `backend/AppHost/Program.cs`
- `backend/AppHost/HelpDesk.AppHost.csproj`
- `frontend/scripts/openapi.mjs`
- `backend/Services/*/Program.cs`
- `backend/Services/*/Tests/Sut.cs`
- `backend/Contracts/*/EventSubscribers.cs`
