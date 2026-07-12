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

Implementation roots: `backend/Services/UserIdentity/Endpoints/Identities/{Register,Login,Verify}/`.

### Request notes

- Register/Login: email + password; FluentValidation (email format; register password min 12 / max 128)
- Register response body is a plain success string (not a token DTO); account remains deactivated until verify
- Duplicate email (normalized) → problem details / validation error on register
- Frontend BFF: `POST /register` form action → Identity `POST /identities/register` (confirm password is client-only)
- Frontend BFF: `POST /login` form action → Identity `POST /identities/login`; JWT stored in HttpOnly `helpdesk_session` cookie (maxAge from `expiresAt`, capped at 7 days); browser never sees the bearer token; optional safe relative `redirectTo` (default `/`)
- Frontend BFF: `POST /verify/{code}` form action → Identity `GET /identities/verify/{VerificationCode}` (email links open the page; activation runs only on button click)
- Email verification links use configured `UserIdentity:FrontendBaseUrl` + `/verify/{code}` (frontend), not Identity HTTP

## UserProfile

| Method | Route | Auth | Behavior |
| --- | --- | --- | --- |
| GET | `/profiles/me` | JWT + `Profiles_Read_Own` (`User`) | Resolve `sub` / nameidentifier → active profile; 401/404/403 otherwise; includes nullable `PictureUrl`; used by root layout shell chrome and `/settings/profile` load |
| PUT | `/profiles/me` | JWT + `Profiles_Update_Own` (`User`) | Update own active profile `DisplayName` (trim); same 401/404/403 gates; returns updated profile + `PictureUrl`; frontend BFF `?/update` on `/settings/profile` |
| PUT | `/profiles/me/picture` | JWT + `Profiles_Upload_Own_Picture` (`User`) | Multipart upload; decoder-verified PNG/JPEG only, configurable size limit (default 5 MiB); center-crop resize 300×300; store key + public URL; frontend BFF `?/uploadPicture` |
| DELETE | `/profiles/me/picture` | JWT + `Profiles_Delete_Own_Picture` (`User`) | Clear picture metadata and delete local file; idempotent when none; frontend BFF `?/deletePicture` |

Public static files: `GET /profile-pictures/**` (no auth) from local storage root.

Implementation: `backend/Services/UserProfile/Endpoints/Profiles/{GetCurrent,UpdateCurrent,UploadPicture,DeletePicture}/`.

### Request notes

- Update: `DisplayName` required, max 100; email/status not client-writable
- Picture upload: form field `File` / generated OpenAPI `file` (`multipart/form-data`); metadata and decoded format must resolve to PNG/JPEG; `MaxUploadBytes` defaults to 5 MiB; single-frame input only, max 4096 px per dimension / 16 million pixels; response includes `PictureUrl`
- Concurrent picture mutations use compare-and-set metadata updates; a stale request receives 409 and its newly written file is removed
- Frontend BFF route `/settings/profile` is auth-gated (redirect to login + safe return URL); browser never calls Profile directly

## Notifications

No public business HTTP API. Program registers handler server / jobs only (plus dummy root endpoint type for host shape).

## Sources

- `backend/Services/UserIdentity/Endpoints/**`
- `backend/Services/UserProfile/Endpoints/**`
- `backend/Services/*/Program.cs`
