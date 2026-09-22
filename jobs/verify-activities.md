## Objective

Verify information published on https://sesport.se for the operator-defined
time frame. Research thoroughly so the data is as accurate as possible at
execution; data quality takes priority over time and cost.

All public information must be short, specific, and written in correct Swedish.

## Global Rules

- Do not change the database until the operator has approved your report of
  recommended changes.

- Keep public-facing content concise whenever possible, especially for small
  screens in portrait orientation. Preserve intentional short forms when the
  meaning remains clear.

- Respect the "SportsDay" concept: a date starts and ends as defined by
  `src/SESport.Core/Domain/SportDay.cs`.

- Always save every evidence source used, using the appropriate type:
  `ActivityEvidence`, `ParticipantStartEvidence`, `ParticipationEvidence`, or
  `ParticipantStarEvidence`.

## Workflow

### 1. Retrieve the Public View

Retrieve the full public view for the provided scope, for example:
https://sesport.se/?date=YYYY-MM-DD. The public page is the source of truth for
what must be verified.

Process the identified activities one at a time using steps 2–6 below.

- Record the number of activity cards. A card containing multiple grouped
  activities still counts as one.

### 2. Verify Times

Verify and update each activity's start and end times. These times represent TV
broadcasts, not the events themselves, so check them against Swedish broadcast
sources such as TV.nu and SVT.

- Round to the nearest five-minute mark.

### 3. Verify Stream Links

Verify that each published activity's displayed TV or streaming provider has a
matching `StreamLink` source whenever a provider-specific link is available.
Match by source title after trimming whitespace and ignoring case.

- Check the activity's existing `StreamLink` sources before researching.

- Resolve each displayed channel in this order: the activity-specific
  `StreamLink`, then the central fixed-channel link catalog. A fixed-channel
  mapping is a presentation fallback and must not be copied into the activity
  or broadcast as a duplicate `StreamLink` row.

- Treat a channel as unresolved only when neither an activity-specific link
  nor a central fixed-channel mapping provides a direct URL.

- For a missing or stale link, use the matching TV.nu event detail page and
  inspect its provider-specific streaming link. Before recommending it, verify
  the event title, operator date, broadcast start time, and provider.

- Recommend the direct provider URL reached through the source wrapper. Remove
  affiliate tracking wrappers and attribution parameters before saving or
  recommending the `StreamLink` URL, including query parameters whose names
  start with `utm_` or equal `tag`. Preserve provider parameters needed to
  select the specific event. Never substitute a generic provider homepage or an
  unverified search-result URL.

- Keep the TV.nu detail page as activity evidence when appropriate, but never
  store a third-party affiliate tracking URL as the `StreamLink`.

- If the event has multiple stream-capable providers, verify each one
  independently and report all matching links.

- If no unambiguous provider link is available, record the unresolved item and
  explain which validation failed.

### 4. Verify Participants

Verify that each event lists the correct participants. Add, remove, or
deactivate participants as required by the evidence. List only competing
athletes—not support crew, coaches, managers, or other staff—and use only
entities of type Person.

- Decide participation or non-participation only from a complete official
  start list or entry list, a confirmed roster, or equivalent authoritative
  source. Articles and other partial lists may support the decision but are not
  exhaustive. If an athlete is on an official squad list but rumors say they
  will not play, the official list wins and the athlete stays listed.

- For a multi-round Golf tournament, the participant list may shrink after a cut,
  but it must never grow on a later day. Treat later-only participants as data
  quality candidates. If confirmed valid, add them from Day 1 through every later
  round in which they should appear; never add them only to a later round.

- Remove an incorrectly listed participant, regardless of the reason, by
  deleting the corresponding record from `activity_entity_links`.

- Do not remove participants eliminated during a competition. Instead, set
  `is_active` to `false`, for example for a golfer who missed the cut.

- If the sport requires participant start times in the database, record them
  for all participants whenever possible. Save every used evidence for start
  times, but make sure a human-readable link is used for the start time link
  on /Index (the time will be rendered as <a href='...'>HH:MM</a>).

- If a discipline is listed in an event's participants table, verify that every
  participant has the correct discipline.

- When adding a Person entity, set the correct gender, birth date, and formative
  club as defined by the project rules. Also create organization-entity
  relationships matching those of the event's existing participants.

### 5. Verify Stars

Participants who qualify as top athletes in their discipline receive a star
beside their name. Verify stars from a Sweden-centric perspective.

- A star requires performance at or close to the absolute top level of
  international senior competition in the discipline. Relevant evidence
  includes Senior World Championships, Olympic Games, major international
  championships, and comparable top-level results.

- Swedish Championship medals and junior-level results may provide context but
  cannot justify a star by themselves. A junior title is not evidence of senior
  international performance.

- Stars represent current top-level competitive status, not career legacy. Do
  not assign a star solely on past achievements to a competitor who no longer
  competes at or near the highest relevant level of current senior competition.
  Results from lower-tier, age-group, developmental, or senior circuits do not
  qualify alone; for example, a golf senior tour is not equivalent to the
  current main professional tour.

- The person must also have meaningful visibility in mainstream sports media or
  equivalent broadly accessible coverage. Do not assign a star solely because
  someone succeeds in a niche sport or discipline with little audience
  interest. Strong results are necessary but insufficient without evidence that
  the person is relevant to the Swedish sports audience.

- A Star Participant is controlled by `Watch Priority` = `tier_0` on the entity
  record of type Person.

### 6. Verify Detail Activities

When precise information is available about when Swedish participants will
compete within a broader event, add a separate activity with the specific title
and time. Use the original activity's sport and activity group, and omit the
collective event name from the new title.

Example: If a Swedish athlete competes in the pole vault final at the Athletics
World Championships, add `Stavhopp - Final` and link the correct Person entity
or entities.

- Use a sourced end time when available. Otherwise, estimate a reasonable
  duration based on the discipline. Prefer slightly too much time over too
  little, but never extend beyond the main activity's end time.

- Because we operate with TV broadcasts, ignore a detailed event that is fully
  outside the broadcast scope; viewers cannot watch it. If unsure, include it.

- Detail activities for sports requiring a start time (such as Golf) are
  generally unnecessary because the participants list already shows it. They
  become relevant only when a broadcast covers one or more specific Swedish
  players.

- Set both `local_end_time` and `ends_at`.

### 7. Verify the Final Public View

Retrieve the public view for the provided scope again. Verify that the changes
render as expected and agree with the rendering logic.

Recount the activity cards as in step 1.


## Final Report

Save a complete but condensed report of recommended changes to:

./jobs/reports/verify-activities-YYYY-MM-DD.md

The report should mention only what needs changing, not what is already
correct.

Provide the operator with:

1. A summary of recommended changes.
2. A summary of anything that could not be resolved.
3. Number of activity cards counted in step 1 and step 7. If the numbers do not
   match, explain why.
