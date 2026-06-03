#!/usr/bin/env bash
set -euo pipefail

if [ $# -lt 1 ]; then
   echo "Usage: database/restore.sh <backup.dump> [--yes]" >&2
   exit 1
fi

database="${SESPORT_POSTGRES_DB:-sesport}"
user="${SESPORT_POSTGRES_USER:-sesport}"
backup_path="$1"
confirm="${2:-}"

if [ ! -f "$backup_path" ]; then
   echo "Backup file not found: $backup_path" >&2
   exit 1
fi

if [ "$confirm" != "--yes" ]; then
   echo "Restore will clean and replace objects in database '$database'."
   read -r -p "Type RESTORE to continue: " answer

   if [ "$answer" != "RESTORE" ]; then
      echo "Restore cancelled."
      exit 1
   fi
fi

backup_file="$(basename "$backup_path")"
container_restore_path="/tmp/$backup_file"

cleanup() {
   docker compose exec -T postgres rm -f "$container_restore_path" >/dev/null 2>&1 || true
}

trap cleanup EXIT

echo "Copying $backup_path into postgres container."
docker compose cp "$backup_path" "postgres:$container_restore_path"

echo "Restoring database '$database' from $backup_file."
docker compose exec -T postgres \
   pg_restore \
   -U "$user" \
   -d "$database" \
   --clean \
   --if-exists \
   --no-owner \
   "$container_restore_path"

echo "Restore completed."
