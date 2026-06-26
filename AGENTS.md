# SE Sport Agent Guidelines

## Setup
1. Copy `.env.example` to `.env` (adjust if needed)
2. Start PostgreSQL: `docker compose up -d` (run in WSL if Docker is only
available there)
3. Run database migrations:
   - Bash: `./bin/db-run-migrations.sh` (run in WSL if Docker is only
     available there)

## Building
- Build solution: `dotnet build`

## Running the Web Application
- After setup, run: `dotnet run --project src/SESport.Web`
- The web app will be available at http://localhost:5109

## Running Tests
- Run all tests: `dotnet test`
- To run tests for a specific project: `dotnet test tests/SESport.Core.Tests`

## Legacy Tools
Several console applications live in `tools/legacy/` for occasional use:
- `SESport.ImportEntities`: Imports entities from (AI-)curated JSON data
- `SESport.ImportEpg`: Imports TV broadcast data from iptv-epg.org
- `SESport.AIActivitySearch`: Performs AI-assisted activity search
- Run with: `dotnet run --project tools/legacy/<tool-folder>`

## Notes
- The solution targets .NET 10.0 SDK
- Database connection string defaults to
  Host=localhost;Port=5432;Database=sesport;Username=sesport;Password=sesport
- The web app uses Npgsql for PostgreSQL data access
- Ensure PostgreSQL is running and migrated before running the web app or
  import tools (PostgreSQL must be started via Docker in WSL if Docker is only
  available there)
- Known build issue: `dotnet build SESport.sln` can fail in this
  environment during restore with missing workload SDK resolvers,
  including `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator`.
  When that happens, verify with project tests or per-project builds
  instead of re-investigating the same restore failure.
- Recurring repo-maintenance guidance lives in
  [docs/maintenance.md](docs/maintenance.md).
- Be careful when publishing to `sesport-dev` with
  `./bin/web-publish-dev.sh` or a manual service restart. It restarts
  the dev web service and can interrupt any currently running AI job,
  which is especially relevant for long-running runs.
- Hard rule: Never seed application data from database migrations.
  Use migrations only for schema changes. If data must be added or
  changed, do it manually via `psql` so existing data cannot be altered
  by surprise.
- Hard rule: Avoid magic strings where practical. Prefer shared constants,
  enums, or helpers such as `TrackedEntityTypeIds`.
- Hard rule: No lines in any file should exceed 80 characters wide unless it's
  required for the file to work.
- Hard rule: All conversations/chats in Swedish, but everything produced for
  the project in English. If the user starts speaking English, respond in
  Swedish and remind them of this rule.
