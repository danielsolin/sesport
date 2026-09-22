# Repository Review

Use this checklist for recurring repository maintenance and cleanup. Keep the
scope focused, preserve unrelated work, and make the smallest change that
improves the repository.

## 1. Establish the Scope

- Check the working tree before making changes.
- Read the applicable `AGENTS.md` instructions.
- Identify the files, projects, and behavior affected by the review.
- Preserve unrelated local changes.
- Do not remove code or data until its ownership and usage are understood.

## 2. Keep Code in the Correct Project

- Put each file in the project that owns its responsibility.
- Keep `SESport.Core` for shared domain types, identifiers, helpers, and
  code-defined application configuration.
- Keep `SESport.Core.Configuration` for configuration defaults, option types,
  environment-variable resolution, keys, and connection-string construction.
  This also applies to subsystem-specific PostgreSQL, AI, web-search, and
  other runtime settings.
- Keep `SESport.Data` for PostgreSQL persistence, SQL, Npgsql usage, and data
  source creation.
- Keep `SESport.AI` for AI provider clients, prompts, and AI job execution.
- Keep `SESport.MCP` for MCP transport, tool definitions, and web-search and
  page-fetch implementations.
- Keep `SESport.Web` for Razor Pages, hosted workers, and application
  orchestration.
- Keep host-specific configuration sources and composition in the executable
  project. For example, Web owns `appsettings.json`, configuration binding,
  and dependency-injection registration.
- Keep UI copy, route and query names, SQL, protocol field names, and
  implementation-only invariants in their owning project unless they are
  intentionally configurable.
- Treat deployment settings and tunable application behavior as configuration.
  This includes limits, windows, timeouts, and public-page display
  thresholds. Razor views and PageModels must consume bound options rather
  than define these values locally.
- Avoid `<Compile Remove>` unless there is no better fix.

## 3. Remove Dead or Obsolete Code

- Use `rg` to find all references before removing a type, method, constant, or
  file.
- Remove helpers with one real call site when they add no meaningful value.
- Remove commented-out code and unreachable branches.
- Code used only by tests is not automatically dead. Keep it when it models a
  deliberate test case, special case, or supported contract.
- Remove stale TODOs and notes about work that is already complete.
- Do not remove code merely because it is currently unused without checking
  reflection, dependency injection, configuration, and tooling entry points.

## 4. Reduce Duplication and Tighten Queries

- Keep logic in one place whenever practical.
- Move repeated text and fixed values into shared constants or helpers when
  they have shared meaning.
- In `SESport.Data`, select only the columns consumers actually read.
- Remove joins, counts, and projections that exist only to provide unused
  fields.
- Preserve query behavior when simplifying a query, especially filtering,
  ordering, grouping, and null handling.

## 5. Check Fixtures, Paths, and Documentation

- Update fixture paths when inputs or fixtures move.
- Use `YYYY-MM-DD` in examples unless the exact date is important.
- Move recurring operational guidance into a dedicated document.
- Keep documentation concise, specific, and consistent with the current
  project structure.

## 6. Verify the Changes

- Inspect database-backed tests before running them. Some tests claim push
  notifications or update stale AI runs across the database. For a full
  suite, establish an isolated database with schema and reference data.
  Override its database name only for the test process; do not edit `.env`.
  Remove the temporary database after verifying the target and completion.
- Run a build or the most relevant project-level tests after code removal.
- Re-run tests after file moves, renamed constants, or date changes.
- After shared cleanup, run:
  - `dotnet test tests/SESport.Core.Tests`
  - `dotnet test tests/SESport.Data.Tests`
  - `dotnet test tests/SESport.Web.Tests`
- Review the final diff and confirm that no unrelated files changed.
- Record the checks that were run and note any verification that could not be
  completed.

## Review Outcome

Report:

1. The changes made and the reason for each change.
2. The tests, builds, or searches that were run.
3. Any unresolved risks, follow-up work, or checks that could not be completed.
