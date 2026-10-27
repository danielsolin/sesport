# Activity review — 2026-10-27

Started: 2026-10-27 07:48:14 UTC

SportDay: 2026-10-27, derived from the database in Europe/Stockholm.

## Reconciliation

- Snapshot: 62 visible, unprocessed broadcasts.
- Final: 6 Processed, 17 Rejected, and 39 Unresolved.
- Every snapshot ID has exactly one outcome. Unresolved records remain visible and
  unprocessed, with an organization marker and typed source evidence.
- The 17 rejected records are hidden. Their IDs and reasons are listed below.

## Processed activities

- WTT Champions Montpellier, new group `10ff8d58-2a4d-44d8-b1f8-7b66068c14c4`,
  2026-10-27–2026-11-01:
  - `765a7144-4d77-4d87-8bc9-e87b1b697098`: Anton Källberg – Elias Ranefur,
    12:30–14:00, SVT Play. Broadcast `4951c043-5932-5136-8383-fc67deb03f73`.
  - `0d1a5f25-46fb-4f26-97b9-b893fe2ec12b`: Truls Möregårdh – Thibault Poret,
    20:45–22:00, SVT Play. Broadcast `86bc6720-ad7d-9138-a332-fd55353a0a65`.
  - SBTF confirms participants and match times; SVT confirms both broadcast windows.
    Activity, participation, and direct StreamLink sources were saved.
- Lidl Hockey Games, reused group `47008f6b-746b-4d45-9569-3e00fb3dc11e`,
  2026-10-28:
  - `7f3e130e-ed08-4a58-89fe-db5c2655da1c`: SVT2 segment. Broadcast
    `533bbfbe-ca0c-d933-a317-cca776680431`.
  - `e3e84205-0209-458d-8ca0-d2dd53bc01a8`: SVT24 continuation. Broadcast
    `e51709c8-1bd3-3e39-b0ac-84b60930e233`.
  - Reused the existing published activities, complete Swehockey roster, and TV.nu
    time evidence. Both broadcasts now point to the matching activity and team.
- Maybank Championship, reused group `71e0f20c-53e9-4b3c-98e0-ec50399f0382`,
  2026-10-29:
  - `ddee35bd-3875-4654-b8ac-f68587fb3417`: Runda 1, 02:30–07:30,
    V Sport Golf. Broadcast `7996d0c4-5d2c-693f-9c58-8d4770f52e5a`.
  - Reused the published activity, LPGA entries, and existing Viaplay segment.
- Curling-EM, reused group `03733f6c-77a0-476a-820a-c69ac24aa590`,
  2026-10-31:
  - `9d075743-2dd5-4014-a571-fd1eddaccee4`: Sverige – Polen,
    09:30–12:30, HBO Max. Broadcast `d83c2292-2354-a137-ba70-187a3820ac27`.
  - The Swedish Curling Federation schedule confirms the match and the men's roster.
    Activity, participation, and direct StreamLink sources were saved.
- Existing people, birthdates, clubs, profiles, and star priorities were reused.
  No person records or star values changed.

## Rejected broadcasts

Step 2 exclusions changed only `hidden_at`.

- Foreign fixtures: Germany–Netherlands curling:
  `2fe40e77-8170-b83c-a42e-a5c2b0780e16`, `91d48927-d767-2937-b2be-4b8f330aa774`.
- Foreign club fixtures: Casa Pia–Sporting:
  `2b00680d-970a-753a-838f-9128af12f5d3`.
- Foreign club fixtures: Benfica–Alverca:
  `755acd52-6846-0532-b5fa-17f7bc6a1274`, `b31c78dc-8e35-613c-9d19-9cde80c15e0d`.
- Domestic Swedish competitions: handball `56f0c8ad-9a07-7f38-a3cb-d6a2767c819d`;
  basketball `d1e49827-cd5d-f839-ae64-5ca17434046c`.
- Studio or recap programmes:
  `79cdf028-bb9b-f437-bccc-a0fe714caf20`, `12ddcca6-eea8-d930-86e5-d7a5bf083f5f`,
  `d8cd63ae-0ee4-3630-a0b1-08237a36c7d5`, `cc6dc8ee-cf8c-0b36-b695-5d12d744311b`,
  `ed80f342-b4e6-133b-9d2f-460c7b573968`, `6b7d665b-9a07-e631-b468-464934c7e588`,
  `c1244348-8be4-053f-abd9-a89cb4b2c009`.
- Vienna tennis: the complete ATP Tour 2026 draw has no Swedish player. Broadcasts
  `810707f4-c7bc-ff37-a2f5-e41e65917ab9` and
  `5e433c58-db27-da38-a3c1-560018a822a0` were hidden. Typed ParticipationEvidence
  links to the [official ATP draw][vienna] were saved.
- Curling men's 30 October 09:30 session: the official schedule has no Sweden game.
  Broadcast `4cbb3607-4322-6b31-be96-fa2685c56118` was hidden with typed
  ParticipationEvidence from the [World Curling schedule][world-curling].

## Visible and unresolved broadcasts

All IDs below remain visible and unprocessed. Each has the listed organization.

- Climbing (1), `Climbing`: `098b23da-f78c-9438-9b9c-1565855ddbd9`.
  The Gunsan event is identified, but available evidence does not establish Swedish
  participation.
- Global Champions Tour (6), `Global Champions Tour`:
  `5ca64ee7-d845-b636-8f3c-b584321f57a6`, `0dda1ee3-3158-9a35-83f3-31b711c36514`,
  `222e1a3c-fb99-4838-ac55-b55b11d016e6`, `90b01838-92b4-0434-b61b-b73ef7eb08b9`,
  `e444fdec-7dbe-c43f-9230-ca48e71ea95b`, `cbe15e23-88d3-8f32-a162-82c5caba4407`.
  Rabat dates are confirmed; no complete current rider list maps Swedish athletes.
  One HBO listing has an equestrian category but an unrelated athletics title,
  so its stream mapping is also unresolved.
- Curlinglandslaget (dam) (6), `Curlinglandslaget (dam)`:
  `343413b3-cd9f-ed33-ae08-b69c7f09a3cb`, `818534eb-5211-053d-bbe3-535654eecb83`,
  `7289dbae-cf7d-273b-b714-8fd5a30cfe75`, `d4ec8a74-5135-ae32-b505-d65f10543313`,
  `5d616d09-d2cc-5e35-9f8e-e04717288382`, `f0a45f0a-61cb-bc3a-857f-87e958059650`.
- Curlinglandslaget (herr) (3), `Curlinglandslaget (herr)`:
  `c6eb28ef-0e1c-6f36-ba7c-6760ed9dd3a4`, `65b700a4-6943-8e37-a1cd-a7f96465a44b`,
  `8d821da4-b599-3f3b-b3c4-119626bd60d2`.
  The semifinal and medal-game feeds depend on qualification not yet known on
  2026-10-27. The official schedule and roster are linked above.
- World Snooker Tour (9), `World Snooker Tour`:
  `5755ecf6-eaf1-b136-a32b-286e29464169`, `e3472469-6595-6d3d-814f-e84bebde3cd6`,
  `1447cd13-d141-d038-bf7b-5d1457ffa7df`, `396f3c9b-7523-c736-bee2-6aeee5fb11b5`,
  `393d934b-8c58-e931-b5b7-ef8b7b6a7d6e`, `7d3f1d35-c780-603c-88d1-2fb54fdf528b`,
  `f86e4d60-d6e0-4134-99fb-25c54ef265c2`, `6128a3df-5209-223d-881c-57b6df703c78`,
  `2e887f04-8243-be3e-b4e2-d4217ea9b658`.
  The official [WST calendar][wst] confirms the event; the available draw and session
  listings do not establish Swedish participation for these feeds.
- Youth Olympic Games Dakar 2026 (3), `Youth Olympic Games Dakar 2026`:
  `c1e08dd6-2ed9-3f36-99f3-6474bcf3dcd6`, `b619cff4-94ea-6e39-903c-ebe76bf5f285`,
  `8547543e-747b-3b3f-be41-4ef9bad78c9c`.
  The beach-volleyball feeds are event-wide and not mapped to a Swedish match; the
  [SVT guide][yog] is event-wide.
- BMX Racing (2), `BMX Racing`:
  `08aa1629-4ad9-d434-89d7-925ae97f5d60`, `8facd66b-b7fb-e93c-849f-4ecb6fc18800`.
  The [UCI event hub][bmx] confirms Sarasota dates, but the current entry list does
  not establish Swedish participation.
- Cycling (1), `Cycling`: `a5da8a46-6b07-2b34-affe-d9ce80018785`.
  The Koppenbergcross programme confirms the event; its entrant list is inconclusive.
- ATP Tour, Paris (6), `ATP Tour`:
  `2a358a2b-ae3f-f03e-a5ef-e475c5cd931e`, `7c143164-4d77-3a3d-8874-4fb93566984d`,
  `98fc23e9-bc40-bd34-a3fb-8970e1c684f0`, `b9cc0343-af6c-6338-83d9-6d7a90012b0d`,
  `423ea488-d3fc-1a39-b6fd-69e9beb184cd`, `8079b5ec-69bc-273a-bd32-8b74f02ffd82`.
  The official [Paris overview][paris] confirms the event; no draw or Swedish match
  mapping was available for these 2 November sessions.
- ATP Tour, Basel (2), `ATP Tour`:
  `58bd9107-0be0-4f38-81c2-657838ce217c`, `bb1c5377-b355-673a-996e-34648f31a79c`.
  André Göransson is in the doubles field, but the finalists are not yet known.
- Typed ActivityEvidence records were added for the six Paris and three new snooker
  feeds. All 39 unresolved broadcasts have at least one typed source record.

## Public and Dashboard verification

| Public date | Cards before | Cards after | Explanation |
| --- | ---: | ---: | --- |
| 2026-10-27 | 3 | 4 | One WTT group card with two match rows. |
| 2026-10-28 | 7 | 7 | Existing hockey and Maybank cards were reused. |
| 2026-10-31 | 7 | 8 | One Sweden–Poland curling card was added. |

All counts were retrieved from `https://sesport.se/?date=YYYY-MM-DD`. New WTT and
curling cards show Swedish titles, participants, ages, clubs, evidence, and streams.
The displayed flag SVGs returned HTTP 200 with `image/svg+xml`.

The hockey card groups the three stored feeds into one timeline row from 17:50 to
20:30 and lists SVT2, SVT Play, and SVT24. It does not show the separate 20:00
SVT24 continuation window. The database retains all three exact segments; the
hosted renderer's team-sport grouping hides their separate windows. Expose those
windows in a future application review.

The production Dashboard showed no upcoming activity issues, zero unreviewed
broadcasts, and no dark attention rows. AI runs showed zero pending, running, or
recent failed runs; the latest broadcast import completed at 08:38 UTC with 49
records imported. The related Broadcasts reminder todo
`2f222b4e-679f-4b29-a1b5-d3fa6a03e533` was marked done after the Dashboard check and
read back successfully.

## Evidence links

[WTT federation schedule][wtt], [SVT schedule][svt], [Curling Federation schedule][curling],
[men's roster][curling-roster], [Vienna draw][vienna], and [World Curling schedule][world-curling].
Other unresolved-event sources are saved as typed evidence against their broadcasts.

[wtt]: https://sbtf.se/landslag/svenskmote-direkt-pa-wtt-champions-montpellier/
[svt]: https://www.svt.se/sport/bordtennis/sa-sander-svt-bordtennis-2026
[curling]: https://www.curling.se/tavlingar--resultat/em-damer-och-herrar-2026
[curling-roster]: https://www.curling.se/team-sweden-curling
[vienna]: https://www.atptour.com/en/scores/current/vienna/337/draws
[world-curling]: https://livescores.worldcurling.org/ecc/aspnet/summary?EventID=1
[wst]: https://www.wst.tv/tournaments/
[yog]: https://www.svt.se/sport/beachvolleyboll/guide-ungdoms-os-2026
[bmx]: https://www.uci.org/competition-hub/2026-uci-bmx-racing-world-cup/3uBWKNkNHvUqz9CaSrtV7Z
[paris]: https://www.atptour.com/en/tournaments/atp-masters-1000-paris/352/overview
