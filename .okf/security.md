---
type: Security
title: Security
description: AuthN/Z, credential handling, and secret configuration surfaces for HelpDesk services.
tags: [security]
---

# Security


## SvelteKit BFF boundary

SvelteKit is the external-client boundary. `frontend/src/lib/server/api/` reads private `IDENTITY_API_BASE_URL` / `PROFILE_API_BASE_URL` values and calls backend services server-side; these names must never use `PUBLIC_`. The JWT convention is the server-managed `helpdesk_session` cookie: `HttpOnly`, `SameSite=Lax`, `Path=/`, at most seven days, and `Secure` in production. Never expose bearer tokens to browser JavaScript or browser storage.

## Authentication

- **Issuer:** UserIdentity login mints RSA-SHA256 JWT via FastEndpoints.Security `JwtBearer.CreateToken`
- Claims:
  - `sub` = identity document id
  - `role` (FE default role claim) = permission **group names** from `UserIdentityEntity.Groups` (default `PermissionGroups.User`)
  - issuer/audience from config; expiry `AccessTokenDays` (default 7)
- Empty/null `Groups` on old docs → login falls back to `PermissionGroups.Defaults` (`User`)
- **Validator:** UserProfile `AddAuthenticationJwtBearer` asymmetric with `PublicKey`, matching issuer/audience
- Login requires `UserIdentityStatus.Active` (unverified accounts rejected)

## Authorization (mesh RBAC)

Invariant: **group name ≡ JWT role claim ≡ FE `AccessControl` groupName ≡ `Allow.{Name}`**.

1. **Common.Tools.PermissionGroups** — shared group **name** constants only (`User`, `Admin`, `All`, `Defaults`). No FE codes, no expand helpers.
2. **Identity** stores `Groups` on the identity; mints those names as JWT **roles**. Never mints FE permission hash codes or imports another service’s `Allow`.
3. **Resource services** (UserProfile today) expand roles → local permission **codes** via `IClaimsTransformation` (`PermissionClaimsTransformation`):
   - Maps known roles to generated `Allow.{Group}` (`IEnumerable` of codes)
   - Adds claim type `permissions` (FE default `PermissionsClaimType`)
   - Unknown roles → no codes (fail closed); multi-role → union
   - Idempotent if `permissions` already present
4. **Endpoints** call `AccessControl(keyName, Apply.ToThisEndpoint, PermissionGroups.User)` so the generator places the code under `Allow.User`. Always pass `PermissionGroups.*` constants for group args—never raw string literals.

Handler rules for profile endpoints still apply **after** the permission gate: missing `sub` → 401; missing profile → 404; non-Active → 403.

| Permission key | Endpoint |
| --- | --- |
| `Profiles_Read_Own` | `GET /profiles/me` |
| `Profiles_Update_Own` | `PUT /profiles/me` |
| `Profiles_Upload_Own_Picture` | `PUT /profiles/me/picture` |
| `Profiles_Delete_Own_Picture` | `DELETE /profiles/me/picture` |

All four are group `User`. AccessControl key names must be unique (generator emits const fields). Profile picture **files** are served anonymously under `/profile-pictures/**` (URL is not secret); only metadata mutation is permission-gated. Uploads honor configured `MaxUploadBytes` (default 5 MiB), accept only decoder-verified PNG/JPEG, reject multi-frame images, and cap input at 4096 px per dimension / 16 million pixels.

Register / login / verify: `AllowAnonymous`.

## Credentials and secrets

Config **names** only (values via user secrets/env—not OKF):

- `UserIdentity:Jwt:PrivateKeyPem` — signing key (PEM)
- `UserProfile:Jwt:PublicKey` — validation key
- `UserProfile:Jwt:PrivateKey` — test-only token minting for profile tests
- `Smtp:Username` / `Smtp:Password` and related SMTP fields
- `ConnectionStrings:MongoDB`

Base appsettings intentionally leave signing/validation key values empty. For local runtime, generate one RSA pair, configure Identity with its private PEM and Profile with the matching public PEM via user secrets or multiline-preserving environment variables, and keep both under ignored local storage; root `README.md` has exact commands. Never put the private key in frontend configuration or commit generated keys.

Passwords stored as ASP.NET Core Identity `PasswordHasher` hashes—never plaintext.

Verification codes: 32 random bytes hex; unique sparse index. They currently have no expiry and activation changes only identity status—the code is not cleared or rotated. Verification is status-idempotent, so a previously issued URL remains usable to resolve the identity after activation; treat links and stored codes as long-lived credentials until this behavior changes.

## Public URL trust

Identity builds verification-link bases from the incoming request scheme/host. Profile pictures use configured `PublicBaseUrl` when present, otherwise the request scheme/host. The services do not currently enable forwarded-header middleware, so reverse proxies can produce internal or incorrect URLs unless deployment supplies an explicit public URL/header strategy. Do not trust arbitrary forwarded headers without configuring known proxies/networks.

## Email delivery

- Default `Smtp:Enabled: false` → `NullEmailSender` (logs only)
- Real SMTP only when Production **and** Enabled—reduces accidental mail in dev/test

## Mesh

- IPC/remote messaging is process topology trust boundary; business auth is JWT for HTTP APIs
- Event payloads may include verification codes—treat event storage/Mongo access as sensitive
- Cross-service permission catalog is **not** event-driven; Identity mints group names only

## Sources

- `frontend/src/lib/server/api/`
- `backend/Common/Tools/PermissionGroups.cs`
- `backend/Services/UserIdentity/`
- `backend/Services/UserProfile/`
- `backend/Services/Notifications/`
