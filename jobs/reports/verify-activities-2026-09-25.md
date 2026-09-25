# Activity verification report - 2026-09-25

## Scope and public-view counts

- Scope: public SportsDay view for 2026-09-25 in Europe/Stockholm.
- Step 1 count: 7 activity cards.
- Step 7 count: 7 activity cards after a second retrieval.
- The operator approved and applied the Open de France and NW Arkansas changes.
- All other recommendations below remain unchanged.

## Applied approved changes

### FedEx Open de France: Dag 2

- Added Marcus Kinhult's 08:30 CEST start time. Saved the official DP World Tour
  tee-times page and GoAndPlay's complete second-round sheet as
  `ParticipantStartEvidence`; the official page is the start-time link on Index.
- Removed Per Längfors and Sebastian Söderberg from this activity's
  `activity_entity_links`.

### NW Arkansas Championship: Dag 1

- Changed the activity start from 18:30 to 17:00 CEST; the 20:00 end remains.
- Saved the TV.nu stream listing and official LPGA broadcast schedule as
  `ActivityEvidence`.

## Remaining recommended changes

### Azerbaijan Grand Prix: Formula 2

- Add an activity-specific `StreamLink` titled `Viaplay Sport` to `Azerbajdzjans
  GP: Sprint`, using the direct event URL below.
- Add an activity-specific `StreamLink` titled `Viaplay Sport` to `Azerbajdzjans
  GP: Race 1`, using the direct event URL below.
- Extend the parent activity end from 13:45 to 13:50. Set both `local_end_time`
  and `ends_at`; Viaplay's direct Race 1 stream ends at 13:50.
- Add the current FIA session guide and TV.nu event pages as `ActivityEvidence`.

### Participant stars

- Change Linn Grant and Peder Fredricson from `tier_0` to `tier_1`.
  Linn's current LPGA profile shows CME rank 81, two top-10 finishes, and no
  wins. Peder's current FEI profile shows Longines rank 60 and 2026 World
  Championship ranks of 52 individually and 10 with the team. These results do
  not meet the current top-level performance rule for a star.
- Save the cited Linn and Peder results as `ParticipantStarEvidence` for the
  priority changes below.
- Add `ParticipantStarEvidence` for Henrik von Eckermann, Ludvig Åberg,
  Alexander Isak, Victor Nilsson Lindelöf, and Viktor Gyökeres. No such evidence
  is currently recorded for these five people. Dino Beganovic's existing star
  evidence remains in place, consistent with the operator's prior instruction.

## Unresolved items

- André Göransson's doubles match has an existing TV4 Play link to the general
  `Chengdu Open (250)` stream. TV.nu lists that stream from 07:00, but no
  match-specific Swedish broadcast window was found. Keep the operator-set
  10:30-12:30 activity time pending a source that confirms the match's exact
  place in the tournament feed.
- Add TV.nu's tournament stream listing as `ActivityEvidence` for this item.

## Evidence sources

### ActivityEvidence

- TV.nu F2 Sprint: `https://www.tv.nu/s/e_4945295_20260925`
- TV.nu F2 Race 1: `https://www.tv.nu/s/e_4952740_20260925`
- FIA F2 current session guide:
  - Host: `https://www.fiaformula2.com`
  - Path part 1: `/en/latest/article/how-to-watch-round-12-in-baku-`
  - Path part 2: `tv-session-times-and-more.5pVVVALjr3uo9hiwPWcNv6`
- TV.nu LPGA stream: `https://www.tv.nu/s/p_1619922_20260925`
- Official LPGA coverage schedule:
  `https://www.lpga.com/tournaments/walmartnwarkansaschampionshippresentedbypg/leaderboard`
- TV.nu Chengdu stream: `https://www.tv.nu/s/p_1614273_20260925`

### ParticipantStartEvidence

- Official DP World Tour tee times:
  `https://www.europeantour.com/dpworld-tour/fedex-open-de-france-2026/tee-times`
- Complete second-round tee sheet:
  `https://goandplay.eu/en/2026-fedex-open-de-france-tee-times/`
- Both tee-time sources are saved as `ParticipantStartEvidence` for the Open de
  France Day 2 activity. The official DP World Tour page is linked from
  Marcus Kinhult's 08:30 start time.

### ParticipantStarEvidence

- Linn Grant current LPGA results:
  `https://www.lpga.com/athletes/linn-grant/101583/results`
- Peder Fredricson current FEI ranking and results:
  `https://www.fei.org/athlete/10002504`
- Henrik von Eckermann current FEI ranking and results:
  `https://www.fei.org/athlete/10001601`
- Ludvig Åberg 2026 PGA TOUR stats:
  `https://www.pgatour.com/player/52955/ludvig-aberg/stats`
- Alexander Isak's current Liverpool match report:
  - Host: `https://www.liverpoolfc.com`
  - Path: `/news/alexander-isak-scores-earn-liverpool-victory-bournemouth`
- Victor Nilsson Lindelöf's current UEFA profile:
  `https://www.uefa.com/european-qualifiers/teams/players/250055905--victor-lindelof/`
- Viktor Gyökeres's 2026 UEFA stats:
  `https://www.uefa.com/european-qualifiers/teams/players/250105927--viktor-gyokeres/statistics/`

### StreamLink

- `Viaplay Sport` F2 Sprint:
  `https://viaplay.se/sport/motorsport/formel-2/baku/s26091505133974374`
- `Viaplay Sport` F2 Race 1:
  `https://viaplay.se/sport/motorsport/formel-2/baku/s26091574515551213`
- Both URLs are direct Viaplay event links with the TV.nu affiliate wrapper and
  tracking parameters removed.
