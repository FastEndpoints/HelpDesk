#!/usr/bin/env bash
set -euo pipefail

root_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
cd "$root_dir"

if [[ ! -f .env ]]; then
  printf 'Error: .env is missing. Copy .env.example to .env and set local MongoDB credentials.\n' >&2
  exit 1
fi

# shellcheck disable=SC1091
source .env
: "${MONGO_ROOT_USERNAME:?Set MONGO_ROOT_USERNAME in .env}"
: "${MONGO_ROOT_PASSWORD:?Set MONGO_ROOT_PASSWORD in .env}"

if [[ ! -f frontend/.env ]]; then
  cp frontend/.env.example frontend/.env
  printf 'Created frontend/.env from frontend/.env.example.\n'
fi

mkdir -p .local/jwt
if [[ ! -s .local/jwt/identity-private.pem || ! -s .local/jwt/profile-public.pem ]]; then
  openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 \
    -out .local/jwt/identity-private.pem
  openssl rsa -pubout -in .local/jwt/identity-private.pem \
    -out .local/jwt/profile-public.pem
  chmod 600 .local/jwt/identity-private.pem .local/jwt/profile-public.pem
  printf 'Generated local JWT keys under .local/jwt/.\n'
fi

./scripts/setup-mongodb.sh

pids=()
compose_started=false
cleaned_up=false
cleanup_status=0

cleanup() {
  if [[ "$cleaned_up" == true ]]; then
    return
  fi
  cleaned_up=true

  # Keep repeated terminal signals from interrupting container teardown.
  trap '' INT TERM
  for pid in "${pids[@]}"; do
    kill -TERM -- "-$pid" 2>/dev/null || true
  done

  local watchdog_pid=
  if ((${#pids[@]})); then
    (
      sleep 10
      for pid in "${pids[@]}"; do
        kill -KILL -- "-$pid" 2>/dev/null || true
      done
    ) &
    watchdog_pid=$!
  fi

  if [[ "$compose_started" == true ]]; then
    podman compose down || cleanup_status=$?
  fi
  if ((${#pids[@]})); then
    wait "${pids[@]}" 2>/dev/null || true
    kill "$watchdog_pid" 2>/dev/null || true
    wait "$watchdog_pid" 2>/dev/null || true
  fi
}

on_exit() {
  local status=$?
  cleanup
  if ((status == 0 && cleanup_status != 0)); then
    status=$cleanup_status
  fi
  trap - EXIT
  exit "$status"
}

shutdown() {
  cleanup
  exit "$cleanup_status"
}

start_process() {
  setsid "$@" &
  pids+=("$!")
}

trap on_exit EXIT
trap shutdown INT TERM

compose_started=true
podman compose up -d

dotnet build HelpDesk.slnx

mongo_username=$(printf '%s' "$MONGO_ROOT_USERNAME" | node -e \
  "process.stdin.on('data', data => process.stdout.write(encodeURIComponent(data)))")
mongo_password=$(printf '%s' "$MONGO_ROOT_PASSWORD" | node -e \
  "process.stdin.on('data', data => process.stdout.write(encodeURIComponent(data)))")
mongo_connection="mongodb://${mongo_username}:${mongo_password}@localhost:${MONGO_PORT:-27017}/?authSource=admin&replicaSet=rs0&directConnection=true"
identity_private_key=$(<.local/jwt/identity-private.pem)
profile_public_key=$(<.local/jwt/profile-public.pem)

start_process env \
  "ConnectionStrings__MongoDB=$mongo_connection" \
  "UserIdentity__Jwt__PrivateKeyPem=$identity_private_key" \
  dotnet run --no-build --project backend/Services/UserIdentity/Services.UserIdentity.csproj

start_process env \
  "ConnectionStrings__MongoDB=$mongo_connection" \
  "UserProfile__Jwt__PublicKey=$profile_public_key" \
  dotnet run --no-build --project backend/Services/UserProfile/Services.UserProfile.csproj

start_process env \
  "ConnectionStrings__MongoDB=$mongo_connection" \
  dotnet run --no-build --project backend/Services/Notifications/Services.Notifications.csproj

start_process bash -c 'cd frontend && exec ./node_modules/.bin/vite dev'

printf '\nFull stack is starting:\n'
printf '  Frontend:      http://localhost:5173\n'
printf '  Identity API:  http://localhost:5000\n'
printf '  Profile API:   http://localhost:5001\n'
printf 'Press Ctrl+C to stop the full stack.\n\n'

set +e
wait -n "${pids[@]}"
status=$?
set -e

if ((status != 130)); then
  printf 'A stack process exited (status %d); stopping the full stack.\n' "$status" >&2
fi
exit "$status"
