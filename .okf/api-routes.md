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
| GET | `/profiles/me` | JWT + `Profiles_Read_Own` (`User`) | Resolve `sub` / nameidentifier → active profile; 401/404/403 otherwise; includes nullable `PictureUrl` |
| PUT | `/profiles/me` | JWT + `Profiles_Update_Own` (`User`) | Update own active profile `DisplayName` (trim); same 401/404/403 gates; returns updated profile + `PictureUrl` |
| PUT | `/profiles/me/picture` | JWT + `Profiles_Upload_Own_Picture` (`User`) | Multipart upload; decoder-verified PNG/JPEG only, configurable size limit (default 5 MiB); center-crop resize 300×300; store key + public URL |
| DELETE | `/profiles/me/picture` | JWT + `Profiles_Delete_Own_Picture` (`User`) | Clear picture metadata and delete local file; idempotent when none |

Public static files: `GET /profile-pictures/**` (no auth) from local storage root.

Implementation: `Services/UserProfile/Endpoints/Profiles/{GetCurrent,UpdateCurrent,UploadPicture,DeletePicture}/`.

### Request notes

- Update: `DisplayName` required, max 100; email/status not client-writable
- Picture upload: form field `File` (`multipart/form-data`); metadata and decoded format must resolve to PNG/JPEG; `MaxUploadBytes` defaults to 5 MiB; single-frame input only, max 4096 px per dimension / 16 million pixels; response includes `PictureUrl`
- Concurrent picture mutations use compare-and-set metadata updates; a stale request receives 409 and its newly written file is removed

## Notifications

No public business HTTP API. Program registers handler server / jobs only (plus dummy root endpoint type for host shape).

## Sources

- `Services/UserIdentity/Endpoints/**`
- `Services/UserProfile/Endpoints/**`
- `Services/*/Program.cs`
