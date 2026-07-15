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

Backend services remain a host-local IPC FastEndpoints mesh. Aspire injects development ports and Identity/Profile listen on localhost; Production uses private container-reachable ports while keeping all three processes co-located. SvelteKit calls Identity/Profile only server-side. Neither topology provides a network or multi-host mesh transport.

## Aspire resource behavior

Aspire 13.4.6:

1. starts MongoDB (`mongo:8.2`) with committed development username/password parameters;
2. injects the `MongoDB` connection into Identity, Profile, and Notifications;
3. waits for MongoDB before starting dependent services;
4. starts Identity before Profile/Notifications complete their dependency chain;
5. starts Vite after Identity and Profile and injects their HTTP endpoints as `IDENTITY_API_BASE_URL` / `PROFILE_API_BASE_URL`;
6. injects the Vite HTTP endpoint into Identity as `UserIdentity__FrontendBaseUrl` for email verification links.

The local MongoDB container is standalone: no replica set, transactions, keyfile, host volume, or durable local data. It is not started through Compose and uses no root `.env`. Container replacement/removal loses local data. The committed MongoDB credentials and matching base connection strings are development-only; deployment must override `ConnectionStrings__MongoDB`.

## Production Compose topology

`compose.yaml` is the supported single-VPS production deployment and is separate from local Aspire orchestration. Caddy is the only public service and publishes host TCP ports 80/443; it obtains and renews public certificates automatically and reverse-proxies to the BFF over private HTTP. The edge network contains Caddy/BFF/backend; the internal data network contains backend/MongoDB, preventing direct edge-to-MongoDB access. Identity `8080`, Profile `8081`, Notifications IPC, BFF `3000`, and MongoDB stay private. All three .NET services run under `backend/Deployment/BackendLauncher`, which forwards shutdown and stops siblings when one exits. Named volumes persist Caddy state, MongoDB, and `/data/profile-pictures`.

Production values come from uncommitted `.env`; `.env.example` is only a manual template. `scripts/deploy-init.sh <domain>` safely creates `.env` once with a random hexadecimal MongoDB password and matching production JWT keys. Compose derives the public `https://` origins and MongoDB connection string from `DOMAIN` and the MongoDB credentials. `scripts/deploy.sh` selects Docker Compose or Podman Compose, validates, builds, starts, prints status, and performs a public HTTPS smoke test. `GET /profile-pictures/**` is proxied by the BFF to Profile.

Compose services use `restart: unless-stopped` for in-process/container crashes while the engine can supervise them. That does not by itself restore a Podman stack after host reboot. For Podman-only hosts, `scripts/install-host-service.sh` installs a systemd oneshot unit from `deploy/helpdesk.service.in` that runs `podman compose --env-file .env up -d` on boot (and `compose stop` on unit stop). Rootful installs `/etc/systemd/system/helpdesk.service`; rootless installs a user unit and enables linger. The install script is separate from `deploy-init.sh` (secrets) and from `deploy.sh` (build/release). Docker hosts rely on `docker.service` plus Compose restart policy instead of this unit.

See root `DEPLOYMENT.md` for provisioning and operations.

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
- Production profile pictures persist in the `profile_pictures` volume; Profile emits `https://${DOMAIN}/profile-pictures/...`, and the BFF proxies that public path to the private service.

## Sources

- `backend/AppHost/Program.cs`
- `backend/AppHost/HelpDesk.AppHost.csproj`
- `compose.yaml`
- `deploy/helpdesk.service.in`
- `scripts/deploy-init.sh`
- `scripts/deploy.sh`
- `scripts/install-host-service.sh`
- `DEPLOYMENT.md`
- `backend/Services/`
- `frontend/scripts/openapi.mjs`
