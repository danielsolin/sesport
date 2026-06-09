#!/usr/bin/env bash
set -euo pipefail

database="${SESPORT_POSTGRES_DB:-sesport}"
user="${SESPORT_POSTGRES_USER:-sesport}"

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/.." && pwd)"

default_backup_dir="$repo_root/data/db-backups"
backup_dir="${1:-$default_backup_dir}"

timestamp="$(date +%Y%m%d-%H%M%S)"
backup_file="$database-$timestamp.dump"
backup_path="$backup_dir/$backup_file"
container_backup_path="/tmp/$backup_file"

mkdir -p "$backup_dir"

cleanup() {
   docker compose exec -T postgres rm -f "$container_backup_path" >/dev/null 2>&1 || true
}

trap cleanup EXIT

echo "Creating backup for database '$database'."

docker compose exec -T postgres \
   pg_dump \
   -U "$user" \
   -d "$database" \
   -Fc \
   -f "$container_backup_path"

docker compose cp "postgres:$container_backup_path" "$backup_path"

if [[ ! -s "$backup_path" ]]; then
   echo "Backup failed: file is missing or empty: $backup_path" >&2
   exit 1
fi

size="$(du -h "$backup_path" | cut -f1)"
echo "Backup written to $backup_path ($size)"
