---
type: Reference
title: Conventions
description: Project-specific coding, FastEndpoints, event, persistence, notifications, and testing conventions.
tags: [conventions, patterns, testing]
---

# Conventions

## C# and project style

- Projects target `net10.0`, enable implicit usings and nullable reference types.
- Web services use `Microsoft.NET.Sdk.Web`; common/contracts use `Microsoft.NET.Sdk`.
- Program startup uses top-level statements in `Program.cs`.
- Service projects keep common global usings in `Meta.cs`.
- Classes are commonly `sealed` when not intended for inheritance.
- Namespaces are short/service-local via file-scoped declarations and global usings.
- Keep solutions simple; prefer existing patterns over new abstractions.

## FastEndpoints patterns

- Endpoints live under `Endpoints/<Area>/<Action>/` with `Endpoint.cs`, `Request.cs`, `Response.cs` when needed.
- Endpoint classes inherit `Endpoint<TRequest, TResponse>` or `EndpointWithoutRequest`.
- Request validators are nested `Validator<Request>` classes using FluentValidation.
- Current public identity endpoints call `AllowAnonymous()`.
- Non-production UserIdentity/UserProfile services expose OpenAPI and Scalar.

## Event and service patterns

- Contract events are `sealed record` types implementing `IEvent`.
- Service name constants live in `Contracts/<Service>/Service.cs`.
- Publishers call `RegisterEventHub<TEvent>()` in `Program.cs`.
- Subscribers call `MapRemote(<publisher service name>, c => c.Subscribe<TEvent, THandler>())`.
- Event handlers implement `IEventHandler<TEvent>` and live under `Subscriptions/<Publisher>/<Event>/`.
- Events describe completed facts and are broadcast only after local persistence succeeds.

## Persistence patterns

- MongoDB.Entities is used for entities and startup index creation.
- Persistence models stay private to their owning service.
- Stores hide MongoDB write concerns and translate duplicate key write errors into service-local exceptions.
- Lookup emails use `Common.Tools.NormalizeForLookup()` (`Trim().ToUpperInvariant()`).
- UserIdentity creates deactivated identities, hashes passwords, and generates 32-byte hex verification codes.
- UserProfile creates deactivated profiles and activates by normalized email after verification.

## Notifications patterns

- `IEmailSender` abstracts delivery.
- Production with `Smtp:Enabled=true` uses `SmtpService`; other scenarios use `NullEmailSender` unless tests override it.
- Email sending is queued through FastEndpoints job queues with `JobRecord` storage.
- Welcome email merge fields must match template markers; mismatch throws.

## Error handling and validation

- Endpoint validation uses FluentValidation and FastEndpoints problem details.
- Duplicate identity/profile emails are treated as bad/ignored according to owner behavior:
  - UserIdentity duplicate registration returns validation error.
  - UserProfile duplicate event reaction is ignored for idempotence.
- Login returns generic invalid credential errors and rejects non-active accounts.

## Testing conventions

- Tests are colocated with endpoint/subscription behavior.
- Shared fixtures live in `Services/<Service>/Tests/Sut.cs`.
- Tests use xUnit v3, FastEndpoints.Testing, Shouldly, and test event receivers when event publication matters.
- Test fixture environment is `Testing`; testing appsettings use `_TESTING` database names.
