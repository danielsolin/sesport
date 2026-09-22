# Activity verification report: 2026-09-13

## Execution

- Scope: SESport SportsDay 2026-09-13.
- Public view: <https://sesport.se/?date=2026-09-13>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- The current `jobs/verify-activities.md` was reread, including the rule
  that public content should stay concise on small portrait screens and that
  participant start-time links on `/Index` should be human-readable.
- Step 1 public view: 34 activity cards, 44 published activity rows, and
  125 visible participant entries.
- The difference between cards and rows is caused by existing grouping. The
  grouping behavior is not changed in this run.
- The operator approved the recommendations. All applicable changes were
  applied in one manual PostgreSQL transaction.
- Step 7 was a cache-busted public refetch after the changes.

## Applied changes

### Broadcast windows and associations

- `Kina: MXGP Lopp 2`
  (`eacd08ea-cb3c-4129-977f-f526acadd51e`): changed the activity window from
  10:00-11:00 to 10:00-10:55. The linked Eurosport 2 broadcast has the
  10:00-10:55 window.
- `Solheim Cup: Sista Runda`
  (`241b65c9-a263-495d-976e-c6ac9f1092b6`): changed the Viaplay activity
  end time from 18:30 to 19:00. The separate V Sport Golf activity remains
  11:00-18:00.
- `1. FC Heidenheim 1846 - Holstein Kiel`
  (`d4aa682c-403f-495a-b33a-35f10aafee46`): replaced the association with
  broadcast `8192d4b2-7727-5f38-82d7-f17907e7d4c9` by the current broadcast
  `86bf470b-e1a3-7031-9901-45a95036e0d3`. The activity window is already
  correct at 13:25-16:00.

### StreamLink cleanup

Removed these activity-specific StreamLink sources because they were generic
Disney+ homepages rather than event-specific links:

- `691e1cd2-d821-47f4-b4dc-a0ece4d8dd57`, for `Celta de Vigo - Málaga CF`
  (`a406141c-c922-4394-b968-e96dbe28d52a`).
- `6c6d08b2-49e1-4b5d-bdee-876a7f6ff0a2`, for
  `Levante UD - FC Barcelona`
  (`21280c5f-e422-4ba8-94fe-2cac80637009`).

Retain the central fixed Disney+ fallback. No verified event-specific
Disney+ URL was available.

### Golf participant start times

Updated the following result values, rounded to the nearest five minutes,
and used human-readable source URLs for the participant start-time links:

| Activity | Participant | Displayed time |
| --- | --- | ---: |
| Solheim Cup: Sista Runda (Viaplay) | Linn Grant | 11:45 |
| Solheim Cup: Sista Runda (Viaplay) | Maja Stark | 13:00 |
| Solheim Cup: Sista Rundan (V Sport Golf) | Linn Grant | 11:45 |
| Solheim Cup: Sista Rundan (V Sport Golf) | Maja Stark | 13:00 |
| Sanford International: Dag 3 | Robert Karlsson | 16:05 |
| Sanford International: Dag 3 | Freddie Jacobson | 16:55 |
| Sanford International: Dag 3 | Henrik Stenson | 16:55 |

The Solheim times came from the official LPGA leaderboard. The Sanford times
came from the official PGA Tour Champions tee-time page. Existing Amgen Irish
Open participant links remain unchanged; the final-round times could not be
verified from a published official list.

### Participants, metadata, stars, and detail activities

- No participant-link changes were needed. Eliminated Amgen golfers
  remain linked and inactive as required.
- All 125 participant entries have gender, birth date, and formative club
  data. No new Person or organization relationship changes are needed.
- No watch-priority changes are recommended.
- No detail activities are recommended; the available golf start times are
  sufficient for the participant-specific information.

## Unresolved items

- `Amgen Irish Open: Sista Rundan` has no verifiable published final-round
  tee-time list at execution time. No final-round times are added.
- The existing grouped-card behavior remains deferred, as requested.

## Evidence saved

- MXGP: <https://www.tv.nu/s/e_4936060_20260913>
- Solheim Cup: <https://www.tv.nu/s/e_4936069_20260913> and
  <https://www.lpga.com/tournaments/the-solheim-cup/leaderboard>
- Heidenheim: <https://www.tv.nu/s/s_4667929_20260913>
- Celta de Vigo: <https://www.tv.nu/s/s_4615657_20260913>
- Levante: <https://www.tv.nu/s/s_4615659_20260913>
- Sanford International:
  <https://www.pgatour.com/pgatour-champions/tournaments/2026/sanford-international/S2026023/tee-times>
- Amgen Irish Open official round 4 page:
  <https://www.europeantour.com/dpworld-tour/amgen-irish-open-2026/tee-times?round=4>
- Amgen Irish Open secondary tee-time page:
  <https://www.golfchannel.com/dp-world-tour/2026/amgen-irish-open/tee-times>

## Public card counts

- Step 1, initial public view: 34 activity cards, 44 published rows, and
  125 visible participant entries.
- Step 7, final read-only public refetch: 34 activity cards, 44 published
  rows, and 125 visible participant entries.
- Difference: none; the database changes did not alter the card or
  participant-entry counts.
