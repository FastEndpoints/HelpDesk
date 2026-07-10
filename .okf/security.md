---
type: Security
title: Security
description: AuthN/Z, credential handling, and secret configuration surfaces for HelpDesk services.
tags: [security]
---

# Security

## Authentication

- **Issuer:** UserIdentity login mints RSA-SHA256 JWT via FastEndpoints.Security `JwtBearer.CreateToken`
- Claims:
  - `sub` = identity document id
  - `role` (FE default role claim) = permission **group names** from `UserIdentityEntity.Groups` (default `AuthGroups.User`)
  - issuer/audience from config; expiry `AccessTokenDays` (default 7)
- Empty/null `Groups` on old docs → login falls back to `AuthGroups.Defaults` (`User`)
- **Validator:** UserProfile `AddAuthenticationJwtBearer` asymmetric with `PublicKey`, matching issuer/audience
- Login requires `UserIdentityStatus.Active` (unverified accounts rejected)

## Authorization (mesh RBAC)

Invariant: **group name ≡ JWT role claim ≡ FE `AccessControl` groupName ≡ `Allow.{Name}`**.

1. **Common.Tools.AuthGroups** — shared group **name** constants only (`User`, `Admin`, `All`, `Defaults`). No FE codes, no expand helpers.
2. **Identity** stores `Groups` on the identity; mints those names as JWT **roles**. Never mints FE permission hash codes or imports another service’s `Allow`.
3. **Resource services** (UserProfile today) expand roles → local permission **codes** via `IClaimsTransformation` (`PermissionClaimsTransformation`):
   - Maps known roles to generated `Allow.{Group}` (`IEnumerable` of codes)
   - Adds claim type `permissions` (FE default `PermissionsClaimType`)
   - Unknown roles → no codes (fail closed); multi-role → union
   - Idempotent if `permissions` already present
4. **Endpoints** call `AccessControl(keyName, Apply.ToThisEndpoint, "User")` so the generator places the code under `Allow.User`. FE generator only accepts **string-literal** group args (not `AuthGroups.User` const refs)—literals must match registry values.

Handler rules for `GET /profiles/me` still apply **after** the permission gate: missing `sub` → 401; missing profile → 404; non-Active → 403.

Register / login / verify: `AllowAnonymous`.

## Credentials and secrets

Config **names** only (values via user secrets/env—not OKF):

- `UserIdentity:Jwt:PrivateKeyPem` — signing key (PEM)
- `UserProfile:Jwt:PublicKey` — validation key
- `UserProfile:Jwt:PrivateKey` — test-only token minting for profile tests
- `Smtp:Username` / `Smtp:Password` and related SMTP fields
- `ConnectionStrings:MongoDB`

Passwords stored as ASP.NET Core Identity `PasswordHasher` hashes—never plaintext.

Verification codes: 32 random bytes hex; unique sparse index.

## Email delivery

- Default `Smtp:Enabled: false` → `NullEmailSender` (logs only)
- Real SMTP only when Production **and** Enabled—reduces accidental mail in dev/test

## Mesh

- IPC/remote messaging is process topology trust boundary; business auth is JWT for HTTP APIs
- Event payloads may include verification codes—treat event storage/Mongo access as sensitive
- Cross-service permission catalog is **not** event-driven; Identity mints group names only

## Sources

- `Common/Tools/AuthGroups.cs`
- `Services/UserIdentity/Persistence/UserIdentityEntity.cs`
- `Services/UserIdentity/Endpoints/Identities/Login/Endpoint.cs`
- `Services/UserProfile/Auth/PermissionClaimsTransformation.cs`
- `Services/UserProfile/Endpoints/Profiles/GetCurrent/Endpoint.cs`
- `Services/UserProfile/Program.cs`
- `Services/Notifications/Program.cs`
- `Services/*/appsettings.json`
