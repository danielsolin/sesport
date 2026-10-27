# Activity review — 2026-10-26

Started: 2026-10-26 10:50:45 UTC

## Reconciliation

- Snapshot: 107 visible, unprocessed broadcasts.
- Final: 63 directly hidden, 12 linked/processed/hidden, 32 visible/unprocessed.
- All 107 snapshot IDs are accounted for; no visible processed broadcasts remain.
- Direct exclusions changed only `hidden_at`.
- All 32 uncertain broadcasts retain event organizations and remain unprocessed.

## Activities and groups

- Hammarby – Spartak Myjava, group `fc322b89-c0a1-43aa-9f6a-ade99a820318`.
  Group dates: 2026-10-28–2026-11-11.
  - `ac00ed20-7e34-4033-978b-3007b74ad198`: Första mötet, 17:45–19:45.
    Broadcast: `27f16d0e-cde7-0932-861a-f50ec9c64ef2`.
- Wistron Ladies Open, reused group `7b24f6a9-cb6f-451b-9f6b-217ae2aa93fb`.
  - `58c486c0-8134-4d04-a24f-8015eec548ea`: Sista rundan, 06:00–09:30, Viaplay.
    Broadcast: `b2e1226a-dab5-0735-abe1-e9fdb2a9e0d5`.
  - `eea62fd0-1434-454c-883e-a540c79c21b3`: Sista rundan, 07:30–09:00, V Sport Golf.
    Broadcast: `bd5a0394-3012-8c3d-936a-0fcecf47b051`.
- Rolex Grand Final, reused group `d8612f47-1d2c-4781-a13c-93974d6d0566`.
  - `103e3bbe-24f2-4cf9-8341-6d1956cc5a35`: Runda 4, 11:30–17:00, Viaplay.
    Broadcast: `233b8464-43ca-7532-8f7d-a0ce8cc3340f`.
  - `bf41ec2f-b627-4e27-9109-1fad7c912079`: Runda 4, 11:30–16:30, V Sport Golf.
    Broadcast: `7592df85-82ea-7b3f-b454-359fab547487`.
- Mexico Open, reused group `97a27dc4-f674-4ae0-895a-967465037a7b`.
  - `4871f47c-e582-43f7-94ae-df3f4bbc68df`: Runda 4, 21:00–23:55.
    Broadcast: `baf33a02-afa1-6634-a734-0305db70176e`.
- New York City Marathon, group `b0bd7eb9-0439-448c-bfc3-1713e57be81d`.
  - `23ca8afb-63b5-46a5-8cd0-a0dc029d6f67`: Herrarnas elitlopp, 15:05–17:10.
    Broadcast: `5a00cb1e-210e-923e-9de4-ae5528034ce5` (Eurosport 1).
  - `9466003f-c4a5-4305-aea2-2742d3d0e65d`: Herrarnas elitlopp, 15:05–17:15.
    Broadcast: `3560b2e6-7b25-6230-afb8-d7678cfb2096` (HBO Max).
  - `40457117-e3ca-4a64-a10b-88bea63f3f91`: Herrarnas elitlopp, 15:05–17:30.
    Broadcasts: `e54992a1-661b-0730-8d59-e115fcb6567b`,
    `de11fd54-76bd-ee32-9895-d9cc88a711c4` (Kunskapskanalen/SVT Play).
- Världscupen i hoppning, Lyon, group `2d739401-c86a-4f83-a5e3-61b6f376e115`.
  Group dates: 2026-10-27–2026-11-01.
  - `362dffd1-10cd-4405-bfc4-4fb5c24bd4aa`: Grand Prix, 15:15–18:00.
    Broadcasts: `a1e98179-8d25-2d3c-9356-93977f153ccf`,
    `834f24ee-09b6-6c31-a99c-b2b006e3ead5` (Sjuan/TV4 Play).

All 10 activities are published and grouped in `sport_day` groups. The 12 linked
broadcasts now point to their activity organizations and are processed and hidden.
The 33 participant links cover 26 unique people. Hammarby's official UEFA squad
supports its 16 players; the other participants are supported by event entry lists
or official programme descriptions. All displayed people have birth dates, clubs,
and profile URLs. Andreas Almgren's URL now points to his World Athletics profile;
his `tier_0` priority was retained. No star changes were supported for other people.

Andreas Almgren's 15:05 start is recorded on the three marathon activities with
`ParticipantStartEvidence`. The link is visible beside the time on the hosted card.
The official NYRR timeline lists the professional men's start at 9:05 a.m. ET,
which is 15:05 in Stockholm on 2026-11-01. The three activity windows start at
that race segment and end at each linked broadcast's listed end. Golf tee sheets
were not published; no player-specific golf details or guessed start times were
added. No stars or activity detail records remain unresolved beyond that limitation.

Direct stream links were copied from linked broadcasts where available. Remaining
providers resolve through the channel catalog. Hosted stream links, participant
profiles, ages, clubs, titles, grouped schedule rows, and time ranges were checked.

## Directly hidden broadcasts

These records were visible at snapshot time. Each ID below was changed only by
setting `hidden_at`.

- MotoGP 2026: complete FIM rider licences contain no Swedish riders.
  All 10 have `ParticipationEvidence` for the FIM 2026 permanent rider list.
  - `6c103225-73d5-9439-b5ab-a40599d789e4`,
    `9af3061f-383f-9536-ab34-5d5878665451`
  - `46a88e67-bc3e-163e-9122-6af1bf3ef506`,
    `5b184bf7-34c4-6b32-a1f4-757bb5af3ba6`
  - `19c13e76-400a-4d3c-9294-2a2d2480afd0`,
    `9e0b9266-405f-d131-a1be-2c329f35df70`
  - `22427390-1e90-d934-b18a-a24b1a402380`,
    `7ee8fb16-5eaa-7037-8421-7d9cdcd0aed7`
  - `6999c1c5-b46e-023e-910b-9eceba19e128`,
    `ccb4b4ad-fa13-9938-8cd5-f54d44d9988a`
- Formula 1 2026: the complete grid has no Swedish driver. Nine IDs have typed
  `ParticipationEvidence` from the official
  [Formula 1 driver list](https://www.formula1.com/en/drivers).
  - `dde882bf-241c-8f3b-b670-79de98c89a79`,
    `60af2c1c-de08-d43f-a81e-d7b7bd79d0b4`
  - `0498be1a-90f5-2e3f-b16d-3c87e9c9d9bf`,
    `5c2c965e-a7c0-3b3c-8794-97fde242dfab`
  - `f12b5a4e-1ddc-c23a-971b-d5e8a1171079`,
    `430e960c-80e0-c63f-a738-7d545ca4ce8f`
  - `36c4b85c-140e-4235-b700-c653c6b3d639`,
    `d4c7793f-d62f-9f37-befd-5dc7d26782fd`
  - `9fe08c75-23ea-2a36-b018-8d9a911f622f`
- Figure skating and short track: complete official entries contain no Swedes.
  - Skate Canada: `613eb38c-4d92-443e-9687-b6a16aeb7884`.
  - Montreal short track: `23103554-1c2b-d43d-a148-c24f1bbef395`.
- Vienna tennis: the official 2026 ATP draw contains no Swedish player.
  Both records have typed `ParticipationEvidence` for the official
  [ATP draw](https://www.atptour.com/en/scores/current/vienna/337/draws).
  - `855e1eb6-6e1c-6f37-9645-b865d76449db`,
    `7a2c3098-c36f-8f39-abc2-dd128848d300`
- Domestic Swedish competitions (21):
  - `49b8c2bf-1db1-fc38-93b1-df5dceeb0bb9`,
    `529aef72-5239-b932-81f0-7cde10c16909`
  - `0d710c67-8f70-2133-864d-e702af8bc040`,
    `e0dfcf08-35fa-1834-9eac-b657deb91078`
  - `424955a0-6008-5433-bcd7-c7c25a13f0ab`,
    `8eed8818-cb6b-fe37-ae71-97e4dfd6a355`
  - `c4a6f710-5902-e53b-b7f3-f1ad9df34b24`,
    `276c9936-5ff9-fd34-8880-248d03fc3022`
  - `e41e7ee8-fa0a-ff38-b3e5-c898acebccde`,
    `a4e5f6d3-1bc0-7237-bd7f-d659db7c8ad1`
  - `3e306e5e-dd98-1931-b02b-36da03b6fdf7`,
    `e4a7917a-a1ad-223e-8176-1ecdd7713b93`
  - `035c23cc-c495-2938-9d3e-c85cd4e47a91`,
    `56d88d47-cb24-9438-b2fa-8f1688a4bd63`
  - `f5744f45-7e83-8430-9ca4-bea13a2bd598`,
    `50a9aeab-cdd8-0b3d-8a7c-7b6fd0a96466`
  - `2d7a3e55-b69e-9c3d-b5dd-4f93418893d4`,
    `9de8070b-9d13-bf35-a4ad-78bc2e25fe24`
  - `e1fbbfa2-892e-6b37-956d-a98d6d3070a2`,
    `9eac4a50-fefb-b33b-bfcc-433013f27589`
  - `5245326f-c189-2b3f-9f0b-02b6d8d4c2e4`
- Foreign club or national-team fixtures (15):
  - `3b667cab-bbfd-3139-af21-7f1676636929`,
    `cc19b2fd-9bdf-2635-97a8-27f9b11bab2d`
  - `9f20d77e-d4ff-ca33-8455-edfc42094825`,
    `cf9a7341-b356-d23d-b235-c71bafb25d07`
  - `0d77901d-fd34-1b31-a8e5-32d47b82f6fa`,
    `2997ff87-c7ff-9a3a-ad53-c94b4a93beba`
  - `dee53661-79bb-4f33-9304-7120e0cbfc9b`,
    `ef661baa-d88f-3738-b511-901bafa4a69c`
  - `ef998cb3-6605-ba39-8d25-42b73b8fb4d4`,
    `50d19685-95d5-cd3f-be62-7ebdb47a7e44`
  - `6ca3c820-23dc-7239-82ba-c180a16e4ef7`,
    `9199a569-bb82-c937-85bb-03553629cc43`
  - `13a58be0-6a1e-8836-af67-f1042e2e8d35`,
    `c27d0d4f-aa3d-4136-af19-113b1e50eb76`
  - `b56a4ae5-1c51-ca35-9f82-c603272bb34f`
- NHL studio recap: `c448a37a-db53-2f36-95e3-ddbd0433fc25`.
- Youth Olympic Games opening ceremonies:
  - `f8ac5a8c-79bb-c83f-a43a-ee72068e60ba`,
    `ce5574f0-6ee1-5d32-ae11-9de99a4d8f7a`
- YOG swimming feed: `89453fb8-2c53-c339-ab79-64116203d22c`.
  The SVT guide's published Swedish roster has no swimmer; typed
  `ParticipationEvidence` was saved with the official
  [SVT guide](https://www.svt.se/sport/beachvolleyboll/guide-ungdoms-os-2026).

## Visible, unresolved broadcasts

All 32 IDs below remain visible and unprocessed. Their `entity_id` values match
the listed organizations.

- Climbing (1): `098b23da-f78c-9438-9b9c-1565855ddbd9`.
  Gunsan participation could not be conclusively assigned to Swedish athletes.
- Curlinglandslaget (dam) (6):
  - `343413b3-cd9f-ed33-ae08-b69c7f09a3cb`,
    `818534eb-5211-053d-bbe3-535654eecb83`
  - `7289dbae-cf7d-273b-b714-8fd5a30cfe75`,
    `d4ec8a74-5135-ae32-b505-d65f10543313`
  - `5d616d09-d2cc-5e35-9f8e-e04717288382`,
    `f0a45f0a-61cb-bc3a-857f-87e958059650`
- Curlinglandslaget (herr) (5):
  - `4cbb3607-4322-6b31-be96-fa2685c56118`,
    `d83c2292-2354-a137-ba70-187a3820ac27`
  - `c6eb28ef-0e1c-6f36-ba7c-6760ed9dd3a4`,
    `8d821da4-b599-3f3b-b3c4-119626bd60d2`
  - `65b700a4-6943-8e37-a1cd-a7f96465a44b`
- Global Champions Tour (6):
  - `5ca64ee7-d845-b636-8f3c-b584321f57a6`,
    `0dda1ee3-3158-9a35-83f3-31b711c36514`
  - `222e1a3c-fb99-4838-ac55-b55b11d016e6`,
    `90b01838-92b4-0434-b61b-b73ef7eb08b9`
  - `e444fdec-7dbe-c43f-9230-ca48e71ea95b`,
    `cbe15e23-88d3-8f32-a162-82c5caba4407`
  Rabat is confirmed by the official GCT schedule. ID `90b01838-92b4-0434-b61b-b73ef7eb08b9`
  has an HBO title naming an unrelated Mohammed VI athletics event; exact stream mapping
  remains unresolved.
- World Snooker Tour (6):
  - `5755ecf6-eaf1-b136-a32b-286e29464169`,
    `e3472469-6595-6d3d-814f-e84bebde3cd6`
  - `1447cd13-d141-d038-bf7b-5d1457ffa7df`,
    `396f3c9b-7523-c736-bee2-6aeee5fb11b5`
  - `393d934b-8c58-e931-b5b7-ef8b7b6a7d6e`,
    `7d3f1d35-c780-603c-88d1-2fb54fdf528b`
  Draw coverage did not establish Swedish participation for the listed sessions.
- Youth Olympic Games Dakar 2026 (3):
  - `c1e08dd6-2ed9-3f36-99f3-6474bcf3dcd6`,
    `b619cff4-94ea-6e39-903c-ebe76bf5f285`
  - `8547543e-747b-3b3f-be41-4ef9bad78c9c`
  Beach-volleyball streams are not mapped to a Swedish match; the SVT listing is
  event-wide.
- BMX Racing (2): `08aa1629-4ad9-d434-89d7-925ae97f5d60`,
  `8facd66b-b7fb-e93c-849f-4ecb6fc18800`.
  The UCI calendar confirms Sarasota; Swedish participation remains uncertain.
- Cycling (1): `a5da8a46-6b07-2b34-affe-d9ce80018785`.
  The official schedule confirms Koppenbergcross; its entrant list was not conclusive.
- ATP Tour (2): `58bd9107-0be0-4f38-81c2-657838ce217c`,
  `bb1c5377-b355-673a-996e-34648f31a79c`.
  André Göransson is in the doubles field; final participants were not yet confirmed.

Event and participation sources for uncertain broadcasts were saved with typed
`ActivityEvidence` or `ParticipationEvidence`. No unsupported detail times were added.

## Public and admin checks

| Public date | Cards before | Cards after | Change |
| --- | ---: | ---: | --- |
| 2026-10-28 | 6 | 7 | Hammarby – Spartak Myjava added. |
| 2026-11-01 | 0 | 5 | Wistron, Rolex, Mexico Open, NYC Marathon, Lyon added. |

Both counts were read from `https://sesport.se/?date=YYYY-MM-DD`. Cards with parallel
channels remain grouped once per event. No other dates were changed.

`https://sesport.se/Admin/Dashboard`: “Activities needing attention” showed
“No upcoming activity issues.” No dashboard fixes remain.

`git diff --check` passed. Automated tests were not run under the job instructions.
