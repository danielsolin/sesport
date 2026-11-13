# Activities

## Job contract

This unattended job creates or updates activities from currently visible broadcasts.
The operator authorizes application-data changes and writing the run report.

- [ ] Continue until the final report is produced; do not stop for approval.
- [ ] Keep work within application data and the run report.
- [ ] Leave code, assets, schema, configuration, and deployment unchanged.
- [ ] Run Steps 1–3 once.
- [ ] Repeat Steps 4–6 for each confirmed event.
- [ ] Finish with Step 7 and one final report.

## Run state and outcomes

Save run state to `jobs/reports/.run-YYYY-MM-DD.md`:
- At the end of Step 3.
- After each event in Steps 4–6.

Each checkpoint must record:
- The starting snapshot.
- The outcome for every broadcast ID.
- Verified facts shared across events.
- Created or changed activity IDs.

Resume reruns from the latest checkpoint.

Every broadcast in the starting snapshot must have exactly one final outcome:

| Outcome | Required state |
| --- | --- |
| Rejected | Hidden, with a recorded reason. |
| Processed | Linked to an activity, marked processed, and hidden. |
| Unresolved | Visible and unprocessed, with a reason and organization when known. |

Treat Unresolved as handled. Revisit it during this run only if new evidence appears.

## Run-wide checklist

- [ ] Prioritize accuracy over time and cost.
- [ ] Write concise, correct Swedish for public content; preserve clear short forms.
- [ ] Use external sources for research.
- [ ] Verify public presentation only on `https://sesport.se`.
- [ ] Use `bin/dbc.sh` from the repository root for `psql` access; it reads `.env`.
- [ ] Pass SQL through standard input, such as `bin/dbc.sh <<'SQL'`; CLI arguments are ignored.
- [ ] Inspect the database target, schema, and comparable records.
- [ ] Make manual changes only in guarded `psql` transactions.
- [ ] Use exact IDs and expected row counts; roll back on any mismatch.
- [ ] Read back changes and preserve unrelated data.
- [ ] Apply supported changes; retain and report unresolved or conflicting facts.
- [ ] Track created or changed activity IDs, including group and link changes.
- [ ] In Steps 5–6, verify only the tracked activity IDs.
- [ ] Research shared person facts, participant lists, and sources once per run.
- [ ] Recheck shared research if evidence conflicts or the event, round, or date differs.
- [ ] Verify each segment's own times and links separately.

Save each evidence source with the matching type:

| Evidence | Type |
| --- | --- |
| Event, schedule, broadcast | `ActivityEvidence` |
| Participation, roster, elimination | `ParticipationEvidence` |
| Participant start time | `ParticipantStartEvidence` |
| Star qualification | `ParticipantStarEvidence` |
| Birthdate, formative club, person profile | `PersonFacts` |

## 1. Snapshot broadcasts

- [ ] Record the run's start timestamp.
- [ ] Snapshot every broadcast with `hidden_at` unset.
- [ ] Include all dates and both processed and unprocessed broadcasts.
- [ ] Record each broadcast's:
  - ID and `processed_at`.
  - Title, description, categories, and channels.
  - Times and organizations.
  - Existing entity and activity links.

## 2. Hide clearly irrelevant broadcasts

### Eligibility

An international competition is eligible when it involves at least one of:
- A Swedish athlete competing individually.
- A Swedish national team.
- A Swedish club or team.

Use only the title, description, categories, channel, and linked context to classify
broadcasts.

### Hide broadcasts that match any condition

- [ ] Domestic competition.
- [ ] Foreign-team fixture, even when Swedish players participate.
- [ ] Standalone opening or closing ceremonies.
- [ ] Generic highlights, recap, or studio programme.
- [ ] Participant-specific broadcast for a competition already covered by a general
  broadcast. Example: a broadcast following only `Ludvig Åberg` in `US Open`.
- [ ] Unidentified event, such as bare `EM` or `Dag 2` / `Golf` without
  competition context.
- [ ] Placeholder descriptions without identifying metadata beyond the placeholder.

Keep a short title when other fields identify the event.

### Uncertain participation

- [ ] Keep uncertain Swedish participation for Step 3, including listings without
  participant names.

### For each rejection

- [ ] Verify the exact broadcast IDs.
- [ ] Change only hidden status.
- [ ] Record each hidden ID and its reason.

## 3. Research Swedish participation

Work only on selected broadcasts that remain visible.

### Research checklist

- [ ] If activities exist, match each broadcast by event, date, time, and context,
  regardless of processed state.
- [ ] Identify what changed and research only facts needed to resolve the changes.
- [ ] Group identical events across channels and research each event once.
- [ ] Associate a generic or event-wide listing only when metadata, channel, and timing
  identify exactly one event.
- [ ] Research participation manually from web sources, starting with the sport or
  series and broadcast season.
- [ ] Record the search and its source.
- [ ] Use complete official entries, start lists, confirmed rosters, or equivalent
  authoritative sources when participation is not conclusively ruled out.

- [ ] If a reliable, complete finding confirms no Swedish participants, apply it to all
  selected broadcasts for that established series and season.
- [ ] In that case, proceed directly to hiding those broadcasts.

### Apply the research result

| Result | Action |
| --- | --- |
| Confirmed in scope | Record event, participants, evidence, and exact broadcast IDs. |
| Confirmed outside scope | Hide the broadcasts under Step 2. |
| Uncertain participation or mapping | Keep visible and unprocessed; set organization if known. |

- [ ] Verify classifications, hidden IDs, and organization markers.

## 4. Create or update activities

- [ ] Process one confirmed event at a time.

### Prepare dates before the first change

- [ ] Derive dates from stored or planned activity times using `SportDay.cs`, the
  group's `public_date_mode`, and `ActivityQueryRepository`.
- [ ] Persist actual calendar times.
- [ ] Retrieve `https://sesport.se/?date=YYYY-MM-DD` for every affected public date.
- [ ] Count cards once per date per run.
- [ ] Before a move, count both the old and new dates.
- [ ] Count a grouped card once.

### Create or update the event

- [ ] Reuse matching activities, groups, participant links, and broadcast links.
- [ ] Create only missing records and links.
- [ ] Create one activity for each distinct timed segment, identified by title,
  description, channel, and exact start and end times.
- [ ] Combine channels covering the same segment in `tv_channel_name`, comma-separated.
- [ ] Use that segment's own broadcast range; keep different segments separate.
- [ ] Create or reuse one titled group for every event, including one-activity events.
- [ ] Attach every event activity to its group.
- [ ] Make the group span all event dates.

### Group and activity titles

- [ ] Omit years and season-year labels, such as `26/27`, from group and activity titles.
- [ ] Use the shortest clear event name for the group.
- [ ] Mention gender only when needed to distinguish the event.
- [ ] Use concise, segment-specific activity titles, such as `Sträcka 2–4`.
- [ ] Do not repeat the group title in activity titles.
- [ ] For a card combining multiple activities in a schedule, display only the group title.
- [ ] For separate cards, use `Group title: activity title`.
- [ ] For single-activity cards in multisport groups, add the sport to the activity
  title unless the event organization or group title identifies it. Example:
  `Hästhoppning Kval 2` under `Ungdoms-OS Dakar`.
- [ ] Retain identifying sponsors, such as `BMW Championship`.
- [ ] Remove nonessential sponsors, such as `Wanda` in `Wanda Diamond League`.
- [ ] Keep titles as short as possible, but longer than a single abbreviation.
- [ ] Omit details already clear from the organization, group, date, or participant list.
- [ ] Keep enough context to identify the event or segment.

### Set and link event data

- [ ] Set sport, activity type, calendar date, broadcast times, time zone, channels,
  and organization.
- [ ] Follow existing publication conventions.
- [ ] Save authoritative event evidence.
- [ ] Link every confirmed Swedish competitor as a Person participant.
- [ ] Add missing active participant links with the event organization as context.
- [ ] Link each broadcast to its corresponding segment.

### Verify and process broadcasts

- [ ] Verify saved activities and links.
- [ ] Set each linked broadcast's organization to the activity's organization.
- [ ] Mark linked broadcasts as processed, then hide them.
- [ ] Read back activity details, links, organizations, and broadcast statuses.

## 5. Verify created or changed activities

- [ ] When activities changed, complete A–F for each activity before moving to the next.
- [ ] Add IDs changed by corrections to the verification set.
- [ ] Read related records as context.
- [ ] Reuse applicable checks already completed in this run.

### A. Times

- [ ] Verify each main activity's broadcast start and end against Swedish TV schedules,
  such as TV.nu or SVT.
- [ ] Round times to the nearest five minutes.
- [ ] Use each segment's own range.
- [ ] Use F for detail activity times.

### B. Stream links

- [ ] Resolve each displayed provider through existing `StreamLink` sources first.
- [ ] If needed, use `BroadcastChannelLinkCatalog` as a presentation fallback.
- [ ] Match trimmed source titles case-insensitively.
- [ ] Find missing or stale provider links on TV.nu event details.
- [ ] Before saving, verify the event, broadcast date and start time, and provider.
- [ ] Follow wrappers to the direct provider URL.
- [ ] Remove affiliate wrappers, attribution parameters, `utm_*`, and `tag`.
- [ ] Preserve parameters that select the event.
- [ ] Save TV.nu as activity evidence.
- [ ] Accept verified direct event or channel URLs.
- [ ] If both activity and catalog resolution fail, report the unresolved provider and
  failed checks.

### C. Participants

- [ ] Use Person entities only for competing athletes.
- [ ] Base inclusion and exclusion on complete official lists, confirmed rosters, or
  equivalent authority.
- [ ] Prefer official squad inclusion over rumors of non-participation.
- [ ] Delete incorrect `activity_entity_links`.
- [ ] Retain eliminated competitors with `is_active = false`.
- [ ] Set `is_active = false` only after complete official results confirm elimination
  from the named event.
- [ ] Keep a person active while they compete in any part of that event.
- [ ] When complete official results confirm all Swedish participants are eliminated from an
  activity or tournament, set affected activities to unpublished:
  `Draft`, `Published = false`.
- [ ] Apply this publication change to today's and future activities.
- [ ] For Golf, compare equivalent coverage and combine parallel Day 1 broadcasts.
- [ ] Investigate participants who appear only in later lists.
- [ ] Add valid omissions from Day 1 through all applicable rounds.
- [ ] Allow later lists to shrink after cuts; do not allow them to grow.
- [ ] Record all required participant start times.
- [ ] Link displayed start times to human-readable sources.
- [ ] Verify every displayed participant's discipline.

### D. Person facts and profiles

- [ ] Store verified birthdates for displayed and new people, including inactive people.
- [ ] If only a birth year is stated or reliably inferred, store January 1 of that year.
- [ ] Record in `PersonFacts` evidence that the month and day are placeholders.
- [ ] Link the formative club when verified; otherwise use the earliest documented
  development club.
- [ ] Record current affiliation in Team links.
- [ ] Ensure each related team has a relevant team URL in `entities.url`.
- [ ] Use authoritative athlete, federation, or club sources.
- [ ] Reuse matching Clubs; create and link missing Clubs.
- [ ] For new Persons, set correct gender and organization-entity links matching the
  event's existing participants.
- [ ] Give every linked Person a verified, dedicated profile in `entities.url`.
- [ ] Prefer federation, club, team, or relevant organization profiles.
- [ ] For known people, a dedicated Wikipedia page is an alternative.
- [ ] Personal websites or sport-related social profiles are alternatives.
- [ ] Prefer Swedish or English. Use another language only as a last resort, with
  verified identity and content.
- [ ] Use articles, interviews, and match or transfer reports as evidence only.
- [ ] Choose a person-specific profile, not a generic homepage or search result.

### E. Stars

Assign Person `Watch Priority = tier_0` only when both criteria hold:

- [ ] Current performance is at or near the absolute international senior top in the
  discipline, evidenced by senior Worlds, Olympics, major championships, or comparable
  results.
- [ ] The athlete has meaningful mainstream or equivalent accessible coverage relevant
  to Swedish sports audiences.

Treat these only as context, not sufficient qualification on their own:
- Domestic medals.
- Junior results or career legacy.
- Lower-tier, developmental, age-group, or senior-tour results.

- [ ] Correct stale star assignments.

### F. Detail activities

- [ ] For sourced Swedish participation times within broader events, reuse or create
  details with the parent's sport and group.
- [ ] Link the correct Persons.
- [ ] Use specific titles, such as `Stavhopp - Final`.
- [ ] Omit the group title from activity titles; the card heading adds it.
- [ ] Include confirmed or uncertain broadcast coverage.
- [ ] Exclude details entirely outside the broadcast coverage.
- [ ] Within the same group, merge activities when one time window is fully contained
  in another and both use the same stream link.
- [ ] Keep the more specific activity, its sourced time window, and its correct Person
  links; remove the generic duplicate.
- [ ] Keep another activity only when a source confirms a separately scheduled broadcast
  with coverage not represented by the retained activity.
- [ ] Set `local_end_time` and `ends_at` from a source when available.
- [ ] Otherwise estimate a generous, discipline-appropriate duration capped at the
  parent activity's end.
- [ ] For sports with individual start times, such as Golf, add details only for
  coverage following specific Swedish players.
- [ ] Record created or changed detail IDs.
- [ ] Apply A–E and Step 6 to detail activities.

## 6. Verify public presentation

- [ ] Retrieve recorded activities' hosted pages, including old dates after moves.
- [ ] Inspect rendered cards and group context.
- [ ] Correct supported data discrepancies.
- [ ] Retrieve affected pages again.

- [ ] Record code, asset, or presentation defects as unresolved report items.
- [ ] Continue with the remaining checks after recording an unresolved item.
- [ ] Confirm every activity has at least one Swedish Person participant.
- [ ] Check publication, clear Swedish titles, event context, grouping, and participants.
- [ ] Confirm every activity has a titled group.
- [ ] Confirm grouped schedule cards use only the group title.
- [ ] Confirm separate card headings use `Group title: activity title`.
- [ ] Check schedule rows' segment titles, channels, stream links, and time ranges.
- [ ] Accept grouped cards showing a combined timeline and channels with only the first
  activity's article.
- [ ] Confirm displayed people, including inactive people, have ages and clubs.
- [ ] Confirm displayed names link to working `entities.url` profiles.
- [ ] Verify team-sport flags under existing foreign or national-team rules.
- [ ] Base data corrections on sourced Team countries.
- [ ] Allow several teams sharing one known country to supply the display country.
- [ ] Verify supported country IDs and successful loads of matching SVGs.
- [ ] Verify public presentation and data corrections only on `https://sesport.se`.
- [ ] Recount cards per date.
- [ ] Explain differences from Step 4.
- [ ] Continue with the next event.

## 7. Save the final report

- [ ] Verify every starting-snapshot ID has exactly one final outcome.
- [ ] Process missing IDs through Steps 2–6.
- [ ] Leave documented Unresolved outcomes for a later run unless new evidence appears.
- [ ] Check "Activities needing attention" at
  `https://sesport.se/Admin/Dashboard`.
- [ ] Read authentication details from `.env`.
- [ ] Correct dashboard data problems under the same evidence rules.
- [ ] Before changing a newly affected date, capture its card count under Step 4.
- [ ] Add affected activity IDs to the verification set.
- [ ] Complete Steps 5–6 for dashboard corrections.
- [ ] Record dashboard issues requiring code or asset changes, or unavailable evidence,
  as unresolved report items.

- [ ] Write `jobs/reports/activities-YYYY-MM-DD.md` using the run date.

Include:

- [ ] Start timestamp.
- [ ] Created or changed activity IDs, public dates, and linked broadcast IDs.
- [ ] Hidden broadcast IDs, reasons, applied corrections, and evidence.
- [ ] Unresolved items, failed checks, and visible broadcast IDs or organizations.
- [ ] Before and after card counts per date on `https://sesport.se`, with explanations.

- [ ] Report when activity verification was unnecessary because nothing changed.
- [ ] Delete the run checkpoint after Step 7 is complete.
- [ ] Return the report link with a brief summary.
- [ ] Point out anything requiring the operator's attention.
- [ ] Commit and push the final report.
