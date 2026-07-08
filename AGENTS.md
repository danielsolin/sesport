# sesport Agent Guidelines

## Setup
1. Copy `.env.example` to `.env` and point it at the shared PostgreSQL
   database on `207.2.120.181:15285`, unless you are deliberately using an
   isolated replacement.
2. Install Docker on machines that run local SearXNG.
3. Start local SearXNG only on machines that run AI jobs:
   `docker compose up -d searxng`
4. Start PostgreSQL with Docker Compose only on the VPS/database host:
   `docker compose up -d postgres`
5. Run database migrations:
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
- The active development and service database is `207.2.120.181:15285`.
  The code falls back to localhost defaults only when no environment
  variables are set.
- SearXNG is a local dependency for AI-run machines and defaults to
  `http://127.0.0.1:8088/`.
- Docker is required only for the local SearXNG container or when operating
  the VPS/database-host PostgreSQL container.
- The web app uses Npgsql for PostgreSQL data access
- Ensure PostgreSQL is reachable and migrated before running the web app or
  import tools.
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
- Hard rule: Country-specific behavior is acceptable when it is part of the
  product domain, but it must use `src/SESport.Core/Domain/PrimaryCountry.cs`
  instead of hard-coded country names or country codes. Site-specific behavior
  is not acceptable unless it can be justified as a generally useful parsing,
  normalization, or extraction rule.
- Hard rule: No lines in any file should exceed 80 characters wide unless it's
  required for the file to work.
- Hard rule: All conversations/chats in Swedish, but everything produced for
  the project in English. If the user starts speaking English, respond in
  Swedish and remind them of this rule.
