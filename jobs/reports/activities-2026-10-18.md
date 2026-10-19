# Activity run — 2026-10-18

## Run and snapshot

- Started: 2026-10-18 07:00:04 UTC.
- Snapshot: 168 visible broadcasts across all dates; all were unprocessed.
- Hidden after start: 60 irrelevant broadcasts and 7 broadcasts linked to activities.
- Final queue: 101 visible, unprocessed broadcasts; every row has an event organization.
- No broadcast was left unattributed or unattended. The 101 unresolved rows below
  remain visible until a full entry list or event mapping settles Swedish relevance.
- Added missing organization markers to 42 selected broadcasts. Created the
  `ISU Short Track World Tour` series entity (`9e08dbec-f338-4431-a4a2-ecc083987ac7`)
  with its official series page and source record.
- All database updates used guarded transactions and were read back. No code or
  asset files changed. For changed activities, evidence URLs and types are stored in `sources`.

## Activities created or changed

### Table tennis semifinal — `5da12b0e-cf49-466f-afb3-5bd9c9d7709e`

- Published for 2026-10-18, 13:35–14:45, SVT Play; title `Anton Källberg – Dang Qiu`.
- Retained Anton Källberg and removed nine unconfirmed non-player squad links.
- Linked broadcast `14123fcb-5ce5-a630-96ed-ef633e770a70`; processed and hidden.
- Kept the pre-existing historical link `f43cbff7-6b03-4733-82a7-940f28c33e87`.
- Evidence: ETTU event and participation pages, SVT/TV.nu schedule, Sportschau
  participant start time, and direct SVT Play stream. Types include
  `ActivityEvidence`, `ParticipationEvidence`, `ParticipantStartEvidence`,
  and `StreamLink`.

### Table tennis final — `d642d0c2-1a34-491b-9f7c-f1c98c6dd249`

- Changed from Published to Draft and cleared `published_at`.
- Removed ten unsupported Swedish squad links. The semifinal had not yet been
  played, so the final participants were unknown at run time.
- Kept the pre-existing hidden broadcast link `7267b028-2c52-9e3c-a8d4-ef9173658e98`.
  Current final broadcast `b3fddada-c3f7-803b-b16c-0432683efd89` remains visible and
  unprocessed under Table Tennis.
- ETTU and TV.nu evidence, the Swedish team page, and the existing SVT Play link are
  retained. The dashboard correctly reports this intentional draft as having no
  participants.

### Sweden–Brazil handball — `7e015b59-22a4-46ef-9829-cebf75bcbcf6`

- Created and published for 2026-10-24, 15:00–17:00, SVT1 and SVT Play.
- Reused group `d945d6e1-a38e-43cb-8530-46067890ac17`; extended its end date to
  2026-10-24. Linked all 17 confirmed squad members.
- Linked broadcasts `78e31674-da03-6e38-be47-ac9987cf8f04` and
  `5b99f9b5-e8f5-a635-b001-29f3b55da6b8`; both are processed and hidden.
- Set the Swedish women's national team URL to the official current squad page.
  Added it as participation evidence. The 17 participants have birthdates,
  formative clubs, and team links.
- Evidence: Swedish Handball Federation roster, SVT schedule, TV.nu listing, and
  direct SVT Play stream.

### France–Sweden F16 — `f5235e9d-6e51-420e-9a69-ba5177dfc216`

- Created and published for 2026-10-24, 17:50–20:30, Viaplay; 21 official squad
  members linked.
- Reused group `d945d6e1-a38e-43cb-8530-46067890ac22` and linked broadcast
  `2e923885-34e5-173c-8d18-e9b4706c30f9`; processed and hidden.
- Added 21 `PersonFacts` source rows for current club affiliations. All 21 have a
  current team link, and the national team has a relevant official URL.
- Unresolved: 20 of 21 birthdates and 17 formative-club links are absent. The
  official SvFF squad page lists current clubs but no birthdates or formative clubs.
  Sample linked SvFF profile requests returned HTTP 403/Cache miss. No facts were
  guessed. The same missing person data also affects the existing 2026-10-21
  activity `d945d6e1-a38e-43cb-8530-46067890ac23`.
- Evidence: SvFF squad and current-club list, TV.nu schedule, and direct Viaplay URL.

### F1 Academy Austin — `ca892e2f-5365-44fc-8864-05d298b60566`

- Created and published for 2026-10-25, 00:55–02:05, Viaplay; title
  `Omvänd startordning`; Alexia Danielsson linked.
- Public sport date is 2026-10-24 under the existing `sport_day` group
  `d945d6e1-a38e-43cb-8530-46067890ac14`.
- Linked broadcast `30575148-361e-4933-ae63-92633e7d797c`; processed and hidden.
- Evidence: Formula 1 Austin timetable and wildcard announcement, TV.nu listing,
  and direct Viaplay event URL. Existing age, club, team, and profile data are present.

### Butterfield Bermuda Championship

- Created `8a366991-afab-46d0-8933-8e353c9f9aa8` for 2026-10-24,
  19:00–22:00, HBO Max.
- Created `85db8107-b06d-4ae4-aafe-18ef8c65423c` for 2026-10-24,
  19:05–21:55, Eurosport 2.
- Reused group `86841011-0000-4000-8000-000000000108` and extended its end date
  through 2026-10-24. Both activities link Henrik Norlander, Jesper Svensson, and
  Pontus Nyholm.
- Linked broadcasts `a86948b4-0819-ce37-81cf-decf426dad27` and
  `71634113-2bcf-6a31-95bf-49c5d5018b17`; both are processed and hidden.
- Evidence: PGA Tour field article and exact TV.nu listings. No event-specific
  provider URL was exposed by TV.nu; the site uses its channel-catalog fallback
  for HBO Max/Eurosport 2. No player-specific golf detail activities were justified.

No participant star priorities changed. Anton's existing star remains; the junior
F16 players and Alexia do not meet the senior tier-0 criteria. No other player-start
details were supported by the general event-wide broadcasts.

## Hidden as irrelevant — 60 broadcasts

Only `hidden_at` changed for these rows.

### Named snooker/billiards matches without Swedish competitors (4)

`e3d5fbc5-22e5-823d-9c06-e103f4854214`
`f1a818ac-103e-ee39-b678-1efc209cc830`
`ed061213-4cc8-a437-ac39-b3695743b55f`
`06bcba5d-6f10-8d3a-83bb-c2a2e04cf775`

### Domestic competitions (20)

`1698a889-98f7-783c-a499-c380466a89fe`
`5c46b219-0511-1139-b4e5-dc093bc9f563`
`90a214f1-2851-4436-9402-5a313066de60`
`e9539faf-76e3-9e3f-8c28-8b6cd0f546c7`
`4f56af99-5520-d53d-8914-f58b7ab8e7e8`
`7ead7197-e5c1-9132-aa08-8fdb04aeed61`
`887b743a-c82b-e733-a1f5-55a55c248362`
`017f2d03-cdd4-4234-a96a-0526f0cb7a7e`
`bac40449-7dc4-5b38-a564-03c52e659a0a`
`9967ec9c-33ad-9e3d-b069-0a8d9bab17ce`
`5d56ac40-080e-3f33-84ff-0429af04831b`
`3c7e635c-f338-6639-8b01-5b70608ab1b0`
`1b6ef0b5-4851-1036-b57c-aad4ff6e9c41`
`2ab5152e-9071-1237-af5d-b8685668d4d1`
`852170f7-897c-253b-b2fa-2da105fbf47e`
`1137409c-0645-923e-b2be-6134e4d125ad`
`37d8938e-8221-3835-8b7d-b92c1d95bcc0`
`dc0a46d5-c780-a43c-a0f6-ac4b2254873f`
`114d75c4-50ee-b53a-ab54-98c3716668a2`
`fc52ab4d-c2db-4a3f-9f21-1735491d3112`

### Foreign-team fixtures outside scope (11)

`781513e1-af18-a63f-8c14-c55dd3ad8baf`
`2591f035-63c5-c333-9af5-7f7794aa6f3f`
`126f25fd-d076-8c38-990b-6a135029432a`
`c4062b79-5c2e-533f-b053-97857f705675`
`5739156a-6808-0636-b158-b3a30a60ff2d`
`bca4fc7a-505e-0536-a740-af4dbb2bc10c`
`c0d53a38-0e19-403e-9c4f-97203015b999`
`48a15e59-74d3-9c3d-86ac-3b42e69bcc18`
`adce8c14-7778-2e36-8b09-e5709d9d44e4`
`b0f77c74-b04d-ee39-b4de-2ce4717d2d12`
`2a391e99-176f-8438-8f2c-98433d5a8136`

### Generic NBA listings (2)

`7e517782-2bcc-c130-bc39-1dc2ef1aad88`
`bef5773f-ffb1-1135-b52e-61434cbb4c60`

### Studio, highlights, recap, or unidentified listings (7)

`6b3c76db-5e7a-133b-b0b1-a1d9e586007a`
`f5da15e2-eebb-813f-a018-1b8a93ad6e90`
`e9bbb872-4345-7433-b111-76b696890eab`
`b6ced9f4-f42b-343c-9624-3c7ddac3ee23`
`123e632c-02f5-8f36-a2df-1d17226dd536`
`b841f9c5-60b1-6733-8c7c-0f5e63d5b04e`
`ee793ef7-4084-1535-8a54-17240548c94d`

### F1, MotoGP, UFC, and unidentified climbing listings (15)

`7ce39434-941e-e832-8992-5e678e646fc3`
`7cf0d005-cb93-c436-8e60-1a9b3edd81e7`
`855386d8-2ea4-5137-874f-b5497da64014`
`3d96bce0-6bbd-eb30-93bf-c42793373931`
`c30319d7-7cf5-0d3f-b881-df14dd109084`
`2dd36e58-4653-7a33-92ca-7aa614ed0a25`
`1ecfd33b-f6ca-a63c-bdd5-b55d172182b3`
`74e468c8-5bc5-a63d-a492-867ae679612c`
`04304a26-45b4-043d-917e-fb856dd23bda`
`2432dfa3-8ca1-e738-bb47-1e90702bea50`
`936f66c6-f14b-4138-8fd8-71acb682d7a4`
`b75c0ac2-1f71-9e31-8cd2-052902c2cea8`
`aaf8b13f-ba12-1834-8072-6257f08dac9b`
`b89e260c-a392-813b-9ecc-b045fa91eda7`
`26f6544a-57b3-f73c-a7cb-0502e4d20766`

### ISU Grand Prix de France, no Swedish entry in complete official entries (1)

`aeb4347e-99ed-be3f-8959-1817d1de4c19`

Official complete entries by discipline showed no Swedish competitor at Grand Prix
de France. Andreas Nordebäck was entered for Skate Canada, not France. Source:
[ISU Grand Prix entries](https://isu-skating.com/en/figure-skating/events/isu-grand-prix/).

## Visible unresolved broadcasts — 101

Each ID is visible and unprocessed under the listed organization. Research checked
official event pages, schedules, entries, draws, and rosters where available. The
listed events were identified, but complete Swedish participation or exact mapping
was not established, so they remain visible. Evidence notes below name the sources
and results; no hide decision was made from incomplete lists.

### ATP Tour — 43

ATP Tour tournament pages and schedules confirmed Almaty, Brussels, and Lyon; full
official draws were unavailable to establish Swedish participants.

`e6c5bd86-da8a-2037-b725-7760c02c65e1`
`1140d888-4455-cf30-918e-91b7393f66e6`
`62c2811c-7b50-633e-8184-3c0bcbaffd4b`
`8f8efb80-a8d7-233f-8663-5581b2abadc0`
`82bbd625-8271-b23d-9a55-70fe8611f936`
`f6d01310-9cfa-073a-a53a-cff6d77fe8ed`
`cbf972b4-a2fe-4437-a16b-df89007fdb33`
`06939314-286d-ed3d-8180-93e24ed2b3cd`
`bb9ce839-ef04-033f-801b-2a30eaa21b54`
`24e29103-bacf-6c3f-b44b-a945c7123a82`
`6f09ea9e-17fe-483b-acb3-a8540dea4410`
`05411a22-3462-4c32-9b84-f03419403244`
`b605c037-c058-b13d-bae2-1176df967797`
`cc8db55f-841a-8335-8796-0997df7b2344`
`831143d6-095b-5c3a-9cc8-0e264930a30b`
`013905ba-6bcc-6631-ad96-bdefbb71c544`
`23cad30f-a5f7-e43d-a631-5757883be8a0`
`e8a785ff-1d7e-603e-9371-72612150381a`
`502ccd8f-40d6-6931-bf63-48fd1075b5e7`
`6772c18d-1f4b-ff3d-bb90-af57bf9e17a4`
`4a9bee37-a47d-b234-a791-5b50248b9e94`
`76375120-4fc2-f93b-9fb0-4b8f8c1d9df2`
`de22e297-42c2-3f32-89ee-113edff0e773`
`77263734-71f1-8c32-ae4c-bed5b677ee21`
`c2588a71-b11b-1d36-9fa4-56ba444fae62`
`857255e9-ce8c-9539-8db4-934b336b67e4`
`7b9144dc-20a4-f239-ae23-743c8d69789c`
`f972a8ed-78eb-ad31-aa2c-2d0542850e73`
`408cc095-21ae-f23d-b23c-5d48850b21b2`
`96faaacb-a40d-9b3d-8018-2657205d4f83`
`265375c2-1fe0-1c36-a648-c367d7f850c9`
`accfa651-b5ae-9c39-8697-8e52d1814c90`
`c8d7cd2a-2d56-e23e-a56a-d33f18a1bc0d`
`c622f9fa-4dbb-db3b-8cf1-e3e147313141`
`e18337f7-f7e6-ec3d-8144-4f6308de8b79`
`94c09067-c3ae-f63d-be00-79dfd270c344`
`c6323a0b-0374-c738-9c0d-4f4aade5287b`
`47e96c72-43ba-2936-bf41-3eaa4b6e6c39`
`9502420e-17e2-9534-a4d2-1b2a44fedf6a`
`350d4a81-31e6-f737-aec3-5aacbe563d4f`
`6aacf8c0-028a-e036-b895-d64a6eb9ea75`
`109cc768-f16d-173f-9714-7e19483efe5c`
`e6df7407-a9a5-3a39-b289-cad86de99abc`

### Climbing — 4

World Climbing Salt Lake City event page confirmed the event; complete Swedish
entries were unavailable.

`baff78af-288b-8231-a3a4-3ee6318a12e2`
`00aeeaca-d78b-e132-b197-7b8a25647f6f`
`25c58e25-c923-093e-b965-09c60028e83e`
`1dd85124-0f60-9b34-80dd-86aaab0fbe17`

### Cycling — 5

UCI event pages confirmed Shanghai events, but full Swedish entry lists were
unclear. Ethias Cross participation was not confirmed from event listings.

`b3f536ca-9c74-5b36-8789-72eb08ee3e65`
`bb721944-7cda-3f32-bfdc-872e65619155`
`62be8e94-cdf3-1932-9366-3a7abda5e21a`
`c28b0fc5-6d3b-a233-819f-851cbb7fcf4b`
`144ad12a-d060-0b34-b462-110b32a42df3`

### DP World Tour — 8

DP World Tour event and field pages confirmed Genesis Championship; the complete
relevant Swedish field was not established.

`0bb41ae0-4344-2236-8bf3-1766191e941e`
`e0b7dfd2-1f9c-b93e-b544-3be07d404e66`
`9a2e0171-0003-b73b-82df-1512b0afdcdd`
`cdfaa94b-e7ca-593f-b67d-3987e94977d4`
`269fb5a1-516e-d83e-a9e8-269be1abbffe`
`9bf8694c-f81a-8236-8242-af7caafc64c1`
`27b82622-cc2d-6b35-af27-c93956653832`
`ece9e210-d956-2337-a3ac-cd62a6867cdd`

### Equestrian — 6

FEI and GCT/GCL event pages showed no Cairo participants entered yet; other entry
lists were incomplete.

`cbab2078-fdb6-553a-8108-3002423b0554`
`195282ed-7f6c-b738-9f7d-b2e1e2080bec`
`603eca8f-88e1-8a34-96e0-67151fb59c24`
`163607f8-c1c1-1a37-8577-85cd9c8ac174`
`d39f5a42-0c19-3c3f-a9b2-085afcceb278`
`fdf64a7d-4716-6b3f-af0e-f62d007e2ec2`

### FIS Alpine Ski World Cup (Women) — 4

The FIS Sölden event calendar confirmed the giant slalom; its start list was not
available.

`4b7263e3-0b5f-f03c-88db-6cc57387c25a`
`55f38474-c916-793f-90bf-6a80a812400a`
`2225fe52-2d9a-5e36-acb1-6fa57977921d`
`e80fb83f-2633-3c35-8ac7-2aa1ae88b951`

### Golden Trail World Series — 2

The official Golden Trail calendar confirmed Muju; complete elite entries were not
found.

`2ecd6d1e-1b5e-4c33-b96a-58c5b9ac8549`
`fb272e53-b3d2-bd35-8f98-712c68200ea9`

### ISU Short Track World Tour — 1

ISU series and Vancouver event pages confirmed the event; the Swedish roster
remained unknown.

`2d16c4cf-3594-5335-873b-22c026f989eb`

### LPGA Tour — 4

LPGA event and field pages confirmed BMW Ladies Championship; complete Swedish
participation was not verified.

`4c2bc427-3062-3036-86f4-89838f70dd02`
`cfbfcfd0-3d52-913d-9cbc-0223698ef823`
`0b04ba0e-a426-2031-8573-a2fdb742cdd1`
`0aadb6c1-ac86-3335-9089-68cf56983918`

### Motorsport — 5

FIA European Rally Championship event and entry-list pages confirmed Portugal;
available entry lists were incomplete.

`6b57a0e8-f738-a33c-a6e3-13bff2c9a87c`
`52d32665-3472-a031-abf6-f39d130626d6`
`047cf820-227b-c33e-9ca2-54ec9f97df18`
`89935c75-2271-5c38-96bf-ba7a8c0847f6`
`4c6776c3-8627-9e39-a965-167809202021`

### PDC / WDF — 6

PDC European Championship event pages confirmed the event; a complete draw and
Swedish roster were not found.

`01609679-2d44-9e3f-9268-1d7ab30d16d7`
`defe5409-b4b9-c337-8709-dfa5ed716257`
`c712a193-30e3-5c32-aeb8-bf14b5cb34c3`
`90a5f840-7794-7e3f-ab42-f9f5e908c8c7`
`ed2e2cf9-08b9-b636-ac7e-ee7fd56d88a3`
`82388484-8951-8e3c-9b8e-9289a9e12121`

### PGA Tour — 1

PGA Tour event and field pages confirmed Zozo Championship; Swedish participation
in the relevant field was not verified.

`03b6d2ee-25bc-d03a-be8a-8a1738cc152e`

### PGA Tour Champions — 2

PGA Tour Champions event and field pages confirmed Charity Classic; a complete
relevant Swedish field was not verified.

`10767dd1-8f7c-f73f-99df-0ced0d791766`
`4994156a-0c8a-e030-b05a-2d64f0c8dc64`

### Table Tennis — 1

ETTU's 2026 championship schedule listed the final; Swedish participants depended
on the future semifinal result.

`b3fddada-c3f7-803b-b16c-0432683efd89`

### World Nineball Tour — 6

The World Nineball Tour event page confirmed the Philippines Open and 128-player
format; its complete roster was unavailable.

`7699eb81-1415-de3d-9912-018e344add52`
`785556a8-8a78-3833-986d-0aca26466e53`
`7a3654af-e943-5d33-b778-c0fa8848cb58`
`87334024-ebaa-c136-875d-46f7e6f6461f`
`48eeb916-021a-4937-b7cc-c3e379f38778`
`1a77e776-5794-f639-87f9-9317f591f37e`

### World Snooker Tour — 3

World Snooker Tour event page confirmed Northern Ireland Open; a complete draw was
not available to confirm Swedish participation.

`3cfb13d2-76d0-2536-87f5-154949202052`
`c89d63cc-b114-ce3a-947e-dc7c93ac2ce1`
`5e7e1abf-af76-223c-8c18-49e0352978fb`

## Dashboard and public verification

- Dashboard login succeeded at `https://sesport.se/Admin/Dashboard`.
- Dashboard issues: `Singelfinal` is an intentional draft pending the semifinal;
  `SAS Championship – Runda 3` lacks sourced participant start times; both the
  2026-10-21 and 2026-10-24 F16 activities flag the same unresolved person data.
- Official PGA Tour tee-time results exposed rounds 1 and 2, but not round 3 at run
  time. No start times were inferred for Freddie Jacobson, Richard S. Johnson, or
  Robert Karlsson.
- Public checks used only `https://sesport.se/?date=YYYY-MM-DD`. All eight pages
  returned HTTP 200. Cards before → after:

| Public date | Before | After |
| --- | ---: | ---: |
| 2026-10-18 | 12 | 12 |
| 2026-10-19 | 0 | 0 |
| 2026-10-20 | 0 | 0 |
| 2026-10-21 | 1 | 1 |
| 2026-10-22 | 3 | 3 |
| 2026-10-23 | 2 | 2 |
| 2026-10-24 | 0 | 4 |
| 2026-10-25 | 0 | 0 |

The 2026-10-24 increase is the handball, F16, Bermuda, and F1 Academy cards.
F1 Academy's 2026-10-25 calendar time displays on sport date 2026-10-24. The
2026-10-18 total stays at 12 because the table-tennis group still has the semifinal
card after its unsupported final was drafted. The public page no longer renders
`Singelfinal`. Swedish and competing-country flags on changed cards loaded with HTTP
200 from `sesport.se`.

## Post-run title correction

- Activity `f141b8e3-0a4e-4e97-b6ea-7caf38a0cada` (2026-10-18) changed from
  `Runda 3` to `SAS Championship – Runda 3`, matching its group title.
- Rechecked `https://sesport.se/?date=2026-10-18` (HTTP 200): the new title is
  visible and the page still has 12 activity cards.

## Loop One participant correction

- Added Anna Magnusson, Johanna Skottheim, and Martin Ponsiluoma to `Finaler`
  (`9570cbab-2ffc-4ab0-a547-d5b80c202b94`); broadcasts:
  `d4f7ced5-127b-fc3c-a077-bc8f5097261c` and
  `05b1cf62-f0f5-303e-86cc-cfe273d0935c`.
- Added Anna Magnusson and Johanna Skottheim to `Damfinal`
  (`8d228e50-a8c1-4315-ba02-84b967fde601`); no separate broadcast link.
- Saved the IBU women's final start list as `ParticipationEvidence` on both
  activities and the men's final start list on `Finaler`:
  https://www.biathlonworld.com/startlist/SB2627SIBUL1MUSWSF
  https://www.biathlonworld.com/startlist/SB2627SIBUL1MUSMSF
- Rechecked `https://sesport.se/?date=2026-10-18` (HTTP 200): added names are
  visible and the page still has 12 activity cards.
- Elvira Öberg's existing links and event-overview participation source were retained.
