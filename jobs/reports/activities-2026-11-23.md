# Activity run report — 2026-11-23

## Run summary

- Started: 2026-11-23 00:30:05 UTC.
- Starting snapshot: 131 visible broadcasts for 26–29 November.
- No starting broadcast had an activity link.
- Final outcomes: 40 rejected, 13 processed, and 78 unresolved.
- Database audit found no status, link, or organization mismatches.
- No code, schema, configuration, asset, or deployment changes were made.
No build or automated tests were run; this was a guarded application-data run.

## Public card counts

Counts are rendered event cards on sesport.se; each grouped event card is counted once.

| Date | Before | After | Change |
|---|---:|---:|---:|
| [2026-11-26](https://sesport.se/?date=2026-11-26) | 2 | 2 | +0 |
| [2026-11-27](https://sesport.se/?date=2026-11-27) | 5 | 5 | +0 |
| [2026-11-28](https://sesport.se/?date=2026-11-28) | 3 | 4 | +1 |
| [2026-11-29](https://sesport.se/?date=2026-11-29) | 0 | 2 | +2 |
The 28 November increase is the Formula 2 sprint card. The 29 November increase is the Formula
2 feature card and SailGP Day 2 card. PGA rounds reused their existing published round cards.
The date-picker badge is a separate total; this table counts rendered group cards.

## Processed activities

Existing event groups were reused. New activities are published in Stockholm time; broadcasts
are linked, processed, hidden, and assigned to the event organization.

### Australian PGA

- Group: `081529c7-c30a-4aed-aaf0-2bf336efc902`, BMW Australian PGA Championship.
Six Tournament activities cover five Swedish entrants on public dates 26–28 November. Each has
all five active Person links with DP World Tour context.
For each round, the opening range is 02:00–03:30 CET and the continuation is 03:30–07:00.
- R2 opening:
  broadcast `62b56398-7efd-b137-b19a-6727aa0a990e`
  activity `de24b6ab-db34-4833-94a0-62b3327faeb9`; public date 2026-11-26.
- R2 continuation:
  broadcast `be5be00f-3aca-0c35-bc88-ac55164dd3c7`
  activity `d9a8faa9-b70b-4dbc-928c-768ecabe6baf`; public date 2026-11-26.
- R3 opening:
  broadcast `e0dc5fc2-1849-4e38-a28a-85848d02c677`
  activity `84f0e07c-970b-40b3-8ea3-cdd3db46fd9f`; public date 2026-11-27.
- R3 continuation:
  broadcast `4f6aa128-24b2-9a3b-9d34-76852e999224`
  activity `c391995a-68ba-4d4d-93fb-ae64129a8c40`; public date 2026-11-27.
- R4 opening:
  broadcast `2e87fd33-c6a7-253c-8d21-36b043c3e195`
  activity `b29568a3-95bb-4dc7-851e-5c92e6192338`; public date 2026-11-28.
- R4 continuation:
  broadcast `9b096508-d6f2-553d-9a73-afdccf617246`
  activity `0e6e3399-d132-4cb7-9c68-d2be7570c515`; public date 2026-11-28.
Every round shows Adam Wallin, Alex Norén, Christofer Blomstrand, David Nyfjäll, and Joakim
Lagergren with ages, clubs, and profile links. Alex Norén remains tier_0; the other four
golfers are tier_2. Tee times were not published; Golf Australia
expected them on 24 November, so no player-specific detail activities were added.
- Evidence: DP World Tour event notice, Svensk Golf entrant report, and TV.nu schedule.
Svensk Golf names Royal Queensland; the current DP World Tour notice names The Lakes, which
was used for the event venue.
- [Golf Australia tee-time FAQ](https://golf.com.au/aus-pga-faqs-2026).
V Sport Golf resolves through its existing Viaplay channel link.

### Formula 2 Lusail

- Group: `78534d07-1868-56dd-a154-651d7440ff42`, Formel 2, Lusail.
Four Race activities cover Dino Beganovic on public dates 28–29 November, with DAMS Lucas Oil
as represented team.
- Sprint, Viaplay:
  broadcast `952650b9-e4a7-1d3a-a773-c563b097d22c`
  activity `6d34dd57-a80e-4666-b1cb-973ed449a07a`; public date 2026-11-28.
- Sprint, V Sport Motor:
  broadcast `408fb8e2-ef6f-5635-9ac6-8f62a350da8b`
  activity `a294dc1f-24d7-4c41-baf6-4db1754602a8`; public date 2026-11-28.
- Feature, Viaplay:
  broadcast `187dc3b1-788f-4d3e-8716-37d6cb2844d6`
  activity `ad5ab94e-cd54-49a2-9fa0-382c774fff2e`; public date 2026-11-29.
- Feature, V Sport Motor:
  broadcast `bbceaee9-8088-7231-b11f-ba6865357b7b`
  activity `07cb205a-5413-4dcb-b11c-f726b8241936`; public date 2026-11-29.
The Viaplay and V Sport Motor ranges remain separate. Viaplay uses its direct event links; V
Sport Motor uses the existing Viaplay channel fallback. Public cards show Dino’s age,
Linköpings Motorsällskap, DAMS Lucas Oil, and his FIA profile.
Sprint: Viaplay 17:10–19:00; V Sport Motor 17:10–18:15.
Feature: Viaplay 12:55–14:35; V Sport Motor 12:55–14:15.
- Evidence: FIA team information and TV.nu schedule details.
- [Formula 2 2026 calendar](https://www.fiaformula2.com/en/racing/2026).
- [Dino Beganovic profile](https://www.fiaformula2.com/en/drivers/dino-beganovic).

### SailGP Abu Dhabi, Day 2

- Group: `2dd00e41-1ee5-4b71-a2c1-bf5204688b6e`, SailGP Abu Dhabi.
- Viaplay, 11:00–13:00:
  broadcast `70f7142f-76ef-d33a-8719-b018e2aa1839` →
  activity `9ce98d1f-66f2-4c47-b210-6e1e9b796d56`; public date 2026-11-29.
- TV4 Play and TV4 Sportkanalen, 12:00–13:30:
  broadcasts `a686ce0b-32a3-2e32-8706-b750c14f9a19` and
  `be0d1db4-adb4-c033-af27-d03ed1ed8113` →
  activity `e2c40c47-db5c-438a-a986-43502bcf50ef`; public date 2026-11-29.
Both activities link Felicia Fernström and Julius Hallström as active Persons. The public card
has one group heading, separate broadcast ranges, ages, clubs, and working profile links.
Both sailors and Dino Beganovic remain tier_2; their birthdates and profile URLs were checked.
Viaplay and TV4 Play use direct event links; TV4 Sportkanalen resolves through the existing
TV4 Play channel catalog.
- Evidence: Artemis crew update and roster; Viaplay schedule listing.
- [SailGP 2026 calendar](https://sailgp.com/general/2026/calendar/).
- [TV.nu guide, 29 November](https://www.tv.nu/kanal/tv4-sportkanalen/2026-11-29).
The official calendar confirms Race Day 2 at 14:00 GST / 10:00 UTC.

## Rejected broadcasts

All 40 are hidden and remain unprocessed; the only broadcast change was hidden_at.

### Domestic competition. (19)

- `d7851755-fd68-f03e-b62c-7639d20120a8`
- `9eb3b5d1-ce8f-d139-a81f-1ed9b9e7e169`
- `16a849b4-1c4c-ec31-9a36-b75ab4d928d8`
- `a3958241-0e20-c032-92a3-59f7258b73f2`
- `e9cd16e0-fa77-7f34-9599-527c4302e01b`
- `b02707ce-781b-6430-95e5-7c7c2a728ca9`
- `606a1168-a4cc-ea31-9418-525637f1dd3b`
- `051c0fb7-c4e2-a53d-a49d-ce2207c7a0b6`
- `f5dcdcca-3b5b-ba35-ab60-311892bc7a4f`
- `c0fcaa84-ec73-d731-a538-bfe0c9d5d6db`
- `606da4cd-5c7f-2c3f-b00c-f01f2da7540a`
- `b9467d90-c1ca-af33-8a89-d74847076371`
- `1ac7e2d5-9480-7d32-affa-cc0127f5f4e0`
- `a4f4dc6d-387d-3132-a6d5-06640d09c2d7`
- `ee0d98f0-e491-7e3b-a9b7-0916d7042612`
- `85ab8812-7855-ea38-9704-a4486fbf4974`
- `d8b13542-e0d6-a735-b22a-7e4954428d9c`
- `064b0ea0-1e7f-953d-9f5d-ddf8860c4de9`
- `5b2bda1d-d7ea-5037-9471-5a1e9b741e50`

### Foreign-club fixture. (6)

- `b7d3ee05-28c8-8932-bf55-6c9eead571e6`
- `3099639e-773d-f83d-9bef-2d4a49aed86b`
- `8b1dd7ad-086e-4936-a33a-598a3f2e4677`
- `ccc440dc-9d42-3431-9f56-21e3456a889a`
- `133d8a69-585b-2a37-b17e-d26b78c17fa3`
- `084049b2-27f3-5b36-9253-5e446c229631`

### Generic studio or highlights. (7)

- `d295af1d-1620-c634-9080-4a5bb0884ec0`
- `bdca0f4b-214e-ac35-afb3-41eaa50bbe1a`
- `21087193-57a6-4b3f-aca8-d442fa559a98`
- `4420afce-adc6-fd3f-9b4b-bf7134a1f173`
- `b6f41a2a-da0c-a63b-9637-4dcfcf4b0cec`
- `500a6e83-0f99-2c3c-a6fc-bb5fccfc3897`
- `d10930b4-8db3-3038-a1b6-b5a57c52953c`

### No Swedish driver or team in the 2026 F1 field. (5)

- `8069e8c0-1d7c-1937-9057-330eec9442e2`
- `ca208211-fb10-6c36-9771-7fa844aea4b9`
- `1a908400-7cd9-9d3c-9669-9d9b8e897f75`
- `53183ed9-41c8-ca32-a711-0918d7816a88`
- `686023d2-16f4-0430-b38d-bc01ef97089a`

### No Swedish entrant on confirmed tournament teams. (2)

- `28300087-e699-ca3e-837f-86d486326f7b`
- `3f9444b0-ec6e-0f3e-ad56-680f2f79092e`

### Unidentified event. (1)

- `3f8a794a-9018-963d-95d9-30f4d617eb5c`

Research for scope decisions:

The FIA 2026 Qatar Grand Prix entry list showed no Swedish driver or team. Confirmed World
Nineball Tour rosters did not list a Swedish Mosconi Cup player; WTGL team rosters and its
schedule confirmed no Swedish entrant for the excluded match. Remaining rejections were
identified from broadcast metadata as domestic fixtures, foreign-club fixtures, generic
studio/highlights, or an unidentified placeholder.

## Unresolved broadcasts

All 78 remain visible and unprocessed. Organization markers are set when a matching
organization was supported; “none” means no marker was assigned.

### Unresolved group 1 (6 broadcasts)

Event: Alpine World Cup; organization: Alpina Landslaget.
Reason: Race-specific Swedish selections and start lists are not published.
- `ec84aab1-fb40-0e3d-9109-3e77070519d6`
- `62109bc7-b791-6c37-afe1-e1a3b1717acb`
- `b8b97070-acaf-b133-a789-c439f28df04e`
- `b4afe6c3-b137-3b30-8194-56c133296b10`
- `63c6011b-16a8-7b38-b3c0-793f92887de4`
- `bd6e4846-dd73-ea37-988f-58dd507ffffd`

### Unresolved group 2 (1 broadcast)

Event: Andalucia Costa Del Sol Open De España; no organization marker.
Reason: Exact Swedish participation or broadcast mapping is not confirmed.
- `2bea250c-e609-7333-977c-5a0d23b6b3ef`

### Unresolved group 3 (8 broadcasts)

Event: Biathlon Kontiolahti; organization: Skidskyttelandslaget.
Reason: Official squad confirmed; exact race entries and relay lineups are pending.
- `de2377ca-7408-9d3e-91f8-a955caf5b696`
- `4cfa039c-7197-3634-a887-3001c15cb56c`
- `1651bfe6-6891-df3a-9dab-05a35d7c58dc`
- `d77dc415-822d-a93c-9679-e89033c03b38`
- `0a9a6352-90b5-0539-9d2e-bacf879b4f19`
- `e9f5eb6f-46ab-c334-8045-535f6fe90865`
- `4c1039bd-ab7c-a737-a90c-05b244d5290c`
- `aff8fb37-0b29-703b-a0c8-18c0307e828b`

### Unresolved group 4 (4 broadcasts)

Event: Cyclocross World Cup, Tabor; organization: Cycling.
Reason: Complete 2026 Swedish race entries were unavailable.
- `42bf7787-6e62-7439-a9a3-22f20cd7cda4`
- `801f42a5-4b24-ab30-b4d4-02fdcbdd8da1`
- `bdaf088a-8222-4536-abed-a73ac7895b5a`
- `78ae8f65-6c1e-ba39-a772-75c9257339f9`

### Unresolved group 5 (1 broadcast)

Event: Finaler; no organization marker.
Reason: Exact Swedish participation or broadcast mapping is not confirmed.
- `ddae6f28-53ca-393e-8a4a-54a4a6e2090d`

### Unresolved group 6 (1 broadcast)

Event: Freeski Big Air; organization: Freeskilandslaget.
Reason: FIS event identified; complete Swedish entry list was unavailable.
- `da762a05-41b3-a73c-bd82-b2dac09df13c`

### Unresolved group 7 (1 broadcast)

Event: ISU Short Track World Tour, Shanghai; organization: International Skating Union.
Reason: Event confirmed; complete Swedish entry list was unavailable.
- `4be61b67-b053-1b3a-b50f-33cb741b15ce`

### Unresolved group 8 (1 broadcast)

Event: Ladies European Tour, Andalucia Costa Del Sol Open De España - Final Round; no
organization marker.
Reason: Exact Swedish participation or broadcast mapping is not confirmed.
- `df47b9dd-0455-e83e-ac3c-aeed01bd1668`

### Unresolved group 9 (6 broadcasts)

Event: Ladies European Tour, Andalucía; organization: Ladies European Tour.
Reason: Official sources announce 75 players; the complete field was unavailable.
- `07c3c8b5-7d56-7a3b-b9b9-bbc45954de3d`
- `097c8dd6-5181-d235-9295-a8a128e7d274`
- `ffa48f81-ce5a-f33c-a4d4-5af26f43c49a`
- `708e62ff-75d9-c437-951e-5b1a9c48da7b`
- `c6f187e4-0c88-d035-b811-5069f736df0f`
- `89edc1e7-3291-5433-b36f-3f414c109d60`

### Unresolved group 10 (2 broadcasts)

Event: Madrid show jumping; organization: Equestrian.
Reason: Event week confirmed; Sunday Grand Prix entry list is incomplete.
- `fa041046-1202-5c3c-8e6c-880ea3b85e79`
- `7394730f-7a40-c237-b207-78a9f6a435db`

### Unresolved group 11 (8 broadcasts)

Event: MotoGP Valencia; organization: MotoGP.
Reason: Official entry list was not yet available.
- `810497fc-2b78-2933-86fe-d6498f53d975`
- `b620ab4a-4756-b23d-b385-b904ffacbb50`
- `cce6a520-c069-6737-9be0-737011796b8a`
- `228c8f2a-9f53-5338-a10b-addf61e88a36`
- `64f6c1c8-fed5-4931-8584-1eb976108963`
- `20e15745-13b4-2c3e-9599-9c364cada973`
- `3d898b5f-dfae-ed33-ad7a-958cb4d7c285`
- `aec2619b-66ac-3034-93d2-e119d9052fec`

### Unresolved group 12 (7 broadcasts)

Event: PDC Players Championship Finals; organization: PDC / WDF.
Reason: Jeffrey de Graaf is drawn; exact feed-to-match mapping is pending.
- `1b1b6842-3288-4735-8e5e-d1c363a6b4ce`
- `6ebd6a87-089a-ef3f-88fa-747cb7093f58`
- `d7834827-7ee1-9d3e-8a95-c8de68cd380d`
- `214157f0-abb7-8036-8ffc-dc2b405ad66b`
- `eb5a1039-4be7-d73b-a216-fd5e92d9d409`
- `e32e4c13-d20b-3731-bd40-2c7d6952bc8a`
- `8c24e400-b95e-b435-8a1b-003c25f577c6`

### Unresolved group 13 (2 broadcasts)

Event: Players Championship Finals; no organization marker.
Reason: Exact Swedish participation or broadcast mapping is not confirmed.
- `e067692a-fcb1-0a3a-aefa-578560b84a90`
- `2632da60-cd92-573c-aad4-8567aa29083d`

### Unresolved group 14 (2 broadcasts)

Event: Ruka Nordic combined; no organization marker.
Reason: FIS start lists pending; no Swedish entrant or matching team entity confirmed.
- `2be808ea-54c6-8c31-a12c-43c80dcd4bb9`
- `20db0379-c4e6-8535-9a69-80efe96e3e8d`

### Unresolved group 15 (9 broadcasts)

Event: Ruka cross-country; organization: Längdlandslaget.
Reason: FIS schedule confirmed; complete 2026 Swedish race entries were unavailable.
- `bdf08ca9-5698-273a-9cd4-dbffeb5c529d`
- `154a2b44-3801-503e-ac88-17682e0ee423`
- `afb35c51-b9f6-4732-9abb-bf668e632b58`
- `f2804282-c529-9f38-ad25-1e91014bed66`
- `97f142c3-3651-b530-b6fe-fe467226506d`
- `03c50c6c-ea76-8e30-99d8-b8762542cef5`
- `de7ac88b-1af5-b43b-abf7-852767f232f8`
- `b317cdf1-54e2-7135-9fd5-0f2aff16be80`
- `d87b3215-4b68-c539-b442-ab7448b49cab`

### Unresolved group 16 (1 broadcast)

Event: Ruka ski jumping; organization: Backhoppningslandslaget.
Reason: Broadcast mapping and Swedish entries are not confirmed in a start list.
- `8dbd3ca4-935f-e338-ab23-cd8ded7a369e`

### Unresolved group 17 (1 broadcast)

Event: Skidskytte: Världscupen Sprint, damer; no organization marker.
Reason: Exact Swedish participation or broadcast mapping is not confirmed.
- `da0f740a-ecdf-7938-be20-594daa5eb55a`

### Unresolved group 18 (1 broadcast)

Event: Skidskytte: Världscupen Sprint, herrar; no organization marker.
Reason: Exact Swedish participation or broadcast mapping is not confirmed.
- `fd2153ad-fade-0338-aff9-ef61c167a60d`

### Unresolved group 19 (1 broadcast)

Event: Slalom åk 1 (D); no organization marker.
Reason: Exact Swedish participation or broadcast mapping is not confirmed.
- `3e02e709-3951-ac3f-9068-14921e3f6a0c`

### Unresolved group 20 (1 broadcast)

Event: Slalom åk 2 (D); no organization marker.
Reason: Exact Swedish participation or broadcast mapping is not confirmed.
- `669fdce3-d3d1-4934-994e-feeb47853adf`

### Unresolved group 21 (1 broadcast)

Event: Snowboard World Cup; organization: Snowboardlandslaget.
Reason: FIS event identified; complete Swedish entry list was unavailable.
- `c39a41cb-8a6c-3631-867e-7e4b8accab7d`

### Unresolved group 22 (1 broadcast)

Event: Sprint, damer; no organization marker.
Reason: Exact Swedish participation or broadcast mapping is not confirmed.
- `4dbe20cb-d2a3-ba33-bc83-7bf3ac0af1c5`

### Unresolved group 23 (1 broadcast)

Event: Sprint, herrar; no organization marker.
Reason: Exact Swedish participation or broadcast mapping is not confirmed.
- `3a0fcacb-ecf5-3336-b98a-c867f6722c8e`

### Unresolved group 24 (1 broadcast)

Event: Storslalom åk 1 (H); no organization marker.
Reason: Exact Swedish participation or broadcast mapping is not confirmed.
- `f9016fc2-8c26-3e3f-a1c1-ded234370a15`

### Unresolved group 25 (1 broadcast)

Event: Storslalom åk 2 (H); no organization marker.
Reason: Exact Swedish participation or broadcast mapping is not confirmed.
- `c207f2bf-8f7e-a53e-868a-732c93efc8fc`

### Unresolved group 26 (8 broadcasts)

Event: UK Championship snooker; organization: World Snooker Tour.
Reason: The draw is not final; Swedish qualifier participation is not mapped.
- `61bcee52-2ccf-8238-a9f2-c6436796057f`
- `71ac1a5f-47c6-c731-856c-dbab94d75b9d`
- `fc3035b4-8588-8d3c-ab59-56cc32f0a5af`
- `aceffee3-6991-0d37-a699-707e38e1a944`
- `c2b94cd5-8121-d23a-8639-a096cabb89f2`
- `53359864-0075-4839-98d0-43b0446b3f3a`
- `8c3d17f0-9cf8-5338-9ebf-52d011d0ed20`
- `2c889033-91ef-ad30-a95a-511d40be7ce9`

### Unresolved group 27 (1 broadcast)

Event: WTGL Match 5; no organization marker.
Reason: Linn Grant is on the five-player roster; exact three-player lineup is pending.
- `f53bfb19-cbdc-1732-9b07-76ad19d72eaf`

## Step 7 and follow-up

- Admin Dashboard, “Activities needing attention”: no upcoming activity issues.
The PGA tee-time list was not available during this run; check again after 24 November and add
sourced player-specific details if the schedule identifies broadcast coverage.
Other unresolved groups await complete official entries, start lists, draws, field
announcements, or event mappings. Sources reviewed included Swedish Biathlon Federation and
IBU releases; FIS schedules; LET field notices; PDC and WPBSA notices; the ISU Shanghai page;
MotoGP Valencia materials; and FEI Madrid information.
No code or asset presentation defect was found on the reviewed sesport.se dates.

## Verification

- Read back all created activities, groups, participants, sources, and broadcast links.
- Audited all 131 starting IDs: 40 hidden, 13 linked and hidden, 78 visible/unprocessed.
- Checked public pages for 26–29 November on https://sesport.se.
- Verified the Dashboard using Admin__Password from .env; no credential was printed.
