# Daily AI Automation

This document defines the recurring verification job for the SESport public
front page. The job is a read-only audit that compares published activity and
participant information with the most specific reliable sources available.

The job may propose corrections. It must not write corrections to the
database or publish changes until the proposal has been reviewed and approved.

## Automation maturity

The long-term goal is to automate this work as far as reliable evidence,
clear rules, and operational trust allow. Automation should expand gradually,
not by silently changing the meaning of an existing rule.

The current operating mode is proposal-only:

- the job may fetch, compare, classify, and explain findings;
- the job may suggest database or display changes;
- the job must not apply changes or publish content automatically.

Future automation levels may be enabled explicitly and separately for each
change type. Before a change type becomes automatic, its evidence threshold,
allowed scope, audit trail, and rollback or review path must be defined. A
previously approved suggestion must not by itself grant permission to apply
all similar future changes.

## Core principles

- Use the highest specificity that the evidence supports.
- Never invent precision that is not present in the source material.
- Keep source wording separate from SESport display wording.
- Record the scope of every claim and every source.
- Prefer a clear review item over an unsupported automatic correction.
- Preserve exact source values even when the public display is rounded.

## Evidence scope

Every participation and time check should identify its scope. The supported
scope levels are:

- `competition`: the whole competition or championship.
- `competition-day`: the relevant date within the competition.
- `broadcast-session`: a broadcaster's transmission window.
- `match`: one match or named contest.
- `heat-or-race`: one heat, race, round, or discipline start.
- `unknown`: the source does not support a reliable scope.

The published activity should be checked at the same scope whenever possible.
For example, an overall championship roster cannot prove that an athlete is
in a particular evening session. If only an overall roster is available, the
report must say so and should not present the result as session-specific.

## Swedish participation

Participation is verified against official entry lists, start lists, draws,
rosters, or equivalent event sources. The check should use the most specific
available date, session, match, heat, or discipline.

The result must distinguish between:

- Swedish participation in the overall event.
- Swedish participation in the published activity or session.
- An unresolved case where the source does not expose sufficient detail.

The job must not turn a general event-level confirmation into a pass-specific
claim. Withdrawals, replacements, and late schedule changes should be kept as
reviewable evidence rather than silently inferred.

## Time verification

The job should compare the most specific available time:

- exact heat, race, or match start;
- otherwise the activity or broadcast session;
- otherwise the event-day window.

The source time must be stored exactly. SESport public times are rounded to the
nearest half hour and are considered good enough at that display precision.
The report should retain both values, for example:

```text
source_time: 18:25
display_time: 18:30
scope: broadcast-session
```

If the published item is a broadcast session but the comparison source is an
exact match, the job should report a scope mismatch rather than treating the
different times as a simple arithmetic error.

## Birth dates

Birth dates are exact facts and must not be rounded. The front page currently
renders an age, so the verifier should compare the stored birth date with
reliable athlete, federation, Olympic, or event profiles and recalculate the
displayed age for the selected date.

A matching age alone is not sufficient evidence for an exact birth date.

## Moderklubb/Klubb

The club check follows the person club policy in
`src/SESport.Core/README.md`:

1. the athlete's actual formative club;
2. the earliest known club in the athlete's development;
3. the athlete's current club.

The stored value is allowed to be a fallback. It must not be presented as
proof of an actual formative club unless the source evidence supports that
interpretation.

The verification report should record the evidence basis separately from the
club name, using values such as:

- `formative`
- `earliest-known`
- `current-fallback`
- `unresolved`

A current-club discrepancy is therefore not automatically an error. A
spelling or canonical-name discrepancy can still be an obvious correction.

## Source labels and display labels

The job must distinguish two labels:

- `source_label`: the wording used by the reliable source;
- `display_label`: the wording chosen for the SESport public page.

Terms such as `Dagspass` and `Kvällspass` are editorial labels, not factual
competition categories. Short labels such as `Dag` and `Kväll` may be a good
choice for paired daily broadcasts, as they were in the current athletics
example, but they are not universal vocabulary rules.

The display label should be chosen case by case using the available source
wording, the relationship between the activities, the risk of ambiguity, and
mobile readability. These labels are for display only and must not be
interpreted as official competition categories. `Sändningspass 1` and
`Sändningspass 2` are possible neutral alternatives when a day/evening pair
would be misleading.

If an official source explicitly calls a window `Försökspass` or `Finalpass`,
that source label may be shown or retained as metadata. The job must not infer
such a label merely from the time of day.

`Sim-EM` is the approved daily display label for the normal 50-metre,
long-course European swimming event. It is intentionally neutral and should
not be flagged as an error merely because it does not say `Långbana`. The
underlying event type must still be checked against authoritative sources.

## Proposed-change policy

Each finding should include:

- `status`;
- `scope`;
- current value;
- suggested value, when applicable;
- reason for the finding;
- source URLs;
- check timestamp and time zone.

Recommended statuses are:

- `PASS`: supported at the published scope;
- `REVIEW`: ambiguity, incomplete source detail, or scope mismatch;
- `OBVIOUS_ERROR`: a reliable source directly contradicts the value;
- `UNKNOWN`: no reliable evidence was found.

An `OBVIOUS_ERROR` should be highlighted clearly, but still remain a proposal.
The first implementation must not update or publish data automatically.

## Daily workflow

1. Fetch the front page and the underlying read-only activity data.
2. Capture the selected date, time zone, and check timestamp.
3. Deduplicate participants while retaining activity membership.
4. Determine the most specific scope supported by each activity and source.
5. Verify participation, time, birth date, and club evidence separately.
6. Compare exact source values with normalized display values.
7. Produce a report with findings, sources, and proposed corrections.
8. Leave all database and publication changes for explicit review.

Existing research jobs for participation, start times, and person data may be
reused. The verification job must compare their evidence with published
values; it must not blindly overwrite the values produced by those jobs.

## Example findings

These examples describe the intended behavior, not automatic changes:

- `Sim-EM` is accepted as a neutral display label when the source identifies
  the event as a 50-metre or long-course championship.
- `EM Kortbana` is an `OBVIOUS_ERROR` when the authoritative event is the
  50-metre championship.
- `Taby IS` versus `Täby IS` is an `OBVIOUS_ERROR` when the canonical source
  uses the latter spelling.
- A participant listed in the overall championship but absent from the
  relevant session is a `REVIEW` or `OBVIOUS_ERROR` depending on whether the
  activity is explicitly session-scoped.
