# Activities

Create or update activities from visible broadcasts.
Derive scope and dates from the database snapshot.
Run Steps 1–3, repeat Steps 4–6 per event, then finish Steps 7–8 with one final report.

## Rules

- Follow `AGENTS.md`; execute this authorized job without approval stops.
- Prioritize accuracy over time/cost. Use concise, correct Swedish for public content;
  preserve clear short forms.
- Read database settings from `.env`; inspect the target, schema, and comparable records.
- Use guarded manual `psql` transactions: exact IDs, expected row counts, rollback on mismatch,
  and readback verification. Preserve unrelated data.
- Apply supported changes; retain and report unresolved/conflicting facts, then continue.
- Track created/changed activity IDs, including group/link changes. Steps 5–6 verify only this set.
- Save every evidence source with its type:

  | Evidence | Type |
  | --- | --- |
  | Event, schedule, broadcast | `ActivityEvidence` |
  | Participation, roster, elimination | `ParticipationEvidence` |
  | Participant start time | `ParticipantStartEvidence` |
  | Star qualification | `ParticipantStarEvidence` |
  | Birthdate, formative club, person profile | `PersonFacts` |

## 1. Snapshot Broadcasts

1. Record the start timestamp. Snapshot every broadcast with `hidden_at` unset, across all dates
   and `processed_at` states. Later imports enter Step 7.
2. Record IDs, `processed_at`, titles, descriptions, categories, channels, times, organizations,
   and existing entity/activity links.

## 2. Hide Clearly Irrelevant Broadcasts

Eligible international competitions involve:

- Swedish athletes competing individually.
- A Swedish national team.
- A Swedish club/team.

Using only title, description, categories, channel, and linked context, hide:

- Domestic competitions, foreign-team fixtures (even with Swedish players), generic highlights,
  recaps, and studio programmes.
- Unidentified events, such as bare `EM` or `Dag 2`/`Golf` without competition context.
  Require identifying metadata beyond placeholder descriptions. Keep short titles when other
  fields identify the event.

Keep uncertain Swedish participation for Step 3, including listings missing participant names.
For these and later rejections, verify exact IDs, change only hidden status, and record IDs/reasons.

## 3. Research Swedish Participation

Work only on selected broadcasts that remain visible.

1. For processed broadcasts, inspect existing activities/links. Reuse consistent matches;
   preserve and report broadcasts with missing/conflicting matches.
2. Group identical events across channels; research once. Associate generic/event-wide listings
   when metadata, channel, and timing identify exactly one event.
3. Research participation manually from web sources, starting with the sport/series and broadcast
   season. Record the search and source. A reliable, complete finding of no Swedish participants
   covers all selected broadcasts for that established series/season; proceed directly to hiding.
4. Otherwise use complete official entries, start lists, confirmed rosters, or equivalent authority.
5. Apply the result:

   | Result | Action |
   | --- | --- |
   | Confirmed in scope | Record event, participants, evidence, and exact broadcast IDs. |
   | Confirmed outside scope | Hide broadcasts under Step 2. |
   | Uncertain participation/mapping | Keep visible and unprocessed; set event organization. |

6. Verify classifications, hidden IDs, and organization markers.

## 4. Create or Update Activities

For one confirmed event, prepare each affected public date before its first change in Steps 4–6:

- Derive dates from stored/planned activity times using `SportDay.cs`, group `public_date_mode`,
  and `ActivityQueryRepository`. Persist actual calendar times.
- Retrieve `https://sesport.se/?date=YYYY-MM-DD`; count cards once per date per run, including both
  dates before a move. A grouped card counts once.

Then:

1. Reuse matching activities, groups, participant links, and broadcast links; create missing ones.
2. Create one activity per distinct timed segment, identified by title, description, channel,
   and exact start/end times. Combine channels covering that segment in `tv_channel_name`,
   comma-separated. Use the segment's own broadcast range; keep different segments separate.
3. Attach all competition activities to one group spanning its dates. Use the shortest clear
   Swedish event name for the group and segment titles such as `Sträcka 2–4` for activities.
   Include event names for standalone clarity. Retain identifying sponsors (`BMW Championship`);
   remove nonessential ones (`Wanda` in `Wanda Diamond League`).
4. Set sport, activity type, calendar date, broadcast times, time zone, channels, and organization.
   Follow existing publication conventions and save authoritative event evidence.
5. Link every confirmed Swedish competitor as a Person participant; add missing active links
   with the event organization as context. Link each broadcast to its corresponding segment.
6. Verify saved activities and links. Set linked broadcasts' organization to the activity's,
   mark processed, then hide. Read back details, links, organizations, and statuses.

## 5. Verify Created or Changed Activities

When activities changed, complete A–F for each before moving to the next. Add IDs changed by
corrections to the verification set; read related records as context.

### A. Times

- Verify each main activity's broadcast start/end against Swedish TV schedules such as TV.nu/SVT;
  round to the nearest five minutes. Use each segment's own range and F for detail activity times.

### B. Stream Links

- Resolve each displayed provider through existing `StreamLink` sources first, then
  `BroadcastChannelLinkCatalog`. Match trimmed source titles case-insensitively.
  Keep catalog links as presentation fallbacks only.
- Find missing/stale provider links on TV.nu event details; verify event, broadcast date/start,
  and provider before saving.
- Follow wrappers to the direct provider URL. Remove affiliate wrappers, attribution parameters,
  `utm_*`, and `tag`; preserve parameters selecting the event. Save TV.nu as activity evidence.
- Accept verified direct event/channel URLs. When both activity and catalog resolution fail,
  report the unresolved provider and failed checks.

### C. Participants

- Use Person entities for competing athletes only.
- Base inclusion/exclusion on complete official lists, confirmed rosters, or equivalent authority.
  Official squad inclusion takes precedence over rumors of non-participation.
- Delete incorrect `activity_entity_links`; retain eliminated competitors with `is_active = false`.
- Golf: compare equivalent coverage, combining parallel Day 1 broadcasts. Investigate later-only
  participants; add valid omissions from Day 1 through all applicable rounds. Later lists may
  shrink after cuts but never grow.
- Record all required participant start times; link displayed times to human-readable sources.
- Verify every displayed participant discipline.

### D. Person Facts and Profiles

- Store verified birthdates for displayed and new people, including inactive participants.
  Link their formative club when verified; otherwise use their earliest documented development club.
  Record current affiliation in Team links.
- Use authoritative athlete, federation, or club sources. Reuse matching Clubs; create/link missing
  ones. New Persons also need correct gender and organization-entity links matching the event's
  existing participants.
- Give every linked Person a verified dedicated profile in `entities.url`, primarily about their
  identity/career. Prefer federation, club, team, or relevant organization profiles; alternatives
  are dedicated Wikipedia pages for known people, personal websites, or sport-related social
  profiles.
- Prefer Swedish/English; use another language as a last resort with verified identity/content.
  Use articles, interviews, and match/transfer reports as evidence only; choose person-specific
  profile destinations rather than generic homepages or search results.

### E. Stars

- Assign Person `Watch Priority = tier_0` when both requirements hold; correct stale stars:
  - Current performance at/near the absolute international senior top in their discipline,
    evidenced by senior Worlds, Olympics, major championships, or comparable results.
  - Meaningful mainstream or equivalent accessible coverage relevant to Swedish sports audiences.
- Treat domestic medals, junior results, career legacy, and lower-tier, developmental, age-group, or
  senior-tour results as context; require current qualification under both criteria above.

### F. Detail Activities

- For sourced Swedish participation times within broader events, reuse/create details with the
  parent's sport/group, correct Persons, and specific titles such as `Stavhopp - Final`.
  Include the event name only for standalone clarity. Include confirmed or uncertain broadcast
  coverage; exclude details entirely outside it.
- Set `local_end_time` and `ends_at`: use a sourced end, otherwise estimate a generous
  discipline-appropriate duration capped at the parent activity's end.
- For sports with individual start times, such as Golf, add details only for coverage following
  specific Swedish players. Record created/changed detail IDs; apply A–E and Step 6.

## 6. Verify Public Presentation

Retrieve the recorded activities' hosted pages, including old dates after moves. Inspect their
rendered cards/group context, correct supported discrepancies, and retrieve affected pages again.

- Every activity should have at least one Swedish participant (person-entity). 
- Check publication, clear Swedish titles, event context, grouping, and participants; check changed
  schedule rows' segment titles, channels, stream links, and time ranges.
- Verify displayed people, including inactive ones, have ages, clubs, and names linking to
  working `entities.url` profiles.
- Verify team-sport flags under existing foreign/national-team rules. Base query corrections on
  linked Team countries; several teams sharing one known country can supply it.
- Verify supported country IDs and successful loads of matching SVGs. Make focused code/asset fixes,
  validate under `AGENTS.md`, and recheck rendering.
- Verify local/hosted rendering separately; report differences, deployment gaps, and incomplete
  checks. Confirm hosted fixes on the hosted site.
- Recount cards per date and explain differences from Step 4. Continue with the next event.

## 7. Check the Remaining Broadcast Queue

Inspect all visible broadcasts, including later imports:

- Assign the relevant event/competition organization as an investigation marker; retain unresolved
  broadcasts unprocessed. Hide unidentified events/organizations under Step 2.
- For later imports, assign organizations or hide junk; defer creation/processing to the next run.
- Confirm every visible broadcast has an organization; record unresolved IDs/organizations.

## 8. Save the Final Report

Write `jobs/reports/activities-YYYY-MM-DD.md` using the run date. Include:

- Start timestamp; created/changed activity IDs, public dates, and linked broadcast IDs.
- Hidden broadcast IDs/reasons, applied corrections, and evidence.
- Unresolved items, failed checks, and visible broadcast IDs/organizations.
- Card counts before/after per date, explanations, and local/hosted discrepancies.

Report when activity verification was unnecessary because nothing changed.
Keep the report to changes, unresolved items, and counts; return its link with a brief summary.
