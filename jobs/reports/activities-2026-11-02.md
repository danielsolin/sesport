# Activity run — 2026-11-02

Started: 2026-11-02 02:57:34 UTC.
Finished: 2026-11-02 03:39:35 UTC.

## Outcome

The starting snapshot contained 109 visible broadcast IDs across all dates and processing states.
Readback verified exactly one outcome for every ID: 30 rejected, 19 processed, and 60 unresolved.
Created 15 published activities in eight event groups, added 129 participant links and 19 broadcast
links, and saved 100 typed evidence records. Four exact existing source tuples were reused. No code,
assets, schema, configuration, or deployment changed.

All processed broadcasts now link to their activity and are hidden. Rejected broadcasts remain
unprocessed and hidden; only `hidden_at` changed. Unresolved broadcasts remain visible and
unprocessed, with organization markers set where identifiable. No broadcast-ignore rows were
changed.

## Activities created

### Youth Olympic Games

New group: `2d15029c-31a8-4fdf-abca-49d973913afb`.

- `7e8ee494-54ab-430e-8b56-2df4907b0a35` — Kval 1, 3 Nov 09:55–11:30, SVT2, SVT Play.
  Feeds: `8149dfee-b5c2-0d3d-bb04-b6ffb1652c24`, `ea547f4f-6d92-9e31-87e9-4a16ba70c891`.
- `98739524-de88-487a-a1c5-ba8007b76ccf` — Kval 2, 5 Nov 09:55–12:00, SVT2.
  Feed: `4cc48470-b5f3-7a30-a6a5-868cfa88f726`.
- `a39ba8ba-32b2-42e3-bbc9-e5aedb7b12bb` — Kval 2, 5 Nov 10:55–13:00, SVT Play.
  Feed: `e49835ad-c976-e537-ac8b-8a383967de9a`.
- `e5930a14-a777-4d17-a137-2d7d6a62006b` — Individuell final, 6 Nov 09:55–14:20, SVT2.
  Feed: `4d51de4d-b68c-7a31-b04e-3baf66fa863f`.
- `a12ec640-ce8b-4dc3-8345-dbc2c06dec87` — Individuell final, 6 Nov 10:55–14:00, SVT Play.
  Feed: `4f808200-0bda-e037-b3fc-4aa505eb5efb`.

### Abu Dhabi Aldar Championship

Reused group: `3ab9fb18-485d-4199-84a7-1ab4aab7444d`.

- `470a028e-1a96-4580-be67-cca668a0afe8` — Runda 4, 8 Nov 08:30–14:30,
  Viaplay Sport and V Sport Golf.
  Feeds: `faa2bc5a-3163-d53a-962b-48d07af89644`,
  `71660445-0268-3136-87de-af08c3cf88f9`.

### FIA WEC Monza

New group: `654e669c-5be0-4ee5-861b-218ca407e663`.

- `ba0159fd-b5ea-4a1d-9a9e-1e8db24aec15` — Loppet, 8 Nov 10:30–17:30, Eurosport 1.
  Feed: `d7c42f35-4cf7-4b3e-9381-43875ba5e790`.

### Världscupen i hoppning: Verona

New group: `69c3b7ff-a735-4d16-a473-88cae4c24c62`.

- `ef264e29-8900-458a-aaa9-0473d86415ee` — Grand Prix, 8 Nov 14:15–17:45, TV4 Play.
  Feed: `3ec56e51-3b58-bb3b-b41d-1f87865f4f73`.
- `ad9e8632-fc8b-45bc-8c23-9ac59382cc9a` — Grand Prix, 8 Nov 14:15–17:55, Sjuan.
  Feed: `4594f77e-18d8-f433-ad22-3dcd64d7b95b`.

### Dam-VM i ishockey

Reused group: `23397ce8-0521-4014-a150-02927368a51b`.

- `fab39b97-53b8-4248-91b9-f18af4a4bcdc` — Finland – Sverige, 8 Nov 14:50–17:30.
  Feeds: `cca404db-77f9-7532-ab44-3a3c90b015cb`,
  `04e11cb4-ca4a-6930-bc42-0643b5a532a0`.

### Handboll, EM-kval

Reused group: `1bd6b218-b54e-5c34-8061-b392166603ad`.

- `19a00208-4599-4605-85db-51e532dbf8fc` — Luxemburg – Sverige, 8 Nov 15:50–17:50.
  Feeds: `da659f93-7e90-ae38-8b49-081334289f72`,
  `f4ef662f-a0c0-cb3c-a1e7-ceb5ebad465a`.

### NOCCO Hockey Games

Reused group: `16874649-9970-5a55-b2a0-73d2f89a57d2`.

- `fc92d808-bced-4ddc-939c-1f0dbd8abb96` — Finland – Sverige, 8 Nov 17:20–20:00,
  SVT Play.
  Feed: `a60bb91a-30f8-2a3b-bc5c-502b1aa2a55a`.
- `e13c52a6-cbb5-478a-8ee4-5eebf82142b6` — Finland – Sverige, 8 Nov 17:20–17:50,
  Kunskapskanalen.
  Feed: `acadeadb-57e5-0c3c-8afe-999e00b64ed1`.
- `857827f8-ed83-45cd-9559-0ae1ba49b757` — Finland – Sverige, 8 Nov 17:50–20:00,
  SVT2.
  Feed: `4695a8ab-f2d6-a436-b362-a764e269d0fe`.

### World Wide Technology Championship

Reused group: `ed4b99fb-5ff0-4ebb-b42a-4def0303ab4c`.

- `c81fde5b-0b05-4ca4-ae74-fa07ad03073c` — Runda 4, 8 Nov 21:00–9 Nov 00:00,
  Eurosport 2.
  Feed: `80dca0f8-6ceb-863c-a60e-cb3d3d1e6ae0`.

## Other data changes

Extended the handball EM-kval group through 8 November. Updated both existing Sweden activities,
`f3961c91-4119-5cc0-9292-65be98d0bf78` and `be9254e0-bc2d-54a9-b121-b25c80f794b5`:
replaced Nikola Roganovic and Edwin Aspenbäck with Jonathan Carlsbogård
(`ef6db823-3fc4-3c30-9928-0b6ac752860e`) and Lukas Sandell
(`90719918-4342-36a7-acc6-cb7016719d2b`). Both activities have 17 active participants.

Added Valentina Jarl (`b121409b-655c-4723-83f2-74d975868312`) with birth year 2009
(1 January stored as a month/day placeholder), profile, formative club Jump Club
Ridsportförening, and current team Franssen Stables. Added Franssen Stables as a Team with its
official URL. Set Lukas Sandell's birthdate to 1997-02-03 and linked his formative club Eslövs HF
(`271be193-8cd9-4a96-a6a3-b1a57f3b8c16`). The official profile source now records his formative
club and current team, Pick Szeged. These corrections were verified on the public schedule.

## Evidence

Saved 100 new typed source records: 82 ActivityEvidence, 12 ParticipationEvidence, and six
PersonFacts. Four exact existing source tuples were reused. Sources include TV.nu listings for
segment times and channels; the SOK Youth Olympic Games squad and SVT guide; FEI Dakar jumping and
Verona schedules; the FIA WEC Monza provisional entry list; Swedish handball roster and player
profiles; and the Swedish Equestrian Federation's week 44–45 calendar.

Participation and rejection checks also used official event sources: the UFC Bonfim–Brady update,
the 2026 MotoGP Qatar event entry list, the Formula 1 driver list, the World Snooker Tour calendar,
Flanders Classics event calendar, the Rolex Paris Masters draw and schedule, and the Danish horse
racing November propositions. Source records and evidence types are attached to their relevant
broadcasts, activities, groups, and person facts.

## Rejected broadcasts — 30

All remain unprocessed and hidden. Each ID appears once in the starting snapshot.

### Replays and generic programming — 4

Reason: replay, recap, or studio programming without eligible live event coverage.

IDs: `4d02005c-705e-5931-a0f5-91fd64cc25d8`, `243acf5f-ccd6-fb35-9cbb-587c9f818c0d`,
`c4702ff4-3c8e-6039-b83d-94d09a3cf8e5`, `8480b957-0a22-b637-8ea6-1e4e1730e783`.

### Canceled UFC event — 1

Reason: Bonfim–Brady was canceled; the replacement card had no verified eligible Swedish
participant.

ID: `6e20929a-1300-6b30-bc00-0b4ab92446a5`.

### Youth Olympic Games listings outside Swedish participation — 2

Reason: the Swedish beach-volleyball pair withdrew before the listed final; the official squad
contained no Swedish artistic gymnast.

IDs: `ce0ac364-3bae-7d30-b49c-c4800a20cd94`,
`d07f245d-d666-0e38-8866-f90ca06abe0e`.

### Domestic Swedish football — 3

Reason: domestic competitions are outside the eligible scope.

IDs: `6a677f95-c021-1b38-93d4-ab580b196232`,
`8832e220-4803-9435-8766-94c227f48d12`,
`88eadac4-df88-f33f-bca5-831986af04f9`.

### Foreign club fixtures and NBA — 14

Reason: foreign club fixtures and NBA games are outside the eligible Swedish scope.

IDs: `fc90d5a8-f91d-3439-b78f-2877f13afb1b`,
`5f3a52da-df19-5831-b480-ac4fff6ce45a`,
`d494057c-7f04-3838-a368-88ef63f5afa8`,
`f1f58179-e30b-7f3a-8de2-4f4ceb52eace`,
`6cc8d6cb-5e91-5738-8626-e37544fa2b29`,
`03d04ea5-c57b-1d39-b774-9a7a0dc06794`,
`e0bff468-3317-4630-ae0c-a2d22a065b5c`,
`5252d9b2-bdf8-eb31-9709-a726a7258645`,
`184af831-6102-b339-90f2-9e884ea842e9`,
`30542d0c-11c7-ed3c-9690-f00fd86fb388`,
`9d4185f3-dfb8-7c3f-bd58-39802ad28b60`,
`270f00e3-9c43-393a-a465-4f7c46ea161c`,
`b4c3b0ef-85a1-a83c-8305-1332bf99cbbe`,
`a2f312cd-252d-d237-8927-2cf4c5a4bb9a`.

### Motorsport without Swedish entrants — 6

Reason: complete official Qatar class entries and the 2026 F1 driver list showed no Swedish riders
or drivers.

IDs: `ce793e31-a9a8-9b31-aaab-02da47975140`,
`ed87fa97-17b2-1c36-a326-0378e935143f`,
`ca79dbb0-3c85-783f-88da-3a4814256cad`,
`d3cbf9c6-d482-ee3f-ad5e-716326c2bb06`,
`1e16a563-b435-6e36-823a-18e7a62cc08c`,
`a19709ea-3f4f-6632-808a-aaa6151525e6`.

## Unresolved broadcasts — 60

All remain visible and unprocessed. Organization markers are set where shown below.

### ATP Tour — 36; entity `e707c3dd-1660-49a0-b770-41c11ae8e900`

Reason: exact Paris Masters and Stockholm Open session-to-player mapping was not established.

IDs: `17afb9f1-2fe7-d93a-9746-88e67574b540`,
`1a659cdc-7f42-a43c-849f-1bafaf79e9c5`,
`1d749c16-ebad-fb31-b326-983ff4cd6fd8`,
`2206cf10-ba3e-ff30-ad67-fb13c5063d00`,
`22d960f2-8618-3135-bfea-bdf682b73abd`,
`2a358a2b-ae3f-f03e-a5ef-e475c5cd931e`,
`2b81c0a7-7426-883c-9811-514f4c4af36d`,
`423ea488-d3fc-1a39-b6fd-69e9beb184cd`,
`426f52c3-3f50-5f30-96b4-32a168ecc6b1`,
`46e35311-4d1e-1f38-a945-c55e6d615665`,
`48b37d60-9723-8b39-a04d-060317a9c61a`,
`49af8fd5-9753-e03e-a3d6-9101a40a8ae3`,
`53e3e171-dca8-8d35-a075-7ec3d0a473bc`,
`5d49e34e-eb21-6030-bbd5-66cb37f0cb19`,
`6e8e02a3-a89b-c834-9386-06131a3b86b4`,
`6f010bdd-4251-2d3d-b04b-db2abfb12a3b`,
`77864914-d88c-6a38-b494-c3542db4462c`,
`7c143164-4d77-3a3d-8874-4fb93566984d`,
`8079b5ec-69bc-273a-bd32-8b74f02ffd82`,
`90018c10-1100-fc3f-898c-2fd523c2b701`,
`96d21ab0-33a7-3235-918f-d0eace29e737`,
`98fc23e9-bc40-bd34-a3fb-8970e1c684f0`,
`a1119652-d972-6e32-bdcf-7d40e1b506d6`,
`a2bf7396-9674-e038-a3ae-68f5913e4903`,
`a3cdd690-f775-1331-99cf-40a7a81258bf`,
`b994d62b-dff6-443d-9a17-bb47a0f66c81`,
`b9cc0343-af6c-6338-83d9-6d7a90012b0d`,
`ba1f53ee-3598-0c39-802a-b79335e5573d`,
`bce5492e-10cd-3439-978e-1732e353bdf0`,
`cb46e96e-e191-1a34-863c-b1cdee2da473`,
`cff0b13d-5bea-5a34-826b-2f11811267c6`,
`d25d861b-8ab6-2638-93ce-c0117a3efae6`,
`dd0cec50-f2e1-603c-9992-071e67505add`,
`e2231b3a-50fb-3e3c-8a97-05ea37b26fea`,
`f0a0e918-f503-6237-a4b3-ef3a0fa8e817`,
`f1a019c2-6dcb-d93a-8954-d62a199ea593`.

### Cycling — 6; entity `467a3c47-a54d-47ea-8aa4-a8742551ea4f`

Reason: the complete entries and exact Swedish participant-to-feed mapping were unavailable for
Superprestige, the men's European Championship, and men's cycling in Singapore.

IDs: `126fde57-e4c4-2735-88bd-b38a7df0c20e`,
`43de7f25-89ea-de35-876d-2b92d99bbdf1`,
`738d359d-de38-013c-8887-8c43054e7f8b`,
`99757053-53ba-d236-97b6-117bcbe6ef5d`,
`f532a367-bab2-ee34-a267-88bb003e39fb`,
`fd21bf81-225f-a03e-b89f-5c077f8f8368`.

### Ladies European Tour — 3; entity `b209cad2-8532-45e7-ba63-2a064dc4460d`

Reason: the complete Aramco China Championship field and Swedish entry status were unavailable.

IDs: `5d287d00-7eb9-333b-b181-7eee04fe29b6`,
`a21dad6c-4781-d63f-9b9a-dd539edd8560`,
`dd6bcc69-cc2e-5530-ac2c-1abd0cd13f68`.

### World Snooker Tour — 14; entity `b3a41eb0-1d5c-4c4d-9386-1766ac97e2c5`

Reason: exact Swedish player, round, and broadcast-feed mapping remained unresolved.

IDs: `0b8371ac-010e-8835-a8c2-cf918f48ac1a`,
`277e5f7a-7842-4638-bab1-dbeac9678843`,
`291cae35-28ab-3b3f-b1c6-4676f86248a8`,
`29d2df4a-279c-453f-b2a5-7e1a41888db1`,
`2e887f04-8243-be3e-b4e2-d4217ea9b658`,
`3e7a4274-b43f-113f-bfe3-ff9d4588b4ab`,
`6128a3df-5209-223d-881c-57b6df703c78`,
`622883ca-f03a-2339-8c8f-79af65e038d8`,
`6a7371b5-1dea-f733-985c-fd5283df9bcf`,
`6e0322fe-0bb9-5339-a709-148421ec8772`,
`92ade674-4df7-0e3d-873c-ccb1f51cb77a`,
`a1369764-9a24-233a-8d65-60c0d41c913f`,
`c29c2f3c-f5ef-8732-a1d7-0270dab60996`,
`cb6e4808-b0b4-163c-ae89-6b5a83436ade`.

### No matching organization — 1

Reason: the Charlottenlund title could not be matched to a complete official field and Swedish
participant list. It remains unmarked because no matching organization entity was identifiable.

ID: `a8639853-c565-a431-8db0-f0c6ea38a493`.

## Public verification

All affected schedule pages on https://sesport.se returned HTTP 200 and were reloaded after the
Lukas Sandell club correction. Card counts below include grouped cards once and show the before
and after counts captured during this run:

- 2026-11-02: 0 → 0 cards.
- 2026-11-03: 0 → 1 card.
- 2026-11-04: 0 → 0 cards.
- 2026-11-05: 4 → 5 cards.
- 2026-11-06: 3 → 4 cards.
- 2026-11-08: 0 → 7 cards.
- 2026-11-09: 0 → 0 cards.

Rendered headings and schedule rows show the event group once, distinct channel segments, Swedish
times, and the expected activities. The 5 November YOG card groups two schedule segments under
one heading; the 6 November YOG card does the same. The Verona and NOCCO segments display as
grouped cards. WWT ends at midnight local time and remains on 8 November.

All displayed participants have ages, formative/current club context, and working dedicated
profiles. All 74 distinct person-profile URLs were checked successfully. Twelve displayed provider
URLs were checked and returned HTTP 200. Thirteen country flag SVGs and five sport icons loaded.
Team-sport flags and activity/group context display correctly.

## Dashboard

The admin dashboard reported no upcoming activity issues and no open todos. Its coverage table
showed one unreviewed broadcast on 8 November: the unresolved Charlottenlund feed
`a8639853-c565-a431-8db0-f0c6ea38a493`. No other dashboard issue required a data correction.

## Validation

Readback matched the 109 snapshot outcomes, 15 new published activities, 129 participant links,
19 broadcast links, and 100 typed evidence records. The two existing handball activities each
have 17 active participants. All processed and rejected status changes matched their expected
scope; all 60 unresolved rows remain visible and unprocessed. No automated tests were run.

## Follow-up — Olympic Games organization and Dakar group

Applied: 2026-11-02 05:13 UTC.

- Reclassified entity `9dcc94d4-352e-4dc8-9b00-c94c2a25f04d` from the Dakar-specific
  Championship to the general Organization `Olympic Games`. Updated its profile to the official
  Olympic Games overview and its stability to long term. Its 14 broadcast references, seven
  activity organization references, seven participant contexts, and two athlete links remain.
- Consolidated the Dakar activities under group `2d15029c-31a8-4fdf-abca-49d973913afb`, titled
  `Ungdoms-OS i Dakar 2026`, with multi-sport scope and dates 31 October–6 November. Moved the
  two beach-volleyball activities from group `44284fbe-d7f0-4696-8e87-3c1ae41a40fa`, then removed
  the empty duplicate group. All seven activities now share the Dakar group.
- Moved the three beach-volleyball participation sources to the retained group and added one
  ActivityEvidence source for the official IOC Dakar sports programme. Seven group evidence
  records remain attached.

The official Olympic Games overview is the organization's profile:
https://www.olympics.com/ioc/celebrate-olympic-games
The IOC Dakar 2026 sports programme is stored on the activity group:
https://www.olympics.com/ioc/dakar-2026-sports-programme

The public schedule pages for 31 October, 1, 3, 5, and 6 November returned HTTP 200. Rendered
entries on 31 October and 3, 5, and 6 November now show `Olympic Games` and
`Ungdoms-OS i Dakar 2026`; 1 November still has no Dakar activity because that activity is Draft.
Activity-entry counts were unchanged across all five dates.
