# Activity verification report: 2026-09-14

## Execution

- Scope: SESport SportsDay 2026-09-14.
- Public view: <https://sesport.se/?date=2026-09-14>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- The current `jobs/verify-activities.md` was reread before verification.
- Step 1 public view: 4 activity cards, 4 published activity rows, and
  6 visible participant entries.
- No grouped card contains multiple activity rows today.
- No database changes were made because this job requires operator approval
  before changes are applied.

## Recommended changes

- No activity, participant, StreamLink, watch-priority, or detail-activity
  changes are recommended.
- Retain the intentionally concise public titles. The participant links and
  represented teams are supported by the current official team sources.
- Retain the current activity windows. Existing activity-specific stream
  links resolve to provider-specific pages and return HTTP 200. The remaining
  TV4 Sport Live 3 and V Sport Premium links correctly use the central fixed
  channel catalog.

## Unresolved items

- `Como - Parma` is currently 18:25-20:30, matching the event-specific
  TV4 Play stream. Its linked TV4 Sport Live 3 broadcast runs to 21:00.
  The current activity window follows the event-specific stream, consistent
  with the existing policy for differing linear and streaming windows. No
  extension is recommended without a separate policy decision.
- `Inter Milano - Udinese Calcio` has conflicting official match times:
  Inter's ticket and calendar pages show 18:45, while Udinese and TV.nu show
  20:45. The Swedish TV4 Play broadcast is 20:35-22:40, so the activity
  window remains correct as a broadcast window. The external time conflict
  remains unresolved.
- Matchday starting lineups were not available in the reviewed sources.
  Current official squad and roster evidence was therefore used, as allowed
  by the job rules; this is not confirmation of starting lineups.

## Evidence to save after approval

Save these current TV.nu detail pages as `ActivityEvidence` for the relevant
activities:

- <https://www.tv.nu/s/s_4669693_20260914>
- <https://www.tv.nu/s/p_1612811_20260914>
- <https://www.tv.nu/s/s_4601148_20260914>
- <https://www.tv.nu/s/s_4601150_20260914>
- <https://www.tv.nu/s/s_4609325_20260914>

Save the current Udinese fixture page as `ActivityEvidence` for
`Inter Milano - Udinese Calcio`:

- <https://www.udinese.it/news/squad/udineses-september-fixtures>

The existing official participation sources stored for the activities were
reused. No new `ParticipantStartEvidence` or `ParticipantStarEvidence` is
required.

## Public card counts

- Step 1, initial public view: 4 activity cards, 4 published rows, and
  6 visible participant entries.
- Step 7, final cache-busted public view: 4 activity cards, 4 published
  rows, and 6 visible participant entries.
- Difference: none; no database changes were applied.
