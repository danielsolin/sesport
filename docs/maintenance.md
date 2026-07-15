# Maintenance Checklist

Use this for recurring repo clean-up.

## Remove Dead Code

- Delete helpers with one real call site if they add no value.
- Remove commented-out code and dead branches.
- Back removals with `rg` and a build or test run.

## Keep Code Where It Belongs

- Put each file in the project that owns that code.
- Keep `SESport.Data` for PostgreSQL persistence and SQL only.
- Keep `SESport.Core` for shared domain types, IDs, and helpers.
- Keep `SESport.AI` for AI clients, prompts, and job execution.
- Keep `SESport.Web` for Razor Pages, workers, and orchestration.
- Avoid `<Compile Remove>` unless there is no better fix.

## Avoid Duplication

- Keep logic in one place whenever practical.
- Keep repeated text and fixed values in shared constants or helpers.

## Check Drift

- Re-run tests after file moves, renamed constants, or date changes.
- Update fixture paths when inputs move.
- Re-run `tests/SESport.Core.Tests` and
  `tests/SESport.BroadcastImporter.Tests` after shared cleanup.
- Remove stale TODOs and notes about finished work.

## Keep Docs Tight

- Use `YYYY-MM-DD` in examples unless the exact date matters.
- Move recurring guidance into dedicated docs.
