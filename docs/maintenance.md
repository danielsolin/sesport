# Maintenance Checklist

Use this for recurring repo clean-up.

## Remove Dead Code

- Delete helpers with one real call site if they add no value.
- Remove commented-out code and dead branches.
- Back removals with `rg` and a build or test run.

## Keep Code Where It Belongs

- Put each file in the project that owns that code.
- Keep `SESport.Data` for PostgreSQL persistence and SQL only.
- Keep `SESport.Core` for shared domain types, IDs, helpers, and all
  code-defined application configuration.
- Put configuration defaults, option types, environment-variable resolution,
  keys, and connection-string construction in
  `SESport.Core.Configuration`, even when they are specific to PostgreSQL,
  AI providers, web search, or another subsystem.
- Keep configured runtime implementations in their owning projects. For
  example, `SESport.Data` creates Npgsql data sources and `SESport.AI`
  creates provider, search, browser, and page-fetch clients.
- Keep host-specific configuration sources and composition in the executable
  project. For example, Web owns `appsettings.json`, configuration binding,
  and dependency-injection registration, while the bound option types and
  defaults remain in `SESport.Core.Configuration`.
- Treat deployment/site settings and tunable application behavior, such as
  limits, windows, timeouts, and public-page display thresholds, as
  configuration. Razor views and PageModels must consume bound options rather
  than define those values locally.
- Keep UI copy, route/query names, SQL, protocol field names, and
  implementation-only invariants in their owning project unless they are
  intentionally configurable.
- Keep `SESport.AI` for AI clients, prompts, and job execution.
- Keep `SESport.Web` for Razor Pages, workers, and orchestration.
- Avoid `<Compile Remove>` unless there is no better fix.

## Avoid Duplication

- Keep logic in one place whenever practical.
- Keep repeated text and fixed values in shared constants or helpers.

## Keep Queries Tight

- In `SESport.Data`, only `SELECT` columns that the consumers actually read.
- Remove joins, counts, and extra projections that exist only to feed unused
  fields.

## Check Drift

- Re-run tests after file moves, renamed constants, or date changes.
- Update fixture paths when inputs move.
- Re-run `tests/SESport.Core.Tests` and
  `tests/SESport.BroadcastImporter.Tests` after shared cleanup.
- Remove stale TODOs and notes about finished work.

## Keep Docs Tight

- Use `YYYY-MM-DD` in examples unless the exact date matters.
- Move recurring guidance into dedicated docs.
