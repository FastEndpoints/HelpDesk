---
type: Security
title: Security
description: AuthN/Z, credential handling, and secret configuration surfaces for HelpDesk services.
tags: [security]
---

# Security

## Authentication

- **Issuer:** UserIdentity login mints RSA-SHA256 JWT via FastEndpoints.Security `JwtBearer.CreateToken`
- Claims: `sub` = identity document id; issuer/audience from config; expiry `AccessTokenDays` (default 7)
- **Validator:** UserProfile `AddAuthenticationJwtBearer` asymmetric with `PublicKey`, matching issuer/audience
- Login requires `UserIdentityStatus.Active` (unverified accounts rejected)

## Authorization

- Register / login / verify: `AllowAnonymous`
- `GET /profiles/me`: authenticated; missing `sub` → 401; missing profile → 404; non-Active profile → 403

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

## Sources

- `Services/UserIdentity/Endpoints/Identities/Login/Endpoint.cs`
- `Services/UserProfile/Program.cs`
- `Services/Notifications/Program.cs`
- `Services/*/appsettings.json`
