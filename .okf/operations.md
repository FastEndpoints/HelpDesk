---
type: Playbook
title: Operations
description: Local SvelteKit, backend service, and authenticated MongoDB runtime configuration.
tags: [ops]
resource: compose.yaml
---

# Operations

## Topology and ports

| Process | Local endpoint | Notes |
| --- | --- | --- |
| SvelteKit | `http://localhost:5173` | External-client/BFF boundary |
| UserIdentity | `http://localhost:5000` | OpenAPI/Scalar outside Production |
| UserProfile | `http://localhost:5001` | OpenAPI/Scalar outside Production |
| Notifications | none | IPC subscriber and jobs only |
| MongoDB | `127.0.0.1:27017` by default (`MONGO_PORT` overrides host port) | authenticated single-node replica set `rs0` |

Backend services are currently a host-local IPC FastEndpoints mesh: startup hardcodes `ListenInterProcess`, while Identity/Profile HTTP listeners use `ListenLocalhost`. SvelteKit calls Identity/Profile only from server modules. No current configuration or deployment manifest provides a network transport or multi-host topology.

`compose.yaml` starts MongoDB and its replica-set initializer only. It does not build or launch the frontend or backend services; start those processes separately. Compose health-checks MongoDB, but the application services currently expose no health/readiness endpoints.

## Private frontend environment

- `IDENTITY_API_BASE_URL` (required when its client is used)
- `PROFILE_API_BASE_URL` (required when its client is used)

Backend origins are private server variables. Never prefix them with `PUBLIC_`.

## MongoDB Compose

1. Install Podman plus a Compose provider (for example, `podman-compose`).
2. `cp .env.example .env`; set both Mongo credentials. Compose fails fast with an actionable error when either is absent.
3. `./scripts/setup-mongodb.sh` creates `.local/mongodb-keyfile` with mode `400`, or preserves an existing keyfile. Stop MongoDB and pass `--rotate` only for intentional rotation.
4. `podman compose up -d`; inspect with `podman compose ps` / `podman compose logs -f mongodb`.
5. Stop with `podman compose down`; `down -v` also deletes local data.

Connection string (substitute `.env` credentials):

```text
mongodb://helpdesk_local_admin:<password>@localhost:27017/?authSource=admin&replicaSet=rs0&directConnection=true
```

The query parameters are required for root authentication and the local single-node transaction-capable topology. Set backend `ConnectionStrings__MongoDB` or equivalent user secrets; base appsettings still contain an unauthenticated placeholder and are not sufficient for Compose.

## Backend configuration names

`ConnectionStrings:MongoDB`, `*:DatabaseName`, `UserIdentity:Jwt:{Issuer,Audience,AccessTokenDays,PrivateKeyPem}`, `UserProfile:Jwt:{Issuer,Audience,PublicKey}`, `UserProfile:ProfilePictures:{StorageRoot,PublicBaseUrl,MaxUploadBytes}`, and `Smtp:*`. Base JWT keys are empty. Generate one RSA pair, give Identity the private PEM and Profile its matching public PEM through user secrets or multiline-preserving environment variables; exact commands are in root `README.md`. Never commit real secrets or generated keys.

All three service projects currently declare the same ASP.NET Core `UserSecretsId`; local settings therefore share one user-secrets store. Use fully qualified configuration keys and consider all service secrets co-located.

## Runtime caveats

- Notification jobs are non-distributed. Email processing is limited to one concurrent command per Notifications process with a two-minute execution limit; multiple instances are not coordinated. Handler failures are rescheduled one minute later.
- Verification links and default profile-picture URLs derive from the raw request scheme/host. No service configures forwarded-header middleware; reverse-proxy deployments need an explicit public URL and trusted-header strategy.
- Deployment destination for verification links and deployment/storage/public-URL strategy for profile pictures remain unresolved. Those decisions block shipping the associated frontend UI flows.

## Sources

- `compose.yaml`
- `.env.example`
- `frontend/.env.example`
- `scripts/setup-mongodb.sh`
- `backend/Services/`
