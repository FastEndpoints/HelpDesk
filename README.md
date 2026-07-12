# HelpDesk

HelpDesk is a monorepo containing a SvelteKit frontend and a .NET 10 brokerless, event-driven microservice backend. .NET Aspire is the supported local full-stack orchestrator.

## Repository layout

```text
HelpDesk/
├── frontend/                     # SvelteKit application and server-only backend clients
├── backend/
│   ├── AppHost/                  # Aspire local full-stack orchestrator
│   ├── Common/                   # reusable infrastructure and helpers
│   ├── Contracts/                # public service event contracts
│   ├── Services/                 # UserIdentity, UserProfile, Notifications
│   └── Directory.Packages.props
├── HelpDesk.slnx
├── package.json                  # root workspace commands
└── pnpm-workspace.yaml
```

Backend services remain independently deployable. Cross-service business workflows use FastEndpoints remote events only: services do not reference another service implementation or call another service's REST API to complete internal work.

## Frontend boundary

`frontend/` is a SvelteKit application using `adapter-node`. It is the external-client/BFF boundary:

- browsers communicate with SvelteKit, not directly with backend service origins;
- typed `openapi-fetch` clients live under `frontend/src/lib/server/api/`, are server-only, and throw `ApiError` with preserved RFC problem details for non-success responses;
- backend origins are private runtime variables named `IDENTITY_API_BASE_URL` and `PROFILE_API_BASE_URL`; Aspire injects both into Vite—never expose them with a `PUBLIC_` prefix;
- JWTs returned by UserIdentity are held by the BFF in the `helpdesk_session` cookie, with `HttpOnly`, `SameSite=Lax`, `Path=/`, a maximum seven-day lifetime, and `Secure` in production. Do not expose JWTs to browser JavaScript or browser storage.

The frontend implements registration, login, email verification, profile editing, profile-picture upload/delete, and signed-in shell chrome through server-side BFF actions. Production serves picture URLs through the BFF's public `/profile-pictures/**` proxy while the Profile origin remains private.

## Production deployment

Production uses the committed `compose.yaml`: Caddy is the only public service and manages HTTPS automatically, SvelteKit runs behind it as the BFF, all three .NET services run together for host-local IPC, and MongoDB/profile pictures use persistent named volumes.

See [DEPLOYMENT.md](DEPLOYMENT.md) for DNS, production secrets, firewall rules, automatic HTTPS, startup, and operations.

```bash
git clone <repository-url> HelpDesk
cd HelpDesk
scripts/deploy-init.sh helpdesk.example.com
# Optionally enable and configure SMTP in .env.
scripts/deploy.sh
# Podman-only hosts: install the boot-time systemd unit after a successful deploy.
scripts/install-host-service.sh
```

Compose is production-only. Local full-stack development continues to use Aspire. On Podman-only hosts, `scripts/install-host-service.sh` installs a systemd unit so the stack returns after reboot; see [DEPLOYMENT.md](DEPLOYMENT.md).

## Prerequisites and bootstrap

- .NET 10 SDK
- Node.js **26 or newer** (`.node-version` selects 26.4.0)
- pnpm **11 or newer** (`packageManager` selects 11.10.0 by default)
- a running Aspire-compatible container runtime for the MongoDB resource

Install the pinned pnpm version from the repository root:

```bash
# Prefer Corepack when the installed Node distribution provides it.
corepack enable
corepack prepare pnpm@11.10.0 --activate

# If Corepack is unavailable:
npm install --global pnpm@11.10.0

pnpm install --frozen-lockfile
```

## Run locally

`backend/AppHost/Program.cs` is the sole supported local full-stack orchestrator. Start it through the root workspace command:

```bash
pnpm stack:dev
```

The Aspire 13.4.6 AppHost starts:

- an ephemeral, authenticated standalone MongoDB container using committed development credentials;
- UserIdentity, UserProfile, and Notifications;
- the Vite/SvelteKit frontend.

Aspire assigns application HTTP ports dynamically, displays resource endpoints in its dashboard, injects the MongoDB connection into all backend services, and injects the Identity/Profile API origins into Vite. MongoDB uses the fixed development endpoint `localhost:27017` so backend tests can use the same Aspire-managed resource. Stop the stack with Ctrl+C.

This local MongoDB has no replica set, transaction support, keyfile, or host volume. Local startup does not use Compose, a root `.env`, or repository setup scripts. Data is ephemeral and is lost with the container. Its committed username/password and matching base connection strings are development-only; deployments must override `ConnectionStrings__MongoDB`.

UserIdentity and UserProfile expose non-production OpenAPI documents at `/openapi/v1.json` and Scalar at `/scalar`. Use the Aspire dashboard rather than assuming fixed localhost ports.

## Development JWT keys

Matching development-only RSA values are committed in the base service settings:

- Identity private value: `backend/Services/UserIdentity/appsettings.json`
- Profile public value: `backend/Services/UserProfile/appsettings.json`

They allow the Aspire development stack to run without generating local keys. They are not production secrets and must not be reused for deployed environments. Standard ASP.NET Core configuration environment variables can override them as `UserIdentity__Jwt__PrivateKeyPem` and `UserProfile__Jwt__PublicKey`; overrides must remain a matching pair.

## Commands

Root workspace commands:

```bash
pnpm backend:restore
pnpm backend:build
pnpm backend:build:release
pnpm backend:test
pnpm backend:format:check
pnpm stack:dev
pnpm frontend:dev
pnpm frontend:check
pnpm frontend:lint
pnpm frontend:format:check
pnpm frontend:test:unit
pnpm frontend:test:e2e
pnpm frontend:build
pnpm frontend:api:check
pnpm check:quick
pnpm check:full
```

Direct backend commands (start `pnpm stack:dev` first for MongoDB-backed tests):

```bash
dotnet restore HelpDesk.slnx
dotnet build HelpDesk.slnx
dotnet test HelpDesk.slnx -c Debug
dotnet format HelpDesk.slnx --verify-no-changes
```

Frontend commands (from `frontend/`):

```bash
pnpm dev
pnpm check
pnpm lint
pnpm format:check
pnpm test:unit
pnpm exec playwright install       # first-time browser installation
pnpm test:e2e
pnpm build
```

`pnpm` continues to manage frontend development and repository validation. `pnpm frontend:dev` starts only the frontend and is not an alternative full-stack workflow. Playwright builds and previews the frontend on port `4173` for end-to-end tests.

## OpenAPI workflow

Start the stack with `pnpm stack:dev`. In the Aspire dashboard, copy the current HTTP URL for each Identity/Profile resource and append `/openapi/v1.json`. Export both complete document URLs before commands that fetch live specifications:

```bash
cd frontend
export IDENTITY_OPENAPI_URL='<identity-http-url>/openapi/v1.json'
export PROFILE_OPENAPI_URL='<profile-http-url>/openapi/v1.json'

pnpm api:refresh       # fetch and normalize live specs into openapi/*.json
pnpm api:generate      # generate src/lib/api/generated/*.d.ts from snapshots
pnpm api:check         # verify generated types match committed snapshots; no live services needed
pnpm api:check:live    # verify live specs, snapshots, and generated types all match
```

`IDENTITY_OPENAPI_URL` and `PROFILE_OPENAPI_URL` are required for `api:refresh` and `api:check:live`; there are no fixed-port defaults. Snapshot normalization removes the runtime-specific top-level `servers` entry so Aspire's dynamic ports do not create false changes. Commit refreshed snapshots and generated types together after an intentional backend API change.

## Backend service map

| Service | Responsibility | Public API |
| --- | --- | --- |
| UserIdentity | Credentials, identity status, RSA JWT issuance, identity events | register, login, verify |
| UserProfile | Profile lifecycle and profile-picture backend capability | authenticated current-profile read/update and picture mutation |
| Notifications | Verification email jobs and SMTP/null delivery | none |

The onboarding event flow is registration → profile creation and verification issuance → notification email → identity verification → profile activation. Contract projects under `backend/Contracts/` are the only cross-service language; they contain service names and events, not persistence or service-local behavior.
