---
type: API Endpoint
title: API Routes
description: Public REST endpoints owned by UserIdentity and UserProfile services.
tags: [api]
---

# API Routes

## UserIdentity

| Method | Route | Auth | Behavior |
| --- | --- | --- | --- |
| POST | `/identities/register` | Anonymous | Create deactivated identity; hash password; broadcast registered + verification-issued events |
| POST | `/identities/login` | Anonymous | Validate credentials + Active status; return RSA JWT (`sub` = identity id) |
| GET | `/identities/verify/{VerificationCode}` | Anonymous | Activate identity; broadcast verified (idempotent if already Active) |

Implementation roots: `Services/UserIdentity/Endpoints/Identities/{Register,Login,Verify}/`.

### Request notes

- Register/Login: email + password; FluentValidation (email format; register password min 12 / max 128)
- Duplicate email (normalized) → problem details / validation error on register

## UserProfile

| Method | Route | Auth | Behavior |
| --- | --- | --- | --- |
| GET | `/profiles/me` | JWT required | Resolve `sub` / nameidentifier → active profile; 401/404/403 otherwise |
| PUT | `/profiles/me` | JWT required | Update own active profile `DisplayName` (trim); same 401/404/403 gates; returns updated profile |

Implementation: `Services/UserProfile/Endpoints/Profiles/{GetCurrent,UpdateCurrent}/`.

### Request notes

- Update: `DisplayName` required, max 100; email/status not client-writable

## Notifications

No public business HTTP API. Program registers handler server / jobs only (plus dummy root endpoint type for host shape).

## Sources

- `Services/UserIdentity/Endpoints/**`
- `Services/UserProfile/Endpoints/**`
- `Services/*/Program.cs`
