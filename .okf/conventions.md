---
type: Reference
title: Conventions
description: Coding, service-boundary, event, persistence, configuration, and testing conventions.
tags: [conventions, style]
---

# Conventions

## Service and contract boundaries

- Keep deployable service implementation under `Services/<ServiceName>/`.
- Keep public cross-service language under `Contracts/<ServiceName>/`.
- Services may reference other services' contract projects only when consuming their events/DTOs.
- Services must not reference other service implementation projects.
- Keep domain behavior, persistence, stores, endpoints, and handlers out of `Common/` and `Contracts/`.

## Event design

- Event records are `public sealed record ... : IEvent` in the owning contract project.
- Event names describe facts already completed: `...RegisteredEvent`, `...VerifiedEvent`.
- Known subscriber IDs for each published event live as data-only `string[]` on the publisher contract's `EventSubscribers` type (literals matching subscriber `Service.Name`; no contract→contract refs).
- Publish with `.Broadcast()` only after local state commits.
- Register publisher hubs in `Program.cs` with `RegisterEventHub<TEvent>(EventSubscribers.SomeEvent)`.
- Register subscribers in `Program.cs` by setting `c.SubscriberID = SubscriberService.Name` inside `MapRemote(...)`, then calling `c.Subscribe<TEvent, THandler>()`.
- Subscriber handlers implement `IEventHandler<TEvent>` and mutate only their own service state or queue local work.

## Endpoint design

- Use FastEndpoints endpoint classes under `Endpoints/<Area>/<Action>/`.
- Keep request/response DTOs beside the endpoint.
- Configure routes in `Configure()` and prefer explicit `AllowAnonymous()` where intended.
- Use FastEndpoints `ThrowError(...)`/problem details for validation/business errors.
- `UserProfile` exposes authenticated profile API endpoints under `Endpoints/Profiles/*`; `Notifications` currently has no public business API. Dummy root endpoints may exist only inside `Program.cs`.

## Persistence and data modeling

- Use MongoDB.Entities for service-local entities and stores.
- Initialize required indexes in `Persistence/*Database.cs` before app run.
- Normalize lookup email values with `Common.Tools.NormalizeForLookup()` (`Trim().ToUpperInvariant()`).
- Model unique email constraints with unique normalized email indexes.
- Catch Mongo duplicate key exceptions in stores and convert to service-local exceptions.
- Keep entity status enums service-local.

## Configuration and dependency injection

- Bind whole service settings from configuration into strongly typed settings classes and `IOptions<T>`.
- Register the generated FastEndpoints reflection cache in each service's `UseFastEndpoints(...)` options, e.g. `c.Binding.ReflectionCache.AddFromServicesUserIdentity()`.
- Default local MongoDB connection string is `mongodb://localhost:27017`.
- Testing environment adds optional user secrets and uses `appsettings.Testing.json` database names.
- `UserIdentity` JWT signing uses `UserIdentity.Jwt.PrivateKeyPem`; keep real keys out of source.
- `Notifications` uses `NullEmailSender` unless environment is Production and `Smtp.Enabled` is true.

## Code style

- Projects target `net10.0`, nullable enabled, implicit usings enabled.
- Many service classes are internal/sealed by omission or `sealed` where no inheritance is needed.
- Prefer small single-purpose classes near the behavior they support.
- Preserve current namespaces by feature folder, e.g. `Identities.Register`, `Subscriptions.UserIdentity.Registration`.
- Keep changes minimal; avoid speculative abstractions.

## Testing conventions

- Tests are colocated under `Tests/` inside endpoint/subscription folders.
- Shared fixture per service lives in `Services/<Service>/Tests/Sut.cs` and derives from `AppFixture<Program>`.
- Use xUnit v3, FastEndpoints.Testing, Shouldly, and test event receivers where events are published.
- Validate public boundaries: REST endpoints, event subscriptions/reactions, and event publication.

## Sources

- `README.md`
- `Services/UserIdentity/Endpoints/Identities/*/Endpoint.cs`
- `Services/UserProfile/Subscriptions/UserIdentity/*/*.cs`
- `Services/Notifications/Subscriptions/UserProfile/Registration/*.cs`
- `Services/*/Persistence/*.cs`
- `Services/*/Tests/Sut.cs`