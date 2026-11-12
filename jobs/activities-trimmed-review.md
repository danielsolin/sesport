# Review of activities-trimmed.md

The trimmed version preserves the main database safeguards and broadcast outcomes, but also
changes verification behavior. These changes need explicit limits and reliable run history.

## Risks and recommendations

1. **Unresolved items may never be researched again.** Each daily report can renew the
   three-day window. Carry forward the date of the last actual research, and calculate the
   interval from that date, not from the latest report.
2. **Unchanged stored data can hide external changes.** TV schedules, stream URLs, rosters,
   and elimination results can change without a database update. Define when each external
   source must be rechecked, especially near or during an event. A prior verification alone
   must not imply that an official list is still unchanged.
3. **Prior verification may not be reproducible.** The skip rules depend on when and what a
   previous run verified, but the report does not require those details. Record the relevant
   verification dates and inputs, or repeat the check when they cannot be established.
4. **The unresolved carry-over check is too narrow.** A description, category, channel, or
   linked context can change while the title and times remain the same. Re-research when
   relevant metadata changes, and use a shorter interval near the event start.
5. **Person facts have different lifetimes.** Birthdates and formative clubs are relatively
   stable; current team affiliations and profile links can change sooner. Set freshness
   periods per fact type and use the actual verification date. Recheck when it is unknown.
6. **A missed Monday skips the star sweep.** Record the last completed sweep and run it when
   due, including after a missed Monday. Define how an incomplete sweep is resumed.

Keep reuse within a run where evidence still applies. For reuse across runs, define the
required evidence and freshness for each check, and default to verification when uncertain.
