# SESport.AIActivitySearchImport

Imports saved AI activity search result JSON files into PostgreSQL as activity
proposals.

By default it imports every JSON file under `data/ai-activity-search-results`:

```powershell
dotnet run --project tools/SESport.AIActivitySearchImport
```

Import one file:

```powershell
dotnet run --project tools/SESport.AIActivitySearchImport -- `
   --file data/ai-activity-search-results/0001-sweden-men-s-football-national-team.json
```

Useful options:

- `--data <path>` imports a directory or one JSON file.
- `--file <path>` imports one JSON file. Can be repeated.
- `--connection-string <value>` overrides the PostgreSQL connection string.
