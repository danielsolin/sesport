# Activity run — 2026-11-01

Started: 2026-11-01 07:31:01 UTC.
Finished: 2026-11-01 08:02:36 UTC.

## Outcome

The starting snapshot contained 163 visible broadcast IDs. Readback verified one final outcome for
every ID: 62 rejected, 17 processed, and 84 unresolved. All 163 status, link, and organization
outcomes matched the guarded transaction.
Created 10 published activities in five reused event groups. Added 55 person links, 17 broadcast
links, and 32 typed evidence sources: 16 ActivityEvidence, 4 ParticipationEvidence, and 12
StreamLink. No code, assets, schema, configuration, or deployment changed.
All 38 competitors reused existing person profiles, birthdates, clubs, and current team links. No
new person facts or stars were needed. The 84 unresolved broadcasts remain visible and
unprocessed; organization markers were set where identifiable.

## Activities created

### Curling-EM
Reused group: `03733f6c-77a0-476a-820a-c69ac24aa590`.

- `7b7dd347-081a-473b-a1c9-11f00fb6b130` — Damfinal, 1 Nov 09:50–13:30, Kunskapskanalen.
  Broadcasts:
  `805f3166-af52-d436-b7a6-9cd3d0f9f9b7`, `f0a45f0a-61cb-bc3a-857f-87e958059650`
- `ea857247-8283-4b98-be39-2de8a4ba3b58` — Damfinal, 1 Nov 09:50–12:20, SVT Play.
  Broadcasts:
  `7289dbae-cf7d-273b-b714-8fd5a30cfe75`
- `530139f1-48d2-4f6a-ab50-b4104fb0e1a9` — Damfinal, 1 Nov 10:00–11:55, Eurosport 2.
  Broadcasts:
  `7740b2d6-dae8-9a3c-8a55-291518d02470`, `d4ec8a74-5135-ae32-b505-d65f10543313`
- `7816155a-438e-4aa6-84b8-33aeb119fe24` — Damfinal, 1 Nov 10:00–13:00, HBO Max.
  Broadcasts:
  `328d2417-c562-c637-9277-4d4947adbf00`, `5a2e1a7d-f30d-6f31-b8a7-a3e1bc24c955`,
  `818534eb-5211-053d-bbe3-535654eecb83`
- `00f5e013-1617-4c90-9cca-3fbf7d12ef0f` — Herrarnas bronsmatch, 1 Nov 13:50–16:20, SVT2, SVT Play.
  Broadcasts:
  `8d821da4-b599-3f3b-b3c4-119626bd60d2`, `c9aaf994-9c5c-6030-97ca-c57ae0c75543`,
  `65b700a4-6943-8e37-a1cd-a7f96465a44b`

### WTT Champions Montpellier
Reused group: `10ff8d58-2a4d-44d8-b1f8-7b66068c14c4`.

- `fe534a6e-759d-4d75-83ae-08ccf58642e3` — Semifinal, 1 Nov 13:15–14:15, SVT Play.
  Broadcasts:
  `9ee92f18-6de6-3730-8612-b4917e1108e9`

### Abu Dhabi Aldar Championship
Reused group: `3ab9fb18-485d-4199-84a7-1ab4aab7444d`.

- `68e25240-4ebc-40ab-ab00-c4f557da0a8a` — Runda 3, 7 Nov 08:00–15:00, Viaplay.
  Broadcasts:
  `5ec7c03f-4900-2135-ad03-98993ec31947`
- `fd9c2843-2a9a-4f7a-bdc3-4964bf49de77` — Runda 3, 7 Nov 08:00–14:00, V Sport Golf.
  Broadcasts:
  `73cf6900-4ad3-693c-97a4-bf0ce82d83ad`

### World Wide Technology Championship
Reused group: `ed4b99fb-5ff0-4ebb-b42a-4def0303ab4c`.

- `5991c6eb-4b88-4cef-8f5a-166b66a97cc6` — Runda 3, 7 Nov 21:00–00:00 (8 Nov), Eurosport 2.
  Broadcasts:
  `c92372fa-1785-e23c-9f03-3352eda5c051`

### NOCCO Hockey Games
Reused group: `16874649-9970-5a55-b2a0-73d2f89a57d2`.

- `25c7c870-87e9-491e-bfee-a7a347edbab4` — Schweiz – Sverige, 7 Nov 13:20–16:00, SVT1, SVT Play.
  Broadcasts:
  `1db9003e-0f51-bd32-941c-2d0c147d026d`, `c9da9c53-5744-2e3b-bf99-8bf505f6466a`

## Other data changes

Extended the NOCCO Hockey Games group end date from 5 to 8 November 2026. Updated the Swedish
Hockey National Team URL to https://www.swehockey.se/trekronor.
Set organization markers on four unresolved broadcasts: Table Tennis for
`d210e421-ad59-9e3d-8280-e59496a65690`; Ladies European Tour for
`a21dad6c-4781-d63f-9b9a-dd539edd8560`; and ATP Tour for `96d21ab0-33a7-3235-918f-d0eace29e737`
and `6f010bdd-4251-2d3d-b04b-db2abfb12a3b`. The UFC row has no matching organization entity.
Rejected broadcasts changed only hidden_at. Processed broadcasts received the activity
organization, processed_at, hidden_at, and one activity link. No broadcast_ignore rows were read
or changed.

## Evidence

New event and participation evidence is stored on the affected groups and activities. TV.nu
schedules support segment times; provider links were saved as StreamLink sources. No
ParticipantStartEvidence was added because the generic golf feeds did not provide verified Swedish
tee times. Existing person facts and star records were reused.

- [World Curling women](https://worldcurling.org/2026/10/ecc2026-womens-semis/) and
  [men](https://worldcurling.org/2026/10/ecc2026-mens-semis/) results confirm final pairings.
- [Swedish Curling Federation](https://www.curling.se/tavlingar--resultat/em-damer-och-herrar-2026)
  roster and [SVT curling guide](https://www.svt.se/sport/curling/guide-curling-em-2026).
- [SVT](https://www.svt.se/sport/bordtennis/truls-moregardh-vidare-efter-makalos-kvartsfinal)
  confirms Truls’s semi-final; the Swedish federation report is saved on the group.
- Official DP World Tour and PGA TOUR field sources are saved on the golf groups.
- [Swedish Hockey Federation](https://www.swehockey.se/trekronor),
  [NOCCO schedule](https://nocco.com/2026/08/27/nocco-hockey-games-2026/), and the official
  November roster PDF are saved on the hockey group.
- [TV.nu curling day](https://www.tv.nu/sport?date=sondag),
  [women](https://www.tv.nu/s/e_4975832_20261101),
  [men](https://www.tv.nu/s/e_4976246_20261101), and
  [WTT](https://www.tv.nu/s/p_1633856_20261101) listings.
- [TV.nu Abu Dhabi Viaplay](https://www.tv.nu/s/p_1633579_20261107),
  [V Sport Golf](https://www.tv.nu/s/e_4981037_20261107),
  [WWT](https://www.tv.nu/s/e_4981034_20261107),
  [hockey SVT1](https://www.tv.nu/s/e_4980980_20261107), and
  [hockey SVT Play](https://www.tv.nu/s/p_1627453_20261107) listings.

## Rejected broadcasts — 62

All remain unprocessed and hidden. Reasons and exact snapshot IDs follow.

### Domestic fixtures — 14
Reason: Swedish domestic competitions are out of scope.
IDs:
  `cb6a9d85-899f-ab37-bdc9-75747e20c0ad`, `c4f97e98-ab4e-633e-b0d3-0c5c6b690da5`,
  `1ecc75cf-464c-2234-be86-d3113dc0f3a0`, `e917300a-79bb-bc34-8bcb-89e0f3f80f8a`,
  `1faf437c-73fc-7637-8e31-b27b9007fe1d`, `e4f13af4-effc-da34-8c26-2ad7235ba566`,
  `cd781141-b2c3-ae3a-afd6-9ceee6a27ebe`, `c43ae431-2278-5b3d-a0d8-e9e9e51155a0`,
  `858441ff-9902-af3e-a75c-ff78459af583`, `35041448-111e-5030-ae3e-92e8d128a769`,
  `7b769d1e-cb10-ac3d-bae3-dc9c26339403`, `641b8558-271f-1e32-ac70-baffbd49e084`,
  `8afaf889-7846-2a37-9c67-ebded4fa2b14`, `e8dbbcb5-c025-9c37-a546-d3cfd3ebd3bf`

### Foreign club fixtures — 21
Reason: Foreign club fixtures are outside the eligible Swedish scope.
IDs:
  `28a8b8e1-602d-4a3a-8751-ec9dd7787ece`, `7780e542-cfc2-7532-93c1-a0311f933151`,
  `e44183aa-9bdb-5b32-bd9d-8a128ffb40bc`, `8ed2d9ff-74fa-3332-8499-c029bc574323`,
  `9995b963-5dc8-9d3b-9705-9fde18ad6128`, `35b8391f-1c78-0e39-b8ae-33038f86f5ed`,
  `7c6550cb-c3f5-f233-8408-5189c070f284`, `482beff0-a395-c632-b1f0-9995d9163e92`,
  `bb6a3014-09e5-2e3a-93f1-0ee836c0bf3f`, `ff0af642-6e23-4635-8902-85a5dd410f4b`,
  `e3e963ff-126e-d030-a301-e1cad96c3f54`, `17a252d1-b9f1-6836-bb05-62e3cb711b96`,
  `7d3d216b-ac39-8a3c-8913-9174fae4bacc`, `490e93e7-f38f-c835-91b1-056443cf568e`,
  `17a62b9f-d91c-a530-973e-10806b6d04b1`, `945a1167-6e56-6030-9672-cebe6d514175`,
  `634ce4c2-7fb6-0c3f-ab5a-1cfdb711c888`, `f34b8cbd-f49d-3e37-8a42-b8d1427a63ef`,
  `52b10c54-d2a8-9234-906e-26ca970f2353`, `58d7e384-e561-d530-b80b-c392a8aaf9f7`,
  `07e2bfe4-c985-a43e-a013-6738f91de36f`

### Generic programming — 7
Reason: Studio, recap, highlight, or unidentified listings lacked an eligible event.
IDs:
  `07174ea3-12dd-4b35-9375-e89757f00c92`, `671eda38-0b9f-8638-b5e8-007421fade4d`,
  `9f2648e4-2f3c-a33e-a997-6fca39fb5bb5`, `88758ccf-dd70-1a36-937c-3e2283303bb4`,
  `a4dc3899-89f9-0338-a369-019b7165b2ec`, `e0060c87-5091-f633-8d88-9b501f0d4998`,
  `da0b14ff-8ec4-8335-8ca5-109108abe114`

### Formula 1 Brazil — 5
Reason: The complete 2026 F1 grid lists no Swedish driver.
IDs:
  `e2773354-41c3-ba33-9883-cbf5a25427d9`, `7789eafc-5c53-ae37-b766-c79bfaec4a31`,
  `12f1d08c-330d-f33a-951b-e6faa59b10ab`, `d8d3f843-e8c1-613c-934b-1814337574a1`,
  `9e3b2a0b-0ad1-e53d-8a1a-96cb8a483a84`

### MotoGP Qatar — 7
Reason: The complete 2026 MotoGP class grids list no Swedish rider.
IDs:
  `a41be5bc-a470-fa38-9897-b4a91ee71a78`, `e68d07a1-686f-4435-817c-c4c40f74f994`,
  `4ad3ab37-51f8-e831-9830-177065ceaa12`, `b5955a2c-1c4d-903a-a6c6-eca16463e140`,
  `83518196-56d8-bf3f-bc35-9bc23fa1a59f`, `803bfae7-4753-293e-acb0-645541f3be2f`,
  `2cfee868-66e6-1e3b-b433-ae482055ffa9`

### YOG non-Swedish sports — 2
Reason: The SOK roster lists no Swede in artistic gymnastics or beach wrestling.
IDs:
  `c32c490f-ee16-113e-ba68-efdf91b9bdb5`, `492fa3cb-796a-c236-848e-dab346ad9215`

### Cup of China — 1
Reason: Official ISU discipline entries list no Swedish entry.
IDs:
  `4f19186b-9d66-7532-8e4b-be2928136f14`

### TOTO Japan Classic — 1
Reason: The official player field lists no Swedish golfer.
IDs:
  `14df8bb9-b2b2-943d-9026-46aa9dbdace9`

### ATP finals — 4
Reason: Official final draws list no Swedish participant.
IDs:
  `066dc0ae-a79e-833e-8fa5-349ad29e8384`, `58bd9107-0be0-4f38-81c2-657838ce217c`,
  `5c6af339-1b4c-333c-a086-4ba1e710b99d`, `bb1c5377-b355-673a-996e-34648f31a79c`

### Rejection source checks

- [F1 driver list](https://www.formula1.com/en/drivers) and
  [MotoGP riders](https://www.motogp.com/en/riders/).
- [SOK](https://sok.se/pressmeddelanden/2026-06-25-truppen-till--ungdoms-os-i-dakar-2026.html).
- [ISU women](https://results.isu.org/events/gp2026/gpswomen.htm),
  [men](https://results.isu.org/events/gp2026/gpsmen.htm), pairs, and dance entries.
- [TOTO field](https://toto-japan-classic.com/en/players.html).
- [Vienna schedule](https://www.erstebank-open.com/en/news/storys/vienna-2026-schedule) and
  [Swiss Indoors draws](https://www.swissindoorsbasel.ch/de/handout/tableau-today-tomorrow.php).

## Unresolved broadcasts — 84

All remain visible and unprocessed. Organization markers are shown with each group; the UFC
listing has no matching entity.

### ATP Tour — 32; entity `e707c3dd-1660-49a0-b770-41c11ae8e900`
Reason: Paris session-to-player mapping was uncertain; qualifying remained open at snapshot.
IDs:
  `17afb9f1-2fe7-d93a-9746-88e67574b540`, `1a659cdc-7f42-a43c-849f-1bafaf79e9c5`,
  `1d749c16-ebad-fb31-b326-983ff4cd6fd8`, `2206cf10-ba3e-ff30-ad67-fb13c5063d00`,
  `22d960f2-8618-3135-bfea-bdf682b73abd`, `2a358a2b-ae3f-f03e-a5ef-e475c5cd931e`,
  `2b81c0a7-7426-883c-9811-514f4c4af36d`, `423ea488-d3fc-1a39-b6fd-69e9beb184cd`,
  `426f52c3-3f50-5f30-96b4-32a168ecc6b1`, `46e35311-4d1e-1f38-a945-c55e6d615665`,
  `48b37d60-9723-8b39-a04d-060317a9c61a`, `49af8fd5-9753-e03e-a3d6-9101a40a8ae3`,
  `53e3e171-dca8-8d35-a075-7ec3d0a473bc`, `5d49e34e-eb21-6030-bbd5-66cb37f0cb19`,
  `6e8e02a3-a89b-c834-9386-06131a3b86b4`, `6f010bdd-4251-2d3d-b04b-db2abfb12a3b`,
  `77864914-d88c-6a38-b494-c3542db4462c`, `7c143164-4d77-3a3d-8874-4fb93566984d`,
  `8079b5ec-69bc-273a-bd32-8b74f02ffd82`, `90018c10-1100-fc3f-898c-2fd523c2b701`,
  `96d21ab0-33a7-3235-918f-d0eace29e737`, `98fc23e9-bc40-bd34-a3fb-8970e1c684f0`,
  `a1119652-d972-6e32-bdcf-7d40e1b506d6`, `a3cdd690-f775-1331-99cf-40a7a81258bf`,
  `b994d62b-dff6-443d-9a17-bb47a0f66c81`, `b9cc0343-af6c-6338-83d9-6d7a90012b0d`,
  `ba1f53ee-3598-0c39-802a-b79335e5573d`, `bce5492e-10cd-3439-978e-1732e353bdf0`,
  `dd0cec50-f2e1-603c-9992-071e67505add`, `e2231b3a-50fb-3e3c-8a97-05ea37b26fea`,
  `f0a0e918-f503-6237-a4b3-ef3a0fa8e817`, `f1a019c2-6dcb-d93a-8954-d62a199ea593`

### BMX Racing — 1; entity `a3955e18-62e5-42bd-ad64-073125e4cc8a`
Reason: The exact Sarasota event and Swedish rider mapping were unverified.
IDs:
  `8facd66b-b7fb-e93c-849f-4ecb6fc18800`

### Cycling — 6; entity `467a3c47-a54d-47ea-8aa4-a8742551ea4f`
Reason: Complete entries or exact Swedish participant/feed mapping were unavailable; one Giro
        listing conflicted.
IDs:
  `126fde57-e4c4-2735-88bd-b38a7df0c20e`, `243acf5f-ccd6-fb35-9cbb-587c9f818c0d`,
  `43de7f25-89ea-de35-876d-2b92d99bbdf1`, `99757053-53ba-d236-97b6-117bcbe6ef5d`,
  `a5da8a46-6b07-2b34-affe-d9ce80018785`, `f532a367-bab2-ee34-a267-88bb003e39fb`

### GT World Challenge — 1; entity `a07ab026-6ffd-4820-8d29-a55fcda878a9`
Reason: A complete Swedish driver list and exact race/feed mapping were unavailable.
IDs:
  `a8af247b-1a35-df3a-9557-8df24cbb249f`

### Global Champions Tour — 5; entity `3237ea4c-5798-4cbf-9a0b-670b51b28a4b`
Reason: Swedish entries and exact feed mapping were unclear; one Rabat listing conflicts with GCT.
IDs:
  `64523980-29d5-ef37-aa10-39fbf8e458ec`, `90b01838-92b4-0434-b61b-b73ef7eb08b9`,
  `cbe15e23-88d3-8f32-a162-82c5caba4407`, `e444fdec-7dbe-c43f-9230-ca48e71ea95b`,
  `fd5283db-e45f-003b-9c91-f5c95c765d7d`

### Ladies European Tour — 3; entity `b209cad2-8532-45e7-ba63-2a064dc4460d`
Reason: The full Aramco China field and Swedish entrant status were unavailable.
IDs:
  `5d287d00-7eb9-333b-b181-7eee04fe29b6`, `a21dad6c-4781-d63f-9b9a-dd539edd8560`,
  `dd6bcc69-cc2e-5530-ac2c-1abd0cd13f68`

### PGA Tour — 1; entity `9126e0d1-7944-4aa7-89cd-e5b6a020dd02`
Reason: No verified Swedish player-to-feed mapping for the Simmons Bank Open.
IDs:
  `4d02005c-705e-5931-a0f5-91fd64cc25d8`

### Table Tennis — 1; entity `690d9a73-4396-451b-85c0-e150072714c4`
Reason: Generic final listing; Truls’s participation depended on the semifinal result.
IDs:
  `d210e421-ad59-9e3d-8280-e59496a65690`

### UFC — 1; no entity
Reason: Bonfim–Brady was canceled; replacement card and Swedish scope were unverified.
IDs:
  `6e20929a-1300-6b30-bc00-0b4ab92446a5`

### World Snooker Tour — 19; entity `b3a41eb0-1d5c-4c4d-9386-1766ac97e2c5`
Reason: Exact Swedish player, round, and feed mapping was unresolved.
IDs:
  `0b8371ac-010e-8835-a8c2-cf918f48ac1a`, `277e5f7a-7842-4638-bab1-dbeac9678843`,
  `291cae35-28ab-3b3f-b1c6-4676f86248a8`, `29d2df4a-279c-453f-b2a5-7e1a41888db1`,
  `2e887f04-8243-be3e-b4e2-d4217ea9b658`, `393d934b-8c58-e931-b5b7-ef8b7b6a7d6e`,
  `396f3c9b-7523-c736-bee2-6aeee5fb11b5`, `3e7a4274-b43f-113f-bfe3-ff9d4588b4ab`,
  `6128a3df-5209-223d-881c-57b6df703c78`, `622883ca-f03a-2339-8c8f-79af65e038d8`,
  `6a7371b5-1dea-f733-985c-fd5283df9bcf`, `6e0322fe-0bb9-5339-a709-148421ec8772`,
  `7d3f1d35-c780-603c-88d1-2fb54fdf528b`, `92ade674-4df7-0e3d-873c-ccb1f51cb77a`,
  `a1369764-9a24-233a-8d65-60c0d41c913f`, `c29c2f3c-f5ef-8732-a1d7-0270dab60996`,
  `cb6e4808-b0b4-163c-ae89-6b5a83436ade`, `f3123484-39f5-9c33-8c5a-440879802928`,
  `f86e4d60-d6e0-4134-99fb-25c54ef265c2`

### Youth Olympic Games Dakar 2026 — 14; entity `9dcc94d4-352e-4dc8-9b00-c94c2a25f04d`
Reason: Broad listings lacked exact disciplines and Swedish athlete mapping.
IDs:
  `4cc48470-b5f3-7a30-a6a5-868cfa88f726`, `4d51de4d-b68c-7a31-b04e-3baf66fa863f`,
  `4f808200-0bda-e037-b3fc-4aa505eb5efb`, `79938eb9-11ae-af32-bae5-4fdc64184b5f`,
  `8149dfee-b5c2-0d3d-bb04-b6ffb1652c24`, `8547543e-747b-3b3f-be41-4ef9bad78c9c`,
  `8cf894bc-c9a3-5033-a7ed-55da021f03e3`, `9ad263f5-5baa-1637-9b58-b725a936c9be`,
  `9baf9990-641f-2c34-bf16-9246da16aa3e`, `9ec107cf-f3ac-713f-992f-b8837d982782`,
  `ce0ac364-3bae-7d30-b49c-c4800a20cd94`, `d368d22c-a99f-8533-b766-fe113a29b01b`,
  `e49835ad-c976-e537-ac8b-8a383967de9a`, `ea547f4f-6d92-9e31-87e9-4a16ba70c891`

### Unresolved source checks

- [UFC cancellation update](https://www.ufc.com/news/ufc-fight-night-bonfim-vs-brady-updates).
Other uncertain event and source mappings remain documented in the [Oct 31
report](activities-2026-10-31.md). No complete official entry or exact broadcast mapping became
available in this run.

## Public verification

All affected pages on https://sesport.se returned HTTP 200. Dates and card counts below were
captured before and after the transaction.

- 2026-11-01: 6 → 9 cards; three new group cards: women’s curling final,
  WTT semi-final, and men’s curling bronze match.
- 2026-11-05: 4 → 4 cards; no change.
- 2026-11-06: 3 → 3 cards; no change.
- 2026-11-07: 0 → 3 cards; Abu Dhabi, WWT, and NOCCO Hockey Games.
- 2026-11-08: 0 → 0 cards; the WWT round ends at midnight local time and
  remains grouped under its 7 November public date.

The new cards show Swedish titles, group context, linked participants, ages, clubs, profile links,
and channel schedules. All 10 new Nov 1 competitors and all 28 Nov 7 competitors rendered as
expected. The 10 existing tier_0 stars were correct. Six requested flag SVGs returned HTTP 200.
Curling’s parallel channel segments render together as one grouped card.

## Dashboard

No open todos or current AI alerts. The latest import completed with 148 imported rows.
Two existing Round 4 activities still need verified participant start times:
`103e3bbe-24f2-4cf9-8341-6d1956cc5a35` in Rolex Grand Final and
`4871f47c-e582-43f7-94ae-df3f4bbc68df` in Mexico Open. The HotelPlanner Tour page exposed no
tee-time rows; PGA TOUR snippets did not unambiguously identify the Round 4 time. No unsupported
times were added.
- [HotelPlanner](https://www.europeantour.com/hotelplanner-tour/grand-final-2026/tee-times).
- [PGA](https://www.pgatour.com/tournaments/2026/vidantaworld-mexico-open/R2026540/tee-times).

## Validation

Readback matched all 163 outcomes, 10 activities, 55 person links, 17 broadcast links,
and 32 evidence sources. Rejected IDs changed only hidden_at; unresolved IDs remain visible
and unprocessed. The final report passed the 100-character line limit.

## Follow-up — YOG beach volleyball withdrawal

Applied: 2026-11-01 10:51:23 UTC.

SVT reported that Henrik Brus became ill and that the Swedish pair withdrew from the tournament
before the 1 November group-play broadcast. The article appeared at 08:10 Stockholm time, before
the activity began at 08:25.

- Drafted activity `aaf8e3ea-d68a-40d8-9c55-0dfc1bda518f` for 1 November and
  cleared `published_at`. Removed its two participant links: Casper Tjernqvist
  (`57a861b1-d7f8-47e0-87f8-d9558855ce42`) and Henrik Brus
  (`7b5e8596-01a4-40e3-a212-1b4172a7ff40`).
- Kept the 31 October activity `04e66d6b-3c8c-4081-b444-e7d0bbd18b0c` published
  with both participants; the official Turkish report confirms they played that day.
- Hid four future beach-volley broadcasts by changing only `hidden_at`. They remain
  unprocessed and unlinked:
  `8cf894bc-c9a3-5033-a7ed-55da021f03e3`,
  `d368d22c-a99f-8533-b766-fe113a29b01b`,
  `9ad263f5-5baa-1637-9b58-b725a936c9be`,
  `9ec107cf-f3ac-713f-992f-b8837d982782`.
- Added three `ParticipationEvidence` sources to the beach-volleyball group: the SVT
  withdrawal article, Swedish Volleyball’s match schedule, and the Turkish federation
  result.
  [SVT](https://www.svt.se/sport/beachvolleyboll/svenskarna-drar-sig-ur-ungdoms-os-efter-sjukdom)
  documents the withdrawal.
  [TVF](https://tvf.org.tr/icerik/plaj-voleybolu-erkek-milli-takimimiz-isvece-maglup-oldu)
  confirms the 31 October match.
  Swedish Volleyball’s schedule is also stored as group evidence.

The original 163 broadcast IDs now read back as 66 rejected, 17 processed, and 80 unresolved. The
mixed-sport SVT YOG listings remain unresolved because they may still cover Swedish competitors in
other sports.

- Public page 1 November: 9 → 8 activity cards; the pair no longer appears.
- Public page 31 October: HTTP 200; both players remain on the completed activity.
- Public page 1 November returned HTTP 200 after the update.


