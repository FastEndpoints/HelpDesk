# Production deployment

The committed Compose stack is the production deployment path. Aspire remains the only supported local full-stack orchestrator.

## Topology

```text
Internet HTTP/HTTPS
  -> caddy:80/443 (automatic HTTPS)
  -> bff:3000 (SvelteKit HTTP)
  -> backend:8080 (UserIdentity)
  -> backend:8081 (UserProfile)

backend container
  -> UserIdentity + UserProfile + Notifications over host-local IPC
  -> mongodb:27017
```

Only Caddy publishes host ports. The BFF, backend HTTP, IPC, and MongoDB stay on private Compose networks. Caddy certificates, MongoDB data, and profile pictures use named volumes.

## 1. Prepare the VPS and DNS

Install Docker Engine with the Compose plugin, OpenSSL, and curl. Clone the repository, then create an `A` record (and `AAAA` when IPv6 is configured) pointing the public hostname to the VPS. Use direct DNS, including Cloudflare's DNS-only mode. The hostname must resolve publicly; Caddy uses inbound TCP 80 or 443 for certificate validation.

At the VPS provider firewall and any Docker-aware host firewall:

1. Allow SSH only from the administration network required by your access policy.
2. Allow inbound TCP `80` and `443` from the internet.
3. Do not allow `3000`, `8080`, `8081`, or `27017`.

Docker-published ports can bypass ordinary UFW `INPUT` rules. Prefer the provider firewall, or enforce equivalent host rules through Docker's `DOCKER-USER` chain or the platform's Docker-aware nftables tooling.

## 2. Initialize production configuration

From the repository root, pass the hostname without `https://`, a path, or a port:

```bash
scripts/deploy-init.sh helpdesk.example.com
```

The script creates `.env` with mode `600`, a random hexadecimal MongoDB password, and a new matching 3072-bit RSA JWT pair. It refuses to overwrite an existing `.env`. The generated credentials are production secrets; back up `.env` securely and never commit it.

By default email delivery is disabled. To send real email, edit `.env`, set `SMTP_ENABLED=true`, and fill in the SMTP values. Port 465 normally uses implicit TLS; port 587 normally uses STARTTLS. This application uses `SMTP_USE_SSL=true` for both secure modes.

For manual setup instead, copy `.env.example` to `.env`, run `chmod 600 .env`, and replace every placeholder. Keep MongoDB username/password values to URI-unreserved characters (`A-Z`, `a-z`, `0-9`, `.`, `_`, `~`, and `-`) because Compose places the same raw values in both MongoDB initialization and the derived connection URI. Avoid `$`, which also has special meaning in Compose environment files. `deploy-init.sh` avoids these ambiguities by generating hexadecimal credentials.

## 3. Deploy

```bash
scripts/deploy.sh
```

The script:

1. selects Docker Compose, falling back to Podman Compose when Docker is unavailable;
2. validates Compose interpolation;
3. builds and starts the stack;
4. prints service status;
5. verifies `https://<DOMAIN>/`, retrying while Caddy obtains its certificate.

If the smoke test fails, the script prints recent Caddy and BFF logs. Common causes are incorrect DNS, blocked ports 80/443, or a malformed `DOMAIN` value.

Docker Compose is the production default. The Podman fallback is suitable when the host is configured to bind ports below 1024. On SELinux-enforcing hosts, Compose applies a private relabel to the Caddyfile mount; ensure the repository filesystem supports relabeling.

## Operations

Deploy a new revision:

```bash
git pull --ff-only
scripts/deploy.sh
```

Useful commands:

```bash
docker compose logs -f caddy bff backend mongodb
docker compose restart caddy bff backend
docker compose down
```

Use `podman compose` in place of `docker compose` when operating with Podman.

`docker compose down` preserves named volumes. **Do not use `docker compose down --volumes`** unless intentionally deleting MongoDB data, profile pictures, and Caddy's certificate state. Back up `mongodb_data` and `profile_pictures` before destructive maintenance or migration.

MongoDB initialization credentials apply only when its data volume is first created. Do not change the MongoDB username/password in `.env` after initialization without performing the corresponding database credential rotation.

## Optional CDN proxy

The simple path uses direct DNS. If a CDN proxy is later enabled, use strict origin TLS, restrict origin access to the provider's current source ranges, and verify that Caddy certificate renewal works through the proxy.
