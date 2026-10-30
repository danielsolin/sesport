# Activities run — 2026-10-29

Started: 2026-10-29 07:20:07 UTC.

Finished: 2026-10-29 08:08:11 UTC.

## Outcome

- Snapshot: 130 visible, unprocessed broadcasts. IDs are in
  `/tmp/activities-snapshot-2026-10-29.csv`.
- Final outcomes: 6 processed, 50 rejected, and 74 unresolved.
- Readback found 130 unique IDs and no invalid status. Processed broadcasts are hidden and each
  has exactly one activity link. Rejected broadcasts are hidden and have no activity links.
  Unresolved broadcasts remain visible, unprocessed, and all have organization markers.
- Public card counts before and after changes:

  | Public date | Before | After | Difference |
  | --- | ---: | ---: | --- |
  | 2026-10-29 | 8 | 9 | Added WTT Truls–Togami |
  | 2026-10-30 | 7 | 7 | Added WTT; removed postponed Extreme H |
  | 2026-10-31 | 8 | 8 | Added beach volleyball; removed Extreme H |
  | 2026-11-01 | 5 | 6 | Added beach volleyball |

## Processed broadcasts — 6

- WTT: `e7e9d417-c5ae-d333-8afe-77efa9f8a568`,
  `265e6386-b688-733c-9e35-804cffcb4627`.
- Lyon: `b40dda1e-432a-ce35-96e5-91680664bcee`.
- YOG beach volleyball: `c1e08dd6-2ed9-3f36-99f3-6474bcf3dcd6`,
  `3b9ec9c1-f155-f139-a503-d4d2065340b0`, and
  `b619cff4-94ea-6e39-903c-ebe76bf5f285`.

## Activities and data changes

- Created WTT activity `36a79a3f-fa2b-4873-aec8-f38b7b6c136d` for Truls Möregårdh–Shunsuke
  Togami on Oct 29, 18:30–19:30.
- Created WTT activity `aeda830e-a1b7-42e2-bd74-a136735710a2` for Elias Ranefur–Patrick
  Franziska on Oct 30, 14:45–16:00.
- Created the beach-volleyball group `44284fbe-d7f0-4696-8e87-3c1ae41a40fa`, Oct 31–Nov 4.
- Created YOG activity `04e66d6b-3c8c-4081-b444-e7d0bbd18b0c` on Oct 31, 08:25–15:10.
- Created YOG activity `aaf8e3ea-d68a-40d8-9c55-0dfc1bda518f` on Nov 1, 08:25–16:50.
- Linked both confirmed Swedish YOG athletes to each beach-volleyball session.
- Added Casper Tjernqvist and Henrik Brus with verified birthdates, profiles, clubs, and
  organization links. Added the KFUM Örebro Volley club and Skate Canada organization.
- Updated Lyon activity `362dffd1-10cd-4405-bfc4-4fb5c24bd4aa`: replaced the TV4 wrapper with
  the direct TV4 Play event URL and added the Sjuan catalog fallback.
- Set Extreme H activities `53c14a52-f0f4-4fdc-8aa0-2b66c922e408` and
  `49cc4e5f-8826-4857-adf9-34d45f6a4bb6` to Draft after FIA postponed the event to 2027.
- Saved typed ActivityEvidence, ParticipationEvidence, ParticipantStartEvidence, and
  PersonFacts sources for the research and corrections.

## Participant start times

Official tee-time sources supplied 12 values across the parallel channel activities. Times below
are Europe/Stockholm.

- Mexico Open round 1: Pontus Nyholm 19:51; Jesper Svensson 20:24. Applied to Eurosport 2
  activity `49e09e0c-f90a-42e1-a665-d3b16953d3bd` and HBO Max activity
  `9443c70b-7f0f-4f5f-9c81-dfd850355a4a`.
- Rolex Grand Final round 1: Adam Wallin 11:24; Christofer Blomstrand 12:00. Applied to V Sport
  Golf activity `41b36111-231b-4536-b58e-a8d243d58609` and Viaplay activity
  `7f0ab179-5ec4-414c-b02d-f7315963c1e5`.
- Maybank Championship round 2: Linn Grant 00:59; Maja Stark 01:32. Applied to V Sport Golf
  activity `09364887-58b6-4c68-a610-a4c7df91285b` and Viaplay activity
  `89416e0b-1b88-4bf7-a0b6-e15ed0e8ba0a`.
- Maybank times were converted from Malaysia local time. Its Oct 30 activity appears on the
  Oct 29 public sport-day page under the existing `sport_day` convention.
- The PGA TOUR page showed round 1 times in UTC; the HotelPlanner Tour tee-time page and data
  supplied round 1 times; the LPGA pairings page supplied round 2. All six activity variants
  now have source-linked values.
- Later-round times remain unavailable: PGA TOUR showed round 1 only; HotelPlanner Tour round 2
  returned no course pairings; LPGA showed round 2 but no round 3 pairings. These future entries
  remain without guessed times and should be revisited when official lists publish.

## Public and dashboard verification

- Retrieved the affected public pages on `https://sesport.se` after the final corrections.
  The six golfers' names, ages, clubs, profiles, and sourced times render correctly.
- Final public card counts are 9, 7, 8, and 6 for Oct 29, Oct 30, Oct 31, and Nov 1.
- Verified the direct SVT Play and HBO Max links, TV4 Play link, and Sjuan fallback. Confirmed
  both new athlete profile pages and the Swedish flag asset load successfully.
- Authenticated to `/Admin/Dashboard`. The missing-start-time rows are cleared. The only
  remaining attention rows are the two expected Draft Extreme H activities after postponement.
- No code, assets, schema, configuration, or deployment changes were made.

## Rejected broadcasts — 50

Each listed ID was hidden and left without an activity link.

### Extreme H postponed to 2027 — 1

- `829979d7-02a5-f93d-b1a1-f9e1c2d9737c`

### Formula 1 — 14

The official 2026 entry guide has no Swedish drivers.

- `ba413bdf-6153-5c3a-aa93-cca7aaf7ec47`, `2f4f0045-0822-e839-ad11-558ecd9dd10a`
- `e7f7e6c8-9ad2-ed38-9725-a2b8af637f4b`, `09dfcf21-85f5-d632-95b2-537744ad7bc2`
- `fca57573-f179-3c31-bf4f-e55de435383d`, `778ccdb7-1cb3-c33a-b704-4242d746e533`
- `8c23d9d9-89bd-753a-a3bb-7d2ff60fbf47`, `df29b22a-4290-b030-9bb6-f304c2159902`
- `12465ccf-5fb1-2436-b21a-de7647f9f799`, `58f03d5a-6dd6-513b-ab99-a7d7fe273541`
- `57faf302-e7b6-c331-9b99-143d0c4bb08c`, `e5db0707-518e-693c-99d0-98eb822e1c2a`
- `17743f9d-2f00-803b-a8cd-d83374bf22ef`, `4860988c-eb44-933d-b568-f7b2b8cfa8ba`

### MMA — 1

OKTAGON 96's official card lists no Swedish fighters.

- `3cbc1f9e-8e19-e13c-a478-e0eb97504460`

### Foreign club fixtures — 20

Foreign-team fixtures are outside the job's eligibility rules, including fixtures with Swedish
players.

- `2f62ba14-5fab-9f3e-ad68-5c7ab9c5ec0e`, `e775edd8-8d2a-a23a-84fd-8b14f11a5d11`
- `fb98d782-5309-9836-bb98-72893fc1cee1`, `854699dd-bb57-8c3b-8e13-03307112c8f1`
- `771af8a3-34b4-f334-ad33-114fb4cc591e`, `22fc5ede-53bd-6f35-a2f1-6080a519ba4b`
- `84dbec83-8a17-e738-bc95-e73f3c854fbe`, `302a83b6-8547-2d3b-ae47-ff161c296364`
- `a8606631-7379-ea3e-8717-e2f9eb76b4d8`, `70111124-abd3-d133-9810-85eebb0a4216`
- `cc964176-a4d4-e430-a171-a9e1006ef0d6`, `cd77fe27-c83f-303b-b4f7-7fb5196a7e80`
- `d3f8b1a2-7653-6c30-a50f-36b4bca6b71e`, `e627c41a-a498-9e3d-8712-6cdcdb1d299f`
- `6f8a66f4-fa4d-3d3b-b795-aa29f142098f`, `3ee80049-f6fc-7b3d-b9ca-009fc38334e1`
- `4bac9ca0-0c15-d43f-b250-b9408ca708fa`, `acb95d29-f2d6-5733-a9c9-f7a5f321d52e`
- `05c79177-5c92-1638-941e-554a5c61ec3a`, `82717457-1c13-633c-b84c-88f4f25bcc51`

### Youth Olympic futsal — 2

SVT and SOK identify no Swedish futsal delegation for these Games.

- `9de28acd-15a1-1733-a04a-5c60dc485c41`, `28e38086-f312-df34-93a2-889f594c5b79`

### Studio or generic programmes — 3

- `cfb5ab48-03e7-b539-80d5-397869fe1e57`, `cb75ddd9-84d0-1137-80eb-f34b9a16b937`
- `da3984cc-ee40-f634-bbe7-bf84386295b2`

### Domestic Swedish fixtures — 7

Domestic competitions are outside the job's eligibility rules.

- `b6b7f659-76a8-b735-bd09-8ee2a2b9211f`, `ff02f937-1d30-a131-acee-edb368f411f2`
- `9394d527-dc32-3a3a-a39f-035dbbe5b75b`, `8bfe790d-d0f2-b13d-9953-43a1806f1408`
- `c284df56-47a7-8833-867c-054c543fca42`, `11a98da3-a8fe-4339-bc76-c7a69184e6b9`
- `e9d560cd-a9d3-ec3e-9a53-f1fbe1daa336`

### TOTO Japan Classic — 2

The official 78-player entry list has no Swedish players.

- `90aea0c9-50ff-423c-be79-5db2ad8f434b`, `1c32feba-e1f9-3839-96f0-6b2e016f1935`

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
