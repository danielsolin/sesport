# Database

This folder contains explicit database migration scripts.

The first migration creates the entity-first activity proposal model:
lookup tables, tracked entities, activity proposals, review grouping,
canonical activities, activity entity links, and activity evidence. Canonical
activities always belong to one known activity date.

The second migration adds publication metadata for the manual launch site:
activity publication statuses, public slugs, and listing indexes.

Start PostgreSQL with Docker Compose:

```bash
docker compose up -d postgres
```

Run migrations in order from a Linux or WSL shell:

```bash
./database/run-migrations.sh
```

Import the curated entity watchlist after migrations:

```bash
dotnet run --project tools/SESport.ImportEntities/SESport.ImportEntities.csproj
```

During pre-launch development, incompatible schema rewrites may require
recreating the local Postgres volume before rerunning migrations.

On Windows, the PowerShell helper can be used instead:

```powershell
.\database\run-migrations.ps1
```

The database schema is intentionally small while the ingestion model is still
forming. Prefer simple, auditable SQL until the persistence layer needs a
higher-level migration tool.
