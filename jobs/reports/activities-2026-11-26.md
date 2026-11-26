# Activity processing report — 2026-11-26

## Run

- Started: 2026-11-26 02:51:20 UTC.
- Database: `sesport`, accessed through `bin/dbc.sh`.
- Starting snapshot: 129 visible broadcasts; 0 processed.
- Final outcomes: 25 Rejected, 10 Processed, and 94 Unresolved.
- Database validation matched every snapshot ID to one final outcome; 0 mismatches.
- Step 2 hid only rejected broadcasts. Eleven Unresolved broadcasts received
  organization markers.

## Processed and changed activities

### Kontiolahti biathlon

- Created group `8f6da01c-a35c-4b05-b195-f7c200cc506c`,
  `Världscupen i Kontiolahti`, spanning 26–29 November.
- Created six athlete-specific details for 26 November:
  - `64f16e85-1abe-44ca-9d5d-588992e03598` — Philip Lindkvist Flötten,
    16:20–17:20.
  - `46980c8a-7c4e-452d-9de7-cf607a715b88` — Viktor Brandt, 16:30–17:30.
  - `678a0913-4137-4021-853a-95e856636efb` — Jesper Nelin, 16:31–17:31.
  - `efcddbc2-30e9-4904-a224-1a4ca71f828e` — Melker Nordgren,
    16:33:30–17:33:30.
  - `e5e2a684-7c34-4423-9a99-2d445011d61f` — Sebastian Samuelsson,
    16:44–17:44.
  - `77d43f5d-f9fc-4111-b5f5-5b7eeb40f3e3` — Alfred Eriksson,
    17:08–18:00, capped at broadcast end.
- All six details use `SVT Play, SVT1`; the three confirmed broadcasts link to
  every detail:
  - `4cfa039c-7197-3634-a887-3001c15cb56c`
  - `de2377ca-7408-9d3e-91f8-a955caf5b696`
  - `e660d892-efb1-1635-9ba8-dbdd758f3fe7`
- IBU's complete event start list supplied exact starts. SVT's schedule verified
  16:05–18:00 coverage. The event-specific IBU list includes Alfred, while an
  earlier SSF selection article listed Martin Ponsiluoma; the event list took
  precedence.
- Six participant rows, ages, clubs, profile links, and start times were checked
  on the public card.

### Alpine skiing

- Reused Killington group `66a17ceb-f833-4f47-a9fe-0fde00e6e3ae` and Copper
  Mountain group `c174bfb1-cf3c-4306-8dd4-6abc0b668656`.
- Reused the four existing 29 November activities; each broadcast maps to one
  matching segment:
  - `4a0b3d71-f1f1-4e3e-a036-e276c8a4017b` ->
    `56e23a7b-bffb-4888-ac37-57fcf60545ee`, women, run 1, 16:20–17:50.
  - `7ff670e3-7cbe-343d-8932-db054a53f2b4` ->
    `442d50cc-c9ed-4805-96b6-1c9040cbff28`, men, run 1, 17:50–19:20.
  - `a4cb5080-720f-d839-912e-f0bd53f30c76` ->
    `9361a088-7925-41eb-8f60-2d87efb1c0e2`, women, run 2, 19:20–20:30.
  - `da7aab88-f65d-8b38-acf4-a7ec0bf2c7c2` ->
    `c9e229bc-1694-4f6d-8198-30c074936596`, men, run 2, 20:50–22:00.
- TV.nu verified the segment schedule. Existing event-specific Viaplay links and
  participant profiles were verified and retained. No activities or groups were
  created.

### SailGP Abu Dhabi

- Reused group `2dd00e41-1ee5-4b71-a2c1-bf5204688b6e`.
- `0ea20634-396d-8d32-a32e-d63e62c01719` links to
  `d6e256d8-bd2e-450e-94eb-fc019fb7f1a9`, 28 November, 11:00–13:00.
- `17f8fbbe-f194-d131-bf9a-3de8ffdd92df` links to
  `9ce98d1f-66f2-4c47-b210-6e1e9b796d56`, 29 November, 11:00–13:00.
- Official SailGP crew sources confirm Felicia Fernström and Julius Hallström.
  Event-specific Viaplay links were verified and retained.

### Players Championship Finals darts

- Processed broadcast `f74e81d0-dc01-ba34-bc1e-d2aebd5fc989` to existing
  activity `62f624b8-1235-4cce-93e4-a03d416d86cc` in group
  `74f230f5-915b-4a86-a727-28b43666dc65`.
- The V Sport Extra segment remains 27 November, 21:45–00:00 Stockholm time.
  It reuses broadcast `214157f0-abb7-8036-8ffc-dc2b405ad66b`; the separate
  Viaplay 20:00–00:30 session remains unchanged.
- TV.nu verifies the V Sport Extra time. The PDC draw confirms Jeffrey de Graaf;
  his displayed age, SSDC club, and dedicated Target Darts profile were checked.
- V Sport Extra resolves through the existing Viaplay channel catalog. No direct
  event-specific V Sport Extra link was available, and no incorrect stream URL
  was added.

### LET roster and dashboard correction

- Dashboard review found missing participant start times on activity
  `5c627961-07ee-40fc-89b1-a172e7344bdf`.
- Reused LET group `021c6593-6c4c-46ba-b74b-71fcb24451c6`; all eight
  round activities remain in that group.
- The official LET entry list and event report confirm five Swedish entrants and
  a no-cut, four-round event. Corinne Viden was missing from all eight existing
  activities, so she was added as an active participant with Ladies European
  Tour context:
  - 26 Nov V Sport Golf: activity `5c627961-07ee-40fc-89b1-a172e7344bdf`
    <- broadcasts `097c8dd6-5181-d235-9295-a8a128e7d274`,
    `eeed8f6c-c533-a933-9221-4aa1055af1f2`.
  - 26 Nov Viaplay: activity `7ea3ad9b-e34e-4520-b8d0-46c176f22760`
    <- broadcast `07c3c8b5-7d56-7a3b-b9b9-bbc45954de3d`.
  - 27 Nov V Sport Golf: activity `430cde15-a950-40e7-a372-c7d8db5b71e0`
    <- broadcasts `708e62ff-75d9-c437-951e-5b1a9c48da7b`,
    `8b1a792c-9e67-4f3e-88a4-00104fee580f`.
  - 27 Nov Viaplay: activity `b5f1cfe6-a384-4bbf-b2eb-ebfea3e3219b`
    <- broadcast `ffa48f81-ce5a-f33c-a4d4-5af26f43c49a`.
  - 28 Nov V Sport Golf: activity `687c3dfb-cb00-4f0c-bfd3-dc9a8a8b8cd5`
    <- broadcasts `89edc1e7-3291-5433-b36f-3f414c109d60`,
    `d32853c0-d995-9e3e-ba6a-95174a550995`.
  - 28 Nov Viaplay: activity `da06247a-02ba-455c-801f-af4f74e2fe55`
    <- broadcast `c6f187e4-0c88-d035-b811-5069f736df0f`.
  - 29 Nov V Sport Golf: activity `954a4ecb-5d22-4b00-b21b-be15b1988ee6`
    <- broadcasts `caa0daf2-bbde-3831-81dd-6b41e80aed08`,
    `df47b9dd-0455-e83e-ac3c-aeed01bd1668`.
  - 29 Nov Viaplay: activity `fc0989db-63da-458f-a1e0-837ee12c50e2`
    <- broadcast `2bea250c-e609-7333-977c-5a0d23b6b3ef`.
- Official LET Round 1 tee times were stored for all five: Lisa 09:30, Louise
  09:41, Moa 10:03, Kajsa 11:09, Corinne 11:20. Round 1 evidence and the group
  participation excerpt were updated.
- Corinne's person-specific profile confirms her Swedish background and Hulta
  formative club; her current Delsjö affiliation is stored. The public card shows
  her age, club, profile link, and 11:20 start.
- After the correction, the authenticated dashboard showed no upcoming activity
  issues. The participant changes created no new cards or broadcast links.

### Supporting biathlon entities

- Created club I2 Idrottsförening Karlstad
  (`0590fbbe-95a5-4b19-833a-6e39a28109ef`) and Dala-Järna IK
  (`29463b72-706d-4a90-b43c-7250eb2ece90`), Team NIBE
  (`7e3111cb-3725-490b-adee-9c2579fc20b4`), and persons Melker Nordgren
  (`03519f5e-4165-4817-ab42-b9a9506b175f`) and Alfred Eriksson
  (`45dc310a-759a-41bc-8c54-c6e8b1b93d51`). The six biathlete records now
  have sourced club/team links, birthdate data, and dedicated profiles.
- The two new birthdates use January 1 placeholders because sources provide only
  the year. Existing person profile and fact corrections were supported by IBU,
  SSF, and personal or federation sources.

## Verification

- Public schedule pages were retrieved from `https://sesport.se` after changes.
- Before and after card counts:

| Public date | Before | After |
| --- | ---: | ---: |
| 2026-11-26 | 3 | 4 |
| 2026-11-27 | 7 | 7 |
| 2026-11-28 | 8 | 8 |
| 2026-11-29 | 6 | 6 |

- The new 26 November card is the Kontiolahti biathlon group. Darts, alpine,
  SailGP, and LET changes reuse existing cards.
- Checked schedule rows, event grouping, titles, channels, streams, people, ages,
  clubs, participant profile links, and start-time source links on the hosted pages.
- The Darts card shows its exact V Sport Extra segment; V Sport Extra uses the
  catalog fallback. LET pages show all five Swedes and the Round 1 tee times.
- No remaining dashboard activity issues, provider failures, or public display
  defects were found.
- No code, assets, schema, configuration, or deployment was changed. No tests
  were run; this data-only job does not call for automated tests.

## Sources

- IBU official event start list and athlete profiles; SVT and TV.nu schedules.
- Official SSF selection, team, and club pages; TV.nu and Viaplay alpine schedules.
- SailGP official crew list and event calendar; TV.nu; Viaplay event streams.
- PDC official schedule and draw; TV.nu; Viaplay event streams and channel catalog.
- LET official entries and Round 1 tee sheet; RFEG event report; Corinne Viden's
  personal profile.

## Final outcomes

- Rejected broadcasts were hidden by changing only hidden_at.
- Eleven unresolved broadcasts received organization markers.
- Final totals: 25 Rejected, 10 Processed, and 94 Unresolved.

- `4cfa039c-7197-3634-a887-3001c15cb56c` — Processed: linked to six Kontiolahti details in group
  `8f6da01c-a35c-4b05-b195-f7c200cc506c`.
- `de2377ca-7408-9d3e-91f8-a955caf5b696` — Processed: linked to six Kontiolahti details in group
  `8f6da01c-a35c-4b05-b195-f7c200cc506c`.
- `e660d892-efb1-1635-9ba8-dbdd758f3fe7` — Processed: linked to six Kontiolahti details in group
  `8f6da01c-a35c-4b05-b195-f7c200cc506c`.
- `bdf08ca9-5698-273a-9cd4-dbffeb5c529d` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `0f732f37-6353-3239-a327-db81606ae14b` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `154a2b44-3801-503e-ac88-17682e0ee423` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `2be808ea-54c6-8c31-a12c-43c80dcd4bb9` — Unresolved: no complete Ruka entries or
  Swedish starters found. Organization:
  unknown.
- `afb35c51-b9f6-4732-9abb-bf668e632b58` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `9676f82c-fa14-f23e-a067-318900df1285` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `f2804282-c529-9f38-ad25-1e91014bed66` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `20db0379-c4e6-8535-9a69-80efe96e3e8d` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization: unknown.
- `1651bfe6-6891-df3a-9dab-05a35d7c58dc` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `9eaa3423-1f89-4a33-a6f4-8a21906433d3` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `d77dc415-822d-a93c-9679-e89033c03b38` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `8dbd3ca4-935f-e338-ab23-cd8ded7a369e` — Unresolved: no current event entry list
  confirms Swedish starters. Organization:
  Backhoppningslandslaget
- `42bf7787-6e62-7439-a9a3-22f20cd7cda4` — Unresolved: no event-specific Swedish
  cyclocross entries found. Organization:
  Cycling
- `ee8235e1-bea4-963c-ac83-6ce7f3f28cf4` — Unresolved: no event-specific Swedish
  cyclocross entries found. Organization:
  Cycling
- `0afcff59-7920-3a32-9bba-158332ec6fe7` — Unresolved: no event-specific Swedish
  freeski entries found. Organization:
  Freeskilandslaget
- `ec9f21cd-af6b-e53d-b78b-d2a510178bf2` — Rejected: Domestic Swedish basketball
  league fixture.
- `801f42a5-4b24-ab30-b4d4-02fdcbdd8da1` — Unresolved: no event-specific Swedish
  cyclocross entries found. Organization:
  Cycling
- `f74e81d0-dc01-ba34-bc1e-d2aebd5fc989` — Processed: linked to existing activity
  `62f624b8-1235-4cce-93e4-a03d416d86cc` in group
  `74f230f5-915b-4a86-a727-28b43666dc65`.
- `c39a41cb-8a6c-3631-867e-7e4b8accab7d` — Unresolved: no event-specific Swedish
  entries found. Organization:
  Snowboardlandslaget
- `4be61b67-b053-1b3a-b50f-33cb741b15ce` — Unresolved: no current ISU entries
  confirm Swedish starters. Organization:
  International Skating Union
- `eedd45a1-0de8-5f3b-8382-125666604027` — Unresolved: no current ISU entries
  confirm Swedish starters. Organization:
  International Skating Union
- `97f142c3-3651-b530-b6fe-fe467226506d` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `03c50c6c-ea76-8e30-99d8-b8762542cef5` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `17b3a92c-adc1-003b-917e-4efdf7c273f1` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `08c09916-e373-be35-a5aa-63bb3a426985` — Unresolved: no complete Ruka entries or
  Swedish starters found. Organization:
  unknown.
- `286beb50-48c3-4138-942a-9d8410d1f510` — Unresolved: no current event entry list
  confirms Swedish starters. Organization:
  Backhoppningslandslaget
- `0ea20634-396d-8d32-a32e-d63e62c01719` — Processed: linked to existing activity
  `d6e256d8-bd2e-450e-94eb-fc019fb7f1a9` in group `2dd00e41-1ee5-4b71-a2c1-bf5204688b6e`.
- `44336c62-26fd-713d-8615-840a6c1e3a91` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `abbd32af-8210-943a-8681-c0173a1c17a9` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `de7ac88b-1af5-b43b-abf7-852767f232f8` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `15f07892-e7e4-5835-9260-002d96aa6dd4` — Rejected: Domestic Swedish football
  promotion playoff.
- `0a9a6352-90b5-0539-9d2e-bacf879b4f19` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `d784cb68-9114-7c3f-ae06-66997b1a16f3` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `e9f5eb6f-46ab-c334-8045-535f6fe90865` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `6aa7cca9-24dd-ae3a-a13c-faf183349c71` — Unresolved: no current event entry list
  confirms Swedish starters. Organization:
  Backhoppningslandslaget
- `61bcee52-2ccf-8238-a9f2-c6436796057f` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `c4751576-aa70-7034-8d35-b0d54a4daf93` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `71ac1a5f-47c6-c731-856c-dbab94d75b9d` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `d40b7c46-8392-0634-bfea-1a5dc7f28681` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization: unknown.
- `09f8f581-4cae-d635-b57b-7706ab0951e5` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `4c1039bd-ab7c-a737-a90c-05b244d5290c` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `aff8fb37-0b29-703b-a0c8-18c0307e828b` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `0cd53d4c-f8b5-fa3c-84e5-dccc2370fbdf` — Unresolved: no current event entry list
  confirms Swedish starters. Organization:
  Backhoppningslandslaget
- `da762a05-41b3-a73c-bd82-b2dac09df13c` — Unresolved: Swedish snowboard prospect
  named, but no event entry list.
  Organization: Snowboardlandslaget
- `6aa4bf59-4f93-1838-94f9-dbd63144739c` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `aceffee3-6991-0d37-a699-707e38e1a944` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `d73b9d55-31bc-5f38-b60a-9c5b2acb6020` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `fc3035b4-8588-8d3c-ab59-56cc32f0a5af` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `c86d522c-729a-c633-afd7-03844dafc00f` — Unresolved: no Sweden in other published
  disciplines; current women’s entries
  remain unverified. Organization:
  International Skating Union
- `f1c35762-796c-d837-9f99-e202bc8da7d7` — Unresolved: no event-specific Swedish
  entries found. Organization:
  Snowboardlandslaget
- `c3e38385-3e60-2837-95fa-70009b7625ce` — Unresolved: no current ISU entries
  confirm Swedish starters. Organization:
  International Skating Union
- `44abdce8-1313-a93d-9fd5-80df835c6cd0` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization: unknown.
- `6d44bb06-5e88-7e30-bc43-ea77dc00069c` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `b317cdf1-54e2-7135-9fd5-0f2aff16be80` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `c03dbfd1-e227-2b31-af03-d721efbf4f5c` — Unresolved: Ruka men 20 km: event entry
  list not published. Organization:
  Längdlandslaget
- `4759e730-a67f-ae30-baee-babeef617d7a` — Unresolved: no complete Ruka entries or
  Swedish starters found. Organization:
  unknown.
- `17f8fbbe-f194-d131-bf9a-3de8ffdd92df` — Processed: linked to existing activity
  `9ce98d1f-66f2-4c47-b210-6e1e9b796d56` in group `2dd00e41-1ee5-4b71-a2c1-bf5204688b6e`.
- `8c1c622c-b978-a531-bb74-7ccfed9bb26f` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `b14b67c0-2c2a-b830-be93-6d42bfe2d235` — Unresolved: Ruka women 20 km: event entry
  list not published. Organization:
  Längdlandslaget
- `d87b3215-4b68-c539-b442-ab7448b49cab` — Unresolved: current Ruka race start list
  not found; selection alone does not
  confirm starters. Organization:
  Längdlandslaget
- `7eeb4534-c077-943c-8c0a-dd8eabded5d7` — Unresolved: no current event entry list
  confirms Swedish starters. Organization:
  Backhoppningslandslaget
- `bdaf088a-8222-4536-abed-a73ac7895b5a` — Unresolved: no event-specific Swedish
  cyclocross entries found. Organization:
  Cycling
- `3a0fcacb-ecf5-3336-b98a-c867f6722c8e` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `45911c4a-5bf0-9338-b4a1-664dc547faa1` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `fd2153ad-fade-0338-aff9-ef61c167a60d` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `092c9061-7e31-2a3e-8693-4e044c402382` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `53359864-0075-4839-98d0-43b0446b3f3a` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `c2b94cd5-8121-d23a-8639-a096cabb89f2` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `e6cd80d7-e8be-7031-82ab-48fe2551e0c9` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `14758203-2174-e93b-b4eb-272b190a9536` — Unresolved: no current event entry list
  confirms Swedish starters. Organization:
  unknown.
- `78ae8f65-6c1e-ba39-a772-75c9257339f9` — Unresolved: no event-specific Swedish
  cyclocross entries found. Organization:
  Cycling
- `33beb555-d90e-fc36-aa84-2f737e94d403` — Rejected: Domestic Swedish Allsvenskan
  football fixture.
- `7394730f-7a40-c237-b207-78a9f6a435db` — Unresolved: FEI event page has no current
  participant entries. Organization:
  Equestrian
- `75ca2fd4-e18c-ee3b-ab80-912fdff209a6` — Rejected: Domestic Swedish Allsvenskan
  football fixture.
- `7a192f0e-ce93-273f-a7e5-b6816ede5203` — Rejected: Domestic Swedish Allsvenskan
  football fixture.
- `d0c701ca-f1a4-a338-899d-50eb311e754c` — Rejected: Domestic Swedish Allsvenskan
  football fixture.
- `bc4fb00d-dde9-623e-bda8-4754932764ae` — Unresolved: FEI event page has no current
  participant entries. Organization:
  Equestrian
- `fa041046-1202-5c3c-8e6c-880ea3b85e79` — Unresolved: FEI event page has no current
  participant entries. Organization:
  Equestrian
- `4dbe20cb-d2a3-ba33-bc83-7bf3ac0af1c5` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `da0f740a-ecdf-7938-be20-594daa5eb55a` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `edbceb19-6c09-5a35-bfb2-d75366b0ce08` — Unresolved: Swedish event selection is
  known, but race-specific starts are not
  confirmed. Organization:
  Skidskyttelandslaget
- `4a0b3d71-f1f1-4e3e-a036-e276c8a4017b` — Processed: linked to activity
  `56e23a7b-bffb-4888-ac37-57fcf60545ee` in group `66a17ceb-f833-4f47-a9fe-0fde00e6e3ae`.
- `7ff670e3-7cbe-343d-8932-db054a53f2b4` — Processed: linked to activity
  `442d50cc-c9ed-4805-96b6-1c9040cbff28` in group `c174bfb1-cf3c-4306-8dd4-6abc0b668656`.
- `a914f930-32ac-2e38-adfe-7ae02416ef5d` — Rejected: Foreign WTGL fixture between
  foreign teams.
- `a4cb5080-720f-d839-912e-f0bd53f30c76` — Processed: linked to activity
  `9361a088-7925-41eb-8f60-2d87efb1c0e2` in group `66a17ceb-f833-4f47-a9fe-0fde00e6e3ae`.
- `2c889033-91ef-ad30-a95a-511d40be7ce9` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `8c3d17f0-9cf8-5338-9ebf-52d011d0ed20` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `b6b2ed74-b5d5-f53d-ab08-7dd15a297a41` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `c6d0484b-05a0-c130-9adb-044d74d39244` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `da7aab88-f65d-8b38-acf4-a7ec0bf2c7c2` — Processed: linked to activity
  `c9e229bc-1694-4f6d-8198-30c074936596` in group `c174bfb1-cf3c-4306-8dd4-6abc0b668656`.
- `e932e171-682f-4c31-a308-3bad73e2cb88` — Rejected: Foreign WTGL fixture between
  foreign teams.
- `a72b9d65-5f58-083f-aee1-536e63780201` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `db32e221-08f2-e73a-b961-0261294f5aa3` — Unresolved: draw programme; no complete
  Swedish tournament entry list.
  Organization: PDC / WDF
- `79446276-0f16-5130-bbc5-c2746e165d29` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `700d1933-d753-fd31-ad6e-8858a1f8a550` — Unresolved: Swedish championship squad is
  known, but final-session entries are
  pending. Organization: Swimming
- `e9c916cd-76d6-7635-9bc2-ee6d4f398ec6` — Unresolved: Swedish championship squad is
  known, but final-session entries are
  pending. Organization: Swimming
- `8d0fc3dc-581f-7a3b-8cba-cf2e5e60f0c1` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `72473098-c1e2-f33b-b488-15492a0e78a2` — Unresolved: Swedish club is named; match
  roster is not yet confirmed.
  Organization: Frölunda HC
- `db7347a5-1646-d23a-84c5-3d2ff15a27db` — Rejected: Generic Champions Hockey League
  overview, not match coverage.
- `f19bdd1b-3623-a534-953b-ab0fdda83976` — Unresolved: Swedish club is named; match
  roster is not yet confirmed.
  Organization: Skellefteå AIK
- `7b5ba390-b17c-bb3c-8963-f0c9d86b6411` — Unresolved: qualifiers continue; the
  complete tournament field is pending.
  Organization: World Snooker Tour
- `79de7a2a-5ae6-6234-865c-5f91c5e3b1cb` — Rejected: Generic Viaplay Vinter studio
  programme.
- `9cd88a30-43b9-643f-b7a3-6b02183b1182` — Rejected: Generic fan discussion programme.
- `6b336fc0-c818-b33a-b2f1-04ac282b1468` — Unresolved: Swedish team selected;
  session entries pending. Organization:
  Swimming
- `a84aa24d-4419-e63a-9481-7954921652ca` — Unresolved: Swedish team selected;
  session entries pending. Organization:
  Swimming
- `63ce1b0b-9f77-d43f-85e7-1aacbe532dd5` — Rejected: Generic Swedish hockey studio
  programme.
- `8615aaa8-9cdb-6531-a753-b339f60e6f69` — Rejected: Generic NHL studio recap
  programme.
- `8e56209b-d02b-0333-b77f-5281e25decd9` — Rejected: Foreign Danish football fixture
  with no Swedish team.
- `330a427a-6f6c-b934-8581-74dc0882774a` — Unresolved: UK Championship qualifiers
  continue; complete field pending.
  Organization: World Snooker Tour
- `44ce0748-ffbb-5d3d-a024-1e50fa3c4ca5` — Rejected: Domestic Finnish Liiga hockey
  fixture.
- `4f4543fc-26f6-2f3e-8000-8dbd1338df26` — Rejected: Domestic Finnish Liiga hockey
  fixture.
- `60b82c40-3cf1-be36-96c7-9c472c7ab6ef` — Rejected: Domestic Finnish Liiga hockey
  fixture.
- `7ca9195b-9303-5b34-acc2-47d477ed11ec` — Rejected: Domestic Finnish Liiga hockey
  fixture.
- `bbce2cb6-8489-d232-8895-bd7d65187007` — Rejected: Generic Premier League studio
  programme.
- `eccb7610-1a6a-f13f-be34-b5f0f1482c16` — Rejected: Foreign Danish cup football
  fixture.
- `4b15b71a-6d70-7d30-88a3-17878ee77cda` — Rejected: Domestic Finnish Liiga hockey
  fixture.
- `8071985b-8bc6-b133-8208-20ccf7975477` — Rejected: Domestic Swedish basketball
  league fixture.
- `0e257082-af69-4a33-a0c0-5e5f14727bc7` — Rejected: Foreign club hockey fixture with
  no Swedish club.
- `88131964-2261-4d30-a593-6d81be8ab8f9` — Unresolved: UK Championship qualifiers
  continue; complete field pending.
  Organization: World Snooker Tour
- `910561fd-4878-c430-9e7f-f945dcec87e2` — Rejected: Awards programme, not competition
  coverage.
- `1a99ff20-6a22-2a30-b98a-21f71214aa92` — Rejected: Foreign Italian cup football
  fixture.
- `04fa35d3-62b5-5c3e-aa28-5f02d6df5fa4` — Unresolved: No complete 2026 field
  confirms Swedish starters. Organization:
  DP World Tour
- `1aedbef2-b6ae-e530-bcef-001fc56c8ebe` — Unresolved: Description names Gary Player
  CC, not the 2026 venue. Organization: DP
  World Tour
- `0bee6724-6134-dc38-ae98-7ef63fae5e67` — Unresolved: Description names Royal
  Melbourne, not the 2026 venue.
  Organization: DP World Tour
- `7241f139-e8c2-aa32-8d26-e800d172b505` — Unresolved: No complete 2026 field
  confirms Swedish starters. Organization:
  DP World Tour
- `07afe8ea-8411-f53b-86f1-412f1e8959b8` — Unresolved: Description names Royal
  Melbourne, not the 2026 venue.
  Organization: DP World Tour
