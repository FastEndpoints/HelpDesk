# HelpDesk

HelpDesk is a monorepo containing a SvelteKit frontend and a .NET 10 brokerless, event-driven microservice backend.

## Repository layout

```text
HelpDesk/
├── frontend/                     # SvelteKit application and server-only backend clients
├── backend/
│   ├── Common/                   # reusable infrastructure and helpers
│   ├── Contracts/                # public service event contracts
│   ├── Services/                 # UserIdentity, UserProfile, Notifications
│   └── Directory.Packages.props
├── HelpDesk.slnx
├── scripts/setup-mongodb.sh
├── compose.yaml
├── package.json                  # root workspace commands
└── pnpm-workspace.yaml
```

Backend services remain independently deployable. Cross-service business workflows use FastEndpoints remote events only: services do not reference another service implementation or call another service's REST API to complete internal work.

## Frontend boundary

`frontend/` is a SvelteKit application using `adapter-node`. It is the external-client/BFF boundary:

- browsers communicate with SvelteKit, not directly with backend service origins;
- typed `openapi-fetch` clients live under `frontend/src/lib/server/api/`, are server-only, and throw `ApiError` with preserved RFC problem details for non-success responses;
- backend origins are private runtime variables named `IDENTITY_API_BASE_URL` and `PROFILE_API_BASE_URL`; never expose them with a `PUBLIC_` prefix;
- JWTs returned by UserIdentity are held by the BFF in the `helpdesk_session` cookie, with `HttpOnly`, `SameSite=Lax`, `Path=/`, a maximum seven-day lifetime, and `Secure` in production. Do not expose JWTs to browser JavaScript or browser storage.

The current frontend is a foundation/landing page with server API helpers. Registration, login, verification, profile editing, and profile-picture UI flows are **not implemented**. Deployment decisions for the verification-link destination and profile-picture serving/public URL remain unresolved; those decisions block shipping the corresponding UI flows.

## Prerequisites and bootstrap

- .NET 10 SDK
- Node.js **26 or newer** (`.node-version` selects 26.4.0)
- pnpm **11 or newer** (`packageManager` selects 11.10.0 by default)
- Podman with a Compose provider (for example, the `podman-compose` package/command)
- OpenSSL (for the local MongoDB keyfile)

Install the pinned pnpm version from the repository root:

```bash
# Prefer Corepack when the installed Node distribution provides it.
corepack enable
corepack prepare pnpm@11.10.0 --activate

# If Corepack is unavailable:
npm install --global pnpm@11.10.0

pnpm install --frozen-lockfile
```

## Local infrastructure

Create local credentials and the MongoDB replica-set keyfile before starting Compose:

```bash
cp .env.example .env
# Replace MONGO_ROOT_PASSWORD in .env (and optionally the local username).
./scripts/setup-mongodb.sh
podman compose up -d
podman compose ps
```

MongoDB is bound to `127.0.0.1:27017` by default. Set `MONGO_PORT` in `.env` if that host port is already occupied. Configure each backend service with the authenticated local connection string (replace values to match `.env`):

```text
mongodb://helpdesk_local_admin:<password>@localhost:27017/?authSource=admin&replicaSet=rs0&directConnection=true
```

For example, use the ASP.NET Core environment variable `ConnectionStrings__MongoDB` or user secrets. Do not commit credentials.

Lifecycle commands:

```bash
podman compose logs -f mongodb
podman compose down
podman compose down -v  # destructive: also removes local MongoDB data
```

`./scripts/setup-mongodb.sh` preserves an existing keyfile and restores mode `400`. Rotation invalidates the replica-set shared secret for running members, so stop MongoDB and run `./scripts/setup-mongodb.sh --rotate` only when intentional.

## Local JWT keys

Base appsettings intentionally leave the Identity signing key and Profile validation key empty. Generate one local RSA keypair and configure the matching private/public halves without committing either file:

```bash
mkdir -p .local/jwt
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 \
  -out .local/jwt/identity-private.pem
openssl rsa -pubout -in .local/jwt/identity-private.pem \
  -out .local/jwt/profile-public.pem
chmod 600 .local/jwt/identity-private.pem .local/jwt/profile-public.pem

dotnet user-secrets set "UserIdentity:Jwt:PrivateKeyPem" \
  "$(cat .local/jwt/identity-private.pem)" \
  --project backend/Services/UserIdentity/Services.UserIdentity.csproj
dotnet user-secrets set "UserProfile:Jwt:PublicKey" \
  "$(cat .local/jwt/profile-public.pem)" \
  --project backend/Services/UserProfile/Services.UserProfile.csproj
```

Environment variables are also supported as `UserIdentity__Jwt__PrivateKeyPem` and `UserProfile__Jwt__PublicKey`; preserve the PEM newlines. The Identity private key and Profile public key must come from the same pair or authenticated Profile requests will fail. Never commit generated keys, put the private key in frontend configuration, or use it in browser code.

## Run locally

After configuring the root `.env`, start the complete local stack in one foreground command:

```bash
pnpm stack:dev
```

The command creates `frontend/.env` and local JWT keys when missing, starts MongoDB and all application processes, and stops the complete stack cleanly on Ctrl+C. To run processes in separate terminals instead:

```bash
cp frontend/.env.example frontend/.env
pnpm backend:restore

dotnet run --project backend/Services/UserIdentity/Services.UserIdentity.csproj
dotnet run --project backend/Services/UserProfile/Services.UserProfile.csproj
dotnet run --project backend/Services/Notifications/Services.Notifications.csproj

pnpm frontend:dev
```

Local ports:

| Process | URL/port |
| --- | --- |
| SvelteKit dev server | `http://localhost:5173` |
| UserIdentity | `http://localhost:5000` |
| UserProfile | `http://localhost:5001` |
| MongoDB | `127.0.0.1:27017` |
| Notifications | no public HTTP port (IPC/jobs only) |

UserIdentity and UserProfile expose non-production OpenAPI documents at `/openapi/v1.json` and Scalar at `/scalar`.

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

Direct backend commands:

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

Playwright builds and previews the frontend on port `4173` for end-to-end tests.

## OpenAPI workflow

With UserIdentity on port 5000 and UserProfile on port 5001:

```bash
cd frontend
pnpm api:refresh       # fetch and normalize live specs into openapi/*.json
pnpm api:generate      # generate src/lib/api/generated/*.d.ts from snapshots
pnpm api:check         # verify generated types match committed snapshots; no live services needed
pnpm api:check:live    # verify live specs, snapshots, and generated types all match
```

`IDENTITY_OPENAPI_URL` and `PROFILE_OPENAPI_URL` may override the default live document URLs. Commit refreshed snapshots and generated types together after an intentional backend API change.

## Backend service map

| Service | Responsibility | Public API |
| --- | --- | --- |
| UserIdentity | Credentials, identity status, RSA JWT issuance, identity events | register, login, verify |
| UserProfile | Profile lifecycle and profile-picture backend capability | authenticated current-profile read/update and picture mutation |
| Notifications | Verification email jobs and SMTP/null delivery | none |

The onboarding event flow is registration → profile creation and verification issuance → notification email → identity verification → profile activation. Contract projects under `backend/Contracts/` are the only cross-service language; they contain service names and events, not persistence or service-local behavior.
