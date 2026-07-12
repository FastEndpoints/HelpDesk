#!/usr/bin/env bash
set -euo pipefail

usage() {
	printf 'Usage: %s <domain>\n' "${0##*/}" >&2
	exit 2
}

[[ $# -eq 1 ]] || usage
domain=${1,,}

if (( ${#domain} > 253 )) || [[ ! $domain =~ ^([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$ ]]; then
	printf 'Invalid domain: %s\nPass a hostname without a scheme, path, or port.\n' "$domain" >&2
	exit 2
fi

repo_root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
cd "$repo_root"

if [[ -e .env ]]; then
	printf '.env already exists; refusing to overwrite it.\n' >&2
	exit 1
fi

command -v openssl >/dev/null 2>&1 || {
	printf 'openssl is required.\n' >&2
	exit 1
}

umask 077
tmp_dir=$(mktemp -d)
env_tmp=$(mktemp ./.env.tmp.XXXXXX)
trap 'rm -rf "$tmp_dir"; rm -f "$env_tmp"' EXIT

private_pem=$tmp_dir/private.pem
openssl genrsa -traditional -out "$private_pem" 3072 2>/dev/null
mongo_password=$(openssl rand -hex 32)
jwt_private=$(openssl rsa -in "$private_pem" -traditional -outform DER 2>/dev/null | openssl base64 -A)
jwt_public=$(openssl rsa -in "$private_pem" -RSAPublicKey_out -outform DER 2>/dev/null | openssl base64 -A)

cat >"$env_tmp" <<EOF
DOMAIN=$domain

MONGO_INITDB_ROOT_USERNAME=helpdesk
MONGO_INITDB_ROOT_PASSWORD=$mongo_password

JWT_PRIVATE_KEY=$jwt_private
JWT_PUBLIC_KEY=$jwt_public

SMTP_ENABLED=false
SMTP_HOST=
SMTP_PORT=587
SMTP_USE_SSL=true
SMTP_USERNAME=
SMTP_PASSWORD=
SMTP_SENDER_NAME=HelpDesk
SMTP_SENDER_EMAIL=
SMTP_ADMIN_NAME=HelpDesk Admin
SMTP_ADMIN_EMAIL=
EOF
chmod 600 "$env_tmp"
if ! ln "$env_tmp" .env 2>/dev/null; then
	printf '.env appeared during initialization; refusing to overwrite it.\n' >&2
	exit 1
fi
rm -f "$env_tmp"
env_tmp=

printf 'Created %s/.env with generated MongoDB and JWT credentials.\n' "$repo_root"
printf 'Review SMTP settings, then run scripts/deploy.sh.\n'
