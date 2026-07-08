# HelpDesk

HelpDesk is a brokerless, event-driven microservice mesh built with .NET and FastEndpoints. The solution is organized around independently deployable service nodes that communicate through public contract events instead of direct service references, synchronous RPC, or a central message broker.

The current system covers the user onboarding path:

```text
register identity
  -> publish UserIdentityRegisteredEvent
  -> create deactivated user profile
  -> publish UserProfileRegisteredEvent
  -> send welcome/verification email
  -> verify identity
  -> publish UserIdentityVerifiedEvent
  -> activate matching user profile
```

## Architecture at a glance

```text
HelpDesk/
├── Common/                 # reusable infrastructure and generic helpers
├── Contracts/              # public service contracts: service names, events, DTOs
├── Services/               # independently deployable service implementations
├── Directory.Packages.props
└── HelpDesk.slnx
```

Current projects:

| Area | Project | Responsibility |
| --- | --- | --- |
| Common | `Common/StorageProvider` | MongoDB-backed FastEndpoints remote event storage. |
| Common | `Common/Tools` | Generic helpers, such as lookup string normalization. |
| Contracts | `Contracts/UserIdentity` | UserIdentity service name and identity events. |
| Contracts | `Contracts/UserProfile` | UserProfile service name and profile events. |
| Contracts | `Contracts/Notifications` | Notifications service name. |
| Service | `Services/UserIdentity` | Public identity REST API, identity persistence, identity event hubs. |
| Service | `Services/UserProfile` | Authenticated profile REST API, profile persistence, and reactions to identity events. |
| Service | `Services/Notifications` | Reactions to profile events and email job processing. |

Projects currently target .NET 10. Package versions are centrally managed in `Directory.Packages.props`.

## Brokerless mesh

Services use `FastEndpoints.Messaging.Remote` to form a mesh of service nodes. There is no RabbitMQ, Kafka, Azure Service Bus, or other central broker in the current architecture.

Each publisher exposes event hubs. Each subscriber maps a remote publisher and subscribes to specific event contracts. The shared `Common/StorageProvider` project provides durable MongoDB-backed event storage for the FastEndpoints remote messaging infrastructure.

The mental model:

```text
Contracts = public event language
Common    = reusable infrastructure/helpers
Services  = isolated deployable nodes
Mesh      = remote event queues between service nodes
Broker    = none
RPC       = none for business workflows
```

## Strict event-driven cross-service communication

Cross-service business workflows use events only.

Rules:

- A service may expose public REST endpoints for external clients.
- A service must not call another service's REST endpoint to complete an internal business workflow.
- A service must not reference another service implementation project.
- A service may reference another service's contract project only when it needs that service's public events or DTOs.
- Events describe facts that already happened, not commands asking another service to do something.
- Events are published only after the owning service has committed its local state change.
- Subscribers update only their own local state or queue their own internal work.

Allowed reference direction:

```text
Services -> Contracts
Services -> Common
Contracts -> package references only
Common -> package references only
```

Never do this:

```text
Services/UserProfile -> Services/UserIdentity
Services/Notifications -> Services/UserProfile
```

Use contracts instead:

```text
Services/UserProfile -> Contracts/UserIdentity
Services/Notifications -> Contracts/UserProfile
```

## Contracts as the only cross-service language

Contract projects are intentionally small. They contain only public cross-service language:

- the stable service name used for IPC/remote mapping;
- event records emitted by the owning service;
- simple DTOs required to interpret those events, when needed.

They must not contain:

- endpoints;
- entities;
- persistence code;
- stores/repositories;
- handlers;
- SMTP/email implementation;
- service-local business logic.

Current event contracts:

```text
Contracts.UserIdentity
├── UserIdentityRegisteredEvent
└── UserIdentityVerifiedEvent

Contracts.UserProfile
└── UserProfileRegisteredEvent

Contracts.Notifications
└── no events currently
```

This keeps cross-service coupling explicit and versionable. Service internals remain private to the service that owns them.

## IPC-first development, remote-ready deployment

The project currently uses IPC for local service-to-service communication:

```csharp
ListenInterProcess(Service.Name)
MapRemote(Service.Name, ...)
```

This makes local development simple. Multiple services can run on the same machine without introducing a broker or configuring network endpoints for every service.

The architecture is still remote-ready. To lift a service out to a separate process, machine, container, or host, the event contracts and handlers do not need to change. The deployment only needs to change how the publisher listens and how subscribers target that publisher. In other words, topology is a configuration/deployment concern; business code remains event-based and contract-based.

## Current onboarding flow

```text
External client
  │
  │ POST /identities/register
  ▼
UserIdentity service
  - creates a deactivated identity
  - stores normalized email and password hash
  - publishes UserIdentityRegisteredEvent
  │
  ▼
UserProfile service
  - subscribes to UserIdentityRegisteredEvent
  - creates a deactivated profile
  - publishes UserProfileRegisteredEvent
  │
  ▼
Notifications service
  - subscribes to UserProfileRegisteredEvent
  - queues/sends welcome email with verification link
  │
  │ GET /identities/verify/{verificationCode}
  ▼
UserIdentity service
  - validates verification code
  - activates identity
  - publishes UserIdentityVerifiedEvent
  │
  ▼
UserProfile service
  - subscribes to UserIdentityVerifiedEvent
  - activates the profile matched by normalized email
  - marks EmailVerified = true
```

UserIdentity public endpoints:

```text
POST /identities/register
POST /identities/login
GET  /identities/verify/{VerificationCode}
```

UserProfile public endpoints:

```text
GET  /profiles/me
```

Notifications currently reacts to events and does not expose public business APIs.

## Service responsibilities

### UserIdentity

Owns identity data and public identity API behavior.

Responsibilities:

- register identities;
- store credentials and identity status;
- validate login credentials;
- issue JWTs;
- verify email/identity activation codes;
- publish identity lifecycle events.

Publishes:

- `UserIdentityRegisteredEvent`
- `UserIdentityVerifiedEvent`

### UserProfile

Owns profile data and the authenticated profile API. Profiles are derived from identity events and kept private to the profile service.

Responsibilities:

- create a deactivated profile when an identity is registered;
- publish profile registration events;
- activate a profile when the corresponding identity is verified;
- return the authenticated user's local active profile from `GET /profiles/me`;
- keep profile persistence and profile rules service-local.

Subscribes to:

- `UserIdentityRegisteredEvent`
- `UserIdentityVerifiedEvent`

Publishes:

- `UserProfileRegisteredEvent`

### Notifications

Owns notification delivery and email job processing.

Responsibilities:

- react to profile events;
- build notification email content;
- queue durable email jobs;
- send through SMTP only when configured;
- use a null sender by default in non-production/disabled SMTP scenarios.

Subscribes to:

- `UserProfileRegisteredEvent`

## Typical service layout

A service should be self-contained. The usual shape is:

```text
Services/<Service>/
├── Program.cs                         # service startup, DI, event hubs/subscriptions
├── Meta.cs                            # service-level global usings/metadata
├── appsettings.json
├── appsettings.Development.json
├── appsettings.Testing.json
├── Properties/launchSettings.json
├── Endpoints/                         # public REST endpoints owned by the service
│   └── <Area>/<Action>/
│       ├── Endpoint.cs
│       ├── Request.cs
│       ├── Response.cs
│       └── Tests/
├── Persistence/                       # private entities, stores, DB initialization
├── Subscriptions/                     # event handlers grouped by publisher/event
│   └── <Publisher>/<Event>/
│       ├── <Event>Handler.cs
│       └── Tests/
├── Tests/                             # shared service-local test fixture/config
└── <service-specific folders>/         # jobs, email, auth, etc. when owned locally
```

Do not force empty folders into every service. Use the shape that matches the service's responsibility.

## Preferred solution layout

Preferred top-level layout:

```text
Common/<ReusableInfrastructureOrHelper>/
Contracts/<ServiceName>/
Services/<ServiceName>/
```

Guidelines:

- Put reusable infrastructure in `Common/` only after it is truly generic.
- Keep domain behavior out of `Common/`.
- Put public service contracts in `Contracts/<ServiceName>/`.
- Put implementation details in `Services/<ServiceName>/`.
- Add tests inside the service project near the code they cover.
- Reference another service's contract only if consuming that service's events.
- Do not create shared domain models across services.

## Testing strategy

Tests are colocated with the service that owns the behavior.

Endpoint tests live beside endpoints:

```text
Services/UserIdentity/Endpoints/Identities/Register/Tests/
Services/UserIdentity/Endpoints/Identities/Login/Tests/
Services/UserIdentity/Endpoints/Identities/Verify/Tests/
Services/UserProfile/Endpoints/Profiles/GetCurrent/Tests/
```

Subscription tests live beside event handlers:

```text
Services/UserProfile/Subscriptions/UserIdentity/Registration/Tests/
Services/UserProfile/Subscriptions/UserIdentity/Verification/Tests/
Services/Notifications/Subscriptions/UserProfile/Registration/Tests/
```

Shared service test setup lives under:

```text
Services/<Service>/Tests/
```

The goal is to verify each service at its public boundaries:

1. public REST endpoints, for client-facing API behavior;
2. event subscriptions/reactions, for cross-service event behavior;
3. event publication, where the service owns and broadcasts a contract event.

With this architecture, cross-service integration tests are not required for baseline confidence. The service boundary is either a REST endpoint or a contract event. If every service verifies its public REST API and its reactions to every event it subscribes to, the important behavior is covered without binding tests to a specific deployment topology.

This keeps tests fast, local, and aligned with service ownership:

- UserIdentity tests its registration/login/verify endpoints and its published identity events.
- UserProfile tests its `GET /profiles/me` endpoint, reactions to identity events, and published profile event.
- Notifications tests its reaction to profile registration and email job/sending behavior.

A full end-to-end test can still be added later for smoke testing a deployed environment, but it is not the primary correctness strategy for this codebase.

## Adding a new service

1. Create `Contracts/<ServiceName>/`.
2. Add a stable `Service.Name` constant.
3. Add events the service owns and publishes.
4. Create `Services/<ServiceName>/` as a standalone FastEndpoints app.
5. Reference the service's own contract project.
6. Reference `Common/StorageProvider` if the service publishes or subscribes to events.
7. Reference other contract projects only for events this service consumes.
8. Configure IPC/remote messaging in `Program.cs`.
9. Add service-owned persistence under `Persistence/`.
10. Add public REST endpoints under `Endpoints/`, if the service exposes an API.
11. Add event handlers under `Subscriptions/<Publisher>/<Event>/`.
12. Add colocated endpoint and subscription tests.
13. Do not expose entities, stores, or service-local rules through contracts or common projects.

## Adding an event

1. Add the event record to the owning service's contract project.
2. Implement `IEvent`.
3. Register the event hub in the owning service.
4. Publish the event only after local persistence succeeds.
5. In each subscriber, reference the owning contract project.
6. Add a handler under `Subscriptions/<Publisher>/<Event>/`.
7. Register the subscription with `MapRemote(...)`.
8. Add/update tests for publication and reaction behavior.

## Local development commands

From the repository root:

```bash
dotnet restore
dotnet build
dotnet test
dotnet format
```

Run individual services with `dotnet run --project`, for example:

```bash
dotnet run --project Services/UserIdentity/Services.UserIdentity.csproj
dotnet run --project Services/UserProfile/Services.UserProfile.csproj
dotnet run --project Services/Notifications/Services.Notifications.csproj
```

Default local settings use MongoDB at `mongodb://localhost:27017`. Testing settings use service-specific `_TESTING` database names.

## Coupling checklist

Before adding or changing service behavior, check:

- Does the owning service commit its own state before publishing an event?
- Is cross-service communication represented as an event contract?
- Is the subscriber using only contract data, without calling back to the publisher?
- Are service internals still private to `Services/<ServiceName>/`?
- Are contracts free of persistence and implementation details?
- Are tests colocated with the endpoint or subscription they cover?
- Can the service still move from local IPC to remote deployment without changing business logic?