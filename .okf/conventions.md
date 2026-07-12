---
type: Reference
title: Conventions
description: Naming, service layout, contracts, events, validation, and persistence patterns used across HelpDesk.
tags: [conventions]
resource: README.md
---

# Conventions

## Naming

- Projects: `Common.<Area>`, `Contracts.<ServiceName>`, `Services.<ServiceName>`
- IPC/service constants: `SCREAMING_SNAKE` (e.g. `USER_IDENTITY_SERVICE`)
- Events: past-tense facts, `*Event` records implementing `IEvent`
- Endpoint folders: `Endpoints/<Area>/<Action>/` with `Endpoint.cs`, `Request.cs`, `Response.cs`, `Tests/`
- Handlers: `Subscriptions/<Publisher>/<Event>/…EventHandler.cs` + colocated `Tests/`
- Entities: service-private under `Persistence/`; Mongo collection via `[Collection("…")]`

## Style

- Prefer `sealed` types for endpoints, handlers, entities, settings
- File-scoped namespaces matching folder purpose (`Identities.Register`, `Persistence`, `Email`, …)
- Global usings in service `Meta.cs` / test `Tests/Meta.cs`
- Nullable enabled; `ImplicitUsings` on
- KISS / no speculative shared domain in Common

## Errors and validation

- FluentValidation nested `Validator` on request DTOs (FastEndpoints `Validator<T>`)
- Domain failures: `ThrowError(...)` / problem details (`c.Errors.UseProblemDetails()`)
- Duplicate keys: catch Mongo duplicate → service exception (`DuplicateIdentityEmailException`, `DuplicateEmailException`) → map to client error or idempotent skip (profile registration)

## APIs and data

- REST only on owning service; routes without leading slash in `Configure()` (e.g. `Post("identities/register")`)
- Anonymous where appropriate (`AllowAnonymous`); profile `GET`/`PUT /profiles/me` authenticated + permission-gated
- Mesh authz: Identity stores/mints **group names** (`PermissionGroups`); resource services expand roles → local FE `Allow` **codes** via `IClaimsTransformation`. Group name ≡ JWT `role` ≡ `AccessControl` group ≡ `Allow.{Name}`
- `AccessControl` group args: always `PermissionGroups.*` constants (never raw string literals like `"User"`)
- Email lookup normalization: `NormalizeForLookup()` → trim + upper invariant; store both raw and normalized
- Events after successful local write; broadcast via `.Broadcast()`
- Contracts: only service name, events, subscriber ID arrays—no entities/endpoints/stores

## Config and DI

- Options pattern: `Get<TSettings>()`, `Configure<TSettings>(Configuration)`
- Store interfaces + Mongo implementations registered in `Program.cs`
- JWT: UserIdentity signs (private PEM) with `sub` + role group claims; UserProfile validates (public key) and expands groups to permissions
- Notifications: `SmtpService` only when Production **and** `Smtp:Enabled`; else `NullEmailSender`
- Local/testing defaults come from committed appsettings; environment variables override deployment-sensitive values

## Sources

- `README.md`
- `backend/Services/*/Program.cs`
- `backend/Services/*/Endpoints/**`
- `backend/Services/*/Persistence/**`
