# Repository Code Review

Use this checklist for recurring review and cleanup of repository code. Review
the whole codebase unless the operator narrows the scope. Base findings on
tracked source, tests, schema files, and code-related configuration. Make the
smallest justified changes and preserve unrelated work.

A directory listing, README scan, search result, or first plausible fix is not
a completed review. Finish the coverage and completion checks below before
reporting that the review is done.

## 1. Establish the Scope

- Check the working tree and read the applicable `AGENTS.md` instructions.
- Inventory solution projects, root and project build configuration, and the
  relevant files under `src/`, `tests/`, `tools/`, `bin/`, and `database/`.
  Use recent changes and the current diff as leads, not as the whole scope.
- Review tracked application source, tests, migrations, SQL, and configuration
  that directly affects build or application behavior.
- Use supporting documentation only to understand code contracts or affected
  behavior. Exclude `deploy/`, historical reports, generated files, binaries,
  vendored content, and untracked files from review scope.
- Limit the review to the current repository. Do not query external services,
  websites, databases, or other runtime state.
- Preserve unrelated local changes. Do not remove code or data until its
  ownership and usage are understood.
- Use `rg` to check references in source, views, tests, tools, and tracked
  configuration before moving or removing code.
- Record operator-defined scope limits. Do not silently skip an area or claim
  full-repository coverage when the review was narrower.
- Do not commit or push changes unless the operator explicitly requests it.

## 2. Review Every Area in Scope

- Read every first-party, non-generated source and test file in the recorded
  scope, including application code, Razor, CSS, JavaScript, scripts, and SQL.
  Inventory excluded generated, binary, historical, and vendored files.
- Work through each feature area. Trace entry points through callers,
  contracts, implementations, configuration, persistence, and relevant tests.
- Read the code around every promising search hit. A search match alone does
  not establish dead code, duplication, ownership, or a behavior defect.
- Check each area for the concerns in sections 3-6. Continue across remaining
  areas after finding or fixing an issue.
- Continue the review until every required first-party file has been read and
  the completion gate is satisfied. Do not stop after finding issues or after
  writing a partial report. If a specific external blocker prevents progress,
  finish every independent review area and record only the blocked items.
- Keep a coverage record with each area's file count, files actually read,
  checks performed, evidence for findings or no finding, and remaining work.
  Update the operator after inventory and again when inspection is done.

## 3. Keep Responsibilities in the Correct Project

- Keep `SESport.Core` for shared domain types, identifiers, formatting and
  broadcast rules, AI contracts, provider-independent models, and shared
  configuration types. Keep database access and provider clients out of Core.
- Keep `SESport.Data` for PostgreSQL repositories, SQL, Npgsql usage, data
  sources, and row mapping. It depends on Core, not `SESport.AI`.
- Keep `SESport.AI` for provider clients, prompt rendering, and AI job
  execution. It depends on Core contracts and must not issue SQL or depend on
  `SESport.Data`.
- Keep `SESport.MCP` for MCP transport, tool definitions, web search, page
  fetching, and approved database tools. Database tools use `SESport.Data`
  repositories and reuse existing domain and service rules.
- Keep `SESport.Web` for Razor Pages, request handling, dependency injection,
  workers, and application orchestration. Use Data repositories instead of
  issuing SQL directly from Web.
- Keep import, collection, and operational programs in `tools/`; older
  manual tools live in `tools/legacy/`. Keep tests under `tests/`.
- Keep application configuration binding and service registration in
  projects. Keep UI copy, routes, protocol fields, SQL, and project invariants in
  their owning project unless they have shared meaning.
- Treat limits, timeouts, windows, and display thresholds as configuration.
  Use shared `PrimaryCountry` values for country-specific domain behavior.
- Keep public-facing content in Swedish; `/Admin` is exempt. Prefer Razor
  partials and CSS over JavaScript-built HTML, and avoid `nth-child` in CSS.

## 4. Remove Dead or Obsolete Code

- Check all references before removing a type, method, constant, or file,
  including reflection, dependency injection, configuration, and tool entry
  points.
- Simplify wrappers and helpers that add no meaningful boundary or behavior.
- Remove commented-out code, unreachable branches, and completed-work notes
  after verifying their status.
- Do not treat code used only by tests as dead when it models a deliberate
  case or supported contract. Check configured callers before removing legacy
  compatibility code.

## 5. Reduce Duplication and Tighten Queries

- Keep a rule in one place when practical. Use shared constants or helpers
  for values with shared meaning, without adding unnecessary abstractions.
- In `SESport.Data`, select only columns consumers read. Remove joins, counts,
  and projections used only for discarded fields.
- Preserve filtering, ordering, grouping, and null handling when simplifying
  SQL or moving logic.
- Avoid broad refactors without a concrete reason and verification plan.

## 6. Check Schema, Paths, and Documentation

- Keep `database/migrations/` for schema changes only. Never seed or update
  application data from a migration.
- Update affected fixture paths, examples, and project documentation when code
  or behavior changes. Keep documentation concise and consistent with the
  current repository structure.
- Keep secrets, generated data, private keys, and untracked artifacts out of
  review reports.

## 7. Verify the Changes

- Run `git diff --check` and review the final diff for unrelated changes.
- For small or straightforward changes, do not create, update, or run
  automated tests until the operator asks for a commit.
- For larger code changes, build and run focused tests for affected projects.
  Choose relevant test projects instead of running a fixed subset after every
  cleanup.
- Run only self-contained tests that do not require external services or
  persistent stores. Review other tests statically and report skipped runs.
- Record the searches, builds, and tests run, plus checks that could not be
  completed.

## 8. Completion Gate and Report

Do not report the review as complete until:

1. Every required first-party file in scope has been read, with coverage counts,
   paths, checks, and exclusions listed by area. Mark unread files explicitly.
2. Each finding cites concrete file locations and behavior. Its disposition is
   clear: fixed and verified, or deferred with a specific reason.
3. Relevant callers, tests, configuration, documentation, and the final diff
   have been checked after each change. Validation follows section 7.
4. The detailed coverage record and findings are saved to
   `jobs/reports/repo-review-YYYY-MM-DD-HHMM.md`.

Summarize coverage by project or area, findings and changes, checks run, and
remaining risks or gaps in the final response, with a link to the report.
Call reviews with incomplete required coverage partial and continue when
possible. Continue working until coverage is complete; a partial report is a
progress record, not a stopping point. Zero findings is valid only with the
same coverage and evidence.
