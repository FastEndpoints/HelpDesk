#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
cd "$repo_root"

if [[ ! -f .env ]]; then
	printf '.env is missing. Run scripts/deploy-init.sh <domain> first.\n' >&2
	exit 1
fi

env_mode=$(stat -c '%a' .env 2>/dev/null || true)
if [[ ! $env_mode =~ ^[0-7]*00$ ]]; then
	printf '.env must not be readable or writable by group/other users; run chmod 600 .env.\n' >&2
	exit 1
fi

if command -v docker >/dev/null 2>&1 && docker compose version >/dev/null 2>&1; then
	compose=(docker compose)
elif command -v podman >/dev/null 2>&1 && podman compose version >/dev/null 2>&1; then
	compose=(podman compose)
else
	printf 'Docker Compose or Podman Compose is required.\n' >&2
	exit 1
fi

mapfile -t domain_values < <(sed -n 's/^DOMAIN=//p' .env | tr -d '\r')
if [[ ${#domain_values[@]} -ne 1 ]]; then
	printf '.env must contain exactly one unquoted DOMAIN value.\n' >&2
	exit 1
fi
domain=${domain_values[0],,}
if (( ${#domain} > 253 )) || [[ ! $domain =~ ^([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$ ]]; then
	printf 'DOMAIN in .env must be an unquoted hostname without a scheme, path, or port.\n' >&2
	exit 1
fi

command -v curl >/dev/null 2>&1 || {
	printf 'curl is required for the deployment smoke test.\n' >&2
	exit 1
}

printf 'Validating Compose configuration...\n'
"${compose[@]}" --env-file .env config >/dev/null

printf 'Building and starting services...\n'
"${compose[@]}" --env-file .env up -d --build
"${compose[@]}" --env-file .env ps

printf 'Checking https://%s/ ...\n' "$domain"
if ! curl --fail --show-error --silent --retry 10 --retry-delay 3 --retry-all-errors \
	--connect-timeout 10 --max-time 30 "https://$domain/" >/dev/null; then
	printf 'Public smoke test failed. Recent edge logs:\n' >&2
	"${compose[@]}" --env-file .env logs --tail=100 caddy bff >&2
	exit 1
fi

printf 'Public edge reachable: https://%s/\n' "$domain"

if [[ ${compose[0]} == podman ]]; then
	printf 'Podman host: install or refresh the boot unit with scripts/install-host-service.sh\n'
fi
