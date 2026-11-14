# Activity run report — 2026-11-14

## Run status

- Started 2026-11-14 04:24:47 UTC (05:24:47 in Stockholm).
- Broadcast snapshot: 2026-11-14 04:25:46.425931 UTC.
- The snapshot held 177 visible, unprocessed broadcast IDs.
- Step 3 classified 110 as rejected, 18 as selected, and 49 as unresolved.
- Step 3 set organization markers on 25 visible rows. Three were GCL semi-final
  broadcasts later processed; 22 belong to the unresolved list below.
- Step 4–6 completed seven event groups and changed 16 activities: 14 created and
  two existing activities updated.
- Final database read-back at 2026-11-14 06:25:21.096984 UTC found 141 of the 177 IDs.
- All 18 processed broadcasts remain linked, processed, and hidden. Of 110 rejected
  IDs, 75 remain present as hidden and unprocessed; 35 are now absent.
- Of 49 unresolved IDs, 48 remain visible and unprocessed; one is now absent.
- A dagger (†) marks a snapshot ID absent at final read-back. Its original Step 3
  classification is retained in the register. The disappearance cause is unknown.
- No broadcast deletion code or database trigger was found. The latest recorded
  broadcast import preceded the snapshot. The absent rows were not recreated.

## Public card counts

Counts were recorded once per date on sesport.se. Grouped cards count once.

- 14 Nov: 10 before, 12 after (+2).
- 15 Nov: 8 before, 10 after (+2).
- 19 Nov: 2 before, 2 after (no change).
- 20 Nov: 0 before, 3 after (+3).

The 14 Nov increase is the Asian Le Mans and Charles Schwab cards. The 15 Nov
increase is the Hockey-VM semi-final and Charles Schwab final-round cards. Austin
segments use its existing group card. On 19 Nov, the existing RSM and GCL cards
gained schedule entries. On 20 Nov, cards appeared for RSM Round 2, Finlandia
Trophy, and the GCL semi-final date.

## Created and changed activities

### Asian Le Mans Series

- Group: `8f6ed439-ffd0-4985-9b0e-f6f88e5a1d15` (existing).
- Created published activity `7d8e716a-082c-5b23-a8a4-67e1c1eb5c05`:
  `Lopp 1`, 14 Nov 12:50–17:45 CET, HBO Max, August Raber.
- Linked and processed broadcast `deedaa01-f513-9c33-a6b9-75d2cd409a0a`.
- Official Circuit Paul Ricard schedule and TV.nu evidence were saved on the activity.
- Added official FP1 ParticipationEvidence source
  `2662d461-d296-4195-9399-67d9f9371ca1` for August Raber in car 30.

### Austin Championship

- Group: `43fd72e5-8fa4-49ee-97c5-9f2be6e81324` (existing).
- Created Round 3 activity `efca9190-6cf7-516c-9336-54077b81f72e`,
  14 Nov 19:40–23:00 CET, HBO Max. Jesper Svensson and Pontus Nyholm are active.
- Linked and processed broadcast `e95de169-030f-4937-a560-09f156d4123d`.
- Created Round 4 activity `7d5c81ef-839a-5cec-a34e-0c1df75da43b`,
  15 Nov 19:40–23:00 CET, HBO Max, with the same two active players.
- Linked and processed broadcast `cf06fbf8-6a25-1033-93fd-5077edc0eeda`.
- Official schedule, PGA Tour field, TV.nu, and provider links were read back.

### Charles Schwab Cup Championship

- New group: `43b9b4ad-504c-5dbc-9d3e-c72e2474c7c7`.
- Created Round 3 `8d7ee703-cdbe-5df9-b4bb-6ec3b2446499`,
  14 Nov 22:00–15 Nov 01:00 CET, HBO Max; Freddie Jacobson is active.
- Linked and processed broadcast `d409b898-7f8a-b73b-9c34-b488ba557b2b`.
- Created Round 4 `23d9459a-285b-55a8-8871-b23af77617f7`,
  15 Nov 22:00–16 Nov 01:00 CET, HBO Max, with Freddie active.
- Linked and processed broadcast `3a6afb70-8d76-803a-aeb0-74c3cd8528d2`.
- PGA Tour event, field, and TV.nu sources were saved and read back.

### Women’s Hockey-VM semi-final

- Existing group: `23397ce8-0521-4014-a150-02927368a51b`.
- Created Match activity `a266a167-b092-519d-a8a7-fb3142c3d6aa`,
  `Sverige – Kanada`, 15 Nov 19:00–21:30 CET.
- Channels: SVT Play and Kunskapskanalen. All 23 active players on Sweden’s roster
  are linked. IIHF pairing and roster sources were saved.
- Linked and processed broadcast IDs:
  `9986469d-9dee-d33b-b3ec-e04c17bc90e4`,
  `f3840800-f4ca-1a38-b6d5-ec7f0849cc3c`,
  `a41d4795-75ad-c234-a53e-75e4456b595a`,
  `c9103837-06b3-2835-ae7b-68506110b688`,
  `cb72e420-9839-0631-b776-c43c2f6da9f6`,
  `d663b80c-5c25-9e3c-a018-938198553550`.

### GCL Super Cup

- Existing group: `3b95bcbf-35f0-4356-a82d-7ab435e9afd1`.
- Created HBO Max quarter-final `3b15e8e9-ccb1-572c-b2c6-3b96481f5c54`,
  19 Nov 20:00–22:00. Angelica Augustsson Zanotelli and Peder Fredricson are active.
- Its processed broadcast is `45b3087d-6421-d93b-93f6-233f676fdff6`.
- Updated existing TV4 Play quarter-final `03e8f2bf-0983-46b0-85f2-463e9704b114`
  by adding Peder. Existing processed broadcast `6d74ab41-5f21-4b3f-a639-59f3c11ff314`
  remains linked.
- Created the 20 Nov semi-final activities:
  HBO Max `fcc4226b-1282-5496-814e-3a5a34232f25`, 20:00–22:00;
  Eurosport 1 `0a5c38b6-5ae1-5a24-bd09-e03fba2f66de`, 20:00–21:00;
  TV4 Play `0b9b0c06-55d7-5a95-9cd7-6afe5cf6cf54`, 20:20–22:15.
- Henrik von Eckermann is active on all three semi-final activities. The processed
  broadcasts are `8945ab08-95cf-7f3e-a4aa-e40ac4dfe7c0`,
  `eeb6a652-2a41-4634-898b-bae5444aacc3`, and
  `895d6881-4d57-ee31-91a6-df1e271ba802`.
- The FEI schedule, official GCL team standings, TV.nu sources, and stream links
  were read back. Eurosport 1 resolves through the existing Max sports catalog link.

### The RSM Classic

- Existing group: `4ea72896-3012-4a47-b62c-5c0803271ebc`.
- Updated existing Round 1 activity `a6c812fe-429f-4887-8370-8e972087ddf0`
  to add Jesper Svensson and Pontus Nyholm; Ludvig Åberg remains linked.
- Its two existing processed broadcast links remain
  `68383578-ab36-d038-a2a1-5adb86233dd5` and
  `f3eb5aeb-166e-d931-9e23-93b5250e7aa2`.
- Created Round 1 activity `fc7f38fe-53b4-58b3-93d2-8c90a4429440`,
  19 Nov 14:30–22:00 CET, HBO Max. Linked broadcast
  `5c6eba58-a65f-8934-93c8-14107b8f4d24` was processed and hidden.
- Created Round 2 activity `e0db5bf5-b43e-5dc4-becb-3b5a7a3d8011`,
  20 Nov 14:30–22:00 CET, HBO Max. Linked broadcast
  `d8f7b94c-27e6-bf39-a042-563f7a8e6d62` was processed and hidden.
- The official field confirms Ludvig Åberg, Pontus Nyholm, and Jesper Svensson.
  All three are active on both rounds.

### Finlandia Trophy

- New group: `d11ff899-b783-5e8a-bd5b-08e989b1d070`, spanning 20–21 Nov.
- Created `0bcf58dc-770f-5366-98e7-a7fe0ec63238`, `Herrarnas kortprogram`,
  20 Nov 16:25–17:55 CET, with Andreas Nordebäck.
- Created `cb60400a-ee21-574e-8f76-9dac5c7a45d8`, `Isdansens rytmdans`,
  20 Nov 18:20–19:40 CET, with Milla Ruud Reitan and Nikolaj Majorov.
- Both activities link the same broadcast
  `47fd8a06-9f25-9a3b-a9bd-343ec6ad28a0`, processed and hidden.
- Official ISU entries, skater profiles, Team Sweden, Norrköpings KK, organizer schedule,
  TV.nu, and Viaplay sources were read back.

## Person and organization data

- Created Milla Ruud Reitan, entity
  `50913fb8-f195-50d4-ba2b-6b2b8739fce6`, born 2005-09-27, female, tier_1.
- Created Nikolaj Majorov, entity
  `0c8015a6-693a-5d83-90f9-11a9dad2222c`, born 2000-08-18, male, tier_1.
- Both new people have dedicated ISU profiles and links to Norrköpings
  Konståkningsklubb, entity `7269e73d-686d-564a-8986-f5c4238c7d8a`,
  and Team Sweden Konståkning.
- Updated Team Sweden Konståkning entity
  `4a60fe43-cb43-35f3-c906-f03c9021f1a7` with its current team page URL.
- Public cards show names, ages, formative clubs, profile links, schedules, channels,
  and stream links for the changed activities.

## Dashboard review

- Authenticated production review of the Admin Dashboard showed four
  “Missing participant start times” issues dated 14 Nov.
- The listed activity IDs are
  `34e93928-5c5d-4eef-884d-6e55743df860`,
  `43fd72e5-8fa4-49ee-97c5-9f2be6e81325`,
  `7813a903-1c70-4587-abec-4c2be20cb584`, and new Charles Schwab Round 3
  `8d7ee703-cdbe-5df9-b4bb-6ec3b2446499`.
- The official event schedule only gives a general 10:30–13:00 local tee-time window.
  The PGA Tour media page has no Round 3 tee-time sheet. No sourced Freddie Jacobson
  start time was available, so this warning remains unresolved.

## Validation

- Re-fetched production pages for 14, 15, 19, and 20 Nov; all returned HTTP 200.
- Final counts, event names, participants, ages, clubs, profile links, schedules,
  channels, and available streams were checked on sesport.se.
- Five existing profile URLs returned 403 or timed out to the HTTP client. Four
  opened in browser checks; the Peder Fredricson FEI profile timed out. Public
  participant anchors match the stored profile URLs.
- The public GCL semi-final lists the Eurosport 1 Max sports catalog fallback link.
- No automated tests or build were run; this run changed application data and the
  final report only.

## Broadcast outcome register

The snapshot contained 177 unique IDs. Each ID appears once in the outcome lists below.
Final audit: 18 processed rows; 75 rejected present, 35 rejected absent; 48 unresolved
present, and one unresolved absent.

## Organization markers added to unresolved IDs

The organization entity was newly set for these 22 unresolved broadcasts. A plus sign in
the unresolved list marks these IDs.

**Olympic Games** (1):
- `a7695312-2d4d-3235-9d8b-fa1acc8f248b`+

**T100 Triathlon World Tour** (1):
- `9c59b7a6-ef77-2530-bfd2-feb0a8bbc0ed`+

**Cycling** (2):
- `02fc4e72-e372-483f-9ba4-9a7c81065a55`+, `529006a8-ae46-7c36-bd71-2753914923e7`+

**PDC / WDF** (6):
- `b70161c9-bd8c-2331-8882-135df3706ba8`+, `2b1fd3c0-dc5d-d734-9a32-ef4ddc99e844`+
- `813bbef8-b5fe-ad31-a5be-d36d3633a188`+, `023e29c6-9ea2-1c35-9927-5d86a0f04558`+
- `0f4a2b1f-ad91-b331-ac56-09844ae62a39`+, `81b99e6d-d495-4532-9102-9cdf3b435505`+

**Ishockeylandslaget (D)** (1):
- `5144d5c3-0e60-b331-b589-2184aa954ab4`+

**World Snooker Tour** (3):
- `ba72504b-e03e-163f-aa41-804c81e1f65a`+, `97cabbc6-02a8-5031-a814-8618ae7b44c1`+
- `90289d77-1f26-4f34-8d25-249450e9c973`+

**International Skating Union** (1):
- `887e630d-67e3-5a39-ac6f-b3f47715796a`+

**Ferrari Challenge Europe** (2):
- `8e169384-58c1-4e3c-9fe2-f23251075635`+, `585ee2bd-28b9-e535-a672-2eee9246d420`+

**Ferrari Challenge North America** (1):
- `79234899-2f39-cd36-99b9-2907c03faa17`+

**Global Champions Tour** (1):
- `e0cf7cfc-ebab-6c32-b9d2-91c4ea031526`+

**PGA Tour** (1):
- `e2e048f3-fe79-e53c-a4af-5d5be908bba8`+

**LPGA Tour** (2):
- `2612f39f-8343-f435-a6f9-90662175d28e`+, `64ec9770-3805-6238-9ada-1f15eab63ee5`+

## Rejected broadcasts (110)

Each ID was hidden at the Step 3 read-back. † marks an ID absent at final audit.

#### Standalone studio, preview or press programme. (6)
- `3394ce50-235a-da38-bfc7-e434e9ee46db`†, `a7c6e011-2aa5-9136-9bc1-741556df081c`†
- `6a33b562-4417-8930-8f3b-4d2e6d6a74ef`, `597b9b88-294e-4039-ba3e-a9c5bf178236`
- `25efda09-58ce-6b3d-ab2e-a3ed949089df`, `219d68bc-79ef-773d-a036-eeed8cd7f085`

#### Dakar beach handball event; the official SOK roster has no Swedish team. (1)
- `eb2d51d2-b126-cd36-91e3-d475159602e5`†

#### Domestic Stockholm Open. (4)
- `6ee75dc1-89d1-ee39-8de6-be51c18ff0b7`†, `eaeb4def-07f4-c630-87a4-f944a9ce32ee`†
- `61600259-b6d2-d737-aa6f-eb21b8058f9a`, `80d8323b-bfc0-7636-88cd-f488bf698977`

#### Unidentified event with placeholder or generic title. (5)
- `328e5a26-48f3-2e3d-b167-08fa83e0948d`†, `d6d941d4-c5d4-d73c-b978-19a44c8258d1`†
- `2a1bc77b-196b-e63f-9929-3e3c80544068`†, `612f1134-15d3-0d37-8ff1-50cd93b357fc`
- `e4c9d644-57e6-c831-a685-adcb8050b414`

#### IIHF quarter-final was Canada–Czechia, not Sweden. (1)
- `8262dbbc-52ca-5036-9245-bdb5e52ffb1d`†

#### Foreign-team fixture. (27)
- `31a871b3-ab77-0d3f-b94a-c22412288fff`†, `bccf4e8b-14ef-0e31-a760-6131870d7ca3`†
- `ceceb2dd-0339-1c3b-8443-bef5bd6fcd6f`†, `fc46f941-aa58-8b32-9c99-57c29009e78e`†
- `fb0f55df-727e-203b-bb84-8f0a58239fbf`†, `c6c24893-6a49-cd32-b880-e77e5a8e9ab6`†
- `0c501368-f56a-573e-97aa-2f5e3a862fa8`†, `0941874c-cff7-143a-9d07-033ac8290cb2`†
- `21760f08-304b-6b33-84ce-7276586033b0`†, `31198686-b024-1137-8026-347a8cee8683`†
- `31eb6325-6fc5-d832-876e-da7554d918eb`†, `6cadb2f3-d43a-f33e-9c98-cbb8d60ef1b3`†
- `92f111ac-a414-a831-8eed-e3f180837d8e`†, `a69ed6b5-da4c-c037-8beb-9af484535409`†
- `db9c563a-1d10-8e3b-b74f-4a45db61908e`†, `e307f2c9-39d1-7a35-b732-bf6b8926ad5d`†
- `eb07fc79-83d8-0f3a-9955-501a8c723f9d`†, `fca9831e-51e9-6736-aa0a-4b076f02d061`†
- `33115378-72fa-a436-8d38-e5a16c6b8176`, `716f69f4-9325-7537-a463-e5d8ccfe1466`
- `29cbc9f7-32c0-fa35-a9e3-933f040184ec`, `89ed551c-dbe2-a637-8d38-e07df08a8c29`
- `e4592c2d-34ef-8f34-8ee3-d5c472fa1e4e`, `ed678724-7665-7930-a008-0d9a791b02a7`
- `c42d9246-96d5-9434-b22a-d0db2cdd980c`, `55f63089-6862-c533-81bb-d3069fd0a7b1`
- `6470bf44-1f78-2b34-802e-851f0fd5a935`

#### Domestic Swedish league or club competition. (20)
- `f88231f6-07cb-0730-a525-eeb0e8ff2db8`†, `2a674f2f-c6fb-443d-9916-a98d2b2ce006`†
- `62e244e9-bfc1-443e-8250-71f7dc3622a5`†, `905b9c9a-cecd-7633-af4c-a4332804bee4`†
- `9bfa10df-8d1f-f636-b262-a02bc9ec5b8b`†, `cfaee330-4420-913a-a363-36c30d288eec`†
- `f2029f6e-0c70-c230-841b-76e06bd8f508`†, `be827d86-3093-e637-86b6-99e9186d66ba`
- `876a40b5-2b7f-7633-8942-2bbb185b73c6`, `c31219b6-5f29-283f-a5cb-ef02354b4350`
- `82307918-dfe7-0631-bb8e-2300fa0f184c`, `9653c87a-6c06-8839-a16d-4e13f551026a`
- `c6ba3ddc-bac2-d535-9bba-539b511eb699`, `63855028-c447-e632-b53c-93186ae67d40`
- `d489d823-99c3-5537-8b01-2b61e0b37606`, `3aaee301-93b5-0d3a-aa16-cff00f4dbf2d`
- `a34d8919-a8a6-c63f-b3ae-399c64c3dac6`, `c984e3f9-4f0d-ad3f-b568-23fb1cd2fa2f`
- `c04266c4-c67a-9939-913b-a95608937c73`, `254edb49-b4fc-dd33-b070-6ee74b1d8ab3`

#### Champion of Champions complete field has no Swedish player. (1)
- `bbcc7787-7d53-7030-87b2-5293ec205121`†

#### Hamme elite start lists have no Swedish riders. (2)
- `46a9a1d1-db09-6937-9294-0433f6bdab77`, `2ff08c7a-ce00-873d-bdba-1bc707be8446`

#### IIHF semi-final was USA–Finland. (7)
- `2aedd7f7-c769-c93d-9d90-f6d37c57da2c`, `79c12ace-35d2-9f30-9912-08e1eae41f1d`
- `e87ee685-f224-b53f-86f9-810730ddb329`, `14f0f9a6-ee62-d93c-9a97-fb5006909ebd`
- `880b3161-4ccd-0038-ace0-771d11d5bb23`, `bc194878-9a2a-563f-9857-de5d8d317e2c`
- `f31e67c1-2ec7-7e31-b6d5-163681bae9bf`

#### Complete ATP Finals singles and doubles fields have no Swedish player. (20)
- `83d0b4dd-1008-a930-bbff-48e21c9cf16a`, `45da8998-def9-d634-ac80-5aed105a0793`
- `7d7a11a0-c57f-8034-8e67-8221086c1841`, `b6d4b89c-f15d-c33c-bea1-9f203f8a7faa`
- `5b4017d6-a6d5-be3f-b8e6-c13d4a98f4a2`, `959f5fc7-b663-593d-baf3-c80d204fef41`
- `94428a59-a59c-9e39-87e5-abbaac631429`, `fef91ee7-304d-8132-bcfb-67e1785325dd`
- `c46b5d5f-1be8-8531-a64e-4aa97bc22e23`, `0b1b1a70-7021-6737-9e0c-ff533f7a7a48`
- `e448e55f-1444-9138-8fa0-a5817380116d`, `7d0dd25f-bf2a-dc30-add2-60fe77d7725b`
- `40ec2a82-f986-3c30-816c-8f97fc8ffab5`, `83b13f5a-9d88-3933-ae0f-f9e24cd9c907`
- `b446e459-4631-5738-b45f-63f9fc75415e`, `be639bbe-19a1-7b30-bf6b-7affbb4ebfba`
- `0b2f0b47-2ad5-7938-9d75-b9f4dcc97983`, `550fefc4-4f19-8b36-8fea-30b4ded7988c`
- `e7f86324-0edb-e735-8249-b770d3abac69`, `f611abf1-cd9d-9936-a1ea-8db11e4cfb4e`

#### F1 Academy Las Vegas grid and wild card have no Swedish driver. (2)
- `9a973a89-a860-8f34-81a0-4c2e17bd0527`, `e69544b5-6d01-1b36-9ca6-280ade14e7eb`

#### Official MotoGP class entry lists have no Swedish rider. (8)
- `34ec5af6-62bf-9436-b122-2032d8357080`, `7819566d-03fe-0a32-b221-fe6a358908c6`
- `af88327b-c4c5-373a-bf1a-dfbc62191ec5`, `df922008-abf7-133a-a5e1-a61886e8e4f0`
- `67399023-ed79-ed35-8a15-40fc15a200f5`, `5638de42-0eb9-5234-b7d1-3011192601a3`
- `957e8b4a-a9b5-2436-9532-f1c282424f5b`, `cf30ec1f-1578-8c3d-8488-882db954590c`

#### Standalone draw, not a sporting competition. (1)
- `1287f775-ff4d-e23c-91bd-f88d6445799a`

#### Official Formula 1 driver list has no Swedish driver. (5)
- `3db97fa9-ac3c-8430-bb15-a2ef2d8cc43e`, `b43da416-b2af-2130-b263-42ffccf21358`
- `a83c8e56-7285-b837-8813-bc11e45f69e0`, `048a6563-b6e9-f43b-a4be-0a9b81112872`
- `5fe829a8-674d-c23d-a60b-e70330292d08`

## Processed broadcasts (18)

Every row below was linked to its activity, marked processed, and hidden at final audit.
+ marks a new organization marker.

- `deedaa01-f513-9c33-a6b9-75d2cd409a0a` — Processed: Asian Le Mans Series, Race 1; activity
  `7d8e716a-082c-5b23-a8a4-67e1c1eb5c05`.

- `e95de169-030f-4937-a560-09f156d4123d` — Processed: Austin Championship, Round 3 HBO Max
  segment; activity `efca9190-6cf7-516c-9336-54077b81f72e`.

- `d409b898-7f8a-b73b-9c34-b488ba557b2b` — Processed: Charles Schwab Cup Championship, Round 3;
  activity `8d7ee703-cdbe-5df9-b4bb-6ec3b2446499`.

- `9986469d-9dee-d33b-b3ec-e04c17bc90e4` — Processed: Women’s World Championship, semi-final 2
  feed; activity `a266a167-b092-519d-a8a7-fb3142c3d6aa`.

- `f3840800-f4ca-1a38-b6d5-ec7f0849cc3c` — Processed: Women’s World Championship, semi-final 2
  feed; activity `a266a167-b092-519d-a8a7-fb3142c3d6aa`.

- `a41d4795-75ad-c234-a53e-75e4456b595a` — Processed: Women’s World Championship, Sweden–Canada
  segment; activity `a266a167-b092-519d-a8a7-fb3142c3d6aa`.

- `c9103837-06b3-2835-ae7b-68506110b688` — Processed: Women’s World Championship, Sweden–Canada
  segment; activity `a266a167-b092-519d-a8a7-fb3142c3d6aa`.

- `cb72e420-9839-0631-b776-c43c2f6da9f6` — Processed: Women’s World Championship, Sweden–Canada
  segment; activity `a266a167-b092-519d-a8a7-fb3142c3d6aa`.

- `d663b80c-5c25-9e3c-a018-938198553550` — Processed: Women’s World Championship, Sweden–Canada
  segment; activity `a266a167-b092-519d-a8a7-fb3142c3d6aa`.

- `cf06fbf8-6a25-1033-93fd-5077edc0eeda` — Processed: Austin Championship, Round 4 HBO Max
  segment; activity `7d5c81ef-839a-5cec-a34e-0c1df75da43b`.

- `3a6afb70-8d76-803a-aeb0-74c3cd8528d2` — Processed: Charles Schwab Cup Championship, Round 4;
  activity `23d9459a-285b-55a8-8871-b23af77617f7`.

- `5c6eba58-a65f-8934-93c8-14107b8f4d24` — Processed: The RSM Classic, Round 1 HBO Max segment;
  activity `fc7f38fe-53b4-58b3-93d2-8c90a4429440`.

- `45b3087d-6421-d93b-93f6-233f676fdff6` — Processed: GCL Super Cup, quarter-final HBO Max
  segment; activity `3b15e8e9-ccb1-572c-b2c6-3b96481f5c54`.

- `47fd8a06-9f25-9a3b-a9bd-343ec6ad28a0` — Processed: Finlandia Trophy, Swedish competitors;
  activities `0bcf58dc-770f-5366-98e7-a7fe0ec63238`, `cb60400a-ee21-574e-8f76-9dac5c7a45d8`.

- `d8f7b94c-27e6-bf39-a042-563f7a8e6d62` — Processed: The RSM Classic, Round 2 HBO Max segment;
  activity `e0db5bf5-b43e-5dc4-becb-3b5a7a3d8011`.

- `8945ab08-95cf-7f3e-a4aa-e40ac4dfe7c0`+ — Processed: GCL Super Cup semi-final HBO Max segment;
  activity `fcc4226b-1282-5496-814e-3a5a34232f25`.

- `eeb6a652-2a41-4634-898b-bae5444aacc3`+ — Processed: GCL Super Cup semi-final Eurosport 1
  segment; activity `0a5c38b6-5ae1-5a24-bd09-e03fba2f66de`.

- `895d6881-4d57-ee31-91a6-df1e271ba802`+ — Processed: GCL Super Cup semi-final TV4 Play segment;
  activity `0b9b0c06-55d7-5a95-9cd7-6afe5cf6cf54`.

## Unresolved broadcasts (49)

IDs remain visible and unprocessed unless marked †. + marks a new organization marker.

#### Broad Youth Olympics listing names several Swedish sports but not its segment. (1)
Organization: Olympic Games
- `a7695312-2d4d-3235-9d8b-fa1acc8f248b`+†

#### Dubai T100 start list or wildcard participation is not fully confirmed. (3)
Organization: T100 Triathlon World Tour
- `4a09555c-a465-723f-a79a-ca449d234dc2`, `9c59b7a6-ef77-2530-bfd2-feb0a8bbc0ed`+
- `fb15c1c6-6162-0a37-8621-82077484f1d8`

#### Official Merksplas page has no complete women’s or men’s entry list. (3)
Organization: Cycling
- `48b30142-56e0-0d38-95a6-d130f104ee66`, `02fc4e72-e372-483f-9ba4-9a7c81065a55`+
- `daf54d93-a7c1-d73b-bf27-942f585dfdfd`

#### Classic 24 entry list gives hometowns, not nationality. (2)
Organization: IMSA Sportscar Championship
- `24cf4bbb-d485-0836-bbb7-d7e8cad9d949`, `e6933904-3066-6536-b629-628a25b3c29a`

#### No complete official Saitama Crit entry list confirms Swedish participation. (1)
Organization: Cycling
- `529006a8-ae46-7c36-bd71-2753914923e7`+

#### The World Equipped event is identified; the relevant final-day field is unverified. (1)
Organization: not tracked
- `417bf59a-c0a5-df34-b744-e951f83fe98e`

#### Preliminary squad is not a confirmed match-day roster. (3)
Organization: Basketlandslaget (dam)
- `6e42c75f-d0b7-1934-9cc6-b0ba283e0977`, `f3569876-e1a0-da37-9d70-b55cfb595f43`
- `50bd5925-7c2f-0535-a2b5-8cb01461f23b`

#### Grand Slam sessions depend on match progression; exact Swedish matches are unknown. (12)
Organization: PDC / WDF
- `b70161c9-bd8c-2331-8882-135df3706ba8`+, `2b1fd3c0-dc5d-d734-9a32-ef4ddc99e844`+
- `6b67db15-d728-7b38-97b4-d0abeca27668`, `de148822-348c-7530-8184-3744ba8efb51`
- `813bbef8-b5fe-ad31-a5be-d36d3633a188`+, `023e29c6-9ea2-1c35-9927-5d86a0f04558`+
- `bf65a69d-a175-d437-9773-f0b796e80bf3`, `c69df824-1d12-423d-8148-cc5f515e2f35`
- `8577a8cd-c9b0-eb33-969c-5203b1e8389d`, `d0a19be9-b371-973a-90b9-308bcb501cb6`
- `0f4a2b1f-ad91-b331-ac56-09844ae62a39`+, `81b99e6d-d495-4532-9102-9cdf3b435505`+

#### The final depends on Sweden winning the semi-final. (4)
Organization: Ishockeylandslaget (D)
- `da878fa6-b3dc-1a39-bc18-b22817834498`, `5144d5c3-0e60-b331-b589-2184aa954ab4`+
- `5895be4b-453f-b13c-a1ce-2d4b76574b4d`, `dfb866d9-19f7-d83b-bab7-95bbc47021df`

#### German Masters broadcast-to-round mapping is not fully confirmed. (3)
Organization: World Snooker Tour
- `ba72504b-e03e-163f-aa41-804c81e1f65a`+, `97cabbc6-02a8-5031-a814-8618ae7b44c1`+
- `90289d77-1f26-4f34-8d25-249450e9c973`+

#### The generic Verona listing does not match an official 18 November event. (1)
Organization: Global Champions Tour
- `d2eb2fc7-5f7a-723a-a3ff-68a8a0a440c8`

#### Ferrari Challenge class entry lists do not confirm Swedish drivers. (2)
Organization: Ferrari Challenge North America
- `e6622f4d-a08d-d239-89eb-3c268f9c5ad9`, `79234899-2f39-cd36-99b9-2907c03faa17`+

#### Ferrari Challenge class entry lists do not confirm Swedish drivers. (4)
Organization: Ferrari Challenge Europe
- `14d51f7b-f81f-1932-bc45-245fa010c850`, `f937e3b5-97bc-7838-b980-c6eed8bf6003`
- `8e169384-58c1-4e3c-9fe2-f23251075635`+, `585ee2bd-28b9-e535-a672-2eee9246d420`+

#### The official CME entry list is incomplete; no Swede is confirmed entered. (4)
Organization: LPGA Tour
- `022561ef-cb25-8c3c-9351-15924eada4f6`, `d24eadb1-37d0-163a-8612-24f1e9f299f6`
- `2612f39f-8343-f435-a6f9-90662175d28e`+, `64ec9770-3805-6238-9ada-1f15eab63ee5`+

#### The official Obihiro schedule lacks a complete Swedish start list. (1)
Organization: International Skating Union
- `887e630d-67e3-5a39-ac6f-b3f47715796a`+

#### Lillehammer ski-jumping entry lists are not yet published. (2)
Organization: not tracked
- `b36fdc5c-d136-be33-80db-f5dbfa2622eb`, `d969e3ed-d925-0131-802d-752051541753`

#### The generic Verona class listing has no confirmed Swedish rider or GCL match. (1)
Organization: Global Champions Tour
- `e0cf7cfc-ebab-6c32-b9d2-91c4ea031526`+

#### The RSM listing combines Days 1 and 2, so its exact round is unclear. (1)
Organization: PGA Tour
- `e2e048f3-fe79-e53c-a4af-5d5be908bba8`+

## Evidence sources

- The original classification research and source links are in the
  [12 Nov report], [12 Nov second run], and [13 Nov report].
- IIHF confirmed the semi-final pairing: [IIHF pairing]. The roster evidence is saved
  on the Hockey-VM activity.
- Asian Le Mans participation and schedule: [ALMS team 30] and [TV.nu Asian]. The
  official FP1 report is saved on the activity as ParticipationEvidence.
- Austin schedule and segment listings: [Austin schedule], [TV.nu Austin R3], and
  [TV.nu Austin R4].
- Charles Schwab event and round evidence: [Schwab overview], [Schwab schedule],
  [TV.nu Charles R3], and [TV.nu Charles R4]. [PGA] lists no Round 3 media sheet.
- GCL schedule and qualification: [FEI event], [GCL standings], and [GCL teams].
- RSM entries and listings: [RSM field], [TV.nu RSM R1], and [TV.nu RSM R2].
- Finlandia schedule and participants: [Finlandia entries], [Finlandia schedule],
  [Andreas profile], [Milla profile], [Nikolaj profile], [Team Sweden], and
  [Norrköpings KK]. TV.nu listing: [TV.nu Finlandia].
- Final public pages: [14 Nov page], [15 Nov page], [19 Nov page], and [20 Nov page].
- TV.nu segment records and provider StreamLink sources were saved on each activity.
- The new ALMS FP1 ParticipationEvidence source is stored on activity
  `7d8e716a-082c-5b23-a8a4-67e1c1eb5c05` as
  `2662d461-d296-4195-9399-67d9f9371ca1`.

[12 Nov report]: activities-2026-11-12.md
[12 Nov second run]: activities-2026-11-12-run-2.md
[13 Nov report]: activities-2026-11-13.md
[IIHF pairing]: https://www.iihf.com/en/events/2026/ww/news/76915/semi-final_pairings_set
[ALMS team 30]: https://www.asianlemansseries.com/en/car/2026/30
[Austin schedule]: https://www.austinchampionship.com/about/schedule-of-events/
[Schwab overview]: https://www.pgatour.com/pgatour-champions/tournaments/2026/S2026539/overview
[Schwab schedule]: https://www.charlesschwabcupchampionship.com/fans/schedule-of-events
[PGA]: https://pgatourmedia.pgatourhq.com/tours/2026/pgatourchampions/charlesschwabcupchampionship
[FEI event]: https://www.fei.org/events/2026_CI_0040
[GCL standings]: https://www.gcglobalchampions.com/news/1182513
[GCL teams]: https://www.gcglobalchampions.com/news/1094513
[RSM field]: https://www.pgatour.com/tournaments/2026/the-rsm-classic/R2026493/field
[Finlandia entries]: https://isu-skating.com/en/figure-skating/entries/isu-gp-finlandia-trophy-2026/
[Finlandia schedule]: https://finlandiatrophy.com/en/schedule/
[Andreas profile]: https://isu-skating.com/en/figure-skating/skaters/andreas-nordeback/
[Milla profile]: https://isu-skating.com/en/figure-skating/skaters/milla-ruud-reitan/
[Nikolaj profile]: https://isu-skating.com/en/figure-skating/skaters/nikolaj-majorov/
[Team Sweden]: https://www.skatesweden.se/tavling/team-sweden-landslag/team-sweden-2026-2027
[Norrköpings KK]: https://www.norkk.se
[14 Nov page]: https://sesport.se/?date=2026-11-14
[15 Nov page]: https://sesport.se/?date=2026-11-15
[19 Nov page]: https://sesport.se/?date=2026-11-19
[20 Nov page]: https://sesport.se/?date=2026-11-20
[TV.nu Asian]: https://www.tv.nu/s/p_1637711_20261114
[TV.nu Austin R3]: https://www.tv.nu/s/p_1637733_20261114
[TV.nu Austin R4]: https://www.tv.nu/s/e_4990594_20261115
[TV.nu Charles R3]: https://www.tv.nu/s/p_1637722_20261114
[TV.nu Charles R4]: https://www.tv.nu/s/p_1637721_20261115
[TV.nu Hockey]: https://www.tv.nu/s/e_4987212_20261115
[TV.nu Hockey alt]: https://www.tv.nu/s/p_1631133_20261115
[TV.nu RSM R1]: https://www.tv.nu/s/p_1637737_20261119
[TV.nu RSM R2]: https://www.tv.nu/s/p_1637067_20261120
[TV.nu Finlandia]: https://www.tv.nu/s/p_1637316_20261120
