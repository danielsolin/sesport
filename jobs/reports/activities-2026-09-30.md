# Activity run — 2026-09-30

Started: 2026-09-30 04:43:33.858515 UTC.

## Outcome

- Snapshot: 199 visible broadcasts, all unprocessed and not hidden. Saved at
  `/tmp/sesport-activities-snapshot-2026-09-30.csv`.
- Created 18 activities, updated 15 activity time ranges, and created 42 broadcast links.
- All 42 in-scope broadcasts now have the matching activity, organization, and processed status;
  they were hidden after readback verification.
- Hid 49 clearly irrelevant broadcasts. Total newly hidden: 91.
- The final visible queue contains 108 unresolved broadcasts. All remain unprocessed and have an
  organization marker.
- Reused groups: WRC `56424e09-0079-4fec-ba3c-daf079d1637e`, WTT
  `f819e634-3b86-4b76-ab9d-81ecd96916c1`, Nations League
  `01071d39-9288-454f-9c4b-54e3d167aecf`, and U21
  `16d6644c-06a8-413f-b4e6-ff87e079278d`.
- Extended the Nations League group through Oct 5 and the U21 group through Oct 5. Created CHL
  group `4f0373e1-59da-4c5b-aa03-d30fe49812a1` for Oct 6.

## Created activities

Times below are Europe/Stockholm. Each activity has the broadcast shown in the link map below.

| Activity ID | Public date | Segment and local range |
| --- | --- | --- |
| `4ab5e72d-2673-40ef-b798-608fc86f98b6` | 2026-10-04 | WRC Sträcka 14, 08:15–09:35 |
| `6c55ebb8-84e8-428e-ac81-71bc9b3751ce` | 2026-10-05 | China Smash Dag 5, 12:00–16:00 |
| `c8b7bd38-fa55-4a82-8ba2-903c4de678eb` | 2026-10-06 | China Smash Dag 6 morning, 05:00–09:00 |
| `a778e50c-41cf-49ff-8f5b-f17f694402b4` | 2026-10-06 | China Smash Dag 6 day, 12:00–16:00 |
| `f4cb6f6a-1130-42d7-9fb1-fbb53c4290e0` | 2026-10-07 | China Smash Dag 7 morning, 05:00–09:00 |
| `20cd76fb-0041-43c3-827d-1601b5df6ca4` | 2026-10-06 | Plzen–Skellefteå, Viaplay, 17:25–21:00 |
| `92878499-9179-4ab9-a43c-099fed277d94` | 2026-10-06 | Plzen, Viaplay Sport 17:25–20:00 |
| `fba3945d-d018-4494-a63b-d81fffb4595c` | 2026-10-06 | Liberec–Frölunda, Viaplay, 17:55–21:30 |
| `5378604b-bc28-45e9-9bfe-6da155ae0438` | 2026-10-06 | Frölunda, V Sport Vinter, 17:55–21:00 |
| `e55477a0-9d5c-40c0-b4b2-701fc249f976` | 2026-10-06 | Växjö–Geneva, Viaplay, 18:55–22:30 |
| `df85f508-d3e1-4458-9b3a-e7422aaeda2c` | 2026-10-06 | Växjö–Geneva, TV10, 18:55–21:30 |
| `b0b836f8-82c9-4e18-9745-ed09166d2496` | 2026-10-06 | Rögle–Davos, Viaplay, 18:55–22:30 |
| `a107ab43-05bc-488b-89fa-be97cf43fe6c` | 2026-10-06 | Rögle–Davos, V Sport Extra, 18:55–22:00 |
| `dd41fd3b-a613-4386-969c-f34424dc5dac` | 2026-10-05 | Romania–Sweden pre-match, 20:00–20:40 |
| `ac27d164-662a-443f-aa28-45b08f9e4eea` | 2026-10-05 | Romania–Sweden, 20:35–23:15 |
| `0f13a6e4-6443-4ef3-890c-ca0c6ab29e5f` | 2026-10-05 | Romania–Sweden Viaplay Sport, 20:40–22:45 |
| `17b2238b-2b96-5c18-996a-e84c0e5a1a4f` | 2026-10-05 | U21 match, Viaplay, 18:20–21:00 |
| `96633e2d-ca48-571a-b023-4c7849d09310` | 2026-10-05 | U21 match, V Sport Premium, 18:20–20:35 |

The U21 fixture uses two separate activities because the broadcast end times differ. Each links
all 23 players on the official squad. The four China Smash activities link five confirmed Swedish
players each. The three Nations League segments link the 24-player senior squad each.

## CHL roster correction

- Follow-up linked each full official 2026/27 club roster to both existing broadcast segments for
  that match. These are season squad lists, not matchday lineups. No CHL activity record was
  created or retimed.
- The 97 distinct players have 194 active links across eight activities. Each public match card
  displays its Swedish roster: Skellefteå AIK 27, Frölunda HC 24, Växjö Lakers 22, and Rögle BK 24.
- Participant links changed on these existing activities:
  - Skellefteå AIK (27 per card): `20cd76fb-0041-43c3-827d-1601b5df6ca4` and
    `92878499-9179-4ab9-a43c-099fed277d94`.
  - Frölunda HC (24 per card): `fba3945d-d018-4494-a63b-d81fffb4595c` and
    `5378604b-bc28-45e9-9bfe-6da155ae0438`.
  - Växjö Lakers (22 per card): `e55477a0-9d5c-40c0-b4b2-701fc249f976` and
    `df85f508-d3e1-4458-9b3a-e7422aaeda2c`.
  - Rögle BK (24 per card): `b0b836f8-82c9-4e18-9745-ed09166d2496` and
    `a107ab43-05bc-488b-89fa-be97cf43fe6c`.
- Created 25 missing Person records, filled four previously blank profile URLs, and linked their
  current Teams and the CHL organization. Created 19 Club entities, reused five, and linked all 25
  new Persons to sourced formative or earliest documented development Clubs.
- The country catalogue lacks Slovenia. HKMK Bled uses `country_id = int`; its entity reason
  records the known Slovenian location. No country row was added.

## Updated activities

Fifteen existing activity ranges were corrected from schedules. Calendar dates and public dates
can differ for overnight golf broadcasts.

| Activity ID | Public date | Updated local range |
| --- | --- | --- |
| `a2c7a81d-c2ac-461f-852e-3948e40931ea` | 2026-10-01 | Alfred Dunhill Links Day 1, 13:00–18:00 |
| `697165ce-caa1-4b12-b17a-481f817cb041` | 2026-10-01 | Bank of Utah Day 1, 15:45–23:55 |
| `65b42d09-e0da-48d8-b35b-23e76b8803c1` | 2026-10-01 | LOTTE Day 1, 02:00–05:00 on Oct 2 calendar |
| `307cba39-6db8-4473-bbcc-62da7f6d781b` | 2026-10-02 | LOTTE Day 2, 02:00–05:00 on Oct 3 calendar |
| `b991397d-07a1-46e2-9ddb-c35693e0b4eb` | 2026-10-02 | WRC Sträcka 2–4, 07:45–14:00 |
| `b72f5194-9173-4359-89ce-a004b075fb11` | 2026-10-03 | LOTTE Day 3, 02:00–05:00 on Oct 4 calendar |
| `539ff46b-86d0-4257-9e62-d88043fff6cf` | 2026-10-03 | WRC Sträcka 9–10, 09:00–12:00 |
| `780a408b-007c-4224-a966-582f94eb52ee` | 2026-10-03 | UEC elite women road race, 13:20–17:40 |
| `377e7e32-b097-4c9d-aecc-9e2617460f5d` | 2026-10-03 | WRC live Sträcka 3, 14:30–15:35 |
| `96cb7012-69aa-4f3b-bf29-4bb221224dd3` | 2026-10-03 | WRC Sträcka 12–13, 15:30–18:00 |
| `979576db-e3d6-4666-b9e4-756ffecf24c0` | 2026-10-04 | LOTTE Day 4, 02:00–05:00 on Oct 5 calendar |
| `26508e1b-513c-4c91-96f4-ed299e63f35c` | 2026-10-04 | WRC Sträcka 14, 08:30–09:45 |
| `5a6aee4b-e778-4921-8249-396f77dffc91` | 2026-10-04 | WRC Sträcka 16, 11:00–13:00 |
| `7db97272-5fe0-4ed9-a482-55e1266f810b` | 2026-10-04 | UEC elite men road race, 12:20–17:15 |
| `b12677cd-d993-45a1-a8d3-4683ccb56ceb` | 2026-10-04 | Motocross of Nations, 15:55–17:15 |

## Broadcast links

Each pair is `activity ID <- broadcast ID`. These 42 links were read back after writing.

### Public date 2026-10-01

- `65b42d09-e0da-48d8-b35b-23e76b8803c1` <- `3243f891-0d99-223f-804a-e8f6f5a1e68f`
- `a2c7a81d-c2ac-461f-852e-3948e40931ea` <- `abe34101-4278-373f-8a9e-1c1c7c1ea286`
- `697165ce-caa1-4b12-b17a-481f817cb041` <- `fd8b5f17-ef2c-4f3c-9421-41ab7e7d8308`
- `cdc19e98-3db7-4dbf-b207-5e56261db5a4` <- `f8e1156d-351d-ee35-9232-6e6b543cf5e1`

### Public date 2026-10-02

- `307cba39-6db8-4473-bbcc-62da7f6d781b` <- `cdea14d6-d925-2a33-bff6-398597f30034`
- `b991397d-07a1-46e2-9ddb-c35693e0b4eb` <- `dff830e9-f44b-3d3a-89a3-8c56b1b677a9`
- `721dd62a-12b6-4695-8abc-476516630798` <- `cb09f5a3-06a3-2230-972a-31b169ad68f1`
- `c000a302-abc5-419e-859b-6277384c48dd` <- `f24dd341-1379-1239-af1d-3f9591668291`

### Public date 2026-10-03

- `b72f5194-9173-4359-89ce-a004b075fb11` <- `a4af0d78-0f65-f43c-a7f0-2a496c44a44f`
- `1fe3d358-cef9-4229-bf77-3f67cb7fdecf` <- `e0cb2446-b759-723a-bba6-83383d243d20`
- `539ff46b-86d0-4257-9e62-d88043fff6cf` <- `ee5be7ae-7d5a-4e39-9617-b7162a5778e0`
- `780a408b-007c-4224-a966-582f94eb52ee` <- `ded05011-e371-ad31-add3-c68caa0d91a2`
- `377e7e32-b097-4c9d-aecc-9e2617460f5d` <- `f27035d3-9225-5c3d-8d40-33922bc12b3e`
- `96cb7012-69aa-4f3b-bf29-4bb221224dd3` <- `2c569ed3-9ee0-1a3e-bb3b-91e83f5d3f1e`
- `45411d19-de43-4612-a99e-4889ba982f8b` <- `14dbb110-6fa0-ff38-9338-756c5802f6a9`
- `fbcdfcd7-05b8-4e2e-9345-bb98b79d78e9` <- `05671436-f9de-0232-9810-76a24ce3b61b`

### Public date 2026-10-04

- `979576db-e3d6-4666-b9e4-756ffecf24c0` <- `66b7601e-b3c7-783f-87e8-3365b8c9a9d3`
- `4ab5e72d-2673-40ef-b798-608fc86f98b6` <- `e2e5e18d-dd0f-7c39-9103-9f064996c7c7`
- `26508e1b-513c-4c91-96f4-ed299e63f35c` <- `b4b7735f-4753-0937-819e-f9b9464642b9`
- `201906f6-36a2-40fb-809e-44d3fc3d6c0b` <- `09078a9c-bb92-aa31-96d1-4590a6ee9e89`
- `5a6aee4b-e778-4921-8249-396f77dffc91` <- `f5b2b067-901b-073c-a11e-043b886331bb`
- `7db97272-5fe0-4ed9-a482-55e1266f810b` <- `12a62f94-97d0-223d-b158-61b3c082ac95`
- `15dfe850-f1cb-4356-80c6-1c5c49bbf15e` <- `6f09f909-4535-3a3c-bca0-cae553c5df6f`
- `b12677cd-d993-45a1-a8d3-4683ccb56ceb` <- `4093a88a-0e4b-a537-9fc0-66907e02e8b8`
- `399cb662-c296-4aa6-84ab-9161ef6a3351` <- `38210b9e-33d4-e23d-bab4-6f713e93e4c5`

### Public date 2026-10-05

- `6c55ebb8-84e8-428e-ac81-71bc9b3751ce` <- `4b68df0d-0d58-9c3d-93d8-fdf3950e9b9d`
- `17b2238b-2b96-5c18-996a-e84c0e5a1a4f` <- `0a454913-1b78-4b37-a2dd-30ae6f70dffe`
- `96633e2d-ca48-571a-b023-4c7849d09310` <- `a5699399-f82f-e437-938b-2e548a08a7a5`
- `dd41fd3b-a613-4386-969c-f34424dc5dac` <- `45fdca2d-974c-3937-97ea-efdadecdce79`
- `ac27d164-662a-443f-aa28-45b08f9e4eea` <- `e86e23b3-a8db-ed37-a9c3-969e0411cfb3`
- `0f13a6e4-6443-4ef3-890c-ca0c6ab29e5f` <- `6b256165-58ac-3835-bbdb-60070ac2f804`

### Public date 2026-10-06

- `c8b7bd38-fa55-4a82-8ba2-903c4de678eb` <- `3d9c1ae6-e67c-2630-9227-c3c941cc365c`
- `a778e50c-41cf-49ff-8f5b-f17f694402b4` <- `7e1984c0-678f-2c3f-9e1f-79e574d50ffa`
- `20cd76fb-0041-43c3-827d-1601b5df6ca4` <- `bcb16cba-7d30-b73f-a155-548a667b29b9`
- `92878499-9179-4ab9-a43c-099fed277d94` <- `28d66379-d754-2832-b259-54467a17a19c`
- `5378604b-bc28-45e9-9bfe-6da155ae0438` <- `29a7f1a8-90e0-8c38-a6ea-fcf8e1195454`
- `fba3945d-d018-4494-a63b-d81fffb4595c` <- `0f134ebc-8d2c-033a-a083-090a9f83be73`
- `a107ab43-05bc-488b-89fa-be97cf43fe6c` <- `66adfadd-510b-a23b-b549-123d0d1a17d1`
- `b0b836f8-82c9-4e18-9745-ed09166d2496` <- `281c570d-2e62-4934-b64b-68eafd58a540`
- `df85f508-d3e1-4458-9b3a-e7422aaeda2c` <- `acd8deac-8216-cc3f-85a0-f9b70f6bad16`
- `e55477a0-9d5c-40c0-b4b2-701fc249f976` <- `a0b6cb37-7104-9938-95a2-22408e641556`

### Public date 2026-10-07

- `f4cb6f6a-1130-42d7-9fb1-fbb53c4290e0` <- `6296dc9e-e90f-ff3a-bc35-ea42b65b8a9b`

## Other corrections and verification

- Added 14 missing U21 Person records; all 23 official squad members now have active links on
  both match segments. Added nine Team and seven Club entities for the roster. Verified birth
  date, profile, formative club, gender, and organization links.
- Added 15 verified Person-to-Team links and 13 Team entities for other event participants:
  Anton Källberg → Borussia Düsseldorf; Christina Källberg → Halmstad BTK;
  Elias Ranefur → Ängby SK; Kristian Karlsson → Borussia Dortmund;
  Truls Möregårdh → Eslövs AI BTK and 1. FC Saarbrücken-TT;
  Caroline Andersson → Liv AlUla Jayco; Jakob Söderqvist → Lidl-Trek;
  Julia Borgström → Hitec Products-Fluid Control;
  Alve Callemo → Young Motion powered by Resa and Team Sweden MXoN;
  Isak Gifting → JK Racing Yamaha and Team Sweden MXoN;
  Alvin Östlund → Team Sweden MXoN; Oliver Solberg → Toyota Gazoo Racing WRT.
- The 16 affected people without a current Team link are 15 golfers and WRC co-driver Jonas
  Andersson; no verified team affiliation was available for them.
- Repointed Elias Ranefur's formative-club relation to the existing correct Ängby SK entity. After
  confirming the misspelled Ångby SK entity had no other references, removed that duplicate.
  Moved relation `d6f72723-bfdc-1060-2b9c-e4eb7738caf9` to Club
  `80a9c891-c321-4c10-ab50-1ad30f965900`; removed typo Club
  `263b2aa7-5ffb-444e-a82f-4d2b303a56d5`. Also removed one unused Jönköpings Södra IF Club.
- Reviewed 10 existing tier_0 stars; all remained qualified. Added 20 star evidence rows without
  changing priorities.
- Bank of Utah pairings: round 1, Pontus Nyholm and Jesper Svensson 17:15, David Lingmerth 21:45;
  round 2, Lingmerth 16:50, Nyholm and Svensson 22:10. LPGA and DP World Tour pairings were not
  published, so no unsupported times were added.
- All 75 affected Persons have a birth date, profile URL, and formative Club after the run.

Evidence saved in the database: 34 ActivityEvidence rows, 19 ParticipationEvidence rows,
2 ParticipantStartEvidence rows, 20 ParticipantStarEvidence rows, 131 PersonFacts rows, and
18 StreamLink rows. Main source authorities included SvFF, TV.nu, WTT/ITTF, CHL, WRC, UEC,
official club/federation pages, SVT, and Golf Channel. Detailed URLs are stored with each row.

## Irrelevant broadcasts hidden

These 49 rows were hidden without processing. Reasons are based on the snapshot metadata and
linked context.

### Foreign national-team fixtures (22)

- `3762563e-5391-ad34-85e3-e1284cfd9237`
- `41d500d2-fe82-103e-b3d1-9ded25cf0293`
- `5215c5dd-c784-1f38-abcc-008b5d436709`
- `5a2dd0a2-62b0-3b34-b66d-00ed4784e490`
- `66fd358b-4f76-c93f-bc47-899368e2bba0`
- `6babd910-2ce5-5338-894d-e725273ae0d3`
- `6f8c90ae-0dc7-8c3d-a706-f6a472fa009a`
- `70c4c64c-8747-b93c-a76b-b2a777563e11`
- `7a7bb467-7325-e633-987e-64b3c6e3746f`
- `7b655e53-db4e-3535-99a3-b5e2537914bb`
- `80d20560-4434-f03e-b5bd-fb3234c8e056`
- `872f58cf-d593-4e30-a057-2fc4b69efc9a`
- `90bdedf3-cf27-7f35-b505-032ed23776bc`
- `9a2f4f78-7d47-1930-a04c-0aa4fbe36b80`
- `a4eddb68-c513-b93b-8012-bb82de405235`
- `b0415e32-9613-5331-8208-3730605ee295`
- `bbe89ff5-3eae-0636-9f44-525959513053`
- `ccca9daa-b15c-263d-82d0-eb9c47fef2ef`
- `d832d806-2219-5530-9390-a884e1383a84`
- `dd266d58-468b-6f32-b0ee-2099622d74d4`
- `ee599909-25f8-f239-9873-4f0773b69eff`
- `f0ef0e8c-614e-5838-a28a-626f2b60da5b`

### Foreign club hockey fixtures (8)

- `1f421708-5372-0c39-970b-299f710ab186`
- `31ae695d-068a-4132-8eeb-d843b8159904`
- `46b810ff-e3dd-9f3a-a2fa-fc603b420359`
- `9a15ea53-d013-e030-9234-4ce37b8da15b`
- `b349e858-2737-0f3a-ae7c-76fec45d123e`
- `ea43d151-11c3-d53a-b78d-611bf9d16d97`
- `fbc55eb4-bc8c-073d-9a8e-296edbaa4554`
- `fe99b487-0b55-9432-ae9f-0a2a05d725af`

### ETRC without Swedish entries (4)

- `23b1a80b-6a31-733b-aa32-58bb19aeec05`
- `dd1c6bee-62e2-2f3a-a51f-7b70bc0936a9`
- `f5542eea-2fbf-c430-ab9c-ee652d922057`
- `ffe22667-fb9f-903b-bef3-e72831922e8d`

### Studio, highlights, or press conferences (11)

- `0871336f-3407-9430-a5af-6b8d4e5ab8ee`
- `0b5edb7d-7767-423a-bc54-275d49738bdc`
- `5c8f74f7-5512-5634-b4c6-4ade5297c665`
- `70461e6f-6b2a-4237-a00c-3e135b30ee43`
- `7736159a-ec16-313e-99f2-96e97170c98f`
- `9ab8f1ee-9f8b-ca37-af57-ec7f902594dd`
- `9e5953d0-2e52-8438-88ae-bdc16cdea66c`
- `a34754b3-c343-6039-85b5-c49b088ff2cf`
- `ab83a577-bb32-2839-b4cc-a13135edc558`
- `bff2a5e2-07a9-bc38-a620-6f75d6d61052`
- `dc9394e4-7dc6-553b-8955-19bbd9e93249`

### Unidentified placeholders (4)

- `8392f43e-e761-073b-b0d8-8f94775af6ba` — generic China world championship listing.
- `dc7c0027-399f-c438-9620-de7bdb95f662` — bare EM listing.
- `e6f7a624-d5f9-9b30-ab47-d503d637be32` — bare Dag 3 listing.
- `f268a35d-3d41-b43c-8e59-b34d8a18f532` — bare Dag 1 listing.

## Remaining visible queue

All listed broadcasts remain unprocessed. Organization counts total 108, with zero missing
organization markers.

### ATP Tour (40)

- `b80a756f-8af5-2a3b-8f38-264b0ba7a99d`, `974787a9-70c5-1037-8eaf-8718272d0dc3`
- `19028d28-1dbd-103d-8d77-7358a280ac17`, `34ece135-934b-633e-92df-9534611fd388`
- `6d22bb68-c16c-be35-b93d-dae7e211673b`, `948d085a-f4c1-9c31-910d-ab47e8ec4e2f`
- `ffa25303-3ef7-5d37-b74f-4c3fb4335719`, `080df358-3b9d-2831-9e4f-63d4f1952d42`
- `8935cb6e-8520-dc34-8441-9bad0ee2f734`, `e4abfa64-ff96-7c3f-b534-075b5fdc0a19`
- `60cd9a78-ae93-a536-ae54-62684d5aeeab`, `6fbbbb85-6162-073a-8c8b-5475a973f55e`
- `f33fd842-1fe5-5131-b6bc-9655ff20c338`, `ef5cacb9-6f9f-8c39-9758-60995821867b`
- `61546402-0b4d-0e3c-8528-ff8c520c061b`, `c3819632-d8eb-8634-93a9-78ea7008144b`
- `57fe01da-d03b-ed39-bdb1-8d762b6fa87b`, `6911c3b9-c92b-b93b-a560-573a8cf26f95`
- `8a0e4940-2452-1a32-9a59-eb363fbd4a9f`, `dcc16ea9-21ee-6e3d-b831-7780e949974e`
- `3f247772-2f8c-023c-a122-9a7ec19ab689`, `e7c133ad-b871-f93d-8a75-ce96c50afbc3`
- `02629f37-47cb-3a31-bc08-4bef2bf01b84`, `f777e1f9-f25f-4332-919d-3f73968ddeac`
- `e318413c-241f-e03e-aa8d-8ca66d1cb009`, `70e9f20f-5115-d334-be04-4f62551acf3e`
- `7bb9dddd-7c20-d33b-870e-68bd1e164418`, `3e9b66f9-eaf7-2b34-a2e3-63e1e059c004`
- `501aeac6-958b-b132-8f10-91bc94ad003b`, `74fd3ce9-5eb9-3f32-b205-19b9052a6e3e`
- `97fae1ff-efd4-fc34-84f8-b5d165ee39f9`, `d29b1e74-db7c-b53f-af15-c917f20842f6`
- `f8014754-1af5-3031-b77c-bef700ee5f52`, `4b5082ea-2d73-3436-b7a6-61a99b82d472`
- `6a917327-1ad3-0536-829a-a9101cc409cc`, `eba61ce1-3c91-963f-a28a-ed02a466122d`
- `23775ea4-61ab-4935-b6ef-11bff6edf978`, `a17165a0-87d7-7c3c-8785-1ed9096668e3`
- `a74d6e4f-3c2e-9934-b127-f72ec3cb045c`, `e74a6c74-0b00-e436-853c-33ba265417a2`

### BMX Racing (1)

- `f5478e9c-051e-d23f-a5f7-4948a0b7d3f4`

### Cycling (2)

- `d0e019ba-361b-8138-8b10-175cdfa3a7dd`, `f2e503f5-a289-713f-8ce4-10a432698ef7`

### Equestrian (2)

- `5634fd82-7d4b-0539-850c-621cb8d064e8`, `d43e5d12-8945-cd32-b7aa-e3e566471a50`

### MotoGP (9)

- `4817bd01-0413-dc3b-b3ef-1e3cb725ec1e`, `46ae1a72-6995-c838-82c6-8b4291d6d7a8`
- `f34f997d-40fe-843d-a0b9-638f53c002b2`, `099069c4-ae88-223f-8b54-8341732fdd41`
- `c839e456-f00b-9935-a5b6-0fb14729f396`, `2f52c945-fd26-c13e-8410-3668e6a76696`
- `9adb12f0-7865-6d33-a736-7c156ee39239`, `69595084-9c8f-9d38-a532-d63c32190365`
- `22fd17c5-96b7-d83c-b890-61be102d3ec7`

### Motorsport (6)

- `695fd993-79c2-1339-8275-7617b4542a6f`, `acc09cf1-e29f-fc3c-b9c1-4eefc65d9b17`
- `8b85ee84-5990-9736-b388-4bea1a831ecc`, `171a3f01-f108-c830-8eb9-2df874e6a6bc`
- `33306556-8be6-b43b-992d-47c63ea48ab7`, `3bec39ac-13c6-783d-8da7-3126aed10daf`

### Mountain Bike (18)

- `4d2ae0e7-8c71-2b3b-8186-359093c06b10`, `eb3d992a-a339-5e3d-b3d1-1b5edcd33122`
- `8b117742-ebc8-6234-a929-1d8d06aaee65`, `bef4919f-fb80-0e31-a9b2-de3fb829d96a`
- `3a845600-e059-9f3a-91ba-fc478f36f4db`, `1b087160-f4e6-e63b-8e99-bd4db4717625`
- `5d9bbf30-e761-8d3a-88b9-97248a072a29`, `6b8602a2-7cfd-7c3e-8cc6-e2401f76beff`
- `0d097239-ad30-493a-8b37-5cd45e79eca8`, `0f6f2c4d-aba0-f135-97df-0ed90504472a`
- `5f693a7a-752b-e538-a0d9-ec28ed790da3`, `657174a0-4d86-653b-a083-1fad9518e97e`
- `d4e3830e-1116-b43e-9db7-56c8a057ab46`, `6db4bf15-b2ff-7338-a4ad-a53b14f0fd53`
- `48a28e13-9e88-2235-a1a9-774de3d49897`, `98b514fc-529c-5337-9c86-36fb0ec6d951`
- `7ff88e85-50f7-3d33-8d6b-ad5adf6cc6ba`, `8341dc3e-dde0-d331-a587-f5f32d2a5e32`

### PDC / WDF (10)

- `a14fd438-46a7-8233-8b0d-c5d55f2e058f`, `26767328-9c96-b53c-a2d2-2d0abd46c86f`
- `a28347e7-3a68-ca3d-9eeb-b2237b0c3a88`, `a150c58b-dec8-2a33-9176-60d57c34126f`
- `267f991b-0715-1234-a0b8-e0d76356709f`, `a44195e0-4d96-f33d-bba6-886c054f7f52`
- `87fc7fe8-f904-4838-924a-79d5154e412a`, `b70fb770-c42b-5736-a853-586bfd72ebb8`
- `2151a250-d4b2-9e34-8804-d313dbb37b99`, `edab4f86-0990-783f-9a6a-4e747488cbed`

### UCI Europe Tour (3)

- `4326db0c-bb75-d537-b613-d42832679de9`, `af89f742-22b2-4037-9bc3-8a489a673861`
- `c641e6c0-0de6-fc3c-a2dd-8aef870f2ccf`

### UCI Pro Series (10)

- `17c1cff8-e757-0436-bb6f-1aefb297c745`, `8106d952-85a4-1534-a5f1-fff3c717cebc`
- `da7055f1-7a80-453a-b025-c242a479da15`, `44fa25a4-8e81-a032-8a56-786cd2c42b02`
- `4f29ee4b-0f38-1535-80ab-d21149a0fe5a`, `9432a0c6-3061-8735-8fbf-53fb3f5c119d`
- `f39b0ae9-d4a9-3f3f-9354-689bc1934fdd`, `7e492797-9928-0d3a-b3c8-0387ff50bd8f`
- `2c928dda-d071-8335-a66d-4dd1fc8d15d1`, `12a9167e-7688-143b-ac61-a72dd2874aa4`

### World Snooker Tour (7)

- `64fb5f58-0739-2b3d-bf50-40766a9a741d`, `9ffcb575-68f3-123f-a48c-20d6a1a47e35`
- `da2c9e3b-5903-8f38-99f3-bfc3e62a8dbb`, `621e2235-ad14-e836-94d7-379af64d4024`
- `faab65e1-f411-7133-a7b6-64a15351a632`, `5b80623b-4b23-853b-890f-24d716049f34`
- `950e9f68-ad8c-dd37-a591-1592bfca665f`

## Public card counts

Hosted counts before and after the database changes:

| Date | Before | After | Change |
| --- | ---: | ---: | ---: |
| 2026-09-29 | 0 | 0 | 0 |
| 2026-09-30 | 4 | 4 | 0 |
| 2026-10-01 | 4 | 4 | 0 |
| 2026-10-02 | 6 | 6 | 0 |
| 2026-10-03 | 8 | 8 | 0 |
| 2026-10-04 | 8 | 8 | 0 |
| 2026-10-05 | 1 | 5 | +4 |
| 2026-10-06 | 0 | 5 | +5 |
| 2026-10-07 | 0 | 1 | +1 |

October 5 adds one U21 card and three separately timed Romania–Sweden cards. October 6 adds one
China Smash card and four CHL match cards. October 7 adds one China Smash card. WRC segments remain
grouped under event cards; player start times do not add cards.

## Presentation and validation

- Fresh hosted pages for Oct 4–7 returned HTTP 200 and matched the final card counts above. The
  October 5 participant row displays Elias Ranefur's Ängby SK club and profile; his star remains.
- The hosted U21 card merges Viaplay and V Sport Premium and has no schedule rows. The database
  retains the separate 21:00 and 20:35 end times.
- Added a focused local builder change so grouped team matches with distinct ranges render one
  schedule row per activity while keeping the fixture title. `dotnet build src/SESport.Web`
  succeeded with 0 warnings and 0 errors; `git diff --check` passed.
- After the CHL roster correction, hosted and local Oct 6 pages each rendered five cards and 102
  participant rows. Every row had an age, Club, and profile link. The four CHL cards displayed
  27, 24, 24, and 22 roster members.
- The local page renders two schedule rows on each CHL card, with separate broadcast ranges. The
  hosted page still omits these per-segment rows because the builder change is not deployed.
- The local app was started with `dotnet run` for inspection and stopped afterward. Automated
  tests were not run.
