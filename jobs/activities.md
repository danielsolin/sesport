# Activities

Create activities from visible broadcasts and verify published activities.
Complete the steps in order without approval stops. Deliver one final report.

## Rules

- Follow `AGENTS.md`; this job authorizes the changes below without further approval.
- Prioritize accurate, thoroughly researched data over execution time or cost.
- Use concise, correct Swedish for public content. Preserve clear short forms for small screens.
- Read database settings from `.env`. Inspect the target, schema, and comparable records first.
- Make data changes through guarded manual `psql` transactions: exact IDs, expected row counts,
  rollback on mismatch, and readback verification. Preserve unrelated fields and records.
- Leave unsupported or conflicting facts unchanged. Record unresolved items and continue.
- Save every source used under the appropriate evidence type:

  | Evidence | Type |
  | --- | --- |
  | Event, schedule, broadcast | `ActivityEvidence` |
  | Participation, roster, elimination | `ParticipationEvidence` |
  | Participant start time | `ParticipantStartEvidence` |
  | Star qualification | `ParticipantStarEvidence` |
  | Birthdate, formative club, person profile | `PersonFacts` |

## 1. Select Broadcasts and Verification Dates

1. Snapshot every broadcast with `hidden_at` unset, regardless of date or `processed_at`.
   Record IDs, titles, descriptions, categories, channels, times, organizations, and entity links.
   Later imports enter only the final queue check in Step 7.
2. Verify the operator's date range plus the dates covered by selected broadcasts and changed
   activities. Without a supplied range, use the selected broadcasts' dates.
3. Interpret public dates using `src/SESport.Core/Domain/SportDay.cs`; store actual calendar times.
4. Retrieve `https://sesport.se/?date=YYYY-MM-DD` for each date before changes. The rendered page
   defines the published activities to verify. Record their IDs and card counts per date;
   a card containing several activities counts once. Capture any newly added date before editing it.

## 2. Hide Clearly Irrelevant Broadcasts

Complete this step before activity changes. Use listing metadata only; research follows in Step 3.

Create activities only for international competition involving:

- Swedish athletes competing individually.
- A Swedish national team.
- A Swedish club against a non-Swedish opponent.

Hide listings clearly outside this scope: domestic competitions, foreign-team fixtures even with
Swedish players, and generic highlights, recaps, or studio programmes.

- Judge title, description, categories, channel, and linked context together.
- Hide unidentified listings: for example, bare `EM` without a sport/event category, or `Dag 2`
  with only `Golf` and no identifiable tournament. Placeholder descriptions provide no identity.
- Keep short titles when other metadata identifies the event.
- Missing Swedish names do not prove non-participation. Keep uncertain listings for Step 3.
- Select and verify exact IDs before hiding. Change only hidden status, retain processed state
  and text, and record each ID and reason.

## 3. Research Swedish Participation

Work only on selected broadcasts that remain visible.

1. For processed broadcasts, inspect the existing activity and links. Reuse a matching activity;
   if absent or conflicting, leave the broadcast unchanged and report it.
2. Group listings for the same event across channels and research once. Assign a generic or
   event-wide listing only when metadata, channel, and timing identify one event without ambiguity.
3. Research manually, without running `decide-swedish-participation`. Start with Swedish
   participation in the sport or series for the current season. A reliable, complete finding of
   no Swedish participants in an established series applies to all selected broadcasts for that
   series and season; skip event-by-event lists. Record the broad search and supporting source.
4. Otherwise check complete official entries, start lists, confirmed rosters, or equivalent
   authoritative evidence. Partial articles, unanswered searches, and missing names do not suffice.
5. Apply the result:

   | Result | Action |
   | --- | --- |
   | Confirmed in scope | Record event, participants, evidence, and exact broadcast IDs. |
   | Confirmed outside scope | Verify IDs, hide broadcasts, preserve processed state. |
   | Uncertain participation/mapping | Keep visible and unprocessed; set event organization. |

6. Verify classifications, hidden IDs, and organization markers. Create activities only in Step 4.

## 4. Create or Reuse Activities

Process each confirmed event from Step 3:

1. Inspect comparable records and search for a matching activity. Reuse matches; create only
   missing activities, groups, participant links, and broadcast links.
2. Create one activity per distinct timed segment, not per channel or whole competition day.
   Compare titles, descriptions, channels, and exact start/end times. Merge channel listings only
   when they cover the same segment; store comma-separated names in `tv_channel_name`.
   Keep different segments separate. Use the segment's broadcast range, not a broader listing's end.
3. Reuse or create one activity group spanning the competition dates; attach all related activities.
   Use its shortest clear Swedish event name. Give activities short segment titles such as
   `Sträcka 2–4`; include the event name wherever a standalone card needs it.
   Remove nonessential sponsors (`Wanda` in `Wanda Diamond League`);
   retain identifying ones (`BMW Championship`).
4. Set sport, activity type, calendar date, broadcast times, time zone, channels, and organization.
   Follow existing publication conventions and save authoritative event evidence.
5. Link every confirmed Swedish competitor as a Person participant. Add missing active links with
   the event organization as context; verify the complete list. Apply the person rules in Step 5.
6. Link every corresponding broadcast to its activity; each segment gets only its own listings.
   Verify saved activities and links before finalizing broadcasts.
7. Set linked broadcasts' organization to the activity organization, mark processed, then hide.
   Update only verified source IDs. Read back activity details, links, organizations, and statuses.

## 5. Verify Every Activity

Process each published activity from Step 1 and each activity created or updated in Step 4.
Complete all checks below for one activity before moving to the next. Apply supported corrections.

### A. Titles and Times

- Check clear, concise Swedish titles and event context, including standalone cards.
- For main activities, verify broadcast start/end times against Swedish TV schedules such as TV.nu
  or SVT. Store broadcast times rather than competition times; round to the nearest five minutes.
  Use F for detail activity times.
- Preserve each grouped segment's time range rather than extending it to broader coverage.

### B. Stream Links

- Check every displayed provider independently. Resolve the activity's existing `StreamLink`
  first, then `BroadcastChannelLinkCatalog`. Match source title after trimming, ignoring case.
- Use fixed-channel mappings as presentation fallbacks; keep them out of activity/broadcast sources.
- For missing or stale links, inspect the matching TV.nu detail page's provider link. Verify event,
  operator date, broadcast start time, and provider before saving.
- Follow wrappers to the direct provider URL. Remove affiliate wrappers, attribution parameters,
  `utm_*`, and `tag`; preserve parameters selecting the event. Save TV.nu as activity evidence.
- Use verified direct links rather than generic homepages or search results. Mark unresolved only
  when neither a valid activity link nor a fixed-channel mapping resolves it; record failed checks.

### C. Participants

- List competing athletes using Person entities; exclude coaches, managers, and support crew.
- Base inclusion/exclusion on complete official lists, confirmed rosters, or equivalent authority.
  Official squad inclusion takes precedence over rumors of non-participation.
- Delete incorrect participation records from `activity_entity_links`. Retain eliminated
  competitors with `is_active = false`, including golfers who missed a cut.
- For multi-round Golf, lists may shrink after cuts but never grow on later days. Investigate
  later-only participants; add valid omissions from Day 1 through every applicable later round.
  Compare equivalent tournament coverage, including parallel Day 1 broadcasts.
- Record all participant start times when the sport requires them. Save all supporting evidence;
  the public start-time link must open a human-readable page.
- Verify each discipline when the participant table displays one.
- New Person entities need correct gender, birthdate, formative Club, and organization-entity
  relationships matching the event's existing participants.

### D. Person Facts and Profiles

- Check linked people, including inactive participants. Every displayed person needs a stored
  birthdate, visible age, and formative club or earliest documented development club.
- Research authoritative athlete, federation, or club sources. Reuse matching Club entities;
  create and link missing ones. Use current affiliation in Team links, never as a formative-club
  fallback. Describe a club as formative only with evidence; report conflicting or missing facts.
- Every linked Person needs a verified dedicated profile in `entities.url`. Prefer federation,
  club, team, or other relevant organization profiles; otherwise use dedicated Wikipedia pages
  for sufficiently known people, personal websites, or sport-related social profiles.
- Prefer Swedish/English; another language is a last resort with verified identity and content.
  The page must primarily describe the person or athletic career. Exclude articles, interviews,
  match/transfer reports, generic homepages, and search results. Report missing profiles.
- Save verified facts and profile sources as `PersonFacts`; verify public names link to these URLs.

### E. Stars

- Set Person `Watch Priority = tier_0` only for current athletes at or near the international senior
  top in their discipline, with meaningful mainstream or equivalent accessible coverage relevant
  to Swedish sports audiences. Verify results and public relevance; correct stale stars.
- Use senior Worlds, Olympics, major championships, or comparable top-level results as evidence.
  Domestic medals, junior success, career legacy, niche results without audience relevance, and
  lower-tier, developmental, age-group, or senior-tour results alone do not qualify.

### F. Detail Activities

- When sourced times identify Swedish participation within a broader event, reuse or create a
  detail activity with the parent's sport and group, correct Persons, and a specific title such as
  `Stavhopp - Final`. Omit the collective event name unless standalone rendering needs it.
- Use a sourced end time; otherwise estimate a generous discipline-appropriate duration, capped
  at the main activity's end. Set both `local_end_time` and `ends_at`.
- Skip details fully outside the broadcast; include when broadcast coverage is uncertain.
- For sports already showing individual start times, such as Golf, create details only when
  coverage follows specific Swedish players. Apply checks A–E to new detail activities.

## 6. Verify Public Presentation

Retrieve the hosted pages again for every verification date. Inspect rendered output, not only data.
Correct supported discrepancies and retrieve affected pages again.

- Check every affected activity: publication, card title, event context, grouping, and participants.
  For grouped schedules, check each segment title, channels, stream links, and correct time range.
- Verify displayed people, including inactive ones, have ages, clubs, and working profile links.
- In team sports, verify applicable team-based flags using existing foreign/national-team rules.
  Check linked Team countries before changing queries; several teams sharing one known country can
  supply it. Row position alone does not demonstrate a rendering bug.
- Verify supported country IDs, matching SVG assets, and successful asset loads. Make focused
  code/asset fixes when required; follow `AGENTS.md` validation rules and recheck rendering.
- Check local rendering separately from the hosted site. Report differences and deployment gaps;
  claim hosted fixes only after hosted verification.
- Recount cards per date and explain changes from Step 1. Report incomplete verification explicitly.

## 7. Check the Remaining Broadcast Queue

Inspect every broadcast still visible, including imports after Step 1.

- Assign each its relevant event/competition organization. This marks investigation, not confirmed
  participation or processing. Keep unresolved broadcasts unprocessed.
- Hide listings whose event or organization cannot be identified under Step 2's junk rule.
- For later imports, change only organization or junk-listing visibility; defer creation/processing.
- Verify no visible broadcast lacks an organization. Record unresolved IDs and organizations.

## 8. Save the Final Report

Write `jobs/reports/activities-YYYY-MM-DD.md`, using the run date. Keep it complete but condensed:

- Verification dates; created/updated activities and corresponding broadcast IDs.
- Hidden broadcast IDs with reasons; other applied corrections and supporting evidence.
- Unresolved items, failed checks, and visible broadcast IDs with organization markers.
- Card counts before/after per date, reasons for differences, and local/hosted discrepancies.

Omit already-correct details. Give the operator the report link and a brief result summary.
Commit or push only on explicit operator request.
