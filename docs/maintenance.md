# Maintenance Checklist

Use this as a recurring clean-up pass for the repo.

## Dead Code Sweep

- Look for methods, classes, and helpers with only one real call site.
- Remove commented-out code and dead branches instead of leaving them as
  "just in case" code.
- Prefer conservative removals that are backed by `rg` and immediate builds.

## Structure Check

- Make sure files still live in the project that owns them.
- Keep namespaces, folders, and project references aligned.
- Avoid virtual ownership hacks like compile-time exclusion when a move is
  the correct fix.

## Test Drift

- Re-check tests after file moves, renamed constants, or changed date logic.
- Update fixture paths when source files or sample data move.
- Verify the important test projects after any cleanup pass.

## Docs Drift

- Keep temporary TODO notes out of long-lived documentation unless they are
  still actionable.
- Move recurring checklists and long-lived guidance into dedicated docs.
- Remove notes that only describe already-finished cleanup work.

## Suggested Cadence

- Run a light sweep after structural refactors.
- Run a broader sweep after major feature work or imports.
- Run a full repo maintenance pass before large releases.
