# Activity run report — 2026-11-19

Run start: 2026-11-19 13:42:15 UTC.
Database target: sesport at 172.18.0.2:5432, PostgreSQL 17.10.
Starting snapshot: 178 visible, unprocessed broadcasts.
Final state: 12 processed, 84 rejected, and 82 unresolved. Processed and rejected broadcasts are
hidden; unresolved broadcasts remain visible and unprocessed.
A readback through bin/dbc.sh verified one final outcome for every snapshot ID. There were no
missing IDs, duplicate links, or state anomalies. All 82 unresolved broadcasts retain an
organization marker.

## Processed broadcast links

Each row maps a starting broadcast ID to its linked activity ID.

- GCL Verona quarter-final, Nov 19: `091c79f3-d7ad-1336-b11d-4d2578e1132e` →
  `82fccbf3-a7dd-4371-afa1-50f33cebf169`.

- SailGP Dubai Day 1, Nov 21: `70d5b356-49a0-953d-b2ab-a23cad25ef7a` →
  `299bb0de-47ef-43bd-9d44-b4ecc2fe3fd1`.

- SailGP Dubai Day 2, Nov 22: `6fbae245-86fb-5539-bdc9-198e3e07e109` →
  `1ef4c7fb-dbc6-44e9-b373-47454cb8656e`.

- SailGP Dubai Day 2, Nov 22: `55e86c14-2fd5-aa36-827f-da06c664d76f` →
  `ecf892eb-96b9-4c8a-8da4-80aa5cca1d33`.

- The RSM Classic Round 3, Nov 21: `8856f2d2-0902-6732-b8e1-425cafa2b97a` →
  `663e0b28-16d6-4ac4-9f5c-2e7fde16db9b`.

- The RSM Classic Round 4, Nov 22: `525b51c1-1ec9-fc36-afb0-bd0e10676a69` →
  `72d669a0-21f6-4d58-b75d-2a1fac962e21`.

- CME Group Tour Championship Round 3, Nov 21: `9632eeec-d401-9f3b-a58e-7faf2e242ef8` →
  `095e4c14-a98f-4881-8c21-1927607b6e8e`.

- CME Group Tour Championship final, Nov 22: `9dacdbfd-9664-0e30-ad4e-8d2b177d1919` →
  `ff966a48-8656-4505-a38d-12da1d4b95de`.

- LGCT Super Grand Prix final, Nov 21: `307b0a21-54c7-dc3f-992e-5d7fa01cb1a8` →
  `48ce304d-c460-4000-87a1-3c1c2b723b6a`.

- BMW Australian PGA Championship, public date Nov 25: `72aa3846-1627-c233-b059-f7f0a9d21e67` →
  `a635ed4a-b985-4281-aa06-09c24df732d5`.

- BMW Australian PGA opening feed, public date Nov 25: `fa9757b1-f524-d234-abca-1cae2549d730` →
  `0db6cd10-9c9e-4ac9-943c-71ba017d7ced`.

- BMW Australian PGA continuation, public date Nov 25: `f6cbb213-f934-363f-81d9-48c5d5e336dd` →
  `339a4183-384d-46a3-b371-03855bc1527f`.

## Activity work and verification

### GCL Verona quarter-final

- Group `3b95bcbf-35f0-4356-a82d-7ab435e9afd1`; reused activity
  `82fccbf3-a7dd-4371-afa1-50f33cebf169` on public date Nov 19.

- The 20:20–22:15 CET Eurosport 1 broadcast matches TV.nu and the activity. Peder Fredricson and
  Angelica Augustsson Zanotelli are active; no detail activity was needed.

- The grouped card shows both riders with ages and clubs. Nov 19 stayed at 3 cards.

### SailGP Dubai

- Group `240b4f08-68a4-4a5e-aec5-1c9908be046e`; public dates Nov 21 and 22.
- Changed activities for Nov 21: `f83a54e8-29ef-4766-8235-f1d721002042`,
  `299bb0de-47ef-43bd-9d44-b4ecc2fe3fd1`, `e5bb6526-69df-49c6-81d7-ebf35665939b`.

- Changed activities for Nov 22: `72bb9cd9-9ba4-48b1-9060-db17c47b2e49`,
  `1ef4c7fb-dbc6-44e9-b373-47454cb8656e`, `ecf892eb-96b9-4c8a-8da4-80aa5cca1d33`.

- Removed 12 incorrect links for Julia Gross and reserve Hugo Christensson. Felicia Fernström and
  Julius Hallström remain active on all six activities.

- Day 1: TV4 Play 12:00–13:30, Viaplay 12:00–14:00, TV4 Sportkanalen 13:00–17:05 CET. Day 2: TV4
  Play 12:00–13:30, TV4 Sportkanalen 12:00–13:50, Viaplay 12:00–14:00 CET. Rows match their TV.nu
  entries and links.

- Cards show the two active sailors with profiles, ages, and clubs. Nov 21 and 22 remained at 5 and
  3 cards.

### The RSM Classic

- Group `4ea72896-3012-4a47-b62c-5c0803271ebc`; affected public dates Nov 19–22.
- Created or changed activities for Nov 19: `fc7f38fe-53b4-58b3-93d2-8c90a4429440`,
  `a6c812fe-429f-4887-8370-8e972087ddf0`, `0162432f-8256-454d-aef4-df226fdc512d`.

- Created or changed activities for Nov 20: `e0db5bf5-b43e-5dc4-becb-3b5a7a3d8011`,
  `4e93358d-3ec8-4754-9227-48d57288dc61`.

- Created or changed activities for Nov 21: `663e0b28-16d6-4ac4-9f5c-2e7fde16db9b`,
  `718649a1-1b99-4967-8e0f-accc2db546cf`.

- Created or changed activities for Nov 22: `72d669a0-21f6-4d58-b75d-2a1fac962e21`,
  `3830a3cb-3309-43ab-b50f-5bfc4bda5605`.

- Corrected the Day 1 split: HBO Max 17:40–21:00 CET, Eurosport 2 18:00–21:00, and the broad HBO Max
  segment 14:30–22:00. The Round 3 and Round 4 broadcasts link to their matching activities.

- PGA TOUR field updates show Ludvig Åberg withdrew on Nov 16. His links are retained as inactive on
  all nine activities. Henrik Norlander was missing and is now linked as active on all nine. David
  Lingmerth, Pontus Nyholm, and Jesper Svensson remain active; these are the four confirmed active
  Swedes.

- Saved official start times for Rounds 1 and 2 on the five Day 1–2 activity variants. Round 1:
  David Lingmerth 15:36, Henrik Norlander 15:47, Pontus Nyholm 16:20, Jesper Svensson 16:31 CET.
  Round 2: David and Henrik 14:30, Pontus 15:14, Jesper 15:25 CET. Round 3–4 tee times were not yet
  available; none were guessed.

- Nov 19–22 cards show the correct active roster, Åberg marked out, sourced times, profiles, ages,
  clubs, times, channels, and streams. Card counts remained 3, 4, 5, 3.

### CME Group Tour Championship

- Group `bcf2df51-bbaa-404c-b900-aa64be25621c`. Processed Round 3 activity
  `095e4c14-a98f-4881-8c21-1927607b6e8e` on Nov 21 and final-round activity
  `ff966a48-8656-4505-a38d-12da1d4b95de` on Nov 22.

- The dashboard-flagged Round 1 activity `064a61b7-8414-4b6c-a5fc-3cbf4462ed78` now has Maja Stark’s
  official 14:35 CET start time. Her tee time is before the 20:00 broadcast, so no detail activity
  was added. Maja is active; Linn Grant is not in the official field.

- Published V Sport Golf segments and stream rows match TV.nu. Maja’s profile, age, and club display
  correctly. Public counts remain 3 on Nov 19, 5 on Nov 21, and 3 on Nov 22.

### LGCT Super Grand Prix

- Group `fb0bee1f-e9be-4128-bcbf-605f87c646f2`; public date Nov 21. Broadcast
  `307b0a21-54c7-dc3f-992e-5d7fa01cb1a8` links to activity `48ce304d-c460-4000-87a1-3c1c2b723b6a`.

- Updated ParticipationEvidence URLs on the three group activities:
  `48ce304d-c460-4000-87a1-3c1c2b723b6a`, `6dab86fd-7c0a-47f5-9b26-90015e53b169`,
  `8716957f-762d-4693-8953-88be3a6dabff`.

- The official field lists Angelica Augustsson Zanotelli; Peder Fredricson and Henrik von Eckermann
  are not in this Super Grand Prix. The grouped card shows Angelica and the three correct channel
  rows. Nov 21 remained at 5 cards.

### BMW Australian PGA Championship

- Created group `081529c7-c30a-4aed-aaf0-2bf336efc902` for Nov 26–29, with public sport date Nov 25.
- Created and published `a635ed4a-b985-4281-aa06-09c24df732d5` (Viaplay; calendar date Nov 26).
- Created and published `0db6cd10-9c9e-4ac9-943c-71ba017d7ced` (V Sport Golf opening segment;
  calendar date Nov 26).

- Created and published `339a4183-384d-46a3-b371-03855bc1527f` (V Sport Golf continuation; calendar
  date Nov 26).

- Processed the three snapshot broadcasts into the three activities listed above. Ranges are
  02:00–07:30, 02:00–03:30, and 03:30–07:00 CET.

- Linked Alex Norén, Joakim Lagergren, Christofer Blomstrand, Adam Wallin, and David Nyfjäll on all
  three activities. Reused Christofer’s existing entity; created David Nyfjäll
  (`f1600212-3787-46e8-a259-b419951902fe`) with verified birthdate, PGA TOUR profile, and Upsala GK
  affiliation.

- Updated Alex Norén’s profile to the DP World Tour and added current Ullna Golf & Country Club
  affiliation. His formative club remains Haninge GK. Added the direct Viaplay event link after the
  first public check showed the catalog fallback.

- Nov 25 shows the grouped card, three time/channel rows, five players with ages, clubs, and
  profiles. Nov 26 has no card under the sport-day rule.

## Dashboard review and final public checks

- The authenticated dashboard initially showed three activity issues: missing participant start
  times for RSM Round 1, RSM Round 2, and CME Round 1.

- Added 21 sourced participant start-time results: four players across three Round 1 RSM activities,
  four players across two Round 2 RSM activities, and Maja Stark on CME Round 1. Added the six
  matching ParticipantStartEvidence sources.

- Final dashboard check returned HTTP 200, zero activity issue pills, and “No upcoming activity
  issues.” The password was read from `.env` and not logged.

## Public card counts

| Public date | Before | After |
| --- | ---: | ---: |
| 2026-11-19 | 3 | 3 |
| 2026-11-20 | 4 | 4 |
| 2026-11-21 | 5 | 5 |
| 2026-11-22 | 3 | 3 |
| 2026-11-25 | 0 | 1 |
| 2026-11-26 | 0 | 0 |

All six pages on https://sesport.se returned HTTP 200 at final verification. Nov 25 gained the
Australian PGA card because its 02:00 CET start falls before the 04:01 sport-day cutoff. It
therefore appears on Nov 25, while Nov 26 remains empty. Participant corrections did not change the
counts for Nov 19–22.

## Source conflicts and later-run notes

- Australian PGA: Svensk Golf lists five Swedes but describes Royal Queensland. The current official
  DP World Tour event source identifies The Lakes Golf Club in Sydney; TV.nu feed descriptions name
  The Australian Golf Club. The official entry page was unreadable at the snapshot and no individual
  tee times were published. The five names are reported as Svensk Golf’s list, not as a complete
  official field.

- The RSM Round 3 and Round 4 tee-time sheets were unavailable during this run. Check these before
  publishing participant-specific times for those rounds.

- WTGL correction: Linn Grant is on New York Golf Club. The rejected snapshot listing is
  specifically Atlanta Drive GC vs Boston Common Golf, so it remains hidden; neither scheduled team
  includes her.

- The 82 unresolved broadcasts remain visible and unprocessed with organizations and evidence-based
  reasons below. They are ready for a later run when new entries, start lists, fixtures, or mappings
  become available.

## Evidence sources

Activity, participation, start-time, and person-fact evidence is stored on the corresponding
activity or entity records. These references support key classifications and corrections:

- [F1 official 2026 driver roster](https://www.formula1.com/en/drivers)
- Nitto ATP Finals official singles field (direct URL is preserved in source evidence).
- Nitto ATP Finals official doubles field (direct URL is preserved in source evidence).
- [WTGL official team roster](https://wtglgolf.com/teams)
- PGA TOUR RSM Round 1 tee times (direct URL is preserved in source evidence).
- PGA TOUR RSM media center and Round 2 tee sheet (direct URL is preserved in source evidence).
- LPGA CME Round 1 pairings (direct URL is preserved in source evidence).
- [TV.nu golf listings for Nov 26](https://www.tv.nu/sport/golf?datum=26-november)
- SailGP Artemis crew update (direct URL is preserved in source evidence).
- [FEI Verona event schedule](https://www.fei.org/events/2026_CI_0040)
- MotoGP Portugal event page (direct URL is preserved in source evidence).
- [FIS Alpine live calendar](https://www.fis-ski.com/DB/alpine-skiing/live.html)
Other official sources in the run record include PDC elimination reports, ISU Seoul entries, FIS
schedules and start lists, the Belgian Cycling calendar, the WST qualifying schedule, Global
Champions field announcements, DP World Tour event information, Svensk Golf’s five-player report,
and TV.nu broadcast listings.

## Rejected broadcasts (84)

Each ID below was hidden and remains unprocessed. The reason is the verified classification recorded
in the Step 3 snapshot.

#### 1 — The official Nov 20 start lists contain no SWE entries.

- `887e630d-67e3-5a39-ac6f-b3f47715796a`

#### 1 — The official Verona programme identifies a Walk of Champions ceremony.

- `e0cf7cfc-ebab-6c32-b9d2-91c4ea031526`

#### 3 — Both Swedish entrants were eliminated before these broadcasts.

- `205914d2-8c67-8a3a-9ffa-0cf0c8ba69ea`
- `bb95ed5e-3bf2-4039-968c-8d97483e3afa`
- `f7b84614-ebf0-c03a-a2b3-b694eb4bedcb`

#### 2 — Complete ISU Seoul entries have no SWE entry.

- `72e5f0a0-5455-df3e-9c7b-9cda369db97b`
- `90e4d8d2-304c-173f-b0db-67a07911db17`

#### 4 — Domestic Swedish competition.

- `12cb75aa-e7b6-e533-a745-33d202f467f5`
- `2007eb2d-70f3-7037-884c-0557b9a93cc7`
- `69ea15ec-50cc-2034-b0a7-86196b57dc83`
- `ad357562-1b9f-fb3b-9fae-37fdec01a32c`

#### 8 — Domestic Swedish league fixture.

- `0934c390-92d5-7638-9538-12b8fe7f0689`
- `32e72446-deb7-2d32-9adb-ead29c592264`
- `34e2942f-4f8b-0336-b38f-b133a69c8ab6`
- `9bf2998c-ca7e-503c-9eb0-f4687bf6d9bb`
- `bd18c446-98af-bd3d-958d-8a37dcc38d4f`
- `d422fb60-9e9d-b135-9795-fe134e2f6edd`
- `dec0e20a-526c-393b-9f48-08f4ab8ac54d`
- `e2ee137b-ac57-d237-b2ad-a3549a669e1e`

#### 25 — Foreign-team fixture; no Swedish club is named.

- `0e0e001b-10b3-e938-937b-c4aaaa13db45`
- `27c62d4b-4ee7-a830-aa24-d19232fd44e2`
- `320f9820-7627-cd3e-9e92-51d92973c987`
- `37687b64-cb9b-b43d-a27c-1d770317c93d`
- `390f68e2-7cb7-b33e-ae54-a4cb5517fe8d`
- `43ed5e99-51fa-343e-8a6a-bfa7b866f2d5`
- `67a4292e-f661-f533-8daa-7e497096fb23`
- `6aab5a62-03ec-4d3c-b417-0839c88a8243`
- `6ba7e7a9-897b-a432-9ac1-0d7a12ae03da`
- `74772e13-acb9-c03b-a59d-67f4c09b7268`
- `75b02314-e45f-163d-9d3f-c766e866b994`
- `a26e2bc1-dec5-143a-bea2-3dc6b0cf3ee5`
- `affc0f0a-ad0b-b933-94d7-2e29bb8bef49`
- `b246405e-f702-df39-bd9c-a8c50863173c`
- `bb8ab642-734a-5a37-8986-25df92f800a8`
- `bba2c7c0-a94b-963a-b0e8-c23f8665fed4`
- `c16ba280-03cf-db33-82b1-5caa204cb07c`
- `d1965426-3d0e-6930-bdc0-720b8ce6f89e`
- `d259964d-5778-5c39-8bf0-c9662a3415b1`
- `d4680b62-0fb7-bb3b-be62-c5c50dfd868a`
- `d59c8b42-8c7d-3a3d-b02b-0da05f0399e7`
- `d9f8e3e3-830c-2f3b-9b97-31f8c7a46736`
- `ec9236ed-3662-d33e-a51d-528285890081`
- `f91d5d95-9a03-2134-9a58-3b44bec2fea2`
- `fc9875f9-d17a-b13a-a1c5-c71e06434c98`

#### 1 — Generic event highlights listing.

- `7cf5604e-983b-143f-8c73-b544a7a46371`

#### 7 — Standalone generic highlights, recap, or studio programme.

- `24dc3bf4-4457-1e35-908d-a42d4f69b391`
- `3b1522b0-2c1d-1e38-b86d-3162d02f9df1`
- `434a7e1c-82e5-ae32-977d-ba5105be925b`
- `4d26e2f5-c73d-a530-a59a-2368f8c3511f`
- `6400459d-7916-d938-96de-563fb27b9fcc`
- `7e4ea9c8-79cb-da31-8785-6c9d29cc922c`
- `a3f3f3fb-cce7-4738-ad75-47afefa397e6`

#### 19 — The official 2026 driver roster has no Swedish driver.

- `23ab76b5-7396-943e-b17b-e8c1b69f873a`
- `25a9c077-077b-8334-ab2a-4e604e1cecd6`
- `2706f811-4726-093c-a219-1e86bd2571f1`
- `2eb9d11c-d0cd-993e-ac6d-55a3323290ae`
- `43841ae5-b7fc-4135-b3e4-ee650608d74b`
- `451b0659-e71d-c13a-b1c5-caa251081293`
- `541e9ba0-4191-153c-a2a2-1fb60d435c84`
- `7b6931f4-cbd1-ac32-938c-b64a69106ceb`
- `7cc1e951-2bec-cf37-86c3-d6fb9855f82b`
- `857b384b-5f7b-f035-b326-5289d58a4b94`
- `88493caa-fa42-0b3a-ad42-c7566b91ad7d`
- `aba8b52b-8083-1f3c-8c74-992ec6cf6577`
- `b1aadb9c-6aea-863f-a3a9-fc0a4792eec3`
- `bb9f7de4-9c2f-8e39-8caf-7467b948ef0f`
- `c981a43b-64e0-a03b-b34d-a68181acefe4`
- `d0233c1f-4f9a-7d36-9c2a-060a0c112db3`
- `d5437099-9624-9035-ba63-cb5f6980696f`
- `d87a71a6-9c6f-fa35-929f-01c9e57f2646`
- `de6fab4f-3959-3f37-b8f1-3e3a9a44db7d`

#### 12 — The official 2026 singles and doubles fields have no Swede.

- `02bc6d17-d3c5-ec34-9d5c-7b058320ed55`
- `03c131b8-199f-a439-9b7d-b9bbf5c54f1c`
- `0ab38ebb-4402-e635-9dbe-938076f0a4ad`
- `20ec2522-ba9b-de35-80a1-2121b6323b63`
- `39013dd0-866c-a13b-a76b-181d85a499f7`
- `95f6dca3-3dbe-5e3c-8f1e-c92008a11e1c`
- `97ade0eb-11b6-d63e-afc2-8f525290fe20`
- `9ca0cde9-05e4-8b3d-9045-eecca4aa911e`
- `b942ddbb-cf28-9d31-a4d3-2d1b6df5762e`
- `be25b1a1-4ebe-6936-82d0-7099c6848141`
- `dfd61a16-c0a3-7f35-8083-68bff89f1afe`
- `e89932d5-09ee-5f39-9207-b394ad6da4d3`

#### 1 — Atlanta–Boston WTGL fixture

Reason: Linn Grant is on New York Golf Club, which is not in this match.

- `cfcb66ac-2f44-153d-9439-aa9f6a765e4f`

## Unresolved broadcasts (82)

Every ID below remains visible and unprocessed. Organization markers are current database values;
each reason is the one recorded in the run checkpoint.

#### Backhoppningslandslaget — 6 unresolved broadcast(s).

  Reason: Lillehammer start lists are unpublished; no Swedish start is confirmed.

  - `0871bf32-0241-e335-b408-8eeba92491e8`
  - `098b6e2c-e41a-443c-99f6-911917c4be62`
  - `1a76f6a5-6ce0-d038-b57c-326ba86e2848`
  - `b36fdc5c-d136-be33-80db-f5dbfa2622eb`
  - `cc680dd5-1994-1b33-bac1-261438ba22e2`
  - `d969e3ed-d925-0131-802d-752051541753`

#### Cycling — 6 unresolved broadcast(s).

  Reason: No complete authoritative event start list confirms Swedish participation.

  - `125e6de6-9201-c433-bdfd-5ce360432784`
  - `1d48b702-89f3-f13b-844a-178262190764`
  - `643e7a75-d19f-d230-a58a-c49b26e35491`
  - `89147eff-732f-4c3e-87ab-2c774c91e5a8`
  - `a4d5ae9c-1776-e438-bb7e-10f4bed0b176`
  - `ff391930-633b-6c37-98dc-cce6395363c0`

#### Cycling — 2 unresolved broadcast(s).

  Reason: No complete official Beringen start list confirming Swedish entries was found.

  - `07d89998-67bc-8732-b931-5ca83ad3bed6`
  - `6f00ba7d-ba45-a131-b6eb-0b830ba18f3c`

#### FIS Alpine Ski World Cup (Men) — 4 unresolved broadcast(s).

  Reason: Race-specific Swedish start lists are not published; selection is made per race.

  - `66aa9475-16f8-a638-b88e-c556c65a5d13`
  - `a71ff7d0-ca08-ef3e-a270-b366c9d31b8e`
  - `bda966da-6823-8237-bcfb-b3e5019c81f3`
  - `de15e723-6c54-ff3a-805c-d193e82831f0`

#### FIS Alpine Ski World Cup (Men) — 3 unresolved broadcast(s).

  Reason: The official Gurgl schedule lists the Nov 21 draw; no race start list is out.

  - `2654b803-d605-3a34-9fd1-07bf307a1b6f`
  - `4537effc-3828-5b3c-ba24-9fc284435f58`
  - `96b5c2f8-6f05-193e-a879-547f0b019639`

#### FIS Alpine Ski World Cup (Women) — 4 unresolved broadcast(s).

  Reason: Race-specific Swedish start lists are not published; selection is made per race.

  - `499d349a-8baf-2d35-9e53-4b9d8b131137`
  - `5ae964eb-184c-ad36-a5d6-76f5cddb39c7`
  - `b0833cda-b9cf-c036-b4f0-20c35ceafc95`
  - `dfa6c209-d79a-7237-9037-a53a88dc916d`

#### FIS Alpine Ski World Cup (Women) — 3 unresolved broadcast(s).

  Reason: The official Gurgl schedule lists the Nov 20 draw; no race start list is out.

  - `06fc23e0-49df-a034-9d91-baf7a7c9e682`
  - `dc635c40-7921-c135-923a-b3b0b2f1e4a1`
  - `e63773a4-96a5-b03b-a4d1-e58019c8225b`

#### FIS Ski Jumping World Cup (Women) — 1 unresolved broadcast(s).

  Reason: The official Lillehammer page has no start list or results yet.

  - `7e7cee5a-3df1-ba3c-80a3-3e5c930dfe1f`

#### FIS Ski Jumping World Cup (Women) — 1 unresolved broadcast(s).

  Reason: The official Lillehammer start list does not confirm a Swedish starter.

  - `8f2d9979-e034-4e36-b72e-1ba6225a2671`

#### Ferrari Challenge Europe — 7 unresolved broadcast(s).

  Reason: No complete authoritative Finali Mondiali entry list confirms Swedish participation.

  - `0973f85e-409b-4d38-940c-04a7fa82a7aa`
  - `14d51f7b-f81f-1932-bc45-245fa010c850`
  - `491cb3f3-d987-1f31-a52e-301d9aeabb57`
  - `585ee2bd-28b9-e535-a672-2eee9246d420`
  - `8e169384-58c1-4e3c-9fe2-f23251075635`
  - `b452083a-35f1-bc30-9efa-01d976fc552e`
  - `f937e3b5-97bc-7838-b980-c6eed8bf6003`

#### Ferrari Challenge North America — 2 unresolved broadcast(s).

  Reason: No complete authoritative Finali Mondiali entry list confirms Swedish participation.

  - `79234899-2f39-cd36-99b9-2907c03faa17`
  - `e6622f4d-a08d-d239-89eb-3c268f9c5ad9`

#### Global Champions Tour — 2 unresolved broadcast(s).

  Reason: Sunday finalists depend on progression through the Verona quarter-final.

  - `52ab1771-0c28-de31-8ce7-e824b3e98f4f`
  - `5a86d995-fcd0-da32-9541-9d29f9086d75`

#### Global Champions Tour — 3 unresolved broadcast(s).

  Reason: Swedish team progression to the Sunday final is not yet confirmed.

  - `38964e6d-58ed-ce3a-bc6e-4ed95fd97245`
  - `4c9811b9-a5a4-c339-b8ce-2e8ed8b2f5c6`
  - `9290f5a4-1200-7a36-b820-6e845ffb255f`

#### International Skating Union — 2 unresolved broadcast(s).

  Reason: No complete official Obihiro speed-skating entry list confirms Swedish starters.

  - `37c9b20f-d041-f030-af24-234fd311d010`
  - `a228274d-11c3-193b-a60c-bed35abbef56`

#### MotoGP — 11 unresolved broadcast(s).

  Reason: The official Portugal event page says the full entry list appears days before the GP.

  - `03394ebb-f85a-2232-9c67-54393709aeeb`
  - `09dcb5c3-851a-643d-8432-e5230750275b`
  - `0bb1fce4-9445-5139-9a28-c1ca13e4e201`
  - `0cbb6a1e-03c6-2e3e-9405-74ab3efcf2ba`
  - `19b3683c-6169-8931-98e3-2a3d67958f01`
  - `3aaa695d-9841-893f-979a-a866e2e57afc`
  - `50777f04-218b-c93e-8d17-de3ff1ff0b4d`
  - `8eff7a97-edab-f234-a452-7726b6d6c65b`
  - `9f3cdc62-0202-c731-947d-1c0fbf017acb`
  - `b8b0f85b-2ed1-4438-afdd-834933cf48ea`
  - `ef1aae20-d59e-4c32-aaaa-f76687ee671e`

#### MotoGP — 22 unresolved broadcast(s).

  Reason: The official Portugal page says the entry list appears days before the GP.

  - `095ce56e-f7af-5338-b582-4ef567796f2e`
  - `096c43e3-b763-ef33-8dd4-16482832c474`
  - `0d34cc07-063e-d933-9284-a030816fd5a7`
  - `0dbe66a1-500e-6038-b908-0af045ac6bde`
  - `1698dc74-2e96-c03b-8fcf-4f939b77d6b1`
  - `21793271-3089-5d3c-9e99-178b4bf932ee`
  - `349f6641-3eb6-c33c-bfce-01a40d83bbe3`
  - `413ceedc-a50e-1c33-a9ea-3995ca61052a`
  - `4f03fcda-5c4f-cf33-aa57-e31ead76f6f1`
  - `591f0ce6-656d-9530-b586-ab699cfeb090`
  - `8b0fa140-6d5f-de3c-9b56-28b25677501a`
  - `8e301381-880d-e430-878d-35bc2df2a4fd`
  - `8f8459fa-e2e8-963e-9c68-e32c129c6e47`
  - `90c29842-7cb6-213e-b896-8d78ef64883f`
  - `973e7d81-b6a1-eb34-a7a8-3f1c10df2cc2`
  - `bb7e134d-f916-f033-8233-75537963f4c3`
  - `bc6b40ad-7636-5b37-ac14-6ab0a0870089`
  - `d3d8fe7b-ccb9-4838-9883-96b850d30c8e`
  - `d4332cbf-cb56-d23e-a378-3079f83ebe2d`
  - `d474368c-aaa9-9a37-940f-bba9a46794f4`
  - `def3cd60-a977-6132-b507-ec20a44f495a`
  - `e2f6e7f9-183e-183e-be65-c8d1feab140d`

#### World Snooker Tour — 1 unresolved broadcast(s).

  Reason: The Nov 23 slot falls after the official qualifying dates; its event mapping is unclear.

  - `d8c017ea-dbba-9536-99e7-c2258df1dfc1`

#### World Snooker Tour — 2 unresolved broadcast(s).

  Reason: The listings name UK Championship qualifying but identify no match or Swede.

  - `3476ef98-4759-cb30-a449-783cbfdbd029`
  - `a062ed96-5041-bb30-960d-4fd1258073ec`
