# Activity verification report: 2026-09-19

## Execution

- Scope: SESport SportsDay 2026-09-19.
- Public view: `https://sesport.se/?date=2026-09-19`.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 23 activity cards, 26 published activity rows, and
  68 visible participant entries.
- World Series of Darts Finals, Zandvoort, and Genève each contain two grouped
  activity rows.
- The operator approved the recommendations. Changes and evidence were applied
  in one manual PostgreSQL transaction.

## Applied changes

### Broadcast windows

- Changed `La Sella Open: Dag 3`
  (`86dcf754-c3aa-48ee-a1b4-98bd8c641c7a`) from 15:00-19:00 to 16:00-20:00.
  Source: [TV.nu detail](https://www.tv.nu/s/p_1616036_20260919).
- Changed `VfB Stuttgart - Borussia Dortmund`
  (`aadb2dbe-a281-4711-a7f1-44ad8280beb3`) from 18:20-20:40 to 18:20-21:00.
  Source: [TV.nu detail](https://www.tv.nu/s/s_4616916_20260919).
- Changed `Toulouse FC - Le Havre AC`
  (`32155c88-a15f-46f1-9786-271715fa7b68`) from 20:40-22:45 to 20:40-23:15.
  Source: [TV.nu detail](https://www.tv.nu/s/s_4604544_20260919).
- Changed `Soldier Hollow: Cross Country Short Track`
  (`edd418fb-c9de-4b8e-ab36-642d4d4773f0`) from 20:45-21:45 to 20:45-21:40.
  Source: [TV.nu detail](https://www.tv.nu/s/e_4941131_20260919).

### Stream link and naming

- Deleted the `StreamLink` source
  (`5b24aeb7-e96f-4c29-b1d1-89f5559f3bca`) from `Celta de Vigo - Racing
  Santander`. It was the generic Disney+ homepage. The central Disney+
  channel mapping remains.
- Renamed `Boussia Mönchengladbach - FSV Mainz`
  (`bed8c5e8-0c46-44c7-b371-eb4035a70510`) and its group to
  `Borussia Mönchengladbach - FSV Mainz`. Updated the activity slug from
  `2026-09-19-boussia-monchengladbach-fsv-mainz-match` to
  `2026-09-19-borussia-monchengladbach-fsv-mainz-match`.
- Renamed `Sail GP: Genève` (`3b518888-831c-4cf9-a5d7-b50473896605`) to
  `SailGP: Genève`. Its slug is the unique
  `2026-09-19-sailgp-geneve-race-1`; the other grouped row owns the base slug.
  The grouped title `Genève` stays unchanged.

### BMW PGA Championship: Dag 3

- Set official Round 3 start times in Europe/Stockholm:

| Participant | Start |
| --- | ---: |
| Alex Norén | 13:05 |
| Niklas Lemke | 08:50 |
| Marcus Kinhult | 10:05 |
| Ludvig Åberg | 12:00 |
| Joakim Lagergren | 12:20 |

- Marked Jens Dantorp, Mikael Lindberg, and Sebastian Söderberg inactive after
  they were absent from the official Round 3 draw. Their participant links were
  retained.
- Kept the human-readable DP World Tour tee-time page as the public start-time
  source and saved the official Round 3 API as supporting evidence.

### Participants and priorities

- No participant additions, removals, discipline corrections, or represented-
  organization changes were needed beyond the BMW inactive links above.
- No watch-priority or star changes were needed.
- No detail activities were needed for precise participant timing.

## Evidence saved

- Saved 26 checked TV.nu detail pages as `ActivityEvidence`.
- Saved the official DP World Tour TV schedule as `ActivityEvidence` for BMW.
- Updated the human-readable BMW tee-time source and saved the official Round 3
  API as `ParticipantStartEvidence`.
- Reused the existing official participation evidence for GT World, GT4 European
  Series, SailGP, football, hockey, and MTB.
- Checked and retained the existing La Sella Round 3 draw source; it still
  returns `Draw for specified round not available(HIDDEN)`.

## Unresolved items

- La Sella Open: Dag 3 still has no published official Round 3 draw. Its seven
  current Swedish participants remain active without start times. Rerun when the
  official draw becomes available.

## Public card counts

- Step 1, initial public view: 23 activity cards, 26 published rows, and
  68 visible participant entries.
- Step 7, final public refetch: 23 activity cards, 26 published rows, and
  68 visible participant entries.
- Difference: card and activity-row counts are unchanged. BMW start times now
  render publicly, while the three eliminated golfers remain visible as inactive.
