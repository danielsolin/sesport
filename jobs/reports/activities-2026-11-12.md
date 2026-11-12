# Activities — 2026-11-12

## Run

- Started 2026-11-12 04:05:54 UTC (05:05:54 Europe/Stockholm).
- Snapshot: 72 visible broadcasts, all unprocessed and without activity links.
- Snapshot SHA-256: 26887ca1aba292d3955f8a9c4bc715a1d129c05e2f14f9c1f49cce1892639abc.
- Final audit matched every snapshot ID: 35 rejected, 6 processed, 31 unresolved.
- Target database: sesport. No code, schema, config, or deployment changes.
- No automated tests or builds were run; this was a data-only job.

## Created and updated activities

### Austin Championship

- Reused group 43fd72e5-8fa4-49ee-97c5-9f2be6e81324.
- Created and published f6d94d22-a943-441b-ab44-bdb011e5884a:
  12 Nov, Round 1, 15:30–23:00, HBO Max.
  Broadcast: 7b5c6893-8b3e-0731-b9ab-229fed3ac3ae.
- Created and published cea86cd3-239c-4791-931f-69173e27e670:
  12 Nov, Round 1 evening, 19:40–23:00, HBO Max and Eurosport 2.
  Broadcasts: 69eb1595-2475-3e3a-a839-e53317a485bf and prior hidden
  7feff955-0d17-4530-9829-eee10a6d265b.
- Created and published 2dace55e-968a-438b-90ab-4edd80c36fa0:
  13 Nov, Round 2, 15:30–23:00, HBO Max.
  Broadcast: 24438e3b-121b-2032-9dab-376fad8dbad2.
- Linked Jesper Svensson and Pontus Nyholm to all three activities.
- Saved official tee-time evidence and participant starts:
  Round 1: Jesper 15:17, Pontus 20:19; Round 2: Pontus 15:39, Jesper 19:57.
- The prior hidden broadcast remains hidden; it is now linked and processed.
  Its original hidden timestamp was preserved.

### Asian Le Mans Series

- Created group 8f6ed439-ffd0-4985-9b0e-f6f88e5a1d15.
- Created and published 21638235-cdb3-4210-bd5a-cbaffc29bc5b:
  4 Hours of Le Castellet, Race 2, 15 Nov, 12:50–17:45, HBO Max.
  Broadcast: 7ea577d0-ebca-c93f-b824-efe64d6b6e57.
- Linked August Raber, confirmed as SWE on the FIA event entry, and saved
  his profile, birthdate, current team Duqueine Team, and formative club Xcel Motorsport.
- Xcel Motorsport is classified as a Club. UAE nationality on the Le Mans Cup profile
  conflicts with the FIA SWE entry; inclusion follows the event entry.
- The country table has no UAE code or flag. The public card uses the International fallback.

### UEFA Women's Champions League

- Reused group 0114eabb-fe44-4af2-b45d-851d3d0ac468 and extended its end date
  from 10 to 18 Nov.
- Created and published 0e854df9-b3e4-410f-9133-bcb2b3c85134:
  BK Häcken–Manchester City, 18 Nov, 18:40–21:00, TV12.
  Broadcast: 854f595d-1e8b-253f-8962-db8f4e04ab64.
- Created and published 6fc188a7-ccf0-45be-82fd-1cb1c38da68a:
  BK Häcken–Manchester City, 18 Nov, 18:40–20:45, TV4 Play.
  Broadcast: 8975ba4c-8081-213c-9877-f16e6613fc1e.
- Linked 14 Swedish first-team players. Added Elin Rubensson
  (7cec1b09-e62b-4ebe-b5ee-210cb9a2efa2) and Josefin Baudou
  (ec17e37c-750a-4e85-b33c-ae43dfdd651d).
- Josefin's source gives only birth year 2005; stored 2005-01-01 as a placeholder.
- Updated both Tilde Karlsson person records to her current UEFA player profile.
  Changed Josefine Rybrink's link to her dedicated BK Häcken profile.

### Existing golf activities

- Added sourced Round 1 start times to both DP World Tour Championship activities:
  15f9eb7f-f91e-4f9d-8caa-36264caad1e9 and
  7947cd73-4b91-4cc4-8e64-8790fa1f9f3e.
  Mikael Lindberg starts at 07:10 Stockholm time.
- Added sourced Round 1 starts for all four Swedes to both The ANNIKA activities:
  17bf649d-28af-4b67-9f78-8e28bd5067e3 and
  60fd16fb-b2e1-455a-86fe-38a934ce83b6.
  Linnea Ström 13:11, Ingrid Lindblad 13:55, Frida Kinhult 14:17,
  and Linn Grant 18:21 Stockholm time.
- The ten participant start-time results and their source records were read back.
  No Round 2 times were set for these events because their tee sheets were not yet published.

## Evidence

- Official SOK roster, IIHF quarterfinal pairings, the German Masters full draw,
  and complete ATP Finals singles and doubles fields supported out-of-scope decisions.
- Remaining fixture metadata and schedule conflicts were checked against organizer,
  tour, league, and broadcast listings.
- Event, roster, player-profile, tee-time, and stream sources were saved with related
  activity or person records.

## Rejected broadcasts

All IDs below were hidden by changing only hidden_at.

- Youth Olympics beach handball: no Swedish entrants.
  - f23b468c-7477-f937-bec0-06b674f1cee9
  - eb2d51d2-b126-cd36-91e3-d475159602e5
- Youth Olympics futsal: no Swedish entrant.
  - 54264ca3-f297-1a33-8347-84386642c9ff
- Domestic Stockholm Open:
  - b2e6f021-c37a-ff39-a109-09b9fa731894
- Domestic Idre biathlon premiere:
  - 45f7769c-e87f-0839-afdd-8e5201659fc9
  - eb0c759f-cbd7-7c32-b46c-45daf8bddd6f
- IIHF quarterfinals were Canada–Czechia, not Sweden:
  - 4cedbdd0-43b6-1c38-b407-7849f1769ff5
  - 8262dbbc-52ca-5036-9245-bdb5e52ffb1d
- German Masters qualifying draw had no Swedish entrants:
  - 0a2e7e34-f3df-1c35-a40e-58029e1ba0e6
  - 5b351333-615e-503f-8343-540af995b456
  - 6952c21b-ef71-1b3f-a445-29486cb47e60
- ATP Finals complete singles and doubles fields had no Swedes:
  - 43776a6a-2c83-f33f-ae1d-80b41078bc1f
  - c6df61c6-00bd-f032-b398-7069d5fffa01
  - 81712fdf-7774-8d31-af0c-6b6442353d35
  - 9ae1d4c7-6d85-1136-969d-d89b69945b45
  - 50b42797-ae26-a731-bbed-b75e0d045516
  - ec31f722-220a-e031-9de8-55c2581b18d7
  - 3fd8e32b-9a1e-b932-a474-ebdf6d953a9e
  - 66c0372e-8d53-ab3e-8786-dc4f21cd90b6
- Generic NHL studio and highlights programming:
  - 48d0ab89-501f-b63e-9deb-a8303e36d858
  - dbdc8b9b-fb56-0933-8b0c-04d728bc3846
- Domestic Finnish Liiga fixtures:
  - 3e26d19e-f2e8-b53c-8f2d-afc39c70555a
  - 4254be7d-a9ee-1539-b3d7-271e262f68ec
  - 5342b588-4144-9a3e-9482-97fa77d6dd79
  - bf1baaf0-0fb1-a333-bd38-38d1cf499c31
  - ecb3d623-ca9f-4733-be67-bc231d81c6e5
- Foreign-club Champions Hockey League fixtures:
  - aacae497-3606-cc37-bdf4-0b08626ac5c7
  - ace5e1c4-d583-7033-911a-440c5aaedcac
  - 20d7dc18-d624-f631-8c06-e6e06069ac10
- Domestic Swedish handball fixtures:
  - 78238c25-f7ee-243a-9f4d-31081c9625c5
  - 41d7d016-ce76-363b-a4ff-fe9e4e994889
  - 9aee4c47-a117-2b3b-baca-d452417ea47b
- Domestic bandy:
  - bb946cce-8bb1-483a-aa8b-7b74881b666a
- Domestic HockeyAllsvenskan:
  - d7baa905-c830-0a3e-80a1-23b0cb090f0b
  - f61e751e-2125-2538-ac0b-c50b0cf6e476

## Unresolved broadcasts

All IDs remain visible and unprocessed. The listed organization is set when known.

- T100 Dubai wildcards are unnamed; T100 Triathlon World Tour:
  - 4a09555c-a465-723f-a79a-ca449d234dc2
  - fb15c1c6-6162-0a37-8621-82077484f1d8
- Cyclocross start lists are incomplete; Cycling:
  - 48b30142-56e0-0d38-95a6-d130f104ee66
  - daf54d93-a7c1-d73b-bf27-942f585dfdfd
  - 0c97916b-3505-093d-beec-26b1f21afeef
  - e95f0a0e-e407-093b-a0d6-8ec1d8ea3611
  - b0c6af74-573d-9c3b-a114-a1ad0d7cb343
  - b1e783ae-93bc-453b-9b8e-50998c9b286a
- Generic Hamme listing has an uncertain event match; Cycling:
  - 2d078751-89ae-7735-a1e8-17ea76042178
- Later IIHF rounds depend on Sweden qualifying:
  - 2aedd7f7-c769-c93d-9d90-f6d37c57da2c
  - 79c12ace-35d2-9f30-9912-08e1eae41f1d
  - e87ee685-f224-b53f-86f9-810730ddb329
  - 14f0f9a6-ee62-d93c-9a97-fb5006909ebd
  - bc194878-9a2a-563f-9857-de5d8d317e2c
  - 9986469d-9dee-d33b-b3ec-e04c17bc90e4
  - f3840800-f4ca-1a38-b6d5-ec7f0849cc3c
  - a41d4795-75ad-c234-a53e-75e4456b595a
  - d663b80c-5c25-9e3c-a018-938198553550
  - da878fa6-b3dc-1a39-bc18-b22817834498
  - 5895be4b-453f-b13c-a1ce-2d4b76574b4d
  - dfb866d9-19f7-d83b-bab7-95bbc47021df
  Organization: Ishockeylandslaget (D).
- Women's basketball preliminary squad is not the confirmed matchday roster:
  - 6e42c75f-d0b7-1934-9cc6-b0ba283e0977
  - f3569876-e1a0-da37-9d70-b55cfb595f43
  - 50bd5925-7c2f-0535-a2b5-8cb01461f23b
  Organization: Basketlandslaget (dam).
- Darts schedule conflict or match progression remains unresolved; PDC / WDF:
  - 623dfb44-ec21-403b-9374-7d5dc00fc80f
  - 6b67db15-d728-7b38-97b4-d0abeca27668
  - de148822-348c-7530-8184-3744ba8efb51
  - bf65a69d-a175-d437-9773-f0b796e80bf3
  - c69df824-1d12-423d-8148-cc5f515e2f35
- Men's football TBA has no safe match association:
  - f49a3711-30ac-3b39-9ac0-1341afb5bdf8
  Organization: Fotbollslandslaget (herr).
- Conflicting Global Champions League event/date mapping:
  - d2eb2fc7-5f7a-723a-a3ff-68a8a0a440c8
  Organization: Global Champions Tour.

## Public verification and dashboard

- All affected hosted pages returned HTTP 200; participant names and links were present.
- The 22 participant profile links checked on the affected cards were present.
  Most returned HTTP 200 directly; official European Tour and UEFA pages also resolved
  in browser search despite automated-request restrictions.
- Card counts before → after:
  - [12 Nov](https://sesport.se/?date=2026-11-12): 4 → 5; Austin Round 1 added.
  - [13 Nov](https://sesport.se/?date=2026-11-13): 5 → 6; Austin Round 2 added.
  - [15 Nov](https://sesport.se/?date=2026-11-15): 7 → 8; Le Castellet added.
  - [18 Nov](https://sesport.se/?date=2026-11-18): 0 → 1; grouped Häcken match added.
- The dashboard has no upcoming activity issues, todos, or current AI alerts.
- The public Häcken card combines its two activity windows and shows the latest end,
  21:00. It does not show TV4 Play's 20:45 end although that time is saved correctly.
  This presentation issue needs a code change and remains unresolved.
- Round 2 tee times for DP World Tour Championship and The ANNIKA were not published
  at the time of this run; add them in a later run when official pairings are released.
- Participant, activity, and start-time evidence was saved in the database source records.
