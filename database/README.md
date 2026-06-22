# Database

This folder contains the database baseline schema plus future migration
scripts.

The application resolves the default database connection from environment
variables, in this order:

1. `SESPORT_POSTGRES_HOST`
2. `SESPORT_POSTGRES_PORT`
3. `SESPORT_POSTGRES_DB`
4. `SESPORT_POSTGRES_USER`
5. `SESPORT_POSTGRES_PASSWORD`

If no variables are set, the code falls back to the local defaults for
`sesport` on `localhost:5432`.

The helper scripts in `bin/` read these values from the repository-root
`.env` file.

To connect with `psql` after copying `.env.example` to `.env`, source the
variables into your shell first, or pass them explicitly:

```bash
set -a
. ./.env
set +a

PGPASSWORD="$SESPORT_POSTGRES_PASSWORD" \
  psql -h "$SESPORT_POSTGRES_HOST" \
  -p "$SESPORT_POSTGRES_PORT" \
  -U "$SESPORT_POSTGRES_USER" \
  -d "$SESPORT_POSTGRES_DB"
```

`001_baseline.sql` defines the current schema from scratch, including the
lookup tables, entity model, activity model, TV sport imports, AI jobs, and
the reference rows needed by the application.

Future schema changes should be added as new numbered SQL files after the
baseline.

Start interactive PostgreSQL session in docker container:

```bash
docker compose exec -it postgres psql -U sesport -d sesport
```

If you want to mirror the application config exactly, use the environment
variables from `.env`:

```bash
set -a
. ./.env
set +a

docker compose exec -it postgres env \
  PGPASSWORD="$SESPORT_POSTGRES_PASSWORD" \
  psql -h localhost -U "$SESPORT_POSTGRES_USER" -d "$SESPORT_POSTGRES_DB"
```

Start PostgreSQL with Docker Compose:

```bash
docker compose up -d postgres
```

Run migrations from a Linux or WSL shell:

```bash
./bin/db-run-migrations.sh
```

If you already have a local database with the current schema and want to
start using the new migration history without changing the schema, mark the
baseline as applied:

```bash
./bin/db-mark-baseline-applied.sh
```

If the local database drifted from the baseline, recreate the local Postgres
volume before rerunning migrations.

On Windows, run the bash script from WSL if Docker is only available there.

The database schema is intentionally small while the ingestion model is still
forming. Prefer simple, auditable SQL until the persistence layer needs a
higher-level migration tool.
