# Activity broadcast reconciliation — 2026-10-28

- Snapshot began: 2026-10-28 07:18:26 UTC.
- Starting scope: 91 visible broadcasts; all were unprocessed.
- Final outcomes: 31 rejected, 2 processed, 58 unresolved.
- No activities or groups were created. Two existing activities were updated
  with broadcast links; two Maybank activities received sourced participant
  start times.
- No source code, assets, schema, or configuration changed. Automated tests
  were not run.

## Processed broadcasts

- `504b8d73-8ac1-5a34-844c-302de6d51e52` linked to activity
  `f47cac0c-f31b-4b00-8ecc-d6dda485db0c`, Tjeckien–Sverige, Curling-EM,
  2026-10-30. Activity and participation sources already verified.
- `868ae910-c3e8-2338-9a28-3722b45c5476` linked to activity
  `40457117-e3ca-4a64-a10b-88bea63f3f91`, Herrarnas elitlopp, New York City
  Marathon, 2026-11-01. Existing activity, participant, start-time, and stream
  evidence verified.
- Both are marked processed and hidden. No new event or activity was needed.

## Rejected broadcasts

Only `hidden_at` changed; each remains unprocessed.

- Domestic studio or domestic competition:
  `f9f159c3-3845-3230-80ee-456fc2a470ad`,
  `96fd1bea-a6f9-5d39-a495-5eb75166f946` (Bollklubben studio);
  `1e6a228c-6a66-3f33-8a31-3587dfdbc541` (Swedish bandy studio);
  `1daaf444-fb7e-7230-bcbc-b6f6cea9f69d` (domestic SBL);
  `e23ebc86-c9b2-e234-9de6-977e925461ae` (Champions League studio).
- Foreign fixtures or events outside the Swedish scope:
  `4848ff94-1a67-3d3a-b912-3dc130475428` (Czechia–Germany);
  `2be3cfcb-419c-2632-9cd4-c68725138c78` (foreign curling teams);
  `4dc34770-ce58-073c-b5f8-a1d4b7eb9005` (Scotland–Italy);
  `37aac62d-485f-ce34-b644-c19bf9d0821e` (Reims–Sochaux);
  `409754d4-24f6-0434-b1e0-806937f12d5f` and
  `49048562-dbf8-8834-b04f-9868168d2648` (foreign UEFA Youth League fixtures);
  `2e412357-dc10-c037-9660-84fb376c7b3a` (Lyngby–Randers);
  `796d0dc3-b4ad-2f34-9089-42e77d96d2cf` (Charlton–Bolton);
  `7cc09b22-f4a5-583c-984a-eeadc6faddee` (Utah Jazz–Portland; foreign NBA
  fixture despite conflicting Lakers description);
  `4a393042-f077-6034-a9d8-67a4e7aa6c38` (foreign UFC bout);
  `a35a1975-6ceb-f53d-8d4c-f2a6e48cf106` (foreign Ligue 1);
  `66ac6158-9e86-2e34-bafe-c74211c2bf22`,
  `3d0267ff-a3e8-483c-b7b0-92d718d99522`,
  `4295a94e-7d73-9e30-81ac-228d99b2c5e1`,
  `366f1656-4a9e-073e-8c4a-c38bbfc42149`,
  `6e634ec5-90fd-b536-9dfb-f73acb944c97`,
  `c72b2d7f-6fb8-c43e-9525-14c42b69d3de`,
  `d3779cba-8b43-f335-85b2-9b19e9a9965f`,
  `2a83f17f-ea7a-b23b-8990-1a9254a1c03c`,
  `31d78957-7f5d-dc37-b820-a3c9b9c119f3`,
  `67fa0a6c-98a2-513f-97fb-e1d9c1ecaf9a`,
  `81b58f03-ffde-f23e-af13-bad3558b0c5a`,
  `9897b07f-8d9c-d33c-9fe4-a6418f9fd22b`,
  `b7730042-806d-d23c-a502-566cb3a22397`,
  `d8205446-d3f7-023e-a73b-9b3ca962430a`,
  `e127bb6b-8a30-f93f-9bbe-8e1d7102f615` (foreign Champions League club
  fixtures).

## Unresolved broadcasts

All remain visible and unprocessed. Each has typed broadcast or participation
source evidence and an organization marker when an applicable entity exists.
Reasons are shared within each event group below.

- ATP Tour — 14; Paris draw was not available from the official draw page at
  review time; Swiss Indoors final qualification remained uncertain.
  `58bd9107-0be0-4f38-81c2-657838ce217c`,
  `bb1c5377-b355-673a-996e-34648f31a79c`,
  `7c143164-4d77-3a3d-8874-4fb93566984d`,
  `98fc23e9-bc40-bd34-a3fb-8970e1c684f0`,
  `2a358a2b-ae3f-f03e-a5ef-e475c5cd931e`,
  `b9cc0343-af6c-6338-83d9-6d7a90012b0d`,
  `8079b5ec-69bc-273a-bd32-8b74f02ffd82`,
  `423ea488-d3fc-1a39-b6fd-69e9beb184cd`,
  `f0a0e918-f503-6237-a4b3-ef3a0fa8e817`,
  `17afb9f1-2fe7-d93a-9746-88e67574b540`,
  `46e35311-4d1e-1f38-a945-c55e6d615665`,
  `426f52c3-3f50-5f30-96b4-32a168ecc6b1`,
  `2b81c0a7-7426-883c-9811-514f4c4af36d`,
  `90018c10-1100-fc3f-898c-2fd523c2b701`.
- BMX Racing — 2; event identified, but complete entries and Swedish
  participation were not verified.
  `08aa1629-4ad9-d434-89d7-925ae97f5d60`,
  `8facd66b-b7fb-e93c-849f-4ecb6fc18800`.
- Climbing — 1; Gunsan para event identified, Swedish participation not
  established: `098b23da-f78c-9438-9b9c-1565855ddbd9`.
- Curlinglandslaget (dam) — 9; Swedish team identified, but later-round
  participation depends on qualification/results.
  `343413b3-cd9f-ed33-ae08-b69c7f09a3cb`,
  `5d616d09-d2cc-5e35-9f8e-e04717288382`,
  `fc34ef77-65f9-e733-b3ef-0ca7add32602`,
  `f0a45f0a-61cb-bc3a-857f-87e958059650`,
  `805f3166-af52-d436-b7a6-9cd3d0f9f9b7`,
  `7289dbae-cf7d-273b-b714-8fd5a30cfe75`,
  `818534eb-5211-053d-bbe3-535654eecb83`,
  `d4ec8a74-5135-ae32-b505-d65f10543313`,
  `328d2417-c562-c637-9277-4d4947adbf00`.
- Curlinglandslaget (herr) — 4; same outcome-dependent reason.
  `c6eb28ef-0e1c-6f36-ba7c-6760ed9dd3a4`,
  `65b700a4-6943-8e37-a1cd-a7f96465a44b`,
  `8d821da4-b599-3f3b-b3c4-119626bd60d2`,
  `c9aaf994-9c5c-6030-97ca-c57ae0c75543`.
- Cycling — 1; Koppenbergcross identified, entrant list inconclusive:
  `a5da8a46-6b07-2b34-affe-d9ce80018785`.
- Global Champions Tour — 9; Rabat event identified, complete Swedish
  rider list unavailable; two listings also had conflicting context.
  `75f97fbf-9e55-ef34-b8a3-020f17651382`,
  `5ca64ee7-d845-b636-8f3c-b584321f57a6`,
  `0dda1ee3-3158-9a35-83f3-31b711c36514`,
  `81b2394b-a401-e438-9ad4-b6887600855d`,
  `222e1a3c-fb99-4838-ac55-b55b11d016e6`,
  `fd5283db-e45f-003b-9c91-f5c95c765d7d`,
  `e444fdec-7dbe-c43f-9230-ca48e71ea95b`,
  `90b01838-92b4-0434-b61b-b73ef7eb08b9`,
  `cbe15e23-88d3-8f32-a162-82c5caba4407`.
- Skate Canada International — 1; event identified, but complete Swedish
  entries were not verified. No matching organization entity exists in the
  database, so no organization marker was assigned:
  `112b9462-56fc-5e3b-aab5-db347df96e5e`.
- World Snooker Tour — 11; event schedule identified, but official sources
  did not establish a Swedish player in the draw/session.
  `5755ecf6-eaf1-b136-a32b-286e29464169`,
  `e3472469-6595-6d3d-814f-e84bebde3cd6`,
  `1447cd13-d141-d038-bf7b-5d1457ffa7df`,
  `396f3c9b-7523-c736-bee2-6aeee5fb11b5`,
  `393d934b-8c58-e931-b5b7-ef8b7b6a7d6e`,
  `7d3f1d35-c780-603c-88d1-2fb54fdf528b`,
  `f86e4d60-d6e0-4134-99fb-25c54ef265c2`,
  `6128a3df-5209-223d-881c-57b6df703c78`,
  `2e887f04-8243-be3e-b4e2-d4217ea9b658`,
  `3e7a4274-b43f-113f-bfe3-ff9d4588b4ab`,
  `6e0322fe-0bb9-5339-a709-148421ec8772`.
- Youth Olympic Games Dakar 2026 — 6; SOK confirmed six Swedish athletes,
  but the exact sport-to-broadcast mapping was not established.
  `c1e08dd6-2ed9-3f36-99f3-6474bcf3dcd6`,
  `b619cff4-94ea-6e39-903c-ebe76bf5f285`,
  `8547543e-747b-3b3f-be41-4ef9bad78c9c`,
  `9baf9990-641f-2c34-bf16-9246da16aa3e`,
  `8149dfee-b5c2-0d3d-bb04-b6ffb1652c24`,
  `ea547f4f-6d92-9e31-87e9-4a16ba70c891`.

## Sources and dashboard

- Added 27 typed evidence records for unresolved broadcasts and 18
  organization markers. The Skate Canada item has no matching entity.
- The dashboard initially flagged Maybank Championship Round 1 for missing
  participant start times. The official LPGA pairings confirm Maja Stark at
  08:21 and Linn Grant at 08:43, both from tee 10. Added those times and
  `ParticipantStartEvidence` to both existing Maybank activity variants:
  `9fe10b7d-136d-4fe9-94c2-77de3634578e` and
  `ddee35bd-3875-4654-b8ac-f68587fb3417`.
- Sources: LPGA Round 1 pairings (lpga.com); SVT New York Marathon;
  NYRR athlete field; Curling-EM schedule (curling.se).
- Other research sources recorded on the related evidence include the official
  ATP draw pages, UCI BMX hub, SOK Dakar roster, SVT Curling-EM guide, GCT
  schedule, WST event page, and Skate Canada event page.

## Public card counts

Retrieved each affected date from `https://sesport.se/?date=YYYY-MM-DD`.
Counts were the same before and after the run:

| Date | Before | After |
| --- | ---: | ---: |
| 2026-10-29 | 8 | 8 |
| 2026-10-30 | 7 | 7 |
| 2026-10-31 | 8 | 8 |
| 2026-11-01 | 5 | 5 |
| 2026-11-02 | 0 | 0 |
| 2026-11-03 | 0 | 0 |

The two linked broadcasts reuse existing public cards. Public pages showed
both curling teams and the marathon details. Maybank start times are saved in
the database, though the public card returned blank time fields. No public card
was added or removed.
