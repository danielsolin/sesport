# Repository Review

Use this checklist for recurring repository maintenance and cleanup. Review
the whole repository unless the operator explicitly narrows the scope. Base
findings on current code, configuration, schema, and runtime behavior. Make
the smallest justified changes and preserve unrelated work.

A directory listing, README scan, search result, or first plausible fix is
not a completed review. Finish the coverage and completion checks below
before reporting that the review is done.

## 1. Establish the Scope

- Check the working tree and read the applicable `AGENTS.md` instructions.
- Inventory the projects in `SESport.sln`, root configuration, and the
  relevant directories under `src/`, `tests/`, `tools/`, `bin/`, `jobs/`,
  `database/`, and `deploy/`. Check recent changes and the current diff as
  leads, not as the entire review scope.
- Identify the affected behavior, projects, files, and callers. Inspect the
  relevant configuration, database schema, and runtime behavior before making
  claims about them.
- Preserve unrelated local changes. Do not remove code or data until its
  ownership and usage are understood.
- Use `rg` to check references in source, Razor views, tests, tools, and
  configuration before moving or removing code.
- Record any operator-defined scope limits. Do not silently skip an area or
  claim full-repository coverage when the review was narrower.
- Do not commit or push changes unless the operator explicitly requests it.

## 2. Review Every Area in Scope

- Read every first-party, non-generated source and test file in the recorded
  scope. For a full review, this includes `src/`, `tests/`, `tools/`, and
  `bin/`, plus root and host configuration, migrations, deployment files,
  maintained documentation, and job instructions. Inventory generated,
  binary, historical-report, and vendored files and explain exclusions.
- Work through each feature area, not just one representative file per
  project. Trace entry points through callers, contracts, implementations,
  configuration, persistence, and relevant tests where those layers apply.
- Read the code around every promising search hit. A search match alone does
  not establish dead code, duplication, ownership, or a behavior defect.
- Check each area for the concerns in sections 3-6. Continue across the
  remaining areas after finding or fixing an issue.
- Keep a coverage record with each area's file count, files actually read,
  checks performed, concrete evidence for findings or no finding, and
  remaining work. "Looks fine" is not evidence.
  Update the operator after the inventory and again when inspection is done.

## 3. Keep Responsibilities in the Correct Project

- Keep `SESport.Core` for shared domain types, identifiers, formatting and
  broadcast rules, AI contracts, and provider-independent models. It must not
  contain live PostgreSQL access or provider-specific AI clients.
- Keep shared option types, defaults, keys, environment resolution, and
  connection-string construction in the `SESport.Core.Configuration` folder
  and namespace. It is part of `SESport.Core`, not a separate project.
- Keep `SESport.Data` for PostgreSQL repositories, SQL, Npgsql usage, data
  sources, and row mapping. It depends on Core, not `SESport.AI`.
- Keep `SESport.AI` for provider clients, prompt rendering, and AI job
  execution. It depends on Core contracts and must not issue SQL or depend
  on `SESport.Data`.
- Keep `SESport.MCP` for MCP transport, tool definitions, web search, page
  fetching, and approved database tools. Database tools use `SESport.Data`
  repositories; MCP handlers should reuse existing domain and service rules.
- Keep `SESport.Web` for Razor Pages, request handling, dependency injection,
  hosted workers, and application orchestration. It uses Data repositories
  instead of issuing SQL directly.
- Keep local import, collection, and operational programs in `tools/`; older
  manual tools live in `tools/legacy/`. Keep tests in the relevant project
  under `tests/`.
- Keep configuration sources, binding, and service registration in executable
  hosts. For example, Web owns `appsettings.json` and its dependency-injection
  setup; MCP owns its own host setup.
- Keep UI copy, route and query names, SQL, protocol fields, and local
  invariants in their owning project unless they have shared meaning.
- Treat limits, timeouts, windows, and display thresholds as configuration.
  Views, PageModels, and services should consume configured values.
- Use shared `PrimaryCountry` values for country-specific domain behavior.
  Site-specific parsing rules need a generally useful justification.
- Keep public-facing content in Swedish; `/Admin` is exempt. Prefer Razor
  partials and CSS over JavaScript-built HTML, and avoid `nth-child` in CSS.

## 4. Remove Dead or Obsolete Code

- Check all references before removing a type, method, constant, or file,
  including reflection, dependency injection, configuration, and tool entry
  points.
- Simplify wrappers and helpers that add no meaningful boundary or behavior.
- Remove commented-out code, unreachable branches, and notes about completed
  work once their status is verified.
- Do not treat code used only by tests as dead when it models a deliberate
  case or supported contract. Check configured callers before removing legacy
  compatibility code.

## 5. Reduce Duplication and Tighten Queries

- Keep a rule in one place when practical. Use shared constants or helpers
  for values with shared meaning, without adding unnecessary abstractions.
- In `SESport.Data`, select only the columns consumers read. Remove joins,
  counts, and projections used only for discarded fields.
- Preserve filtering, ordering, grouping, and null handling when simplifying
  SQL or moving logic.
- Avoid broad refactors without a concrete reason and verification plan.

## 6. Check Schema, Paths, and Documentation

- Keep `database/migrations/` for schema changes only. Never seed or update
  application data from a migration; use a verified `psql` target for an
  explicitly requested data change.
- Update affected fixture paths, examples, project READMEs, and operational
  instructions when behavior or files move. Keep documentation concise and
  consistent with the current project structure.
- Keep secrets from `.env`, generated data, private keys, and local
  operational output out of reports and commits.

## 7. Verify the Changes

- Run `git diff --check` and review the final diff for unrelated changes.
- For small or straightforward changes, do not create, update, or run
  automated tests until the operator asks for a commit.
- For larger code changes, build and run focused tests for affected projects.
  The solution has Core, Data, AI, MCP, and Web test projects under `tests/`;
  choose the relevant ones instead of running a fixed subset after every
  cleanup.
- Before running database-backed tests, inspect their effects and verify the
  effective connection target. The shared test bootstrap reads the root
  `.env`, which names the single active project database. Do not assume a
  separate test database or change `.env` as a routine test step.
- Keep database test activities on safely distant dates and unpublished
  unless publication is under test. Ensure cleanup also runs on failure.
  Review broad operations, such as push-notification claims or stale AI-run
  updates, before running a full suite against the active database.
- Record the searches, builds, and tests run, plus any checks that could not
  be completed.

## 8. Completion Gate and Report

Do not report the review as complete until:

1. Every required first-party file in the recorded scope has been read, with
   coverage counts, paths, checks, and exclusions listed by area. Mark unread
   files explicitly; do not count them as reviewed.
2. Each finding cites concrete file locations and behavior. Its disposition
   is clear: fixed and verified, or deferred with a specific reason. Do not
   hide an in-scope issue behind a general follow-up note.
3. Relevant callers, tests, configuration, docs, and the final diff have
   been checked after each change. Validation follows section 7.
4. The detailed coverage record and findings have been saved to
   `jobs/reports/repo-review-YYYY-MM-DD-HHMM.md`.

Summarize the coverage by project or area, findings and changes, checks run,
and remaining risks or gaps in the final response, with a link to the report.
A review with incomplete required coverage must be called partial; continue
it when possible instead of declaring the task finished. Zero findings is
valid only with the same coverage and evidence.
