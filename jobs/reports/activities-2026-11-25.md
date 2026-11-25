# Activity processing report — 2026-11-25

- Started: 2026-11-25 03:41:40 UTC.
- Finished: 2026-11-25 04:51:53 UTC.
- Snapshot: 126 visible broadcasts; initially unprocessed and unlinked.
- Final outcomes: 19 Processed, 21 Rejected, 86 Unresolved.
- Final database audit: all 126 IDs have one outcome; no state mismatches.
- Rejected rows are hidden; unresolved rows remain visible and unprocessed.
- No code, assets, schema, configuration, or deployment files changed.

## Created and changed activities

Created 11 activities in two groups. Four existing alpine activities were reused and updated.
All dates below are public calendar dates; times are Stockholm local time.

### Alpine World Cup — reused activities

- Killington group: `66a17ceb-f833-4f47-a9fe-0fde00e6e3ae`.
- Copper Mountain group: `c174bfb1-cf3c-4306-8dd4-6abc0b668656`.
- Public date 2026-11-29; existing activities and groups were reused.
- Activity `56e23a7b-bffb-4888-ac37-57fcf60545ee`; broadcast
  `61f015ec-4a0b-f63b-8e69-c24af1854c2a`.
- Activity `442d50cc-c9ed-4805-96b6-1c9040cbff28`; broadcast
  `94c629f6-d720-5a38-bd3c-472630f424fa`.
- Activity `9361a088-7925-41eb-8f60-2d87efb1c0e2`; broadcast
  `57967b2f-d11e-5d3a-8315-50e062c03849`.
- Activity `c9e229bc-1694-4f6d-8198-30c074936596`; broadcast
  `fbe9a3ca-73a2-1c3d-93ee-c96d236ca358`.
- Copied the direct Viaplay links to the matching activities.

### Scotland–Sweden football — created group

- Group `e45a9fd7-4f1c-40c5-849f-e518c1f0f979`.
- Public date 2026-12-01; 23 provisional players linked to all three activities.
- The Swedish description marks the roster as provisional pending the match-day team.
- Activity `b58ed2bc-75da-48cc-aee4-08737b0c696d`: 20:00–20:50 CET, SVT2.
  Broadcast: `9a22071e-bd4c-4033-b50b-11cc756649fd`.
- Activity `2ebfe0d7-08e9-4ca5-8738-0a66b281f612`: 20:00–22:45 CET, SVT Play.
  Broadcast: `fb04718d-3fe4-5f3f-813b-19def0444435`.
- Activity `adbd3397-e92e-4610-b045-dababe632ecd`: 20:50–22:45 CET, SVT1.
  Broadcast: `71bc59f9-3f97-133d-83c7-c7abe6bdff5e`.

### Andalucía Costa del Sol Open de España — created group

- Group `021c6593-6c4c-46ba-b74b-71fcb24451c6`; public dates 2026-11-26–29.
- Four Swedish players linked to all four rounds; all are active for the no-cut event.
- Round 1, 2026-11-26:
  - Viaplay activity `7ea3ad9b-e34e-4520-b8d0-46c176f22760`, 14:00–18:00 CET.
    Broadcast: `07c3c8b5-7d56-7a3b-b9b9-bbc45954de3d`.
  - V Sport Golf activity `5c627961-07ee-40fc-89b1-a172e7344bdf`,
    14:00–17:00 CET.
    Broadcasts: `eeed8f6c-c533-a933-9221-4aa1055af1f2`,
    `097c8dd6-5181-d235-9295-a8a128e7d274`.
- Round 2, 2026-11-27:
  - Viaplay activity `b5f1cfe6-a384-4bbf-b2eb-ebfea3e3219b`,
    14:00–17:30 CET.
    Broadcast: `ffa48f81-ce5a-f33c-a4d4-5af26f43c49a`.
  - V Sport Golf activity `430cde15-a950-40e7-a372-c7d8db5b71e0`,
    14:00–17:00 CET.
    Broadcasts: `8b1a792c-9e67-4f3e-88a4-00104fee580f`,
    `708e62ff-75d9-c437-951e-5b1a9c48da7b`.
- Round 3, 2026-11-28:
  - Viaplay activity `da06247a-02ba-455c-801f-af4f74e2fe55`,
    14:00–17:30 CET.
    Broadcast: `c6f187e4-0c88-d035-b811-5069f736df0f`.
  - V Sport Golf activity `687c3dfb-cb00-4f0c-bfd3-dc9a8a8b8cd5`,
    14:00–17:00 CET.
    Broadcasts: `d32853c0-d995-9e3e-ba6a-95174a550995`,
    `89edc1e7-3291-5433-b36f-3f414c109d60`.
- Round 4, 2026-11-29:
  - Viaplay activity `fc0989db-63da-458f-a1e0-837ee12c50e2`,
    14:00–17:30 CET.
    Broadcast: `2bea250c-e609-7333-977c-5a0d23b6b3ef`.
  - V Sport Golf activity `954a4ecb-5d22-4b00-b21b-be15b1988ee6`,
    14:00–17:00 CET.
    Broadcasts: `caa0daf2-bbde-3831-81dd-6b41e80aed08`,
    `df47b9dd-0455-e83e-ac3c-aeed01bd1668`.
- Viaplay and V Sport Golf have distinct coverage windows; duplicate V Sport
  Golf broadcasts for each round share one activity.

## Other application data corrections

- Set the national team URL to `https://www.svenskfotboll.se/landslag/dam/`.
- Updated Liverpool F.C. to `https://www.liverpoolfc.com/teams/womens-team`.
- Updated Tottenham to `https://www.tottenhamhotspur.com/teams/womens/squad` and
  Arsenal to `https://www.arsenal.com/women`.
- Added Cornelia Kapocs's current Liverpool team link.
- Changed Kajsa Arwefjäll and Moa Folke profile URLs to official SGF Golfdata profiles
  after the previous URLs returned 403 in the direct URL check.
- Kajsa: `https://golfdata.se/sgfranking/Players_startpage?PlayerID=53484`.
- Moa: `https://golfdata.se/sgfranking/Players_startpage?PlayerID=44203`.
- The updated profiles and all 27 active participant profile URLs now return HTTP 200.
- No star assignments were changed.
- Set or corrected organization markers on 11 unresolved broadcasts:
  - Snowboardlandslaget: `da762a05-41b3-a73c-bd82-b2dac09df13c`;
    corrected from Freeskilandslaget.
  - Längdlandslaget: `6d44bb06-5e88-7e30-bc43-ea77dc00069c`,
    `8c1c622c-b978-a531-bb74-7ccfed9bb26f`.
  - Backhoppningslandslaget: `286beb50-48c3-4138-942a-9d8410d1f510`.
  - World Snooker Tour: `8d0fc3dc-581f-7a3b-8cba-cf2e5e60f0c1`,
    `7b5ba390-b17c-bb3c-8963-f0c9d86b6411`.
  - PDC / WDF: `db32e221-08f2-e73a-b961-0261294f5aa3`.
  - Swimming: `700d1933-d753-fd31-ad6e-8858a1f8a550`,
    `e9c916cd-76d6-7635-9bc2-ee6d4f398ec6`.
  - Frölunda HC: `72473098-c1e2-f33b-b488-15492a0e78a2`.
  - Skellefteå AIK: `f19bdd1b-3623-a534-953b-ab0fdda83976`.

## Rejections and unresolved research

- Rejected 21 broadcasts and changed only `hidden_at`.
- Rejections: five foreign WTGL matches, eight domestic or foreign fixtures,
  six studio or recap programmes, one unidentified placeholder, and one F1 listing
  with no Swedish driver.
- The full list of rejected IDs and individual reasons is in the outcome register below.
- Of 86 unresolved broadcasts, 79 have a known organization and seven do not.
- Main unresolved groups: 19 World Snooker Tour qualifiers, 18 biathlon races,
  22 Ruka cross-country listings, six ski jump listings, five cyclocross events,
  and four ISU listings.
- Other unresolved rows cover swimming (2), equestrian (3), snowboard (3),
  two club rosters, freeski (1), and PDC / WDF (1).
- Six Ruka listings and one ski jump listing have no known organization.
- LET entries confirm the four Swedes, and RFEG confirms 72 holes without a cut.
  LET tee-time pages were blocked by Cloudflare; no player-specific details were added.
- Remaining unresolved IDs, organizations, and their exact reasons follow below.
  Leave these visible and unprocessed until new evidence confirms participation.

## Evidence sources

- Alpine: Swedish Ski Association selection, FIS schedules, and TV.nu schedules.
  [Swedish Ski Association](https://www.skidor.com/), [FIS](https://www.fis-ski.com/),
  [TV.nu](https://www.tv.nu/).
- Football: the official SvFF roster and replacement notice, SVT Play event page,
  and TV.nu channel schedule.
  [SvFF](https://www.svenskfotboll.se/), [SVT Play](https://www.svtplay.se/),
  [TV.nu](https://www.tv.nu/).
- Golf: official LET entries, RFEG no-cut report, the event page, and TV.nu schedules.
  [LET](https://ladieseuropeantour.com/), [RFEG](https://rfegolf.es/),
  [La Cala](https://www.lacala.com/), [TV.nu](https://www.tv.nu/).
- Individual player profiles and facts were checked against official SGF Golfdata,
  federation, team, or club pages and saved as `PersonFacts` evidence.
- Event and schedule sources were saved as `ActivityEvidence`; roster and entry sources
  were saved as `ParticipationEvidence`.

## Public calendar verification

| Public date | Before | After | Change |
| --- | ---: | ---: | ---: |
| 2026-11-26 | 2 | 3 | +1 golf card |
| 2026-11-27 | 6 | 7 | +1 golf card |
| 2026-11-28 | 7 | 8 | +1 golf card |
| 2026-11-29 | 5 | 6 | +1 golf card |
| 2026-12-01 | 0 | 3 | +3 football cards |

Golf appears as one grouped card per date with separate coverage rows for each channel.
The football broadcasts have distinct times and channels, so they display as three cards.
Alpine activities reused existing published cards and added no new calendar cards.

All five affected public pages returned HTTP 200 after the profile corrections. The
Swedish flag asset returned HTTP 200 and rendered on the golf and football participant
rows. The Admin Dashboard showed no upcoming activity issues, open todos, or current AI
alerts; the latest import had completed.

## Broadcast outcome register

- `03c50c6c-ea76-8e30-99d8-b8762542cef5` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `07c3c8b5-7d56-7a3b-b9b9-bbc45954de3d` — Processed.
  Activity: `7ea3ad9b-e34e-4520-b8d0-46c176f22760`; hidden.
  Organization: Ladies European Tour.
- `08c09916-e373-be35-a5aa-63bb3a426985` — Unresolved: no complete Ruka entries or Swedish
  starters found. Organization: unknown.
- `092c9061-7e31-2a3e-8693-4e044c402382` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `097c8dd6-5181-d235-9295-a8a128e7d274` — Processed.
  Activity: `5c627961-07ee-40fc-89b1-a172e7344bdf`; hidden.
  Organization: Ladies European Tour.
- `09f8f581-4cae-d635-b57b-7706ab0951e5` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `0a9a6352-90b5-0539-9d2e-bacf879b4f19` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `0afcff59-7920-3a32-9bba-158332ec6fe7` — Unresolved: no event-specific Swedish freeski entries
  found. Organization: Freeskilandslaget.
- `0cd53d4c-f8b5-fa3c-84e5-dccc2370fbdf` — Unresolved: no current event entry list confirms
  Swedish starters. Organization: Backhoppningslandslaget.
- `0f732f37-6353-3239-a327-db81606ae14b` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `14758203-2174-e93b-b4eb-272b190a9536` — Unresolved: no current event entry list confirms
  Swedish starters. Organization: unknown.
- `154a2b44-3801-503e-ac88-17682e0ee423` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `1651bfe6-6891-df3a-9dab-05a35d7c58dc` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `17b3a92c-adc1-003b-917e-4efdf7c273f1` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `20db0379-c4e6-8535-9a69-80efe96e3e8d` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: unknown.
- `265ce1f2-18d9-d433-bce6-1e6e5f31b234` — Rejected: WTGL match between foreign teams; no Swedish
  club or team. Organization: unknown.
- `286beb50-48c3-4138-942a-9d8410d1f510` — Unresolved: no current event entry list confirms
  Swedish starters. Organization: Backhoppningslandslaget.
- `292839f0-e06a-193b-a579-c3b542340363` — Rejected: Domestic Swedish handball league fixture.
  Organization: unknown.
- `2ad1923d-b275-2032-a227-5b53b6c21398` — Rejected: WTGL match between foreign teams; no Swedish
  club or team. Organization: unknown.
- `2be808ea-54c6-8c31-a12c-43c80dcd4bb9` — Unresolved: no complete Ruka entries or Swedish
  starters found. Organization: unknown.
- `2bea250c-e609-7333-977c-5a0d23b6b3ef` — Processed.
  Activity: `fc0989db-63da-458f-a1e0-837ee12c50e2`; hidden.
  Organization: Ladies European Tour.
- `2c889033-91ef-ad30-a95a-511d40be7ce9` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `307eab4a-cb5b-a037-9c92-25946944e0a6` — Rejected: Domestic Swedish bandy league fixture.
  Organization: unknown.
- `37b6efd9-afb2-4238-a80d-74b4e73cdaf8` — Rejected: Generic Premier League studio programme, not
  competition coverage. Organization: unknown.
- `3a0fcacb-ecf5-3336-b98a-c867f6722c8e` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `42bf7787-6e62-7439-a9a3-22f20cd7cda4` — Unresolved: no event-specific Swedish cyclocross
  entries found. Organization: Cycling.
- `44336c62-26fd-713d-8615-840a6c1e3a91` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `44abdce8-1313-a93d-9fd5-80df835c6cd0` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: unknown.
- `45911c4a-5bf0-9338-b4a1-664dc547faa1` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `4723012b-e7e0-023f-9fe5-8ec06eb0ce1b` — Rejected: Generic Viaplay Vinter studio programme, not
  event coverage. Organization: unknown.
- `4759e730-a67f-ae30-baee-babeef617d7a` — Unresolved: no complete Ruka entries or Swedish
  starters found. Organization: unknown.
- `4be61b67-b053-1b3a-b50f-33cb741b15ce` — Unresolved: no current ISU entries confirm Swedish
  starters. Organization: International Skating Union.
- `4c1039bd-ab7c-a737-a90c-05b244d5290c` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `4cfa039c-7197-3634-a887-3001c15cb56c` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `4dbe20cb-d2a3-ba33-bc83-7bf3ac0af1c5` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `5172989c-352d-3230-8b81-f0503d6b52ee` — Rejected: Foreign hockey fixture with no Swedish club
  or team. Organization: unknown.
- `53359864-0075-4839-98d0-43b0446b3f3a` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `57967b2f-d11e-5d3a-8315-50e062c03849` — Processed: linked to existing activity
  `9361a088-7925-41eb-8f60-2d87efb1c0e2`; hidden. Organization: Alpina Landslaget.
- `5e43124b-9038-6f30-9e6c-289ce977363b` — Rejected: Placeholder contains no identifying event
  metadata. Organization: unknown.
- `61bcee52-2ccf-8238-a9f2-c6436796057f` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `61ecf08b-38c4-ee31-ae64-ea4721cfd24f` — Rejected: Generic fan discussion programme, not
  competition coverage. Organization: unknown.
- `61f015ec-4a0b-f63b-8e69-c24af1854c2a` — Processed: linked to existing activity
  `56e23a7b-bffb-4888-ac37-57fcf60545ee`; hidden. Organization: Alpina Landslaget.
- `67899aa2-8957-e539-9d14-3a4a7ac18d9e` — Rejected: Official 2026 Formula 1 driver entry list has
  no Swedish driver. Organization: unknown.
- `6aa4bf59-4f93-1838-94f9-dbd63144739c` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `6aa7cca9-24dd-ae3a-a13c-faf183349c71` — Unresolved: no current event entry list confirms
  Swedish starters. Organization: Backhoppningslandslaget.
- `6d44bb06-5e88-7e30-bc43-ea77dc00069c` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `700d1933-d753-fd31-ad6e-8858a1f8a550` — Unresolved: Swedish championship squad is known, but
  final-session entries are pending. Organization: Swimming.
- `708e62ff-75d9-c437-951e-5b1a9c48da7b` — Processed.
  Activity: `430cde15-a950-40e7-a372-c7d8db5b71e0`; hidden.
  Organization: Ladies European Tour.
- `71ac1a5f-47c6-c731-856c-dbab94d75b9d` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `71bc59f9-3f97-133d-83c7-c7abe6bdff5e` — Processed.
  Activity: `adbd3397-e92e-4610-b045-dababe632ecd`; hidden.
  Organization: Fotbollslandslaget (D).
- `72473098-c1e2-f33b-b488-15492a0e78a2` — Unresolved: Swedish club is named; match roster is not
  yet confirmed. Organization: Frölunda HC.
- `7394730f-7a40-c237-b207-78a9f6a435db` — Unresolved: FEI event page has no current participant
  entries. Organization: Equestrian.
- `78ae8f65-6c1e-ba39-a772-75c9257339f9` — Unresolved: no event-specific Swedish cyclocross
  entries found. Organization: Cycling.
- `79446276-0f16-5130-bbc5-c2746e165d29` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `7b5ba390-b17c-bb3c-8963-f0c9d86b6411` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `7cb2f18c-fa47-343b-9005-ec3fe66c964a` — Rejected: Studio discussion about domestic Swedish
  football. Organization: unknown.
- `7eeb4534-c077-943c-8c0a-dd8eabded5d7` — Unresolved: no current event entry list confirms
  Swedish starters. Organization: Backhoppningslandslaget.
- `801f42a5-4b24-ab30-b4d4-02fdcbdd8da1` — Unresolved: no event-specific Swedish cyclocross
  entries found. Organization: Cycling.
- `85bc7bdd-42f5-4831-9763-3bf329e62212` — Rejected: Foreign Coppa Italia fixture with no Swedish
  club or team. Organization: unknown.
- `89edc1e7-3291-5433-b36f-3f414c109d60` — Processed.
  Activity: `687c3dfb-cb00-4f0c-bfd3-dc9a8a8b8cd5`; hidden.
  Organization: Ladies European Tour.
- `8b1a792c-9e67-4f3e-88a4-00104fee580f` — Processed.
  Activity: `430cde15-a950-40e7-a372-c7d8db5b71e0`; hidden.
  Organization: Ladies European Tour.
- `8c1c622c-b978-a531-bb74-7ccfed9bb26f` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `8c3d17f0-9cf8-5338-9ebf-52d011d0ed20` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `8d0fc3dc-581f-7a3b-8cba-cf2e5e60f0c1` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `8dbd3ca4-935f-e338-ab23-cd8ded7a369e` — Unresolved: no current event entry list confirms
  Swedish starters. Organization: Backhoppningslandslaget.
- `94c629f6-d720-5a38-bd3c-472630f424fa` — Processed: linked to existing activity
  `442d50cc-c9ed-4805-96b6-1c9040cbff28`; hidden. Organization: Alpina Landslaget.
- `9676f82c-fa14-f23e-a067-318900df1285` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `97f142c3-3651-b530-b6fe-fe467226506d` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `9a22071e-bd4c-4033-b50b-11cc756649fd` — Processed.
  Activity: `b58ed2bc-75da-48cc-aee4-08737b0c696d`; hidden.
  Organization: Fotbollslandslaget (D).
- `9eaa3423-1f89-4a33-a6f4-8a21906433d3` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `a72b9d65-5f58-083f-aee1-536e63780201` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `a7c5db38-0b05-843f-9ebd-7a2d67c359d9` — Rejected: WTGL match between foreign teams; no Swedish
  club or team. Organization: unknown.
- `abbd32af-8210-943a-8681-c0173a1c17a9` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `aceffee3-6991-0d37-a699-707e38e1a944` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `afb35c51-b9f6-4732-9abb-bf668e632b58` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `aff8fb37-0b29-703b-a0c8-18c0307e828b` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `b317cdf1-54e2-7135-9fd5-0f2aff16be80` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `b6b2ed74-b5d5-f53d-ab08-7dd15a297a41` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `bbefc117-ef68-ba31-97a9-0b47842bb32d` — Rejected: Domestic Finnish league fixture.
  Organization: unknown.
- `bc4fb00d-dde9-623e-bda8-4754932764ae` — Unresolved: FEI event page has no current participant
  entries. Organization: Equestrian.
- `bdaf088a-8222-4536-abed-a73ac7895b5a` — Unresolved: no event-specific Swedish cyclocross
  entries found. Organization: Cycling.
- `bdf08ca9-5698-273a-9cd4-dbffeb5c529d` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `c2b94cd5-8121-d23a-8639-a096cabb89f2` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `c39a41cb-8a6c-3631-867e-7e4b8accab7d` — Unresolved: no event-specific Swedish entries found.
  Organization: Snowboardlandslaget.
- `c3e38385-3e60-2837-95fa-70009b7625ce` — Unresolved: no current ISU entries confirm Swedish
  starters. Organization: International Skating Union.
- `c40f77d5-5d91-2b33-8f20-83a076ed9e24` — Rejected: Foreign football fixture with no Swedish club
  or team. Organization: unknown.
- `c4751576-aa70-7034-8d35-b0d54a4daf93` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `c6d0484b-05a0-c130-9adb-044d74d39244` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `c6f187e4-0c88-d035-b811-5069f736df0f` — Processed.
  Activity: `da06247a-02ba-455c-801f-af4f74e2fe55`; hidden.
  Organization: Ladies European Tour.
- `c86d522c-729a-c633-afd7-03844dafc00f` — Unresolved: no Sweden in other published disciplines;
  current women’s entries remain unverified. Organization: International Skating Union.
- `caa0daf2-bbde-3831-81dd-6b41e80aed08` — Processed.
  Activity: `954a4ecb-5d22-4b00-b21b-be15b1988ee6`; hidden.
  Organization: Ladies European Tour.
- `d32853c0-d995-9e3e-ba6a-95174a550995` — Processed.
  Activity: `687c3dfb-cb00-4f0c-bfd3-dc9a8a8b8cd5`; hidden.
  Organization: Ladies European Tour.
- `d40b7c46-8392-0634-bfea-1a5dc7f28681` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: unknown.
- `d73b9d55-31bc-5f38-b60a-9c5b2acb6020` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `d77dc415-822d-a93c-9679-e89033c03b38` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `d784cb68-9114-7c3f-ae06-66997b1a16f3` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `d87b3215-4b68-c539-b442-ab7448b49cab` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `da0f740a-ecdf-7938-be20-594daa5eb55a` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `da762a05-41b3-a73c-bd82-b2dac09df13c` — Unresolved: Swedish snowboard prospect named, but no
  event entry list. Organization: Snowboardlandslaget.
- `db32e221-08f2-e73a-b961-0261294f5aa3` — Unresolved: draw programme; no complete Swedish
  tournament entry list. Organization: PDC / WDF.
- `de2377ca-7408-9d3e-91f8-a955caf5b696` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `de7ac88b-1af5-b43b-abf7-852767f232f8` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `df47b9dd-0455-e83e-ac3c-aeed01bd1668` — Processed.
  Activity: `954a4ecb-5d22-4b00-b21b-be15b1988ee6`; hidden.
  Organization: Ladies European Tour.
- `e3e61f17-fe70-ac3b-b581-307c22dd503b` — Rejected: Domestic Italian league fixture.
  Organization: unknown.
- `e660d892-efb1-1635-9ba8-dbdd758f3fe7` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `e6cd80d7-e8be-7031-82ab-48fe2551e0c9` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `e7f70f2b-1ad2-973e-8cf0-ca2df3c599b1` — Rejected: Generic NHL studio recap, not competition
  coverage. Organization: unknown.
- `e98472bf-9e4a-cf37-9e55-831ebac46ab0` — Rejected: Domestic Swedish bandy league fixture.
  Organization: unknown.
- `e9c916cd-76d6-7635-9bc2-ee6d4f398ec6` — Unresolved: Swedish championship squad is known, but
  final-session entries are pending. Organization: Swimming.
- `e9f5eb6f-46ab-c334-8045-535f6fe90865` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `edbceb19-6c09-5a35-bfb2-d75366b0ce08` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `ee8235e1-bea4-963c-ac83-6ce7f3f28cf4` — Unresolved: no event-specific Swedish cyclocross
  entries found. Organization: Cycling.
- `eedd45a1-0de8-5f3b-8382-125666604027` — Unresolved: no current ISU entries confirm Swedish
  starters. Organization: International Skating Union.
- `eeed8f6c-c533-a933-9221-4aa1055af1f2` — Processed.
  Activity: `5c627961-07ee-40fc-89b1-a172e7344bdf`; hidden.
  Organization: Ladies European Tour.
- `f19bdd1b-3623-a534-953b-ab0fdda83976` — Unresolved: Swedish club is named; match roster is not
  yet confirmed. Organization: Skellefteå AIK.
- `f1c35762-796c-d837-9f99-e202bc8da7d7` — Unresolved: no event-specific Swedish entries found.
  Organization: Snowboardlandslaget.
- `f2804282-c529-9f38-ad25-1e91014bed66` — Unresolved: current Ruka race start list not found;
  selection alone does not confirm starters. Organization: Längdlandslaget.
- `f53bfb19-cbdc-1732-9b07-76ad19d72eaf` — Rejected: WTGL match between foreign teams; no Swedish
  club or team. Organization: unknown.
- `fa041046-1202-5c3c-8e6c-880ea3b85e79` — Unresolved: FEI event page has no current participant
  entries. Organization: Equestrian.
- `faf4c3df-532a-473e-9c8c-69b6c197c9dd` — Rejected: WTGL match between foreign teams; no Swedish
  club or team. Organization: unknown.
- `fb04718d-3fe4-5f3f-813b-19def0444435` — Processed.
  Activity: `2ebfe0d7-08e9-4ca5-8738-0a66b281f612`; hidden.
  Organization: Fotbollslandslaget (D).
- `fbe9a3ca-73a2-1c3d-93ee-c96d236ca358` — Processed: linked to existing activity
  `c9e229bc-1694-4f6d-8198-30c074936596`; hidden. Organization: Alpina Landslaget.
- `fc3035b4-8588-8d3c-ab59-56cc32f0a5af` — Unresolved: qualifiers continue; the complete
  tournament field is pending. Organization: World Snooker Tour.
- `fd2153ad-fade-0338-aff9-ef61c167a60d` — Unresolved: Swedish event selection is known, but
  race-specific starts are not confirmed. Organization: Skidskyttelandslaget.
- `fda184b8-64e6-0439-aeb4-a41dc60f39c9` — Rejected: Generic NHL studio recap, not competition
  coverage. Organization: unknown.
- `ffa48f81-ce5a-f33c-a4d4-5af26f43c49a` — Processed.
  Activity: `b5f1cfe6-a384-4bbf-b2eb-ebfea3e3219b`; hidden.
  Organization: Ladies European Tour.
