# Tools

## SESport.ImportEntities

Imports the curated entity watchlist from `data/entity-watchlist.json` into
PostgreSQL. The import is idempotent: entities and sports are upserted with
stable deterministic ids.

Run from the repository root after database migrations:

```bash
dotnet run --project tools/SESport.ImportEntities/SESport.ImportEntities.csproj
```

Use `--data <path>` or `--connection-string <value>` to override defaults.
