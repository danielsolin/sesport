# Daily AI Automation

This document defines the recurring verification job for the SESport public
front page. The job is normally a read-only audit that compares published
activity and participant information with the most specific reliable sources
available.

The job may propose corrections. It must not write corrections to the
database or publish changes until the proposal has been reviewed and approved.

## Automation maturity

The long-term goal is to automate this work as far as reliable evidence,
clear rules, and operational trust allow. Automation should expand gradually,
not by silently changing the meaning of an existing rule.

The current recurring operating mode is proposal-only:

- the job may fetch, compare, classify, and explain findings;
- the job may suggest database or display changes;
- the job must not apply changes or publish content automatically.

An operator may explicitly authorize a one-off manual application after
review. That authorization does not enable automatic changes for later runs.

Future automation levels may be enabled explicitly and separately for each
change type. Before a change type becomes automatic, its evidence threshold,
allowed scope, audit trail, and rollback or review path must be defined. A
previously approved suggestion must not by itself grant permission to apply
all similar future changes.

## Broadcast import

The broadcast data import is currently run manually, at least once per day,
with `bin/broadcasts-import.sh`. It may be run on another machine, so local
files under `data/broadcasts` may be stale or absent. The shared PostgreSQL
database is the source of truth for imported broadcasts.

Imported broadcasts are partly human-curated upstream material, not verified
editorial truth. They may be generic, incorrect, duplicated, outdated, or
otherwise unreviewed. SESport does not control that upstream curation; it
should use the import as input, verify it where possible, and report remaining
uncertainty clearly.

The AI must not infer the current import status from local files, file
timestamps, or the presence or absence of database rows. Before reviewing a
date, it must ask the operator whether the relevant broadcast data has been
imported into the shared database. The operator's answer is authoritative for
the current run and must be recorded in the report.

If the operator says that the data has not been imported, or is unsure, the
report must say so and must not claim that the broadcast review is complete.
An automatic import-status check may replace this question only when it can
reliably prove the import's scope and completion.

### Review horizon

The import window and the verification horizon are different things. The
importer currently collects roughly eight days of broadcasts, but that is not
a rule for how far ahead the AI should verify or propose activities.

The AI should review as far ahead as the available information makes
reasonable. Future plans, athlete statements, and general schedules are useful
leads but do not prove participation in a specific activity. Far-future
information is provisional and should be rechecked as the activity approaches.
When the evidence is too uncertain, report `REVIEW` instead of forcing a fixed
date-based decision.

## Broadcast curation state

Imported broadcasts are source material and also an operator curation queue.
The absence of a public activity does not by itself mean that a broadcast is
irrelevant or that no Swedish participation exists.

The `broadcasts.hidden_at` field has this editorial meaning:

- `hidden_at IS NULL`: the broadcast has not been dismissed. It is an open
  curation item that must be reviewed.
- A non-null `hidden_at`: an operator has chosen to hide it. This is a human
  curation decision, not ground truth. It may mean no Swedish participant,
  an out-of-scope transmission, a training match, a duplicate, or another
  editorial reason.

The verification job must therefore:

- inspect unhidden broadcasts for the relevant local date before claiming
  that an activity or source is missing;
- distinguish an unreviewed broadcast from a broadcast that was explicitly
  hidden by an operator;
- treat a hide decision as a strong prior, not as proof that the decision is
  correct;
- treat an unhidden broadcast with no activity as a candidate for matching
  to an existing activity or proposing a new activity;
- when a new broadcast gives a more specific scope for an existing published
  activity, reconcile that activity in the same run. Move or add only the
  participants supported by the specific source, preserve separately verified
  participants when the source covers only a subset, and link the handled
  broadcast records to the resulting activity;
- never infer "no Swedish relevance" from the absence of an activity link
  alone;
- re-open or report a hidden broadcast when newer or stronger evidence
  contradicts the earlier decision;
- never hide, delete, or otherwise dismiss a broadcast automatically under
  the current proposal-only mode.

Date-based review must use the broadcast's local time zone, normally
`Europe/Stockholm`, rather than its raw UTC date. Multiple source or channel
records may describe the same transmission, so they should be grouped before
proposing an activity or a hide decision.

If a date still has unhidden broadcasts, the report must identify curation as
incomplete even when all currently published activities pass their checks.

## External policy and capability changes

The workflow depends on the available AI model, tools, and operating policies.
If OpenAI changes its rules, model availability, tool access, or runtime
behavior in a way that makes a required step difficult or impossible, the job
must report the problem explicitly.

The job must not silently skip the affected check, present an unverified result
as `PASS`, or invent a replacement result. A blocker report should identify:

- the affected workflow step;
- what changed or is unavailable;
- the last successful check, when known;
- which findings or data are affected;
- a proposed workaround or the decision needed from the operator.

An affected check may be classified as `REVIEW` or `UNKNOWN`, but the external
blocker itself must remain visible in the report.

## AI worker and trust boundary

The current AI jobs that provide research and extraction data are executed by
a locally hosted `gpt-oss-20b` instance on separate infrastructure. This is
primarily a personal technical choice to explore local LLMs and VRAM-based
inference. It is not a claim that this is the technically best model or
deployment for the workflow.

The model's output must be treated as preliminary, low-trust input:

- it may extract information and propose findings;
- it must not be treated as an authoritative source;
- Codex must apply this document, verify the evidence, and make the final
  classification;
- no database or publication change may rely on model output alone.

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

### Source freshness and revisions

Official schedules, start lists, and broadcaster guides may be corrected or
updated on the day of an activity. The audit must fetch the current version
and record the source's publication or update time when it is available.

A later direct revision from a reliable source takes precedence over cached
research or an earlier version of the same page. If a source changes the
participant list or time after a previous check, the activity must be
re-evaluated before it is classified as `PASS`.

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

### Session participant-list verification

For each published activity, compare the complete participant set against the
most specific source available for that activity. The audit must verify
activity-to-person membership, not only whether each person exists in the
database.

- A person named by a session-specific source but missing from the published
  activity is an `OBVIOUS_ERROR`.
- A person published in a session or date where a reliable source places them
  elsewhere is an `OBVIOUS_ERROR`.
- A general championship roster does not prove membership in a particular
  session; such a mismatch is `REVIEW` unless more specific evidence exists.
- The report should distinguish missing participants, extra participants, and
  participants assigned to the wrong session or date.

For a final or other limited session, an overall team roster must not be
copied directly into the participant list. If the official source has not
identified the actual qualifiers, classify the membership as `REVIEW`, state
the uncertainty, and recheck it against the official start list when
published. A one-off editorial correction may use the strongest explicit
source clue, but it must remain subject to that recheck.

### Conditional participation markers

A source marker such as `Ev.` means that the person may participate if they
qualify, advance, or are selected for the relevant part of the session. It
supports candidate discovery and may justify creating a verified `Person`
entity, but it does not prove membership in the published activity.

Because the current activity-participant model has no conditional status, the
job must not add a person as a confirmed participant based on `Ev.` alone.
Keep the case as `REVIEW` until a start list, result, or more specific source
confirms the activity membership.

### Participant entities and disciplines

Participant completeness has two separate parts: the person entity must exist,
and the person must be linked to the activity. A reliable source-supported
Swedish participant who is missing from the database is a data-quality finding.
The job should propose creating a `Person` entity and adding the activity link.
An explicitly approved one-off run may create the data manually, but the
current recurring job remains proposal-only.

Creating a person requires evidence for the identity and the relevant fields.
The job must not create a person from an unverified name alone, copy data from
an ambiguous namesake, or invent a birth date, club, watch priority, or other
profile value. If the identity or required profile data cannot be resolved,
report `REVIEW` and explain what is missing.

For sports or activities where a discipline is relevant, every published
participant must also have a relevant relation to a `Discipline` entity. The
relation is stored in `entity_to_entity_links` between the person and an entity
whose `entity_type_id` is `Discipline`. The linked discipline must match the
sport and the activity evidence; adding a generic relation merely to fill the
public column is not sufficient.

This check is required even when the activity already has a visible discipline
column. `/Index` shows that column when at least one participant has a
discipline relation, but it renders an empty value for participants without a
matching relation. A partially populated column is therefore a data-quality
finding, not a successful completeness check.

For multi-event sports such as swimming, a discipline relation is not required
when the published activity is a grouped session covering several events. Do
not assign one participant's event discipline to the whole participant table
if that would create a misleading partial column. The activity title and
source may still carry the more specific event information.

Use `2026-08-15 Friidrotts-EM: Dag` as the canonical example. The audit must
first add or propose any source-supported Swedish participants missing from
the person table, then verify a discipline relation for every linked person,
such as `Sprint`, `Triple Jump`, or `Race Walking` when supported by the
relevant event evidence.

### Swedish relevance in specific activities

A specific activity may be identified by a contest involving a Swedish
participant and a foreign opponent. The Swedish participant is the reason the
activity belongs on SESport and is the participant to retain. The opponent may
remain in the activity title, description, or source metadata, but must not be
added to the SESport participant list solely because they take part in the
same contest.

When a generic broadcast activity is later resolved to a specific match,
update the activity scope and reconcile its participant links. Do not carry
over unrelated Swedish participants from the generic activity, and do not
treat a foreign opponent as a Swedish participant.

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

## Watch priority and public stars

`Watch Priority = 0` (`watch_priority_id = tier_0`) is reserved for `Person`
entities. From the Swedish editorial perspective, it means that the athlete
can reasonably be seen as being among the top competitors and as able to
contend for a medal at the Olympics, World Championships, or European
Championships. It is a performance-based curation claim, not a popularity or
participation marker.

The assessment should err towards inclusion for Swedish readers. Foreign
commentary or a lack of broad international consensus is not by itself a
reason to withhold the star. When a credible borderline case remains after
checking the available evidence, choose `tier_0` rather than creating a
false negative.

When a person participates in a published activity, priority 0 produces a
star beside the person's name on the public page. The audit must check both
the presentation and the underlying curation:

- a star should appear exactly when the participant is a person with
  `tier_0` priority;
- `tier_0` must not be assigned to non-person entities;
- each person with `tier_0` should have supporting evidence for the medal-
  contender assessment;
- strong medal contenders who lack `tier_0` should be reported as proposed
  candidates, never silently promoted.

Fame, a historic result by itself, or participation in a major event is not
enough to establish the priority. If the available evidence is incomplete or
stale, use Swedish editorial judgment; a credible borderline case may still
be included, while a genuinely unresolved case should be reported as
`REVIEW`.

### Editorial judgment

A `tier_0` decision may sometimes be based on deliberate Swedish editorial
judgment rather than decisive quantitative evidence. This is valid when the
purpose is to reflect Swedish audience relevance, recognition, or affinity.
It must be recorded as a judgment, not presented as an objective performance
fact.

For a borderline inclusion, record the editorial rationale separately from
the source evidence. Recommended report fields are:

- `basis: editorial_judgment`;
- `perspective: Swedish audience`;
- `decision: include_as_tier_0`;
- `reason: ...`;
- `quantitative_evidence: none_decisive`;
- `confidence: deliberate_borderline_inclusion`;
- reviewer and decision timestamp.

The judgment may override the absence of decisive numbers for a Swedish
audience decision, but it must not be disguised as a ranking, result, or
other source-derived fact.

### Candidate discovery and retrospective surprises

The audit must actively look for strong Swedish medal candidates who are
missing `tier_0`. It must not only validate the people who already have a
star.

For athletes entered in a major championship, the candidate check should
compare the information available before the relevant competition, including:

- current seasonal and relevant championship rankings;
- recent Olympic, World, European, or equivalent championship results;
- official start lists, previews, and federation assessments;
- Swedish context that supports a borderline editorial decision.

An exact medal result is not required for `tier_0`. The question is whether
the athlete could reasonably contend for a medal. A medal won later is
confirmation, not the sole basis for a retrospective promotion.

If an athlete wins an apparently unexpected medal but the pre-competition
evidence already supported medal contention, classify the case as a missed
candidate rather than an unforeseeable surprise. Record the pre-competition
evidence and propose `tier_0` through the normal review process.

The Wictor Petersson review is an example: his pre-competition European
ranking and recent major-championship result supported `tier_0` before his
European Championship medal. The result confirmed the assessment but did not
create it.

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

1. Ask the operator whether the relevant broadcast data has been imported
   into the shared database, and record the answer.
2. Fetch the front page and the underlying read-only activity data.
3. Capture the selected date, time zone, and check timestamp.
4. Deduplicate participants while retaining activity membership.
5. Check for missing person entities and missing activity-to-person links.
6. Check relevant person-to-`Discipline` relations and `/Index` completeness.
7. Determine the most specific scope supported by each activity and source.
8. Verify participation, time, birth date, and club evidence separately.
9. Compare exact source values with normalized display values.
10. Check public star rendering and the evidence for priority-0 people.
11. Run the reverse candidate check for strong people missing `tier_0`.
12. Produce a report with findings, sources, and proposed corrections.
13. Leave all database and publication changes for explicit review unless the
    operator has explicitly authorized a one-off manual application.

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
