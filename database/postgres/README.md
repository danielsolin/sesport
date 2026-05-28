# PostgreSQL

This folder contains explicit PostgreSQL migration scripts.

The first migration creates the `competitions` table and seeds the 2026 IIHF
Ice Hockey World Championship as an ongoing competition.

Start PostgreSQL with Docker Compose:

```powershell
docker compose up -d postgres
```

Run migrations in order:

```powershell
.\database\postgres\run-migrations.ps1
```

The database schema is intentionally small while the ingestion model is still
forming. Prefer simple, auditable SQL until the persistence layer needs a
higher-level migration tool.
