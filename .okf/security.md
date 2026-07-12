---
type: Security
title: Security
description: BFF auth boundaries, JWT configuration, authorization, and Aspire development credentials.
tags: [security]
---

# Security

## SvelteKit BFF boundary

SvelteKit is the external-client boundary. `frontend/src/lib/server/api/` reads private `IDENTITY_API_BASE_URL` / `PROFILE_API_BASE_URL` values injected by Aspire and calls backend services server-side; these names must never use `PUBLIC_`. The JWT convention is the server-managed `helpdesk_session` cookie: `HttpOnly`, `SameSite=Lax`, `Path=/`, at most seven days, and `Secure` in production. Never expose bearer tokens to browser JavaScript or browser storage.

## Authentication

- UserIdentity login mints RSA-SHA256 JWTs via FastEndpoints.Security `JwtBearer.CreateToken`.
- `sub` is the identity document id; `role` claims are permission group names from `UserIdentityEntity.Groups`.
- Issuer/audience and expiry come from config (`AccessTokenDays` defaults to 7).
- Empty/null groups on old documents fall back to `PermissionGroups.Defaults` (`User`).
- UserProfile validates asymmetrically with the matching public value, issuer, and audience.
- Login requires `UserIdentityStatus.Active`.
- BFF login (`POST /login` action → Identity `POST /identities/login`) persists `accessToken` only in the HttpOnly `helpdesk_session` cookie via `writeSessionToken`; cookie `maxAge` is derived from response `expiresAt` and capped at seven days. Token never goes to `localStorage` / JS.

## Development JWT configuration

Matching development-only RSA values are intentionally committed in base appsettings:

- `backend/Services/UserIdentity/appsettings.json` contains `UserIdentity:Jwt:PrivateKeyPem`.
- `backend/Services/UserProfile/appsettings.json` contains `UserProfile:Jwt:PublicKey`.

This pair lets the Aspire development stack run without key generation or user-secrets setup. These values are public repository development material, not production credentials. Never reuse them in deployed environments. Environment variables `UserIdentity__Jwt__PrivateKeyPem` and `UserProfile__Jwt__PublicKey` may override them; override both with a matching pair. Never put a private key in frontend configuration or browser code.

## Production edge and secrets

Caddy is the production public edge and terminates HTTPS with automatically obtained, publicly trusted certificates persisted in the `caddy_data` volume. Caddy alone publishes host ports 80/443 and reverse-proxies to the private HTTP BFF. The simple deployment path uses direct DNS; a later CDN proxy requires strict origin TLS and source-restricted origin firewall rules. Keep production ports 3000, 8080, 8081, and 27017 closed publicly.

Production `.env` is uncommitted. `scripts/deploy-init.sh` creates it with mode 600, a random hexadecimal MongoDB password, and a new matching 3072-bit RSA JWT pair; it refuses to overwrite an existing file. `DOMAIN` derives the public HTTPS origins used by the application and Caddy. Optional SMTP credentials remain operator-supplied. Separate Compose edge (Caddy/BFF/backend) and internal data (backend/MongoDB) networks prevent direct edge-to-MongoDB access. The BFF proxies public anonymous `/profile-pictures/**` requests to private Profile storage.

## Authorization (mesh RBAC)

Invariant: **group name ≡ JWT role claim ≡ FE `AccessControl` groupName ≡ `Allow.{Name}`**.

1. `Common.Tools.PermissionGroups` contains shared group-name constants only.
2. Identity stores groups and mints those names as JWT roles; it never mints FE permission hash codes or imports another service’s `Allow`.
3. Resource services expand roles to local permission codes through `IClaimsTransformation`.
4. Endpoints call `AccessControl(keyName, Apply.ToThisEndpoint, PermissionGroups.User)`; use constants, not raw group strings.

Handler rules apply after the permission gate: missing `sub` → 401; missing profile → 404; non-Active → 403.

| Permission key | Endpoint |
| --- | --- |
| `Profiles_Read_Own` | `GET /profiles/me` |
| `Profiles_Update_Own` | `PUT /profiles/me` |
| `Profiles_Upload_Own_Picture` | `PUT /profiles/me/picture` |
| `Profiles_Delete_Own_Picture` | `DELETE /profiles/me/picture` |

All four are group `User`. Profile-picture files are served anonymously under `/profile-pictures/**`; only metadata mutation is permission-gated. Uploads honor `MaxUploadBytes`, accept decoder-verified PNG/JPEG, reject multi-frame images, and cap dimensions/pixels. Register/login/verify are anonymous.

## Other credentials and secrets

- Aspire uses committed development-only MongoDB username/password parameters and injects the authenticated connection. Base service connection strings match them so tests can use the running local resource. Deployments must override `ConnectionStrings__MongoDB`; never reuse these values outside development.
- Deployment-managed secrets include production JWT private material, SMTP username/password, and production MongoDB connection strings.
- Passwords are stored as ASP.NET Core Identity `PasswordHasher` hashes, never plaintext.
- Verification codes are random 32-byte hex values with a unique sparse index. They currently have no expiry and are not cleared after activation; treat links and stored codes as long-lived credentials.

## Public URL trust

Identity builds verification-link bases from configured `UserIdentity:FrontendBaseUrl` (frontend origin + `/verify/{code}`). Register rejects when that setting is empty. Profile pictures use configured `PublicBaseUrl` when present, otherwise the request scheme/host. Services do not enable forwarded-header middleware, so proxies need an explicit trusted proxy/public URL strategy for non-configured public hosts.

## Email and mesh

- SMTP is real only when Production and `Smtp:Enabled`; development/testing uses null/test senders.
- IPC/remote messaging is a process topology trust boundary; HTTP business auth uses JWT.
- Event payloads may include verification codes—treat event storage and MongoDB access as sensitive.

## Sources

- `backend/AppHost/Program.cs`
- `backend/Services/UserIdentity/appsettings.json`
- `backend/Services/UserProfile/appsettings.json`
- `frontend/src/lib/server/api/`
- `backend/Common/Tools/PermissionGroups.cs`
- `backend/Services/`
