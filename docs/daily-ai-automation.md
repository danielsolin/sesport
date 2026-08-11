# Daily AI Automation

This document defines the recurring verification job for the SESport public
front page. The job compares published activity and participant information
with the most specific reliable sources available and may apply
evidence-gated corrections directly.

The job must still leave uncertain findings for later review. Direct execution
is permitted only within the dispositions, evidence thresholds, and product
rules defined in this document.

## Automation maturity

The long-term goal is to automate this work as far as reliable evidence,
clear rules, and operational trust allow. Automation should expand gradually,
not by silently changing the meaning of an existing rule.

The project operator has authorized Codex to apply evidence-gated changes in
the recurring job. This replaces the former proposal-only default. The
authorization covers the following actions:

- link, reconcile, and hide broadcasts when the underlying transmission and
  activity relationship are clear;
- hide an unambiguous out-of-scope broadcast under the exclusion rule below;
- correct an existing activity, draft activity, participant link, person
  entity, discipline relation, time, club value, or watch priority when the
  relevant evidence and rules support the change;
- create a missing person or activity when identity, scope, Swedish relevance,
  and the required display data are sufficiently supported;
- publish an activity only when it passes the publication gate defined below.

The job must not apply a change classified as `REVIEW` or `UNKNOWN`. It must
not invent values, delete broadcasts, or silently broaden a source's scope.
When a change is applied, the run report must contain the affected identifier,
the action, the evidence, the timestamp, and the reversible rollback path.

This authorization applies to later runs of this job. It does not authorize
unrelated code changes, external messages, or changes outside the SESport
database and public presentation.

Automation must remain evidence-gated. Before applying a new class of change,
this document must define its evidence threshold, allowed scope, audit trail,
and rollback or review path. A previous approval must not justify a change
that falls outside these rules.

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
- A non-null `hidden_at`: an operator or the authorized automatic curation
  rule has chosen to hide it. This is a curation decision, not ground truth.
  It may mean no Swedish participant, an out-of-scope transmission, a
  training match, a duplicate, or another editorial reason.

The verification job must therefore:

- inspect unhidden broadcasts for the relevant local date before claiming
  that an activity or source is missing;
- distinguish an unreviewed broadcast from a broadcast that was explicitly
  hidden by an operator;
- treat a hide decision as a strong prior, not as proof that the decision is
  correct;
- treat an unhidden broadcast with no activity as a candidate for matching
  to an existing activity or creating a new activity;
- apply a safe source reconciliation directly when the broadcast is an exact
  source, duplicate, or clearly scoped segment of an existing activity;
- when a new broadcast gives a more specific scope for an existing published
  activity, reconcile that activity in the same run. Move or add only the
  participants supported by the specific source, preserve separately verified
  participants when the source covers only a subset, and link the handled
  broadcast records to the resulting activity;
- if a specific broadcast is only a segment within an already published
  session on the same channel, do not create an overlapping activity. Attach
  the specific source to the existing activity unless it is independently
  televised;
- never infer "no Swedish relevance" from the absence of an activity link
  alone;
- re-open or report a hidden broadcast when newer or stronger evidence
  contradicts the earlier decision;
- do not auto-hide a broadcast unless the automatic broadcast exclusion rule
  below matches;
- never delete a broadcast or alter its imported source values automatically.

Date-based review must use the broadcast's local time zone, normally
`Europe/Stockholm`, rather than its raw UTC date. Multiple source or channel
records may describe the same transmission, so they should be grouped before
proposing an activity or a hide decision.

If a date still has unhidden broadcasts, the report must identify curation as
incomplete even when all currently published activities pass their checks.

### SESport relevance boundary

The product scope follows the project README: SESport covers sport in an
international context when Swedish participation is relevant. Swedish
participation may be through an athlete, national team, Swedish club, coach,
or a person meaningfully involved in a foreign team.

Public activity participants are limited to people who actually compete in
the activity. Coaches, trainers, support crew, managers, and similar staff
must be omitted from the participant list, even when they travel with or
support a competing team. The exception is a rally co-driver, who is a
participant because they compete from inside the car. When a source
distinguishes racers from support staff, the roles must not be collapsed into
one generic participant type.

The following examples define the boundary:

- `AIK - Hammarby` in Allsvenskan is domestic sport and is out of scope.
- `SM i Friidrott` is a domestic Swedish championship and is out of scope,
  even when prominent Swedish athletes take part. Their opponents are other
  Swedish participants, so the competition is not international.
- `Hammarby - Arsenal` in the Champions League is an international candidate.
  The job must check which Swedish people are meaningfully involved before
  creating or updating a public activity.
- A friendly, training, or pre-season club match is out of scope even when
  Swedish people take part, unless stronger evidence establishes a separate
  relevant international competition.

An international match is therefore a candidate, not an automatic publication
decision. A public activity requires source-supported Swedish participation at
the most specific reliable scope available.

### Automatic broadcast exclusion

AI may automatically set `broadcasts.hidden_at` when the source material gives
an explicit and unambiguous reason that the broadcast is outside SESport's
scope, such as:

- a domestic league or cup match with no international context;
- a friendly, training, or pre-season match;
- an exact duplicate or short scheduling fragment of an already handled
  broadcast, when the underlying transmission is clear.

The job must not auto-hide an international competition merely because the
broadcast has no activity link or because Swedish participation has not yet
been found. A reliable source must establish the absence of relevant Swedish
participation before a negative decision is treated as safe. Every automatic
hide must be listed in the run report with its rule, evidence, timestamp, and
reversible broadcast identifier.

### Evidence-gated dispositions

Every reviewed broadcast or activity receives a status and a disposition
before the job changes anything. The status describes the evidence:

- `PASS`: the current data is supported and requires no change;
- `OBVIOUS_ERROR`: a reliable source directly contradicts the current value;
- `REVIEW`: the item is relevant or potentially relevant, but the evidence is
  incomplete, conflicting, or too broad for a safe change;
- `UNKNOWN`: no reliable evidence is available for the required check.

The disposition describes the action: `NONE`, `APPLIED`, `HIDDEN`, or
`LEFT_OPEN`. `APPLIED` is permitted only when reliable evidence supports the
correction at the published scope or a more specific scope.

`REVIEW` and `UNKNOWN` broadcasts remain unhidden. They must be listed as
open work, not treated as irrelevant. A generic competition broadcast may be
used to discover a candidate, but it does not prove that a person belongs to
a particular session, heat, match, or discipline.

For a direct change, prefer evidence in this order:

1. the official federation, organiser, competition, start list, draw, or
   roster source;
2. an official broadcaster page with explicit event, participant, or time
   information;
3. a reliable event or sports-news source that corroborates the claim;
4. imported broadcast metadata, which is a lead and not authoritative person
   evidence by itself.

An exact source match may be linked to the existing activity and hidden when
the activity relationship is clear. A source that is a segment of an existing
session must be linked to that session and must not create an overlapping
activity. A source that resolves a generic activity to a specific contest may
update the draft or activity scope and participant links when the evidence
supports the change.

The job may create a new activity when the international context, Swedish
relevance, activity scope, and time are sufficiently supported. It may create
or update the required person and relationship data when the identity is
unambiguous. Unsupported optional profile values remain unknown; they must
not be invented merely to pass the publication check.

### Publication gate

The job may publish an activity directly only when all blocking checks pass:

- the activity is within SESport's international relevance boundary;
- Swedish participation is supported at the activity's actual scope;
- the time and scope are specific enough for the public page;
- every published participant has a resolved person identity and activity
  link;
- required discipline relations exist, except for grouped multi-event
  activities where a discipline would mislead;
- the activity does not overlap another activity representing the same
  transmission;
- no blocking `REVIEW` or `UNKNOWN` finding remains.

For the public participant presentation, a name alone is not sufficient when
the profile is otherwise unsupported. After reasonable research, a person
with neither an exact birthdate nor a reliable formative/current club should
normally be omitted from the public participant list, while the database link
is kept for later review.

This is a collective quality check, not an all-or-nothing rule for every
person. A small number of missing birthdates or clubs in an otherwise well-
supported roster must not by itself remove the whole activity from the public
page. For example, one or two unresolved profiles in a football match with
15 well-established Swedish participants should not block the match. The
unresolved people may be omitted individually, and the report must explain
the omission.

If missing profile data is widespread or systematic, and the visible roster
would mainly look like names with blank context, keep the whole activity
unpublished rather than create a misleading impression of knowledge. There is
no fixed percentage threshold: use the activity size, the number and
importance of the gaps, the reliability of the remaining identities, and the
overall public impression. Record that judgment in the verification report.

A draft must remain unpublished when participation, scope, identity, timing,
or the overall profile quality is materially unresolved.

### Public-page sanity check

After the evidence checks and any proposed changes, open the actual public
index for each affected date, using `/Index?date=YYYY-MM-DD` (or the
equivalent public date URL). Review the page as a visitor would see it,
including empty age or club cells, partial participant lists, duplicate or
overlapping activities, and whether the overall presentation gives a
reasonable impression of knowledge.

This is a qualitative product check, not a binary truth test. There is no
exactly correct numeric threshold for when a participant list looks too thin.
The job must use judgment, state the reason, and choose `PASS`, `REVIEW`, or
`LEFT_OPEN` accordingly. A source-supported activity may still be held back
when its current public presentation looks misleading or premature.

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

Some AI jobs may use a locally hosted `gpt-oss-20b` instance on separate
infrastructure. Its presence is primarily the result of the operator's
personal technical interest in local LLMs and VRAM-based inference. It is an
experiment, not a required component of the daily workflow, and its absence
must not prevent the workflow from being completed with the available tools.
It is not a claim that this is the technically best model or deployment for
the workflow.

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
published. An editorial correction may use the strongest explicit source
clue, but it must remain subject to that recheck.

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
The job may create a `Person` entity and add the activity link directly when
the identity is unambiguous and the required fields are supported by reliable
evidence. It must leave the item as `REVIEW` when the identity, country
relevance, or required profile data cannot be resolved without guessing.

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

When the activity evidence supports the discipline, the job may create the
missing discipline relation directly. If the evidence does not support a
discipline, it must leave the relation absent rather than infer one from the
person's general sport.

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
first add any source-supported Swedish participants missing from the person
table, then verify a discipline relation for every linked person, such as
`Sprint`, `Triple Jump`, or `Race Walking` when supported by the relevant
event evidence.

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
- strong medal contenders who lack `tier_0` may be promoted directly when
  the assessment is supported by the evidence and Swedish perspective below;
  the rationale must be recorded in the run report.

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
evidence and apply `tier_0` when the normal evidence and Swedish editorial
rules support it. A result alone is not enough for a retrospective promotion.

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

## Disposition and change policy

Each finding should include:

- `status`;
- `disposition`;
- `scope`;
- current value;
- applied or suggested value, when applicable;
- reason for the finding;
- source URLs and source update time, when available;
- check timestamp and time zone;
- affected database identifier and rollback action when a change was applied.

Recommended statuses are:

- `PASS`: supported at the published scope;
- `REVIEW`: ambiguity, incomplete source detail, or scope mismatch;
- `OBVIOUS_ERROR`: a reliable source directly contradicts the value;
- `UNKNOWN`: no reliable evidence was found.

Recommended dispositions are:

- `NONE`: no change is required;
- `APPLIED`: an evidence-gated correction was executed;
- `HIDDEN`: a broadcast was hidden under a documented exclusion rule;
- `LEFT_OPEN`: a broadcast remains unhidden for later review.

An `OBVIOUS_ERROR` with reliable, non-conflicting evidence may be corrected
directly. A `REVIEW` or `UNKNOWN` finding must remain unchanged, except that
an explicitly safe broadcast exclusion may still be applied. The report must
make every applied change and every intentionally unresolved broadcast
visible; a successful database write is not evidence that the underlying
editorial decision was correct.

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
12. Classify each finding using the evidence-gated dispositions.
13. Apply permitted corrections in explicit database transactions. Never
    delete imported broadcasts or overwrite exact source values.
14. Re-read the affected database rows and public presentation after writes.
15. Produce a report with findings, sources, applied changes, rollback IDs,
    and broadcasts intentionally left open.

Existing research jobs for participation, start times, and person data may be
reused. The verification job must compare their evidence with published
values; it must not blindly overwrite the values produced by those jobs.

## Example findings

These examples illustrate the intended behavior and evidence thresholds:

- `Sim-EM` is accepted as a neutral display label when the source identifies
  the event as a 50-metre or long-course championship.
- `EM Kortbana` is an `OBVIOUS_ERROR` when the authoritative event is the
  50-metre championship.
- `Taby IS` versus `Täby IS` is an `OBVIOUS_ERROR` when the canonical source
  uses the latter spelling.
- A participant listed in the overall championship but absent from the
  relevant session is a `REVIEW` or `OBVIOUS_ERROR` depending on whether the
  activity is explicitly session-scoped.
- A full source for `Hammarby - Raków Częstochowa` may correct the draft's
  scope, time, source links, and supported participants directly. It may be
  published only when the resulting activity passes the publication gate;
  an unresolved match or broadcast scope remains `REVIEW`.
