#!/usr/bin/env bash
set -euo pipefail

usage() {
	cat >&2 <<'EOF'
Usage: install-host-service.sh [--remove]

Install or update a systemd unit that brings the HelpDesk Compose stack up on boot
using Podman Compose. Run after scripts/deploy-init.sh (and preferably after a
successful scripts/deploy.sh so images already exist).

  --remove   disable and remove the installed unit

Rootful (uid 0): installs /etc/systemd/system/helpdesk.service
Rootless:        installs ~/.config/systemd/user/helpdesk.service and enables linger
EOF
}

remove=0
while [[ $# -gt 0 ]]; do
	case $1 in
	--remove)
		remove=1
		shift
		;;
	-h | --help)
		usage
		exit 0
		;;
	*)
		printf 'Unknown argument: %s\n' "$1" >&2
		usage
		exit 2
		;;
	esac
done

repo_root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
cd "$repo_root"
unit_name=helpdesk.service
template=$repo_root/deploy/helpdesk.service.in

if [[ ! -f $template ]]; then
	printf 'Missing unit template: %s\n' "$template" >&2
	exit 1
fi

if ! command -v podman >/dev/null 2>&1; then
	printf 'podman is required. This host service is Podman-only.\n' >&2
	exit 1
fi

if ! podman compose version >/dev/null 2>&1; then
	printf 'podman compose is required (Podman Compose plugin or compatible provider).\n' >&2
	exit 1
fi

if ! command -v systemctl >/dev/null 2>&1; then
	printf 'systemctl is required.\n' >&2
	exit 1
fi

podman_bin=$(command -v podman)
if [[ $podman_bin != /* ]]; then
	resolved=$(readlink -f -- "$podman_bin" 2>/dev/null || true)
	if [[ -n $resolved && $resolved == /* ]]; then
		podman_bin=$resolved
	fi
fi
if [[ $podman_bin != /* ]]; then
	printf 'Could not resolve an absolute path for podman.\n' >&2
	exit 1
fi

if [[ $(id -u) -eq 0 ]]; then
	rootful=1
	systemctl_cmd=(systemctl)
	unit_dir=/etc/systemd/system
	wanted_by=multi-user.target
	scope=system
	status_hint="systemctl status $unit_name"
else
	rootful=0
	systemctl_cmd=(systemctl --user)
	unit_dir=${XDG_CONFIG_HOME:-$HOME/.config}/systemd/user
	wanted_by=default.target
	scope=user
	status_hint="systemctl --user status $unit_name"
fi

unit_path=$unit_dir/$unit_name

remove_unit() {
	if [[ -f $unit_path ]] || "${systemctl_cmd[@]}" cat "$unit_name" >/dev/null 2>&1; then
		"${systemctl_cmd[@]}" disable --now "$unit_name" >/dev/null 2>&1 || true
		rm -f -- "$unit_path"
		"${systemctl_cmd[@]}" daemon-reload
		printf 'Removed %s (%s).\n' "$unit_path" "$scope"
	else
		printf 'No %s unit installed for this %s scope.\n' "$unit_name" "$scope"
	fi
}

if [[ $remove -eq 1 ]]; then
	remove_unit
	exit 0
fi

if [[ ! -f .env ]]; then
	printf '.env is missing. Run scripts/deploy-init.sh <domain> first.\n' >&2
	exit 1
fi

env_mode=$(stat -c '%a' .env 2>/dev/null || true)
if [[ ! $env_mode =~ ^[0-7]*00$ ]]; then
	printf '.env must not be readable or writable by group/other users; run chmod 600 .env.\n' >&2
	exit 1
fi

if [[ $rootful -eq 0 ]]; then
	if ! command -v loginctl >/dev/null 2>&1; then
		printf 'loginctl is required to enable linger for rootless Podman.\n' >&2
		exit 1
	fi
	if ! loginctl enable-linger "$USER"; then
		printf 'Failed to enable linger for %s. Rootless units will not start at boot without it.\n' "$USER" >&2
		exit 1
	fi

	port_start=$(sysctl -n net.ipv4.ip_unprivileged_port_start 2>/dev/null || printf '1024')
	if [[ $port_start =~ ^[0-9]+$ ]] && ((port_start > 80)); then
		printf 'Warning: rootless Podman cannot bind ports below %s (current ip_unprivileged_port_start).\n' "$port_start" >&2
		printf 'Caddy needs host ports 80/443. Use rootful Podman, lower that sysctl, or configure equivalent capability/port mapping.\n' >&2
	fi
fi

tmp_unit=$(mktemp)
trap 'rm -f -- "$tmp_unit"' EXIT

while IFS= read -r line || [[ -n $line ]]; do
	line=${line//@REPO_ROOT@/$repo_root}
	line=${line//@PODMAN@/$podman_bin}
	line=${line//@WANTED_BY@/$wanted_by}
	printf '%s\n' "$line"
done <"$template" >"$tmp_unit"

mkdir -p -- "$unit_dir"
install -m 644 "$tmp_unit" "$unit_path"

"${systemctl_cmd[@]}" daemon-reload
"${systemctl_cmd[@]}" enable --now "$unit_name"

printf 'Installed %s (%s scope).\n' "$unit_path" "$scope"
printf 'Boot start: enabled. Status: %s\n' "$status_hint"
printf 'Release deploys still use scripts/deploy.sh (build + up). This unit only runs compose up -d / stop.\n'
