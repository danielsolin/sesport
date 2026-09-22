# Job Instruction: Audit and Repair Golf Activity Participant Lists

## Objective

Inspect all Golf activities in the database and make their participant lists
correct. The job must find both missing participant links and incorrectly added
links, investigate each candidate, and apply confirmed corrections.

This job is deliberately limited to Golf. The same participant pattern is normal in
many other sports because of heats, substitutions, matches, relays, or
event-specific entries. Do not scan or modify non-Golf activities as part of this
job.

The expected list is the set of tracked people who should be shown for the
activity, not necessarily every person in the full tournament field. Do not add
unrelated competitors merely because they appear in an official field list.

## Data model and comparison rule

- Use `activities.activity_group_id` to identify activities belonging to the same
  event. Never compare activities from different groups based only on their titles.
- Restrict the scan and every possible correction to activities with
  `activities.sport_id = 'golf'`. Do not modify non-Golf activities.
- Use `activity_entity_links` joined to `entities` with
  `entities.entity_type_id = 'Person'`. Do not change team, pair, organization, or
  other non-person links in this job.
- Treat a person as present when an activity link exists, regardless of its current
  `is_active` value. An inactive link still records that the person was associated
  with the activity.
- Order activities in a group by `activity_date`, then `starts_at`, `created_at`,
  and `id` for deterministic output.
- Use the union of person links on the earliest activity date as a comparison
  baseline. This is useful for finding later-only links, especially when several
  activities on that date are parallel broadcasts on different channels.
- Treat the baseline as a discovery heuristic, not as proof of the correct list.
  Official round-specific evidence decides whether a person belongs in an
  activity.
- Compare participant sets by `entity_id`, not by display name.
- Include published, draft, and unpublished activities in the scan. Report their
  publication status so that corrections are auditable.

## Discovery

Run a read-only database scan across all Golf activities and Golf activity groups.
The scan must report, for each candidate group or activity:

- sport and activity-group title;
- activity-group ID and date range;
- all activity IDs and titles on the initial date;
- all later activity IDs, dates, titles, and publication statuses;
- all linked participant names, entity IDs, link IDs, and `is_active` values;
- possible later-only participants and possible missing baseline participants;
- the number of linked participants per activity and activity date.

Do not use one arbitrary first-day activity as the baseline. Merge the participant
sets from all parallel first-day broadcasts in the same activity group.

The scan must not alter the database. Save the candidate report before any update
to:

```text
./jobs/reports/audit-golf-participants-YYYY-MM-DD.md
```

## Investigation rules

Review each candidate group individually.

### Golf tournament rule

For a continuous golf tournament, the first two rounds normally use the official
entry or start list. Later rounds use the official tee times, leaderboard, or cut
status. A person missing from one activity may therefore require an added link,
while a person linked to the wrong activity may require deletion.

Use round-specific evidence. An official tournament field alone does not prove that
every tracked person belongs in every broadcast activity.

If a person is in the official event field and has an official start or result for a
round, add the person to that activity when the corresponding tracked entity exists.
For example, if Linn Grant or Madelene Sagström is missing from an activity in The
Standard Portland Classic despite being in that round's official field or tee
times, add the missing activity link.

Do not remove a first-round participant merely because they missed the cut. If the
person remains linked to a later activity that represents the tournament, retain the
link and set `is_active` to `false` when the evidence shows elimination.

Only add, delete, or deactivate a link when authoritative event evidence and the
activity context support the decision. If the evidence is incomplete or
contradictory, leave the link unchanged and mark it unresolved in the report.

### Parallel broadcasts

Do not merge or delete separate broadcasts merely because they have the same title
and date. They may be different channels. Use their combined first-day set for
discovery, then verify whether each broadcast has the same participant scope before
adding or deleting a link.

### Evidence

Use official start lists, entry lists, rosters, results, or equivalent authoritative
sources. Save every source used with the appropriate existing evidence type:

- `ParticipationEvidence` for participation or non-participation evidence;
- `ParticipantStartEvidence` for a start list or scheduled participant evidence;
- `ActivityEvidence` for evidence about the activity or event itself.

Record the source URL, access date, relevant event/date, and the conclusion. Do not
use a partial article as the sole proof that a complete participant list is wrong.

## Correction rules

Apply changes only after the operator has approved the candidate report.

For each confirmed participant-list correction:

1. Add a missing link only for an unambiguous existing Person entity. Use the
   existing participant mutation path so organization and represented-entity fields
   are resolved consistently and duplicate links are prevented.
2. Delete only a confirmed erroneous link using its exact `activity_id` and
   `entity_id` (or exact link ID).
3. Do not delete the activity, activity group, entity, or a valid participant link.
4. Set `is_active` to `false` for a linked participant who was eliminated but still
   belongs to the tournament activity history. Do not use deletion for elimination.
5. Do not create a new Person entity in this job. Report an unresolved entity match
   when the participant is not already represented unambiguously in the database.
6. Keep uncertain candidates unchanged and list the missing evidence.

Perform additions, deletions, and status changes in a transaction per activity
group. Before committing, assert that the affected activity IDs, entity IDs, and
operation types are exactly the approved changes. After committing, query the
activity group again and verify every repaired activity against its evidence.

Do not change titles, times, publication status, sports, activity groups, or
participant metadata. Do not modify any non-Golf record, even if it has the same
pattern.

## Execution workflow

1. Discover all Golf candidate groups and activities with a read-only query.
2. Write the complete candidate report.
3. Research missing, extra, and status-mismatched participants with authoritative
   evidence.
4. Ask the operator to approve or reject the proposed corrections.
5. Apply only approved, confirmed additions, deletions, and status changes.
6. Run the post-correction comparison for every changed activity group.
7. Save the final report with scanned, added, deleted, deactivated, retained, and
   unresolved counts.

The job is complete only when every candidate is either corrected, justified as
valid, or explicitly left unresolved with a reason.

## Final report

The final report must include:

- the database scan date and scope;
- counts of Golf groups and activities scanned;
- candidates by Golf activity group and activity;
- evidence and decision for every candidate participant;
- exact added activity-link IDs, activity IDs, and entity IDs;
- exact deleted activity-link IDs, activity IDs, and entity IDs;
- exact `is_active` changes, activity IDs, and entity IDs;
- candidates retained because they were valid;
- candidates left unresolved and the reason;
- post-correction counts for every changed group;
- confirmation that no non-Golf records or unrelated tables were modified.
