# Activity verification report: 2026-09-08

## Execution

- Scope: SESport SportsDay 2026-09-08.
- Public view: <https://sesport.se/?date=2026-09-08>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 11 activity cards, 11 published activity rows, and
  21 visible participant names.
- No grouped card contains multiple activity rows today.
- The operator approved the recommendations. They were applied in two
  transactions, with a final rendering correction after public verification.
- Step 7 final public view: 10 activity cards, 10 published activity rows,
  and 20 visible participant names.

## Applied changes

### Participants

- Added the missing person-to-team relation between `Christoffer Rifalk`
  (`7df9e405-4af6-d70d-1c1b-777c7a912aed`) and `Ilves`
  (`98ced3d0-9553-858c-77ae-d3fd8724c293`). The current Ilves roster lists
  Rifalk together with the other five Ilves participants in `HPK - Ilves`.
  His existing relations are retained. The activity link was also corrected
  to present Rifalk as representing Ilves rather than Ässät.

- Deleted `Bristol City - Lincoln`
  (`55ca14ed-2a61-4e56-a966-4a301468c20b`) from today's published scope.
  Its activity-participant link, single-activity group, group facts,
  activity-specific facts, and sources correlated with the activity or group
  were deleted as part of the cleanup. The `Noah Eile` entity and his
  existing Bristol City relation were retained.

  The official Lincoln City fixture list shows `AFC Bournemouth - Lincoln
  City` on 2026-09-08 and `Bristol City - Lincoln City` on 2026-09-15:

  - <https://www.weareimps.com/fixture/list/122>

  The current TV.nu schedule likewise contains Bournemouth–Lincoln, not the
  stale Bristol–Lincoln activity. No replacement activity is recommended in
  this verification because no Swedish participant is confirmed for the
  Bournemouth match.

### Stream links

- Updated the `Viaplay` source for `Club Brügge - Aston Villa`
  (`817b88f6-0e40-478b-ade8-14e1a1ddb811`). The old URL returned HTTP 200 but
  redirected to the generic Champions League page. The source now uses the
  current provider-specific URL from the TV.nu detail page:

  <https://viaplay.se/sport/fotboll/uefa-champions-league/cl-studion-club-brugge-aston-villa/s26090197972108103>

  TV.nu detail page:

  <https://www.tv.nu/s/s_4664946_20260908>

### Times

- No time changes are recommended. The current TV.nu event and stream
  windows match all other published activity windows.
- Club Brügge–Aston Villa has a linear `Viaplay Sport` window of 18:30–20:45
  and a longer `Viaplay` stream of 18:30–21:25. Retain the activity window
  18:30–20:45, following the existing policy for differing linear and stream
  windows.

### Stars and detail activities

- No watch-priority changes are recommended. The two `tier_0` participants,
  Victor Nilsson Lindelöf and Anthony Elanga, remain supported as current
  top-level senior footballers.
- No detail activity has enough precise participant-specific information to
  create one today.

## Unresolved items

- None. The Bournemouth–Lincoln broadcast was a separate current event and
  was not used to relabel the stale Bristol–Lincoln activity.

## Evidence saved

The following current TV.nu event-detail pages were saved as
`ActivityEvidence` for the relevant activity groups:

- <https://www.tv.nu/s/p_1613309_20260908>
- <https://www.tv.nu/s/s_4664946_20260908>
- <https://www.tv.nu/s/s_4612362_20260908>
- <https://www.tv.nu/s/s_4612365_20260908>
- <https://www.tv.nu/s/s_4612367_20260908>
- <https://www.tv.nu/s/s_4612368_20260908>
- <https://www.tv.nu/s/s_4612373_20260908>
- <https://www.tv.nu/s/s_4664950_20260908>
- <https://www.tv.nu/s/s_4664951_20260908>
- <https://www.tv.nu/s/s_4664952_20260908>

The existing official roster sources were reused. The current Ilves roster
source already exists on `HPK - Ilves` and supports the relation:

- <https://www.ilves.com/joukkue/>

The current star review reused the official club sources already present for
the participants. No new `ParticipantStartEvidence` or
`ParticipantStarEvidence` was required.

## Public card counts

- Step 1, initial public view: 11 activity cards.
- Step 7, final public view: 10 activity cards.
- The card count decreased by one because the stale Bristol City–Lincoln
  activity was deleted. The visible participant count decreased by one for
  the same reason.
