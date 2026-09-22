# Activity verification report: 2026-09-18

## Execution

- Scope: SESport SportsDay 2026-09-18.
- Public view: `https://sesport.se/?date=2026-09-18`.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- The current `jobs/verify-activities.md` was reread before verification.
- Step 1 public view: 14 activity cards, 16 published activity rows, and
  53 visible participant entries.
- Tour of Abruzzo and Flandern Runt each contain two grouped activity rows.
- The operator approved the recommendations, including removal of the same invalid
  BMW golfers from all upcoming tournament days.
- The changes were applied in two manual PostgreSQL transactions. The second
  transaction corrected the La Sella participant identifiers.

## Applied changes

### Goodwood naming

- Renamed activity `Goodwood Festival of Speed`
  (`c87a9754-48d9-4d13-b386-28d57a3c57b0`) to `Goodwood Revival`.
- Renamed its activity group from `Goodwood Festival of Speed, Goodwood Revival`
  to `Goodwood Revival`.
- Updated the activity slug to match the new title.

### Broadcast windows

- Changed `Flandern Runt` on HBO Max
  (`3dfa2260-6cc4-47bb-ba8e-c09798ce303a`) from 15:30-17:30 to
  15:30-17:20.
- Changed `La Sella Open: Dag 2`
  (`dcedbd83-57a6-474d-9904-3eff5f98b302`) from 16:00-19:30 to
  16:00-20:00.

### BMW PGA Championship: Dag 2

- Deleted the activity links for Albin Bergström, Hugo Townsend, Per Längfors,
  Simon Forsström, and Tobias Jonsson from Dag 2, Dag 3, and Dag 4.
  This removed 15 links in total.
- Removed the corresponding obsolete participant-start result rows.
- Set the following Stockholm start times for the eight Dag 2 golfers who have
  official Round 2 tee times:

| Participant | Start |
| --- | ---: |
| Alex Norén | 08:50 |
| Ludvig Åberg | 08:50 |
| Jens Dantorp | 13:10 |
| Joakim Lagergren | 14:10 |
| Mikael Lindberg | 14:20 |
| Niklas Lemke | 14:30 |
| Sebastian Söderberg | 14:30 |
| Marcus Kinhult | 14:40 |

- Kept the human-readable DP World Tour tee-time page as the rendered start-time
  link.

### La Sella Open: Dag 2

- Replaced the stale ParticipantStartEvidence URL. It pointed to the 2025
  season and rendered the Round 1 draw. The source now uses the 2026 Round 2
  OCS/LET draw.
- Set the following Stockholm start times:

| Participant | Start |
| --- | ---: |
| Caroline Hedwall | 10:50 |
| Kajsa Arwefjäll | 11:10 |
| Moa Folke | 11:10 |
| Lisa Pettersson | 11:30 |
| Corinne Viden | 15:20 |
| Louise Rydqvist | 15:40 |
| Andrea Lignell | 15:50 |

## Evidence saved

- TV.nu detail pages for Goodwood, BMW PGA Championship, Flandern Runt, and
  La Sella Open.
- Official Goodwood Revival event page and 2026 entry list.
- Official DP World Tour tee-time page and Round 2 tee-time data.
- The corrected official OCS/LET Round 2 draw for La Sella Open.
- The official PDPA draw for World Series of Darts Finals, confirming Viktor
  Tingström's participation.

## Unresolved items

- None.

## Public card counts

- Step 1, initial public view: 14 activity cards, 16 published rows, and
  53 visible participant entries.
- Step 7, final public refetch: 14 activity cards, 16 published rows, and
  48 visible participant entries.
- Difference: card and activity-row counts are unchanged. Five Dag 2 BMW
  participant entries were removed from the current public view.
