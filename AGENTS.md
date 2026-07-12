# sesport Agent Guidelines

## Setup
1. Copy `.env.example` to `.env` and set the `SESPORT_POSTGRES_*` values
   for the single PostgreSQL database used by the project.
2. Install Docker on machines that run local SearXNG.
3. Start local SearXNG only on machines that run AI jobs:
   `docker compose up -d searxng`
4. Start PostgreSQL with Docker Compose only on the machine that is
   intentionally operating the database referenced by `.env`:
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
- `.env` is the source of truth for the PostgreSQL database connection.
  There is only one active project database. Code paths that fall back to
  localhost defaults are legacy guards and should not be treated as another
  active database.
- There is no separate test database. Any database-backed test or script
  talks to the live PostgreSQL database referenced by `.env` unless it is
  explicitly isolated in some other way.
- SearXNG is a local dependency for AI-run machines and defaults to
  `http://127.0.0.1:8088/`.
- Docker is required only for the local SearXNG container or when operating
  the PostgreSQL container for the database referenced by `.env`.
- `SESport.Data` uses Npgsql for PostgreSQL data access.
- Known build issue: `dotnet build SESport.sln` can fail in this
  environment during restore with missing workload SDK resolvers,
  including `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator`.
  When that happens, verify with project tests or per-project builds
  instead of re-investigating the same restore failure.
- Recurring repo-maintenance guidance lives in
  [docs/maintenance.md](docs/maintenance.md).
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

## Project Structure
- `src/SESport.Core`: shared domain types, identifiers, formatting helpers,
  broadcast parsing rules, country constants, and AI contracts/models that
  are used across project boundaries. It must not contain PostgreSQL access
  or provider-specific AI clients.
- `src/SESport.Data`: PostgreSQL persistence. Repository classes, SQL,
  Npgsql usage, data-source creation, and database-specific mapping belong
  here. It depends on `SESport.Core` and must not depend on `SESport.AI`.
- `src/SESport.AI`: AI provider clients, prompt rendering, web-search and
  page-fetching clients, activity-search orchestration, and AI job execution
  runtime. It depends on `SESport.Core` and must not contain PostgreSQL
  access.
- `src/SESport.Web`: Razor Pages UI, dependency injection, request handling,
  hosted workers, and application-level orchestration. It should call
  repositories from `SESport.Data` instead of issuing SQL directly.
- `tools/`: current import and collection tools. Tools may use `SESport.Data`
  for persistence and should read database settings from `.env` or an
  explicit `--connection-string`.
- `tools/legacy/`: older console tools kept for occasional manual use.
- `tests/`: test projects. Database-backed tests resolve their connection
  from `.env` through the shared test bootstrap.
