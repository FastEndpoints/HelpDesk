---
type: Playbook
title: Operations
description: Aspire-managed local topology, dynamic endpoints, and runtime configuration injection.
tags: [ops]
resource: backend/AppHost/Program.cs
---

# Operations

## Supported local topology

Run `pnpm stack:dev`; it invokes `dotnet run --project backend/AppHost/HelpDesk.AppHost.csproj`. The AppHost `Program.cs` is the sole supported local full-stack orchestrator.

| Resource | Endpoint | Notes |
| --- | --- | --- |
| Aspire dashboard | emitted at startup | resource status, dynamic application endpoints, and logs |
| SvelteKit/Vite | dynamically assigned | external-client/BFF boundary |
| UserIdentity | dynamically assigned HTTP | OpenAPI/Scalar outside Production |
| UserProfile | dynamically assigned HTTP | OpenAPI/Scalar outside Production |
| Notifications | no public HTTP endpoint | IPC subscriber and jobs only |
| MongoDB | `localhost:27017` | Aspire-managed authenticated standalone, ephemeral |

Application HTTP ports are dynamic; use the dashboard for the current run. MongoDB alone uses fixed development port `27017` so repository tests can share the Aspire-managed instance.

Backend services remain a host-local IPC FastEndpoints mesh: startup hardcodes `ListenInterProcess`, while Identity/Profile HTTP listeners use orchestrator-injected ports. SvelteKit calls Identity/Profile only from server modules. No deployment manifest provides a network transport or multi-host topology.

## Aspire resource behavior

Aspire 13.4.6:

1. starts MongoDB with committed development username/password parameters;
2. injects the `MongoDB` connection into Identity, Profile, and Notifications;
3. waits for MongoDB before starting dependent services;
4. starts Identity before Profile/Notifications complete their dependency chain;
5. starts Vite after Identity and Profile and injects their HTTP endpoints as `IDENTITY_API_BASE_URL` / `PROFILE_API_BASE_URL`;
6. injects the Vite HTTP endpoint into Identity as `UserIdentity__FrontendBaseUrl` for email verification links.

The local MongoDB container is standalone: no replica set, transactions, keyfile, host volume, or durable local data. It is not started through Compose and uses no root `.env`. Container replacement/removal loses local data. The committed MongoDB credentials and matching base connection strings are development-only; deployment must override `ConnectionStrings__MongoDB`.

## Configuration names

- Aspire-managed: `ConnectionStrings__MongoDB`, service HTTP-port configuration, `IDENTITY_API_BASE_URL`, `PROFILE_API_BASE_URL`, `UserIdentity__FrontendBaseUrl`
- Service settings: `*:DatabaseName`, `UserIdentity:FrontendBaseUrl`, `UserIdentity:Jwt:{Issuer,Audience,AccessTokenDays,PrivateKeyPem}`, `UserProfile:Jwt:{Issuer,Audience,PublicKey}`, `UserProfile:ProfilePictures:{StorageRoot,PublicBaseUrl,MaxUploadBytes}`, `Smtp:*`
- Environment override forms use ASP.NET Core double underscores, including `UserIdentity__FrontendBaseUrl`, `UserIdentity__Jwt__PrivateKeyPem` and `UserProfile__Jwt__PublicKey`

Matching development JWT private/public values are committed in the Identity/Profile base appsettings. They support local development only and must be overridden with deployment-managed secrets outside development.

## Live OpenAPI

Identity/Profile expose `/openapi/v1.json` outside Production. For `api:refresh` or `api:check:live`, copy each current HTTP endpoint from the Aspire dashboard, append `/openapi/v1.json`, and set both `IDENTITY_OPENAPI_URL` and `PROFILE_OPENAPI_URL`.

## Runtime caveats

- Application services currently expose no health/readiness endpoints.
- Notification jobs are non-distributed. Email processing is limited to one concurrent command per Notifications process with a two-minute execution limit; multiple instances are not coordinated. Handler failures are rescheduled one minute later.
- Verification email links use configured `UserIdentity:FrontendBaseUrl` + `/verify/{code}` (frontend origin). Deployments must set this to the public SPA/BFF URL. Default profile-picture URLs still derive from request scheme/host or `UserProfile:ProfilePictures:PublicBaseUrl`. No service configures forwarded-header middleware; reverse-proxy deployments need an explicit public URL and trusted-header strategy.
- Profile-picture deployment/storage/public-URL strategy remains unresolved.

## Sources

- `backend/AppHost/Program.cs`
- `backend/AppHost/HelpDesk.AppHost.csproj`
- `backend/Services/`
- `frontend/scripts/openapi.mjs`
