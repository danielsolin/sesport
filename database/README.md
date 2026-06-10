# Database

This folder contains the database baseline schema plus future migration
scripts.

`001_baseline.sql` defines the current schema from scratch, including the
lookup tables, entity model, activity model, TV sport imports, AI jobs, and
the reference rows needed by the application.

Future schema changes should be added as new numbered SQL files after the
baseline.

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

Import the curated entity watchlist after migrations:

```bash
dotnet run --project tools/SESport.ImportEntities/SESport.ImportEntities.csproj
```

If the local database drifted from the baseline, recreate the local Postgres
volume before rerunning migrations.

On Windows, run the bash script from WSL if Docker is only available there.

The database schema is intentionally small while the ingestion model is still
forming. Prefer simple, auditable SQL until the persistence layer needs a
higher-level migration tool.
