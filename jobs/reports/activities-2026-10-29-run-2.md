# Activities run — 2026-10-29 (run 2)

Started: 2026-10-29 08:27:10 UTC.
Finished: 2026-10-29 08:40 UTC.

## Snapshot and outcomes

- Database: `sesport`, resolved from the repository `.env`.
- Snapshot: 74 visible broadcasts, all unprocessed. File:
  `/tmp/activities-snapshot-2026-10-29-run-2-0826.csv`.
- Final outcomes: 0 processed, 0 rejected, 74 unresolved.
- Readback matched all 74 snapshot IDs to the final visible set. All remain unprocessed and have
  organization markers. No broadcast status changed.
- No new evidence changed the previous unresolved classifications. The categorized reasons and
  complete ID ledger below are carried forward from [the prior report](activities-2026-10-29.md).

## Activity correction

- Verified activity `3d90f838-79e5-47ce-bcef-8dde4613fad9`, `Skottland – Sverige`, in group
  `Curling-EM`. It remains Published for 29 October, 09:20–11:50.
- Its five active Person links match the official 2026 EM roster. Added current `Lag Wranå` Team
  links for all five and linked the Team to `Sundbybergs CK`.
- Changed the club shown under `Moderklubb/Klubb` where older development records support it:

  | Person | Club shown | Evidence |
  | --- | --- | --- |
  | Almida de Val | Göteborgs CK | 2013–14 Team Rilea |
  | Isabella Wranå | Sundbybergs CK | SOK says her curling career began there |
  | Linda Stenlund | Östersunds CK | 2013–14 Team Framtiden |
  | Maria Larsson | Danderyds CK | 2014 younger-junior record; old club mapped by federation |
  | Moa Dryburgh | Sundbybergs CK | 2025–26 Team Dryburgh |

- Added official club URLs:
  - `Sundbybergs CK`: `https://www.kck.se/sundbyberg`
  - `Göteborgs CK`: `https://medlem.goteborgcurling.se/`
  - `Östersunds CK`: `https://www.ostersundscurling.se/`
  - `Danderyds CK`: `https://www.danderydscurlingklubb.se/`
- Created Club entities:
  - Göteborgs CK: `14d8dc96-dc28-c9a3-eab7-00cd9f74033d`
  - Östersunds CK: `419027b0-ae3b-e4c6-cc37-030770334cb8`
  - Danderyds CK: `6724aa3c-4030-4461-4e2d-599c8b94ab69`
- Created Team `Lag Wranå`: `f10ed79c-55d7-7bdb-1b7b-7a5b9058ff73`.
  URL: `https://www.thegrandslamofcurling.com/teams/team-wrana-71`.
- Set Moa Dryburgh's Watch Priority from `tier_2` to `tier_0`, based on Team Wranå's 2026 Worlds
  bronze, her current role in the team, and SOK coverage.
- Added 15 typed evidence sources: PersonFacts, ActivityEvidence, and ParticipantStarEvidence.
  The new activity evidence includes the TV.nu event detail.
- The changed activity verification set contains only
  `3d90f838-79e5-47ce-bcef-8dde4613fad9`.

## Verification

- SVT confirms the activity's 09:20–11:50 broadcast window. TV.nu confirms the event, SVT2, and
  09:20 start; SVT confirms the segment end at 11:50.
- SVT Play's direct event URL and the SVT2 catalog fallback resolve. No stream-link update was
  needed.
- Public page `https://sesport.se/?date=2026-10-29` returned HTTP 200. The five names, ages,
  profile links, club links, group heading, times, channels, and Swedish flag rendered correctly.
  All nine activity cards remained present before and after.
- The five person profiles, four club pages, Team Wranå profile, and Swedish flag asset returned
  HTTP 200.
- Authenticated dashboard readback shows only the two expected Draft Extreme H activities after
  the FIA postponement. There are no open todos.
- No detail activities were needed; no individual start times apply to this curling match.
- No code, assets, schema, configuration, or deployment changed. No tests were run.

## Unresolved broadcasts — 74

Each remains visible and unprocessed. An organization was set for every ID.

### Climbing Gunsan — 1

The official event PDF did not establish a Swedish participant for the listing.

- `098b23da-f78c-9438-9b9c-1565855ddbd9`

### Global Champions Tour Rabat — 9

The schedule identifies the event, but the broadcast listing does not confirm the Swedish
competitor or exact coverage.

- `75f97fbf-9e55-ef34-b8a3-020f17651382`, `5ca64ee7-d845-b636-8f3c-b584321f57a6`
- `0dda1ee3-3158-9a35-83f3-31b711c36514`, `81b2394b-a401-e438-9ad4-b6887600855d`
- `222e1a3c-fb99-4838-ac55-b55b11d016e6`, `fd5283db-e45f-003b-9c91-f5c95c765d7d`
- `90b01838-92b4-0434-b61b-b73ef7eb08b9`, `e444fdec-7dbe-c43f-9230-ca48e71ea95b`
- `cbe15e23-88d3-8f32-a162-82c5caba4407`

### Skate Canada — 1

The official event announcement includes a TBC competitor; Swedish participation is unconfirmed.

- `112b9462-56fc-5e3b-aab5-db347df96e5e`

### Snooker — 16

The WST draw source does not resolve these feed entries to confirmed Swedish participants.

- `5755ecf6-eaf1-b136-a32b-286e29464169`, `e3472469-6595-6d3d-814f-e84bebde3cd6`
- `1447cd13-d141-d038-bf7b-5d1457ffa7df`, `396f3c9b-7523-c736-bee2-6aeee5fb11b5`
- `393d934b-8c58-e931-b5b7-ef8b7b6a7d6e`, `7d3f1d35-c780-603c-88d1-2fb54fdf528b`
- `f86e4d60-d6e0-4134-99fb-25c54ef265c2`, `6a7371b5-1dea-f733-985c-fd5283df9bcf`
- `6128a3df-5209-223d-881c-57b6df703c78`, `c29c2f3c-f5ef-8732-a1d7-0270dab60996`
- `2e887f04-8243-be3e-b4e2-d4217ea9b658`, `3e7a4274-b43f-113f-bfe3-ff9d4588b4ab`
- `291cae35-28ab-3b3f-b1c6-4676f86248a8`, `6e0322fe-0bb9-5339-a709-148421ec8772`
- `29d2df4a-279c-453f-b2a5-7e1a41888db1`, `cb6e4808-b0b4-163c-ae89-6b5a83436ade`

### BMX Sarasota — 2

The UCI competition hub does not identify the participants for these broadcast slots.

- `08aa1629-4ad9-d434-89d7-925ae97f5d60`, `8facd66b-b7fb-e93c-849f-4ecb6fc18800`

### Curling — 13

The event is identified, but exact teams and sessions are not established by the broadcast data.

- `5d616d09-d2cc-5e35-9f8e-e04717288382`, `343413b3-cd9f-ed33-ae08-b69c7f09a3cb`
- `fc34ef77-65f9-e733-b3ef-0ca7add32602`, `805f3166-af52-d436-b7a6-9cd3d0f9f9b7`
- `f0a45f0a-61cb-bc3a-857f-87e958059650`, `7289dbae-cf7d-273b-b714-8fd5a30cfe75`
- `d4ec8a74-5135-ae32-b505-d65f10543313`, `818534eb-5211-053d-bbe3-535654eecb83`
- `328d2417-c562-c637-9277-4d4947adbf00`, `c6eb28ef-0e1c-6f36-ba7c-6760ed9dd3a4`
- `c9aaf994-9c5c-6030-97ca-c57ae0c75543`, `8d821da4-b599-3f3b-b3c4-119626bd60d2`
- `65b700a4-6943-8e37-a1cd-a7f96465a44b`

### Youth Olympic beach-volleyball progression — 8

Later pool-stage progression is not known, and the SVT multi-sport feeds do not map to exact
Swedish sports or match times.

- `9baf9990-641f-2c34-bf16-9246da16aa3e`, `8547543e-747b-3b3f-be41-4ef9bad78c9c`
- `8cf894bc-c9a3-5033-a7ed-55da021f03e3`, `8149dfee-b5c2-0d3d-bb04-b6ffb1652c24`
- `ea547f4f-6d92-9e31-87e9-4a16ba70c891`, `9ad263f5-5baa-1637-9b58-b725a936c9be`
- `9ec107cf-f3ac-713f-992f-b8837d982782`, `ce0ac364-3bae-7d30-b49c-c4800a20cd94`

### Koppenbergcross — 1

The official programme does not settle Swedish participation or the listed broadcast segment.

- `a5da8a46-6b07-2b34-affe-d9ce80018785`

### ATP Basel and Paris — 20

Official tournament pages identify the events; the exact rounds and Swedish player mapping remain
unconfirmed.

- Basel: `58bd9107-0be0-4f38-81c2-657838ce217c`, `bb1c5377-b355-673a-996e-34648f31a79c`
- Paris: `2a358a2b-ae3f-f03e-a5ef-e475c5cd931e`, `7c143164-4d77-3a3d-8874-4fb93566984d`
- `98fc23e9-bc40-bd34-a3fb-8970e1c684f0`, `b9cc0343-af6c-6338-83d9-6d7a90012b0d`
- `423ea488-d3fc-1a39-b6fd-69e9beb184cd`, `8079b5ec-69bc-273a-bd32-8b74f02ffd82`
- `46e35311-4d1e-1f38-a945-c55e6d615665`, `426f52c3-3f50-5f30-96b4-32a168ecc6b1`
- `17afb9f1-2fe7-d93a-9746-88e67574b540`, `f0a0e918-f503-6237-a4b3-ef3a0fa8e817`
- `2b81c0a7-7426-883c-9811-514f4c4af36d`, `90018c10-1100-fc3f-898c-2fd523c2b701`
- `1d749c16-ebad-fb31-b326-983ff4cd6fd8`, `5d49e34e-eb21-6030-bbd5-66cb37f0cb19`
- `49af8fd5-9753-e03e-a3d6-9101a40a8ae3`, `e2231b3a-50fb-3e3c-8a97-05ea37b26fea`
- `48b37d60-9723-8b39-a04d-060317a9c61a`, `1a659cdc-7f42-a43c-849f-1bafaf79e9c5`

### PGA Tour and cycling — 3

- Mexico Open round 4: Swedish entrants were known, but the cut had not been played.
  ID: `c19beef5-5f0d-3932-b194-33ea3043c007`.
- Simmons Bank Open: the official event ran in September; the future feed item is not marked as
  a replay. ID: `4d02005c-705e-5931-a0f5-91fd64cc25d8`.
- Giro d'Italia: the race ended May 31; the future feed item is not marked as a replay.
  ID: `243acf5f-ccd6-fb35-9cbb-587c9f818c0d`.

All 130 starting broadcast IDs appear once in the outcome ledger above. No tests were run.
