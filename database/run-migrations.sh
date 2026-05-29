#!/usr/bin/env bash
set -euo pipefail

database="${SESPORT_POSTGRES_DB:-sesport}"
user="${SESPORT_POSTGRES_USER:-sesport}"

migration_dir="database/migrations"

if [ ! -d "$migration_dir" ]; then
   echo "Migration directory not found: $migration_dir" >&2
   exit 1
fi

shopt -s nullglob
migrations=("$migration_dir"/*.sql)

if [ ${#migrations[@]} -eq 0 ]; then
   echo "No migration files found in $migration_dir"
   exit 0
fi

for migration in "${migrations[@]}"; do
   file_name="$(basename "$migration")"
   container_path="/migrations/$file_name"

   echo "Running $file_name"

   docker compose exec -T postgres \
      psql \
      -U "$user" \
      -d "$database" \
      -v ON_ERROR_STOP=1 \
      -f "$container_path"
done
