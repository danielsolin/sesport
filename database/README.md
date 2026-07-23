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

The repository-root `.env` file is the source of truth for the active
PostgreSQL database. The project has one active database; do not document or
assume a second local, development, or service database.

If no variables are set, the code still falls back to the legacy local
defaults for `sesport` on `localhost:5432`. Treat that only as a defensive
fallback. Normal application, script, and test runs should use `.env`.

The helper scripts in `bin/` and the integration-test bootstrap read these
values from the repository-root `.env` file, so they target the same
database by design.

The `postgres` and `searxng` containers in `compose.yaml` are deliberately
started by service name. Start `postgres` only on the machine that is
intentionally operating the database referenced by `.env`. Start `searxng`
only on machines that run AI jobs.

To connect with `psql`, source the variables into your shell first, or pass
them explicitly:

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
lookup tables, entity model, activity model, broadcast imports, AI jobs, and
their indexes and constraints. It contains no application data; reference
rows must be managed outside migrations.

Future schema changes should be added as new numbered SQL files after the
baseline.

Start an interactive PostgreSQL session in the Docker container only when
this machine is operating the database referenced by `.env`:

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

Start PostgreSQL with Docker Compose only when this machine is intentionally
operating the database referenced by `.env`:

```bash
docker compose up -d postgres
```

Start local SearXNG with Docker Compose on machines that run AI jobs:

```bash
docker compose up -d searxng
```

Run migrations from a Linux or WSL shell:

```bash
./bin/db-run-migrations.sh
```

If the active database already has the current schema and you want to
start using the new migration history without changing the schema, mark the
baseline as applied:

```bash
./bin/db-mark-baseline-applied.sh
```

If the Postgres volume for the active database drifted from the baseline,
recreate the volume before rerunning migrations.

On Windows, run the bash script from WSL if Docker is only available there.

The database schema is intentionally small while the ingestion model is still
forming. Prefer simple, auditable SQL until the persistence layer needs a
higher-level migration tool.
