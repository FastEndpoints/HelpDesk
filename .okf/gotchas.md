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
- `compose.yaml`, root `.env.example`, deployment scripts, Dockerfiles, and `backend/Deployment/` are production-only. They must not replace or be documented as an alternative to Aspire local development. Service launch settings and `frontend/.env.example` remain obsolete.
- Aspire injects `ConnectionStrings__MongoDB` into backend resources and private `IDENTITY_API_BASE_URL` / `PROFILE_API_BASE_URL` values into Vite. Never rename the frontend values to `PUBLIC_*`.
- Live OpenAPI commands have no fixed-port defaults. For `api:refresh` and `api:check:live`, copy Identity/Profile HTTP URLs from the Aspire dashboard, append `/openapi/v1.json`, and set both `IDENTITY_OPENAPI_URL` and `PROFILE_OPENAPI_URL`. Normalization removes runtime-specific top-level `servers`; offline `api:check` needs no live URLs.
- Matching development JWT private/public values are committed in base Identity/Profile appsettings. Do not regenerate keys or require user-secrets for normal local startup. Environment variables may override both; keep overrides paired and never reuse repository development material in production.
- `pnpm` still owns frontend package management and validation. `pnpm frontend:dev` is frontend-only, not an alternative full-stack orchestrator.
- Frontend is a SvelteKit BFF: JWT stays in the HttpOnly session cookie and backend origins remain private.
- Frontend has registration (`/register`), verification (`/verify/[code]`), login (`/login`), and profile (`/settings/profile`) via BFF form actions; signed-in shell loads Profile `GET /profiles/me` and links to the profile page. Logout UI does not exist yet.
- Verification emails use `UserIdentity:FrontendBaseUrl` + `/verify/{code}`. Aspire injects the local Vite endpoint; production Compose sets the public origin. Production profile URLs use the BFF `/profile-pictures/**` proxy and persist files in a named volume.
- **Never** reference `backend/Services/*` from another service—only projects under `backend/Contracts/*` or `backend/Common/*`. Cross-service workflow = events only, not REST callbacks. The AppHost may reference service host projects solely for orchestration.
- Events are facts after commit, not commands. Publish only after local persistence succeeds.
- Keep `backend/Contracts/*/EventSubscribers` arrays aligned with real consumer `Service.Name` values.
- Contracts must stay free of entities, stores, endpoints, handlers, and SMTP.
- Tests need MongoDB; start `pnpm stack:dev` first so the Aspire-managed instance is available on `localhost:27017`. Testing database names end with `_TESTING`; fixtures drop collections—do not point tests at shared production data.
- Identity JWT carries role group names, not FE permission hash codes. Resource services must register `IClaimsTransformation` or protected endpoints return 403.
- `AccessControl` group args must use `PermissionGroups.*`; key names must be unique. Generated `Allow` classes appear only for groups used by endpoints.
- Notifications SMTP is off unless Production and `Smtp:Enabled`; Notifications has no public business HTTP endpoint.
- Caddy needs `DOMAIN` to resolve publicly and port 80 or 443 to reach the VPS for certificate validation. The simple path uses direct DNS; a CDN proxy needs strict origin TLS, source-restricted firewall rules, and verified renewal.
- Compose `restart: unless-stopped` is not a full Podman reboot story. On Podman-only hosts install `scripts/install-host-service.sh` after deploy; do not fold host-unit install into `deploy-init.sh`. Rootless units need linger and must be able to bind 80/443. The unit runs `compose up -d` / `stop` only—release rebuilds still use `scripts/deploy.sh`. Docker hosts should enable `docker.service` instead of this Podman unit.
- Production Compose derives MongoDB's connection string from raw root credentials, so manual values must contain only URI-unreserved characters and avoid Compose-special `$`; `deploy-init.sh` generates safe hexadecimal values. Mongo initialization credentials apply only to a new data volume; changing `.env` later does not rotate the database user.
- Docker-published ports can bypass plain UFW INPUT rules. Enforce public-port policy at the provider firewall and, when needed, Docker's `DOCKER-USER`/Docker-aware nftables layer.
- Email lookup always uses `NormalizeForLookup` (trim + uppercase). Duplicate checks depend on normalized unique indexes.
- Profile activation on `UserIdentityVerifiedEvent` correlates by `UserIdentityId`, not email. Email on the profile is denormalized for display/unique lookup only.
- Profile create on identity registration is idempotent on duplicate email; identity registration is not.
- Profile pictures store object keys, never image bytes, in MongoDB. Static `/profile-pictures` files are unauthenticated by design.
- Release builds strip Tests; do not rely on test code in Release publish.
- Service communication supports host-local IPC only. Production therefore co-locates all three service processes under the PID 1 launcher; do not split backend services into containers or claim network/multi-host mesh transport.
- Verification codes have no expiry and are not cleared after activation; do not assume one-time or time-limited links.
- Verification links require `UserIdentity:FrontendBaseUrl`. Production must set explicit Identity frontend and Profile picture public bases because backend services do not configure forwarded-header middleware.
- Do not invent a central broker; architecture is brokerless FastEndpoints remote messaging.

## Sources

- `README.md`
- `backend/AppHost/Program.cs`
- `backend/AppHost/HelpDesk.AppHost.csproj`
- `frontend/scripts/openapi.mjs`
- `backend/Services/*/Program.cs`
- `backend/Services/*/Tests/Sut.cs`
- `backend/Contracts/*/EventSubscribers.cs`
