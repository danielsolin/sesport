# sesport Agent Guidelines

## Language

- Source code, identifiers, comments, job instructions, and project
  documentation must be in English.
- All public-facing content must be in Swedish (does not include /Admin).
  This includes public UI text, emails, public error and status messages,
  and about/help content. Keep technical protocol names and external proper
  names unchanged.

## Rules

- For small or very straight-forward changes, do not create, update or run
  automated tests until the operator asks you to commit.
- Prefer server-rendered HTML and CSS, including Razor partials, over
  JavaScript that constructs HTML or DOM trees. Use JavaScript where it is
  needed for interaction, polling, or progressive enhancement.
- Treat limits, timeouts, windows, thresholds, and other tunable behavior as
  configuration. Avoid hard-coding them in views, PageModels, or services.
- The repository-root `.env` is the source of truth for PostgreSQL
  connection settings. There is one active project database and no separate
  test database unless a task explicitly establishes an isolated one.
- Database-backed tests must not create visible test data in the live UI.
  Use safely distant dates, keep test activities unpublished unless
  publication is part of the behavior under test, and clean up on failure.
- Country-specific behavior is acceptable when it is part of the product
  domain, but it must use
  `src/SESport.Core/Configuration/PrimaryCountry.cs` instead of hard-coded country
  names or country codes. Site-specific behavior is not acceptable unless
  it can be justified as a generally useful parsing, normalization, or
  extraction rule.
- Do not use `nth-child` selectors in CSS. Use semantic classes or other
  explicit selectors instead.
- Never seed application data from database migrations. Use migrations only
  for schema changes. If data must be added or changed, do it manually via
  `psql` so existing data cannot be altered by surprise.

## Workflow

- Inspect the current code, configuration, database schema, and runtime
  behavior before making claims about them.
- Keep changes focused and preserve unrelated worktree changes.
- Use `rg` to search the repository. Before removing code, verify all
  references, including references from tests and tools.
- Prefer the smallest change that fully addresses the task. Do not make
  broad refactors without a concrete reason and verification plan.
- Run `git diff --check` and focused tests or builds appropriate to the
  change. Report validation failures instead of hiding them.
- Do not commit or push changes unless the operator explicitly asks for it.
- Do not use destructive Git or database operations without explicit
  authorization and a verified target.

## Setup and Common Commands

- Copy `.env.example` to `.env` and set the `SESPORT_POSTGRES_*` values for
  the intended PostgreSQL database.
- Start local SearXNG only on machines that run AI jobs:
  `docker compose up -d searxng`.
- Start PostgreSQL with Docker Compose only on the machine intentionally
  operating the database referenced by `.env`:
  `docker compose up -d postgres`.
- Apply schema migrations using `bin/db-run-migrations.sh` after verifying
  the target database.
- Build the solution with `dotnet build`.
- Run all tests with `dotnet test`, or run a specific project, for example:
  `dotnet test tests/SESport.Core.Tests`.
- Run a local web instance with `dotnet run --project src/SESport.Web`; its local HTTP
  endpoint is `http://localhost:5109`.
- The hosted development site is `https://dev.sesport.se`.

## Project Structure

- `src/SESport.Core`: shared domain types, identifiers, formatting helpers,
  broadcast parsing rules, country constants, AI contracts and models, and
  code-defined application configuration. Configuration defaults, option
  types, environment-variable resolution, keys, and connection-string
  construction belong in `SESport.Core.Configuration`. Core must not contain
  live PostgreSQL access or provider-specific AI client implementations.
- Executable projects own their configuration sources, binding, and
  composition, such as Web's `appsettings.json` and dependency-injection
  registration.
- `src/SESport.Data`: PostgreSQL persistence, repositories, SQL, Npgsql
  usage, data-source creation, and database-specific mapping. It consumes
  configuration from `SESport.Core.Configuration`, depends on
  `SESport.Core`, and must not depend on `SESport.AI`.
- `src/SESport.AI`: AI provider clients, prompt rendering,
  activity-search orchestration, and AI job runtime.
  It consumes configuration from `SESport.Core.Configuration`, depends on
  `SESport.Core`, and must not contain PostgreSQL access.
- `src/SESport.MCP`: MCP transport, tool definitions, web-search and
  page-fetching implementations, and exposure of approved application
  capabilities. Reuse existing domain and service behavior instead of
  duplicating it in MCP handlers.
- `src/SESport.Web`: Razor Pages UI, dependency injection, request handling,
  hosted workers, and application-level orchestration. It should call
  repositories from `SESport.Data` instead of issuing SQL directly.
- `tools/`: private local import, collection, and operational tools. Tools
  may use `SESport.Data` for persistence and should read database settings
  from `.env` or an explicit `--connection-string`.
- `tools/legacy/`: private local older console tools kept for occasional
  manual use.
- `tests/`: test projects. Database-backed tests resolve their connection
  from `.env` through the shared test bootstrap.
