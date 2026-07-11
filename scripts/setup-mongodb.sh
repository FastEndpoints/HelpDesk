#!/usr/bin/env bash
set -euo pipefail

keyfile=.local/mongodb-keyfile
rotate=false

case "${1:-}" in
  "") ;;
  --rotate) rotate=true ;;
  *)
    printf 'Usage: %s [--rotate]\n' "$0" >&2
    exit 2
    ;;
esac

mkdir -p .local
umask 077

if [[ -e "$keyfile" || -L "$keyfile" ]]; then
  if [[ ! -f "$keyfile" || -L "$keyfile" ]]; then
    printf 'Error: %s must be a regular file, but another filesystem object exists there.\n' "$keyfile" >&2
    printf 'Stop MongoDB, remove that path, and rerun this script.\n' >&2
    exit 1
  fi

  if [[ ! -s "$keyfile" ]]; then
    printf 'Error: %s is empty. Stop MongoDB, remove it, and rerun this script.\n' "$keyfile" >&2
    exit 1
  fi

  if [[ "$rotate" == false ]]; then
    chmod 400 "$keyfile"
    printf 'Preserved existing %s; use --rotate to replace it.\n' "$keyfile"
    exit 0
  fi
fi

temporary_keyfile=$(mktemp .local/mongodb-keyfile.XXXXXX)
trap 'rm -f "$temporary_keyfile"' EXIT
openssl rand -base64 756 > "$temporary_keyfile"
chmod 400 "$temporary_keyfile"
mv -f "$temporary_keyfile" "$keyfile"
trap - EXIT
printf '%s %s with restrictive permissions.\n' "$([[ "$rotate" == true ]] && printf 'Rotated' || printf 'Created')" "$keyfile"
