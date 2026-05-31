# Database

This folder contains explicit database migration scripts.

The first migration creates the entity-first activity proposal model:
tracked entities, activity proposals, review grouping, canonical activities,
entity links, and evidence.

Start PostgreSQL with Docker Compose:

```bash
docker compose up -d postgres
```

Run migrations in order from a Linux or WSL shell:

```bash
./database/run-migrations.sh
```

On Windows, the PowerShell helper can be used instead:

```powershell
.\database\run-migrations.ps1
```

The database schema is intentionally small while the ingestion model is still
forming. Prefer simple, auditable SQL until the persistence layer needs a
higher-level migration tool.
