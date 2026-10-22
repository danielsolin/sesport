# Activities job - 2026-10-22

Started: 2026-10-22 11:44:00 UTC.

Snapshot: 116 broadcasts with `hidden_at` unset, across all dates and process states.

- Hid 63 clearly irrelevant broadcasts by changing only `hidden_at`.
- Linked 9 confirmed feeds to published activities, set their organization, marked them
  processed, and hid them.
- Kept 44 uncertain broadcasts visible and unprocessed, with event organizations and
  `ActivityEvidence` sources. No visible broadcast is missing an organization marker.

## Published activities

All times are Europe/Stockholm on 2026-10-28.

- 08:20-10:50, `Sverige – Norge`, SVT2 and SVT Play.
  Activity `7b83fe90-c8b6-466e-bec0-4be4e0ba758e`.
  Broadcasts `e1a5d070-973d-cc3d-8181-c88c3aa36071`,
  `d17d1452-16aa-1635-ae80-1dd5f0b42911`.
  Direct stream: https://www.svtplay.se/video/KwqanzE
- 11:50-14:20, `Sverige – Danmark`, SVT1 and SVT Play.
  Activity `93fd1454-e73c-4907-a549-5d4b024db6c1`.
  Broadcasts `a3081c47-60fa-7139-8975-af7117adfef4`,
  `945dd014-6257-e93f-b111-3ededb784eb7`.
  Direct stream: https://www.svtplay.se/video/K5dVnAP
- 17:50-20:00, `Sverige – Tjeckien`, SVT2.
  Activity `7f3e130e-ed08-4a58-89fe-db5c2655da1c`.
  Broadcast `14e08a05-f95d-5831-9309-e2dc86541ff8`.
- 17:50-20:30, `Sverige – Tjeckien`, SVT Play.
  Activity `974b59e8-54d7-4b55-9d42-eba5c2f28e49`.
  Broadcast `f2c4a9c2-acb1-f438-a963-ebd1eca96fa8`.
  Direct stream: https://www.svtplay.se/video/8LJpDxX
- 18:50-21:20, `Sverige – Spanien`, Kunskapskanalen and SVT Play.
  Activity `f9006e5e-72d1-42a4-91df-cc1741232c1a`.
  Broadcasts `32874e38-ad77-1c35-a525-598b1cf3bbe6`,
  `725ea692-9273-973e-bb08-67e385a0e9ac`.
  Direct stream: https://www.svtplay.se/video/jz7ak3A
- 20:00-20:30, `Sverige – Tjeckien`, SVT24.
  Activity `e3e84205-0209-458d-8ca0-d2dd53bc01a8`.
  Broadcast `b6505ca8-3358-d537-93f3-d5dd713fff6b`.

Reused `Curling-EM` (`03733f6c-77a0-476a-820a-c69ac24aa590`) and extended its end
date from 2026-10-27 to 2026-11-01. Created `Lidl Hockey Games`
(`47008f6b-746b-4d45-9569-3e00fb3dc11e`), spanning 2026-10-26 through 2026-10-31.

## Participants and corrections

- Linked the four confirmed men's curling roster members to the three men's matches.
  Created Oskar Eriksson, Rasmus Wranå, Simon Olofsson, and Christoffer Sundgren with
  verified birthdates, SOK profiles, current team and club links, and `tier_0` priority.
  Oskar's formative club is Lit CC; the other three are linked to Karlstad CK.
- Linked all five confirmed women's roster members to `Sverige – Danmark`.
- Linked all 24 official Damkronorna selections to each of the three hockey activities
  (72 links). All have birthdates, profile URLs, formative clubs, and current team links.
- Corrected current teams: Ebba Hedqvist to Luleå HF; Josefin Bouveng to Las Vegas;
  Lisa Johansson to Frölunda HC; Lova Blom to Brynäs IF; Michelle Löwenhielm to
  Linköping HC; and Thea Johansson to HV71. Added missing links for Ebba, Lisa, Lova,
  and Michelle after readback. Replaced Luleå's generic homepage URL with its official
  Luleå Hockey/MSSK squad page. The four added links have `PersonFacts` sources.
- Created the Las Vegas and HV71 Team entities with official URLs. Created the Global
  Champions Tour entity for the unresolved Cairo broadcasts.
- Set official team URLs for LHC, SDE, Färjestad, Ohio State, Minnesota-Duluth, Brynäs,
  Hamilton, Frölunda, Toronto, Penn State, and the women's national team. The 16 current
  teams linked across the roster all have URLs.
- Added official event and roster evidence, four direct SVT Play stream links, and
  eight `ParticipantStarEvidence` sources for the four men's curlers.

Conflicting facts retained: the SOK profile gives Simon Olofsson's birthdate as
1999-04-22; World Curling gives 2000-03-08. Both sources are saved, and the SOK date
remains stored. The Swedish federation lists Alexander Lindström as coach, while the
World Curling roster lists him as alternate A. He was not linked as a competitor.

## Existing activity corrected

Dashboard review found missing participant start times in the published Butterfield
Bermuda Championship round 1 activity `86841011-0000-4000-8000-000000000109` for
2026-10-22. Added sourced local start times: David Lingmerth 12:46, Jesper Svensson
13:52, Henrik Norlander 17:38, and Pontus Nyholm 18:33. The source is the official
PGA TOUR round 1 tee sheet:
https://beta.pgatour.com/tournaments/2026/butterfield-bermuda-championship/R2026528/tee-times

## Hidden broadcasts

The 63 rows below were hidden without changing their process state or organization.

- Complete international fields with no Swedish participants (8; 10
  `ParticipationEvidence` sources):
  `d83d6b44-51da-4932-a83b-636c02a10fbd`,
  `02009b5e-eb82-9630-80bf-3795ac732366`,
  `72f9a920-f02f-ad3a-bb4c-4c0148553492`,
  `1fe64784-adf1-693b-9243-567415e31220`,
  `aa070ed0-a7dd-5230-a45b-3c0b0c79e966`,
  `be958ce5-a9cb-3932-8ed7-14b846e724b7`,
  `6acc274a-ccbd-2334-96f9-f1e61775ff07`,
  `2243cc2d-5638-5d32-892c-1ee6ae337ef9`.
- Domestic competitions (16):
  `88393f72-d7a8-203a-aa85-b1789ed657a6`,
  `18462afe-e90c-0e38-8f10-6c472272e3ab`,
  `5121bfbb-96be-033e-93cc-1890a7344a5a`,
  `870f43ff-bcb2-7b3a-9e46-9ead67fd6d39`,
  `413b978e-eb69-8134-903f-56eb9119f58b`,
  `95b7503c-576e-983e-933f-c0e00cbf842e`,
  `5c099bc7-6687-0d34-9662-392558a967b8`,
  `94488fa0-7501-6a35-8356-d0ff8e08cb7d`,
  `56213a1c-b08c-9631-8ebb-6b7a59ca6ac6`,
  `a3840d81-6ee5-cb33-b2c4-6c0e77945444`,
  `016f40ee-098d-7636-aba1-4f204a0317c7`,
  `fd264af0-160f-2c3b-a7d9-9f4665e3f1c9`,
  `b18ea0e7-00c4-aa3d-882c-f3954a64c0a3`,
  `419f8034-6c5a-1234-a361-13667fc25a9c`,
  `9b61bd70-39e9-3f32-8f8e-0a2740a3d9e1`,
  `d29d7bfb-4c1e-1734-9252-c5d1fb5b3f11`.
- Studio, preview, or recap programs (3):
  `3f737cbb-a1f4-d63c-9ea0-70afbfd115b3`,
  `3c3b7f9e-18c1-db3b-ba5c-1833052ce437`,
  `13fdd91f-6b5e-863b-9f71-9cf7806cd223`.
- Foreign football fixtures (30):
  `69ca55b9-0200-3635-a450-e4eea560b9d0`,
  `7680df69-24cb-7133-9b6e-4ce05a5f64ee`,
  `8bfe0255-5732-2332-a55d-5877f70a6732`,
  `f4c2a610-e539-3e30-8161-2fb19709e008`,
  `737c75ad-7ea2-743b-9f7b-40e7c9632858`,
  `ec1450a5-5625-c43d-b491-b9e0c95fd9c4`,
  `eb4841f2-5dcd-e239-bcfb-9f59a9db42ad`,
  `f8877d05-66e8-5334-8504-0779568155ea`,
  `fcca933a-ed8a-6837-800f-e3a2885e5e5b`,
  `9918bce3-c441-a837-9836-2c62c16bae78`,
  `89cbb0be-e951-5538-95e1-76eedc32b6a9`,
  `de7ae37b-6467-193c-86c9-cbc52b6f867f`,
  `8cac282e-d593-9a33-8601-6cf2a3cff8ca`,
  `ae5e6b1a-2c8b-f338-a62b-d1800b551581`,
  `d63d7b06-53e8-d730-8725-c1fe0bfa1cdc`,
  `b99ff0ee-968e-8f32-8ceb-fb449f3522ff`,
  `7c5f761b-49df-5e38-a55a-c2953a2b939b`,
  `5be8dada-b6c6-aa3c-8b84-f92edc7d0280`,
  `adbc7463-caca-4c3c-9216-5363208e0198`,
  `4db94841-558b-1d39-82ed-5d09ce94d2f5`,
  `299acf3a-65aa-c53f-9c73-4f7d947a16fb`,
  `0eafb27a-9e5e-fd38-a96e-360b31fc7c0d`,
  `31f7e969-07c2-7234-b85f-46fa1891dfe7`,
  `03ad57c4-95b0-583d-bc2c-52519e9332e2`,
  `9a88c062-bf99-3334-8022-e8a6911ce8f1`,
  `71510055-bbd6-9535-bca1-f35f6b795c09`,
  `b105caec-2570-103a-8f80-b31883fe717d`,
  `b7ec7737-3812-3c3a-8b90-77b900fa345c`,
  `13891621-aa91-d03f-af08-6e5cf4fd8c28`,
  `3674abfa-6035-1936-812f-776d72f8ad07`.
- Finnish Liiga (3):
  `a1fff281-e594-ee3a-9ca2-c69423760a55`,
  `c8577d4c-8c09-833e-bd3c-2ab94928057c`,
  `e4aa35c7-3e2a-3036-a664-18da3ae6c7a6`.
- Foreign curling and beach volleyball (3):
  `0831a5e3-de7b-f73f-80ac-ef36a296ee41`,
  `f4b36ff6-f926-c032-85e3-f7f6491830d9`,
  `30d933b1-50d7-9c3c-ad26-a2cf67ac3ad9`.

## Unresolved visible broadcasts

All 44 remain visible and unprocessed, with the listed organization and saved
`ActivityEvidence`. Complete official participation lists or exact event mappings were
not available.

- World Nineball Tour, Philippines Open (4): event page lacked the complete draw.
  `7a3654af-e943-5d33-b778-c0fa8848cb58`,
  `87334024-ebaa-c136-875d-46f7e6f6461f`,
  `48eeb916-021a-4937-b7cc-c3e379f38778`,
  `1a77e776-5794-f639-87f9-9317f591f37e`.
- Motorsport, Australian event (12): no complete grid was available.
  `3234cb5f-8700-8d34-a44b-e6201f922daa`,
  `9f945ce3-a4ab-ca3c-9158-98d42a6ec71e`,
  `293bd103-7df1-f336-8642-e748d32269cc`,
  `2d57ded2-4202-0f35-8c85-af32ef627165`,
  `ced0d20e-c7ad-7639-a715-4ef92a20d787`,
  `4cff8f0d-2d36-163e-8709-f4be842bc757`,
  `d056e142-380e-2733-9f5d-772566363523`,
  `259d7764-ed01-233f-beed-658fc9307127`,
  `2c0c58eb-0704-9f36-9fb1-eb1edf854523`,
  `1be94f34-9100-473e-91a9-bb27333f6c16`,
  `24e0dee0-6c44-d637-921f-e86989bd3f06`,
  `0a479599-0010-2833-8899-de9816f8791c`.
- Golden Trail World Series, Muju (4): official event page lacked the full field.
  `2ecd6d1e-1b5e-4c33-b96a-58c5b9ac8549`,
  `9f6d6a22-f7eb-e73d-a6a8-5adf31be4db3`,
  `fb272e53-b3d2-bd35-8f98-712c68200ea9`,
  `9523721b-fc14-0b31-aa07-f9204d856815`.
- Climbing (3): the official start list was not accessible.
  `c50665d2-7277-5131-a379-95951de3a0b5`,
  `417bea28-3fc1-4631-a005-f26e858088f7`,
  `71f4494d-6c04-4335-a84f-094ec8bbcf99`.
- Cycling, HG Cross and Superprestige (5): no complete field was available.
  `144ad12a-d060-0b34-b462-110b32a42df3`,
  `82167e54-e1bf-3c32-82b6-58ebb697babe`,
  `98a8ed58-106d-e43d-ad78-0d87e022e25d`,
  `6a90d0ec-2618-613a-8360-fc1db48d0762`,
  `144a32fe-0843-6a39-9c72-7dda120c5a03`.
- Global Champions Tour, Cairo (1): start list unavailable. Organization entity
  `3237ea4c-5798-4cbf-9a0b-670b51b28a4b`.
  `ecc663d7-98f1-0d39-8fca-18f71a9657ea`.
- ATP Tour, Vienna and Basel (12): the 2026 draws were not published.
  `b51a703f-b53f-ca32-a516-172286c93996`,
  `081be24b-1fe1-fc34-ae47-47db6f56dbc1`,
  `a2901efc-418d-e63a-9529-be92532a3d54`,
  `b7ca1054-077b-3430-9e66-7fa649760eed`,
  `491b4c64-21b7-d33b-b9ed-363e6c8eb23d`,
  `8c14d0a7-9500-f739-8542-c73805d2c1ab`,
  `7c594318-bb24-b73d-a450-208ffbc6416c`,
  `c35b46f7-07d0-a934-b50c-aeb28f6bb51b`,
  `8169f842-8be2-9a30-8450-367a3edf810e`,
  `d50be9d7-193b-5932-beef-5a0508a4fdba`,
  `eaba3cbd-5cdd-7e38-8984-dac923fdf4a1`,
  `04912ec1-ae53-f731-8882-7015d500b361`.
- LPGA Tour, Maybank (2): the field remained unsettled; Francesca Kinhult was reserve
  number 8.
  `98d041b0-f9a9-3339-9515-48fceb3de7db`,
  `d134b4c7-59e3-ba35-bdd5-c9b57a0e4e0b`.
- Generic curling feed (1): its 18:00-21:00 range did not identify one scheduled
  match. Marked to Curlinglandslaget (herr), `62eb64b5-f26d-402a-8756-3eacea10b993`.
  Broadcast `4eaab9da-4d5b-1c35-a7da-ce9792411992`.

## Public and dashboard verification

- 2026-10-27: 2 cards before, 2 after. The date still shows the F16 match and the
  existing Sweden-Estonia curling match.
- 2026-10-28: 0 cards before, 4 after. Three curling matches are separate cards;
  the three distinct hockey broadcast segments appear in one grouped match card.
  The database retains the ranges 17:50-20:00, 17:50-20:30, and 20:00-20:30.
- 2026-10-22: 4 cards after the tee-time correction. A pre-change count was not
  recorded; only participant start-time metadata changed, so the card structure stayed
  the same.
- All 24 hockey roster profiles and all linked curling participants are linked in the
  public markup and display ages and formative clubs. All 16 linked hockey team URLs
  loaded. Country flags for Sweden, Canada, and the United States loaded; the hosted
  curling SVG returned 200.
- Seven profile URLs returned 403 to command-line `curl`; the browser reader loaded all
  seven dedicated profiles and confirmed the matching players.
- Admin Dashboard now says “No upcoming activity issues.” Two existing Todos remain:
  the upcoming-coverage yellow-mark check and the daily verify-data reminder.

## Evidence and failed checks

Saved this run: 44 broadcast `ActivityEvidence`, 10 broadcast
`ParticipationEvidence`, 6 activity `ActivityEvidence`, 6 activity
`ParticipationEvidence`, 4 direct `StreamLink`, 2 new group `ActivityEvidence`,
33 `PersonFacts`, 8 `ParticipantStarEvidence`, and 1 `ParticipantStartEvidence`.
Representative sources saved:

- SVT curling schedule: https://www.svt.se/sport/curling/guide-curling-em-2026
- Swedish Curling Federation event roster:
  https://www.curling.se/tavlingar--resultat/em-damer-och-herrar-2026
- Swedish Ice Hockey Association roster PDF:
  https://via.tt.se/files/2133149/4604435/449656/sv
- TV.nu hockey listing: https://www.tv.nu/p/1Xm9p8-R-kRuP
- SOK and World Curling athlete profiles, PGA TOUR tee times, and direct SVT Play
  pages are saved with their evidence types in the database.

The PGA TOUR tee-time page returned 403 to direct fetch; its official indexed page
provided all four group times. Three guarded write attempts aborted and rolled back on
row-count or psql-scope checks. Corrected transactions passed readback; no partial
changes remained. `git diff --check` passed. No tests or builds were run because this
job changed database records and a report only.
