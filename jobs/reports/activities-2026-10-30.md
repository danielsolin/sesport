# Activities run — 2026-10-30

Started: 2026-10-30 04:34:24 UTC.
Finished: 2026-10-30 05:19 UTC.

## Outcome

- Snapshot: 135 visible broadcasts across all dates and processing states.
- Final outcomes: 7 processed, 16 rejected, 112 unresolved.
- Readback matched all 135 snapshot IDs exactly: no missing or duplicate outcomes.
- All 7 processed broadcasts are linked, processed, and hidden. Their snapshot IDs are
  `c1123d30-670b-ac34-9563-23dfaa980fdb`, `87240527-69de-c73a-8ad4-b68fb7aeaa86`,
  `b50ed2d4-0299-f03d-92e4-aecc4169c9fc`, `1bb46a20-a828-ca3d-9265-67215c1808c0`,
  `988aafdc-f8f9-4936-8c05-15511746b64e`, `1e8c26f4-efdf-b632-a5b5-82f5c428d36e`,
  and `13997ac6-f205-cb3b-8c5a-988456ce4cd4`. The 16 rejected broadcasts are hidden and
  unprocessed. All 112 unresolved broadcasts remain visible and unprocessed.
- Of the unresolved broadcasts, 105 have an organization marker. Seven lack a matching
  organization entity; their IDs are listed below.
- Created five activities in two groups. Updated four existing golf activities with sourced
  start times. No code, assets, schema, configuration, or deployment changed.

## Created activities — 2026-11-05

- Group `Nocco Hockey Games`: `16874649-9970-5a55-b2a0-73d2f89a57d2`.
  - `ec2b1568-5193-58dd-988f-11085835976b` — Tjeckien – Sverige, SVT Play, 17:50–20:30.
    Broadcast: `c1123d30-670b-ac34-9563-23dfaa980fdb`.
  - `f026a196-d9d7-5212-b1c5-ae4d5b405c64` — Tjeckien – Sverige, SVT2, 18:50–20:00.
    Broadcast: `87240527-69de-c73a-8ad4-b68fb7aeaa86`.
  - `3ccad4f9-8d21-5fa9-9e1f-1623420e70e5` — Tjeckien – Sverige, SVT24, 20:00–20:30.
    Broadcast: `b50ed2d4-0299-f03d-92e4-aecc4169c9fc`.
- Group `EM-kval 2028`: `1bd6b218-b54e-5c34-8061-b392166603ad`.
  - `f3961c91-4119-5cc0-9292-65be98d0bf78` — Sverige – Grekland, SVT2 and SVT Play,
    18:20–18:50. Broadcasts: `1bb46a20-a828-ca3d-9265-67215c1808c0` and
    `988aafdc-f8f9-4936-8c05-15511746b64e`.
  - `be9254e0-bc2d-54a9-b121-b25c80f794b5` — Sverige – Grekland, Kunskapskanalen and
    SVT Play, 18:50–20:15. Broadcasts: `988aafdc-f8f9-4936-8c05-15511746b64e` and
    `1e8c26f4-efdf-b632-a5b5-82f5c428d36e`.
- All five activities are Published, use Europe/Stockholm, and have complete federation
  rosters linked: 24 hockey players and 17 handball players. The overlapping SVT Play
  broadcast links to both handball segments because its coverage spans both.

## Updated activities — 2026-10-30

- Rolex Grand Final group: `d8612f47-1d2c-4781-a13c-93974d6d0566`.
  - `f0d08f87-1882-4aab-a8b6-21c3d4d34ef7` — V Sport Golf, 12:00–17:00;
    linked broadcasts `13997ac6-f205-cb3b-8c5a-988456ce4cd4` and
    `ca248265-1f9c-aa35-8794-505b0ecfb405`.
  - `1b45a00b-33cc-4468-a620-f3641a81d33e` — Viaplay, 12:00–17:30;
    linked broadcast `f5bb9657-e33b-1234-915b-e3357a0aac6c`.
  - Both titles are `Runda 2`. Added displayed tee times: Christofer Blomstrand 10:07;
    Adam Wallin 10:19. Kept the activities separate because their stream links and
    broadcast windows differ.
- Mexico Open group: `97a27dc4-f674-4ae0-895a-967465037a7b`.
  - `80139332-645a-40a7-a95c-af6e0d3f6669` — HBO Max, 20:40–00:00;
    broadcast `840fb177-3dec-6833-91ea-b96fedb3beb7`.
  - `98ca2c99-8fbe-412b-ae74-8efc109a77f7` — Eurosport 2, 21:00–23:55;
    broadcast `eb11ba17-80b5-c639-8096-b8e1fef0e8e6`.
  - Added displayed tee times for both activities: Pontus Nyholm 16:11 and Jesper Svensson
    16:44, converted from PGA TOUR's Round 2 times in UTC.
- The HotelPlanner Tour's complete 45-player field has two Swedish entrants, Adam Wallin
  and Christofer Blomstrand. Replaced the stale six-player claim in saved field evidence.
  Existing participant links were already correct.

## Participant and evidence changes

- Created eight Persons: Adam Ollas Mattsson, Emil Mellegård, Filip Roos, Gabriel Carlsson,
  Isac Hedqvist, Jack Berglund, Melker Thelin, and Viktor Lodin. Added their verified
  birthdates, profiles, current-team links, and formative or earliest documented clubs.
- Added Team `Skjern Håndbold` (`d3909195-de77-58a4-a2ca-d3fc767f6e35`) and Club
  `FoC Farsta IF` (`e3bf8ea1-75d3-52bc-bbb4-67081b858def`). Linked Axel Månsson to Skjern
  and Jesper Frödén to FoC Farsta IF.
- Added Viktor Lodin's formative-club link to Leksands IF, supported by SVT Nyheter. Filled
  five missing birthdates and corrected seven existing athlete URLs to official profiles.
  Added or corrected team profile URLs where missing.
- Added 66 person, team, and club graph links. No Watch Priority changes were made.
- Saved typed evidence: 46 `PersonFacts`, 10 broadcast `ParticipationEvidence`, 7
  `ActivityEvidence`, 3 group `ActivityEvidence`, 2 group `ParticipationEvidence`, 4
  `ParticipantStartEvidence`, and 7 `StreamLink` sources.
- Roster, birthdate, club, team, and profile facts use official federation or club profiles.
  Event and schedule evidence uses the Swedish hockey and handball federations, SVT, TV.nu,
  Tvtider, the HotelPlanner Tour, Svensk Golf, Golf-live, PGA TOUR, and SVT Nyheter.
  Source URLs and excerpts are stored with their evidence types.

## Rejected broadcasts — 16

- `5057ed73-a0d9-b037-a692-a54899f76ba1` and
  `89b873d2-ce24-0f3f-a14b-80a8cde6ce25` — hockey studio shows.
- `0039891c-bed7-c735-80ff-d61659e6ba4c`,
  `c821a616-5f5e-533b-aab2-cf8ada27be8c`, and
  `740eb9cd-c4d7-8c31-bdd6-99958a9e4fd6` — domestic TV-pucken matches.
- `5dfec2aa-a494-333a-aeb6-c9899a6e8898` — Formula 1 race; no Swedish driver in the
  official 2026 grid.
- `90d26239-c97b-1735-a994-c868ea726a60` — foreign Ligue 1 fixture, Lyon–Angers.
- `217c9484-5394-2e3a-b8ec-71a215649b10`,
  `3aa1eff2-38d0-fd3d-a5e7-3d5358a1816e`,
  `993ee022-2a54-983b-ad16-2b53e791decf`, and
  `5689af1f-9cb7-773d-9347-323d7b3738b3` — generic NHL studio/highlight programmes.
- `5cc23859-7a33-ca3e-8d13-c01d7b581ad8` — Formula 1 highlights.
- `7094773d-8341-0134-a394-c23a9e00f8ee` — Formula 1 studio programme.
- `232b9100-c563-8d37-9db5-c69703d10de9` and
  `fa966da8-c2ba-a034-a2cc-dc3e0d5966ee` — football studio programmes.
- `131af98e-78a0-ed31-8ce9-420f997eca5f` — foreign NHL fixture, Colorado–Nashville;
  no Swedish club is involved.

## Unresolved broadcasts — 112

The 74 carried-forward IDs retain their group-specific reasons and sources in the
[29 October run 2 report](activities-2026-10-29-run-2.md). This run found no new evidence
for those items. The 38 new unresolved IDs below remain visible and unprocessed. Each has
an organization marker when one is identifiable; `no entity` means no matching organization
entity exists in the database.

- Wistron Ladies Open has three Swedish entrants, but exact participant-to-broadcast
  mapping remains unresolved; see its ID in the list below.
- Seven unresolved broadcasts have no matching organization entity; each is marked
  `no entity` in the list below.
- Other new items lack confirmed Swedish participation, exact event/round mapping, or
  broadcast coverage. Their organization markers and IDs are listed below.

### Carried-forward IDs — 74

See the 29 October run 2 report for the group-specific unresolved reason and source.

- `098b23da-f78c-9438-9b9c-1565855ddbd9` — Climbing
- `75f97fbf-9e55-ef34-b8a3-020f17651382` — Global Champions Tour
- `5ca64ee7-d845-b636-8f3c-b584321f57a6` — Global Champions Tour
- `0dda1ee3-3158-9a35-83f3-31b711c36514` — Global Champions Tour
- `112b9462-56fc-5e3b-aab5-db347df96e5e` — Skate Canada
- `5755ecf6-eaf1-b136-a32b-286e29464169` — World Snooker Tour
- `e3472469-6595-6d3d-814f-e84bebde3cd6` — World Snooker Tour
- `08aa1629-4ad9-d434-89d7-925ae97f5d60` — BMX Racing
- `1447cd13-d141-d038-bf7b-5d1457ffa7df` — World Snooker Tour
- `343413b3-cd9f-ed33-ae08-b69c7f09a3cb` — Curlinglandslaget (dam)
- `fc34ef77-65f9-e733-b3ef-0ca7add32602` — Curlinglandslaget (dam)
- `5d616d09-d2cc-5e35-9f8e-e04717288382` — Curlinglandslaget (dam)
- `81b2394b-a401-e438-9ad4-b6887600855d` — Global Champions Tour
- `222e1a3c-fb99-4838-ac55-b55b11d016e6` — Global Champions Tour
- `c6eb28ef-0e1c-6f36-ba7c-6760ed9dd3a4` — Curlinglandslaget (herr)
- `396f3c9b-7523-c736-bee2-6aeee5fb11b5` — World Snooker Tour
- `393d934b-8c58-e931-b5b7-ef8b7b6a7d6e` — World Snooker Tour
- `7289dbae-cf7d-273b-b714-8fd5a30cfe75` — Curlinglandslaget (dam)
- `805f3166-af52-d436-b7a6-9cd3d0f9f9b7` — Curlinglandslaget (dam)
- `f0a45f0a-61cb-bc3a-857f-87e958059650` — Curlinglandslaget (dam)
- `818534eb-5211-053d-bbe3-535654eecb83` — Curlinglandslaget (dam)
- `d4ec8a74-5135-ae32-b505-d65f10543313` — Curlinglandslaget (dam)
- `328d2417-c562-c637-9277-4d4947adbf00` — Curlinglandslaget (dam)
- `8facd66b-b7fb-e93c-849f-4ecb6fc18800` — BMX Racing
- `9baf9990-641f-2c34-bf16-9246da16aa3e` — Youth Olympic Games Dakar 2026
- `8547543e-747b-3b3f-be41-4ef9bad78c9c` — Youth Olympic Games Dakar 2026
- `7d3f1d35-c780-603c-88d1-2fb54fdf528b` — World Snooker Tour
- `f86e4d60-d6e0-4134-99fb-25c54ef265c2` — World Snooker Tour
- `fd5283db-e45f-003b-9c91-f5c95c765d7d` — Global Champions Tour
- `a5da8a46-6b07-2b34-affe-d9ce80018785` — Cycling
- `65b700a4-6943-8e37-a1cd-a7f96465a44b` — Curlinglandslaget (herr)
- `c9aaf994-9c5c-6030-97ca-c57ae0c75543` — Curlinglandslaget (herr)
- `8d821da4-b599-3f3b-b3c4-119626bd60d2` — Curlinglandslaget (herr)
- `90b01838-92b4-0434-b61b-b73ef7eb08b9` — Global Champions Tour
- `e444fdec-7dbe-c43f-9230-ca48e71ea95b` — Global Champions Tour
- `cbe15e23-88d3-8f32-a162-82c5caba4407` — Global Champions Tour
- `58bd9107-0be0-4f38-81c2-657838ce217c` — ATP Tour
- `bb1c5377-b355-673a-996e-34648f31a79c` — ATP Tour
- `c19beef5-5f0d-3932-b194-33ea3043c007` — PGA Tour
- `4d02005c-705e-5931-a0f5-91fd64cc25d8` — PGA Tour
- `6a7371b5-1dea-f733-985c-fd5283df9bcf` — World Snooker Tour
- `6128a3df-5209-223d-881c-57b6df703c78` — World Snooker Tour
- `c29c2f3c-f5ef-8732-a1d7-0270dab60996` — World Snooker Tour
- `8cf894bc-c9a3-5033-a7ed-55da021f03e3` — Youth Olympic Games Dakar 2026
- `b9cc0343-af6c-6338-83d9-6d7a90012b0d` — ATP Tour
- `2a358a2b-ae3f-f03e-a5ef-e475c5cd931e` — ATP Tour
- `7c143164-4d77-3a3d-8874-4fb93566984d` — ATP Tour
- `98fc23e9-bc40-bd34-a3fb-8970e1c684f0` — ATP Tour
- `2e887f04-8243-be3e-b4e2-d4217ea9b658` — World Snooker Tour
- `8079b5ec-69bc-273a-bd32-8b74f02ffd82` — ATP Tour
- `423ea488-d3fc-1a39-b6fd-69e9beb184cd` — ATP Tour
- `3e7a4274-b43f-113f-bfe3-ff9d4588b4ab` — World Snooker Tour
- `291cae35-28ab-3b3f-b1c6-4676f86248a8` — World Snooker Tour
- `9ad263f5-5baa-1637-9b58-b725a936c9be` — Youth Olympic Games Dakar 2026
- `8149dfee-b5c2-0d3d-bb04-b6ffb1652c24` — Youth Olympic Games Dakar 2026
- `ea547f4f-6d92-9e31-87e9-4a16ba70c891` — Youth Olympic Games Dakar 2026
- `f0a0e918-f503-6237-a4b3-ef3a0fa8e817` — ATP Tour
- `46e35311-4d1e-1f38-a945-c55e6d615665` — ATP Tour
- `426f52c3-3f50-5f30-96b4-32a168ecc6b1` — ATP Tour
- `17afb9f1-2fe7-d93a-9746-88e67574b540` — ATP Tour
- `243acf5f-ccd6-fb35-9cbb-587c9f818c0d` — Cycling
- `6e0322fe-0bb9-5339-a709-148421ec8772` — World Snooker Tour
- `90018c10-1100-fc3f-898c-2fd523c2b701` — ATP Tour
- `2b81c0a7-7426-883c-9811-514f4c4af36d` — ATP Tour
- `29d2df4a-279c-453f-b2a5-7e1a41888db1` — World Snooker Tour
- `9ec107cf-f3ac-713f-992f-b8837d982782` — Youth Olympic Games Dakar 2026
- `e2231b3a-50fb-3e3c-8a97-05ea37b26fea` — ATP Tour
- `1d749c16-ebad-fb31-b326-983ff4cd6fd8` — ATP Tour
- `5d49e34e-eb21-6030-bbd5-66cb37f0cb19` — ATP Tour
- `49af8fd5-9753-e03e-a3d6-9101a40a8ae3` — ATP Tour
- `cb6e4808-b0b4-163c-ae89-6b5a83436ade` — World Snooker Tour
- `ce0ac364-3bae-7d30-b49c-c4800a20cd94` — Youth Olympic Games Dakar 2026
- `1a659cdc-7f42-a43c-849f-1bafaf79e9c5` — ATP Tour
- `48b37d60-9723-8b39-a04d-060317a9c61a` — ATP Tour

### New unresolved IDs — 38

Participation, round, or broadcast mapping remains incomplete unless stated above.
Organization names are database markers; `no entity` means no matching entity exists.

- `35d0be66-0a26-5d32-9b7a-c18a00ae6411` — Ladies European Tour
- `20bf389c-bd74-2430-98eb-a880dac393e0` — MotoGP
- `94d09763-dd29-3438-889c-c3926b690cd5` — ATP Tour
- `3046f78c-2b5b-3d3b-9c20-f0371879622f` — no entity (Breeders Cup)
- `5c8d5496-8815-2834-96a5-de0f0bd68b0c` — PGA Tour
- `63aefa56-ed9f-033e-8d50-b8d788618ef7` — GT World Challenge
- `60d4c306-7914-1535-b661-42552479cf59` — LPGA Tour
- `d93ea8a1-4fe1-7c3f-967c-2f9bd338e608` — MotoGP
- `0341b6bf-2a48-803a-b2f6-4cc1002923de` — MotoGP
- `8b4b99fb-57fd-6334-bc37-ada17847a99f` — GT World Challenge
- `5b67deb9-4209-e137-806e-e822f3abce92` — MotoGP
- `4e6a1855-42be-583f-80a4-ff5aeb9eeb31` — no entity (Italian F4)
- `93a0bd53-18e9-9f32-a090-f2d4ce2ade42` — no entity (Italian F4)
- `146b232c-1ab0-5631-9986-075deb2d75da` — no entity (Breeders Cup)
- `2ebd0272-c58e-4d3a-aec0-b341b0e803be` — MotoGP
- `4cb46ac6-06d6-723c-bad3-0c40636fd8d1` — no entity (Italian F4)
- `2edfad67-9035-6c3b-98ea-3ccd8e2987b7` — no entity (Italian F4)
- `3ed9dcf3-335d-1d3e-b9d7-4d01c1703566` — no entity (Melbourne Cup)
- `5d287d00-7eb9-333b-b181-7eee04fe29b6` — Ladies European Tour
- `277e5f7a-7842-4638-bab1-dbeac9678843` — World Snooker Tour
- `a871f49b-3ac4-1636-ad3c-07b9c40562c4` — DP World Tour
- `e8514ae1-1698-af3f-b169-28fc8e480b0d` — DP World Tour
- `4cc48470-b5f3-7a30-a6a5-868cfa88f726` — Youth Olympic Games Dakar 2026
- `e49835ad-c976-e537-ac8b-8a383967de9a` — Youth Olympic Games Dakar 2026
- `bce5492e-10cd-3439-978e-1732e353bdf0` — ATP Tour
- `77864914-d88c-6a38-b494-c3542db4462c` — ATP Tour
- `b994d62b-dff6-443d-9a17-bb47a0f66c81` — ATP Tour
- `53e3e171-dca8-8d35-a075-7ec3d0a473bc` — ATP Tour
- `a1369764-9a24-233a-8d65-60c0d41c913f` — World Snooker Tour
- `43de7f25-89ea-de35-876d-2b92d99bbdf1` — Cycling
- `126fde57-e4c4-2735-88bd-b38a7df0c20e` — Cycling
- `99757053-53ba-d236-97b6-117bcbe6ef5d` — Cycling
- `f532a367-bab2-ee34-a267-88bb003e39fb` — Cycling
- `a3cdd690-f775-1331-99cf-40a7a81258bf` — ATP Tour
- `a1119652-d972-6e32-bdcf-7d40e1b506d6` — ATP Tour
- `e4c9a27e-7ea6-293e-99b0-3b98a70ad6d4` — PGA Tour
- `767e8720-d886-323f-aa3e-16935e4be737` — LPGA Tour
- `723e6fd0-9bb5-1337-a4d7-a7ac1d305b21` — LPGA Tour

## Public and admin verification

- All five hosted pages returned HTTP 200. The public 5 November cards show all 24 hockey
  and 17 handball participants, with ages, clubs, profile links, and Swedish flags. All
  nine referenced flag SVGs loaded. Of 45 unique athlete profile URLs, 43 returned HTTP
  200 directly; the two European Tour profiles were confirmed in the browser.
- Public presentation defect: the 5 November team-sport cards merge the activity data but
  omit all schedule segment rows, channel labels, and segment time ranges. Their headings
  show `Nocco Hockey Games: Tjeckien – Sverige` and
  `EM-kval 2028: Sverige – Grekland`. This requires a site code change and remains
  unresolved under this job's scope.
- The authenticated dashboard has no open todos. It still lists two existing Draft
  activities: `53c14a52-f0f4-4fdc-8aa0-2b66c922e408` (`Dueller`, 30 Oct) and
  `49cc4e5f-8826-4857-adf9-34d45f6a4bb6` (`Flerbilslopp och final`, 31 Oct).
  These remain Draft following the previously reported FIA postponement.
- The dashboard also flags Maybank Championship Round 3 for missing participant start
  times. Activities `6a65b2f6-e82f-4b87-8b81-d02dc8abdffb` and
  `7af7fff1-7620-4a4f-b5d5-3284e9c1320e` contain Maja Stark and Linn Grant. The official
  LPGA pairings page showed Round 2 only while that round was in progress; Round 3 times
  were not published. No times were inferred. The 31 Oct card count remains 8.

## Public card counts

Counts are for `https://sesport.se/?date=YYYY-MM-DD`. No existing card was added or removed;
the two new published event groups add two cards on 5 November.

| Public date | Before | After | Difference |
| --- | ---: | ---: | --- |
| 2026-10-29 | 9 | 9 | None |
| 2026-10-30 | 7 | 7 | None |
| 2026-10-31 | 8 | 8 | None |
| 2026-11-01 | 6 | 6 | None |
| 2026-11-05 | 0 | 2 | Two new event groups |
