# Activity run — 2026-11-07

- Started: 2026-11-07 07:11:40 UTC.
- Database target: `sesport`, resolved from `.env` through `bin/dbc.sh`.
- Snapshot: 74 visible, unprocessed, unlinked broadcasts.
- Final audit: all 74 snapshot IDs have exactly one outcome.
- Outcomes: 14 processed, 45 rejected and hidden, 15 unresolved and visible.
- Database state matched all 74 recorded outcomes; no mismatches were found.
- The operator's `Femnationsturnering` title was preserved.

## Processed events

### WWT Championship

- Reused group `ed4b99fb-5ff0-4ebb-b42a-4def0303ab4c`.
- R3 activity `5991c6eb-4b88-4cef-8f5a-166b66a97cc6` is linked to broadcast
  `62317a79-ff11-f738-a729-4e09b9d26b78`.
- R4 activity `c81fde5b-0b05-4ca4-ae74-fa07ad03073c` is linked to broadcast
  `267dff6a-4e6a-443f-ba85-489e902f20a3`.
- Both activities already had the correct published status, times, channels, and
  participants. Both broadcasts now use PGA Tour, are processed, and are hidden.
- PGA Tour still showed R2 in progress at final review; official R3/R4 pairings
  and the cut were not available. Both players remain active.
- The dashboard still flags R3 once and R4 twice for missing participant start times.
- No exact player times or golf details were added without official pairings.
- The three dashboard rows remain unresolved for a later run.

### Stockholm Open

- Reused group `50bec65f-d583-4591-b13f-78d2c3fccb58`.
- Added Leo Borg to six existing R1 activities:
  `08285cce-56d6-4736-8b33-debb69e0e15a`,
  `bca3645c-f611-4f77-b729-f62fc7e31ef0`,
  `e1ad18a0-835f-4d99-85d6-c8d917a62626`,
  `9f6a9f9e-7d99-45f0-a569-13e291688806`,
  `3354d6d9-2916-49ce-a9ce-25620ca06d47`,
  `5ee00321-6315-4dec-9476-5188f7bfb44d`.
- Created six published activities. The three official singles entrants remain active.
- Nov 11, 13:00–17:00: `17fe2433-8309-409a-bcb7-cac30242fafa`;
  broadcast `5d17dd4f-4375-973c-a3b6-6898a28f9cf3`.
- Nov 11, 18:00–22:00: `848667aa-3ccd-40d8-a4e4-a5e11a4edf7f`;
  broadcast `c9fa5a6a-89d4-e63d-8d11-226432950488`.
- Nov 13, 14:00–16:00: `969996f4-e7ea-4a84-9fa4-6f9d42175a58`;
  broadcasts `d33ec174-de47-3c3a-885e-c9f7ad51e6ea`,
  `aeb20015-e115-7332-ae4f-be5a0dfdb43d`.
- Nov 13, 14:00–17:00: `2137b7b7-0da1-4481-b172-273b04d13efe`;
  broadcast `65d5540f-661e-5e30-a55a-a23a344bb56d`.
- Nov 13, 17:00–19:00: `fb7e60ed-769a-4508-b143-85bcbf76b005`;
  broadcasts `e19ce0f4-1289-ee3f-9e32-c4f299eddb47`,
  `9e06a859-1b34-cf3f-8dcf-63c2afbb2870`.
- Nov 13, 17:00–19:30: `25aaca6a-3999-4590-9cc2-d4d6c5171355`;
  broadcast `688e6136-0221-dd3b-87b0-3e3257ce9204`.
- The organizer's 2026 wild-card announcement and TV.nu schedule pages were used.

### Dam-VM

- Reused group `23397ce8-0521-4014-a150-02927368a51b`.
- Created published Japan–Sweden activity
  `b95919b3-62c7-4a85-a099-2c7331123d39` for Nov 11, 19:00–21:30.
- Linked broadcast `89db7432-951b-733a-81e5-43ba9651540d`; it is processed and hidden.
- Linked the 23 Swedish players on the official IIHF roster.
- Removed Wilma Sundin from the three existing activities:
  `80cbf85a-6a93-4f3d-838b-1a55eee1149b`,
  `fab39b97-53b8-4248-91b9-f18af4a4bcdc`,
  `d2c563e8-1a92-45e8-9e8b-f3d5f2efccf1`.
- IIHF roster and schedule pages, plus the exact SVT Play event, were used.

### The ANNIKA

- Reused group `2802ff15-5f6d-454a-a2df-1f8385dad269`.
- Created two published Nov 13 R2 activities and linked the four Swedish entrants.
- 17:00–20:00, V Sport Golf:
  activity `04bae385-5e91-48b0-ba6c-f398e2cc68a8`,
  broadcast `c60d0674-53fc-343c-b554-a62dc664a63c`.
- 17:00–20:30, Viaplay:
  activity `568718d1-faf1-463a-967d-46e0e4ed71dc`,
  broadcast `5d7a5c74-cdbb-c335-aa6c-e0048fcd7198`.
- LPGA entries confirmed Linn Grant, Frida Kinhult, Ingrid Lindblad, and Linnea Ström.
- The official pairings URL returned 404. No player start times or detail activities
  were added. The entries and TV.nu schedule sources were saved.

### E1 Miami GP

- Created group `8f853524-2463-4b18-a6d2-ba417c374476` for Nov 13–14.
- Created published activity `8980f58b-6e43-418b-a48c-2bd1e8ec8eb8`:
  Finalerna, Nov 13, 18:30–20:00, Viaplay.
- Linked broadcast `9ee04826-fe7f-e638-8429-f54dcafb0390`; it is processed and hidden.
- Linked Timmy Hansen (`86c2c110-bd02-4ce3-834d-5f37984dfcee`), age 34,
  with Götene Motorklubb and his dedicated Red Bull profile.
- Created team `7aa1be34-62fe-42e5-b264-41002c7ae147` for Sports.com Predict
  Team Miami Powered by Magnus, with the official E1 team URL and US country.
- Added Timmy's team relationship and updated his person profile URL.
- E1 calendar and team page, Red Bull profile, and TV.nu schedule evidence were saved.

## Rejected and hidden broadcasts

- All 45 are hidden and unprocessed, with no activity link.
- Step 2 changed only hidden status. Step 3 outside-scope results have
  `ParticipationEvidence` sources.

### Generic studio, recap, highlights, or unidentified listing (10)

- `57a2704b-a1b6-d93e-9879-4dcc84bd70f7`
- `97d1f79d-852e-e13f-9eda-8c630f92e2fd`
- `eaf6cb44-03ac-2e3c-bac8-b6d879978f30`
- `cb49e51c-e985-373b-a740-f35e93ba36b0`
- `b379679c-07fd-1f3e-ad6b-859280cf01ff`
- `ddd003dd-65a8-c138-b322-dee7c9cabe5d`
- `2db6bd1c-7992-0137-bec0-df37441c06e7`
- `a7c6e011-2aa5-9136-9bc1-741556df081c`
- `2a1bc77b-196b-e63f-9929-3e3c80544068`
- `29cb843a-93e4-2237-9acc-5e4f085587bc`

### Foreign national-team fixtures (13)

- `c492f94a-0928-bb32-83a5-10aa775e4b39`
- `fca9831e-51e9-6736-aa0a-4b076f02d061`
- `0941874c-cff7-143a-9d07-033ac8290cb2`
- `0db70945-2ddd-4234-95e0-65ac2485e3e2`
- `31198686-b024-1137-8026-347a8cee8683`
- `31eb6325-6fc5-d832-876e-da7554d918eb`
- `6cadb2f3-d43a-f33e-9c98-cbb8d60ef1b3`
- `9644980d-f6ef-f230-8a14-0381667c6432`
- `a69ed6b5-da4c-c037-8beb-9af484535409`
- `db9c563a-1d10-8e3b-b74f-4a45db61908e`
- `e307f2c9-39d1-7a35-b732-bf6b8926ad5d`
- `eb07fc79-83d8-0f3a-9955-501a8c723f9d`
- `92f111ac-a414-a831-8eed-e3f180837d8e`

### Domestic competitions (14)

- `b4edd527-655d-b034-8f09-edf3fe6b8669`
- `31a871b3-ab77-0d3f-b94a-c22412288fff`
- `bccf4e8b-14ef-0e31-a760-6131870d7ca3`
- `ceceb2dd-0339-1c3b-8443-bef5bd6fcd6f`
- `fc46f941-aa58-8b32-9c99-57c29009e78e`
- `f88231f6-07cb-0730-a525-eeb0e8ff2db8`
- `c6c24893-6a49-cd32-b880-e77e5a8e9ab6`
- `2a674f2f-c6fb-443d-9916-a98d2b2ce006`
- `0c501368-f56a-573e-97aa-2f5e3a862fa8`
- `62e244e9-bfc1-443e-8250-71f7dc3622a5`
- `905b9c9a-cecd-7633-af4c-a4332804bee4`
- `9bfa10df-8d1f-f636-b262-a02bc9ec5b8b`
- `cfaee330-4420-913a-a363-36c30d288eec`
- `f2029f6e-0c70-c230-841b-76e06bd8f508`

### Confirmed outside scope after research (8)

- Paris Masters singles semifinal schedule; no Swedish singles player:
  `9090782b-fae4-583b-a7e0-8ffdbb257c23`.
- The complete 2026 Formula 1 driver list has no Swedish driver:
  `42c5403d-1f9c-063f-8b81-9df0bef1a216`,
  `109c7234-4133-aa34-bc33-8d0e9b656518`.
- Complete UEC Men Elite entry list has no Swedish rider:
  `e44c045a-8f61-d839-a448-2e83679b75e9`.
- Official World Snooker Tour draw and schedule have no Swedish player:
  `4232de2b-6de8-9a30-8d26-0203bf817f20`,
  `7cdc3309-14c2-d53d-b16f-813495a23065`,
  `bbcc7787-7d53-7030-87b2-5293ec205121`.
- Official Skate America entries across four disciplines have no Swedish skater:
  `cfd48512-4a30-6030-8743-26207afe29f0`.

## Unresolved visible broadcasts

- All 15 remain visible and unprocessed. Each has a recorded reason and organization.

- Mixed Martial Arts — fight times do not confirm feed coverage:
  `836a89ad-2e32-c230-8f17-9662682bc2f9`.
- Cycling — no complete official Singapore Criterium start list:
  `804fa31b-1d5f-3d37-b6d7-bf28fa3cfbda`,
  `fd21bf81-225f-a03e-b89f-5c077f8f8368`.
- Cycling — no complete current Niel start lists:
  `d1ecf1fe-4af5-a138-a1df-f28e4a74aba1` (women),
  `df601e3d-4489-0836-a25b-cdade000806c` (men).
- Fotbollslandslaget (P18/08) — listing conflicts with the SvFF fixture:
  `d2bc4d24-afc8-523b-9f68-0b74f5411846`.
- DP World Tour — the complete Championship field was not yet available:
  `b07b2fe1-f600-193a-befc-e0a069508b0e`,
  `a054b2d1-872e-7439-a4ad-485dc99f19ef`,
  `6d3f4473-d073-f538-a1a5-b1594015b928`,
  `85766d85-554b-cf39-ad44-74ad84716fa4`.
- Basketlandslaget (dam) — no official current Swedish roster found:
  `53449156-cb6a-5139-8816-c220f891695c`,
  `f4896f1a-3f6a-fd33-ac49-d51522456299`.
- International Skating Union — no complete official speed-skating entry list:
  `8fbad84b-bd02-9137-baa1-5895cedd5ace`.
- Ishockeylandslaget (D) — quarterfinal opponents and Swedish participation uncertain:
  `4cedbdd0-43b6-1c38-b407-7849f1769ff5`.
- Olympic Games — listing has no discipline or competition context:
  `a7695312-2d4d-3235-9d8b-fa1acc8f248b`.

## Public verification

- Verified rendered pages and card counts on `https://sesport.se`.
- All 21 tracked activity IDs completed Steps 5–6. No unsupported data was added.
- Dam-VM cards show the 23-player roster, profiles, ages, and clubs.
- Stockholm Open cards show the confirmed players, segment times, channels,
  profiles, ages, and clubs.
- The ANNIKA and E1 cards show their verified Swedish participants and profile data.
- Provider links resolved for all processed activities through saved sources or the
  broadcast channel catalog.

| Public date | Baseline cards | Final cards |
| --- | ---: | ---: |
| 2026-11-06 | 4 | 6 |
| 2026-11-07 | 6 | 6 |
| 2026-11-08 | 11 | 11 |
| 2026-11-09 | 1 | 1 |
| 2026-11-10 | 4 | 4 |
| 2026-11-11 | 1 | 3 |
| 2026-11-12 | 1 | 1 |
| 2026-11-13 | 0 | 3 |

- Nov 11 gained the Stockholm Open and Japan–Sweden cards.
- Nov 13 gained one card each for Stockholm Open, The ANNIKA, and E1 Miami GP.
- Nov 6 currently includes two unchanged pre-run activities created on Nov 2 and Nov 5.
  The run-start page count was two lower; the cause of that undercount is unknown.
- No other public card counts changed.

## Follow-up items

- WWT PGA Tour R3/R4 tee times were unavailable at final review. The dashboard still
  has three missing-start-time rows for the two activities.
- LPGA pairings were unavailable; no Annika player start times or golf detail activities
  were added.
- No tests or builds were run for this application-data-only job.
