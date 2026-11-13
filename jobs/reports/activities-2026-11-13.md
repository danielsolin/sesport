# Activities — 2026-11-13

## Run and final audit

- Started 2026-11-13 05:44:35 UTC (06:44:35 Europe/Stockholm).
- Snapshot captured 05:45:17 UTC: 85 visible, unprocessed broadcasts without activity links.
- Target database: sesport, public. SQL access used bin/dbc.sh.
- Final ID audit: 85 snapshot IDs; 27 rejected rows hidden, 3 processed, 33 unresolved,
  and 22 snapshot rows absent before the guarded write.
- No invalid status combinations were found among rows still present.
- No tests or builds were run. No code, schema, assets, configuration, or deployment changed.

The 22 absent rows were all dated 12 November. They could not be updated because they no longer
existed in the active broadcasts table. Their IDs are still recorded below as rejected, but no
hidden_at value could be set. The cause of their absence was not established; this needs operator
attention. I did not recreate the rows.

Step 3 added 38 typed source rows. Two exact source records already existed, and four evidence
targets were absent with their broadcasts, so no orphan source rows were created.

## Created activities

### The RSM Classic

- Group 4ea72896-3012-4a47-b62c-5c0803271ebc spans 19–22 November.
- Activity a6c812fe-429f-4887-8370-8e972087ddf0: published Tournament, Runda 1, 19 November,
  17:40–21:00 Europe/Stockholm.
- Broadcasts f3eb5aeb-166e-d931-9e23-93b5250e7aa2 and
  68383578-ab36-d038-a2a1-5adb86233dd5 are linked, processed, hidden, and assigned to PGA Tour.
- Parallel HBO Max and Eurosport 2 coverage was combined into one activity.
- Ludvig Åberg (14d93669-d693-5d37-9c30-733c99a84201) is linked as the Swedish participant.
- The organizer listed Åberg among the players scheduled for the event. No participant-specific
  tee time was added because the round pairing sheet was not available in this run.
- Saved organizer, participation, and TV.nu evidence plus the existing direct HBO Max event URL.
  Eurosport 2 uses the active Max sports catalog link.

### GCL Super Cup

- Group 3b95bcbf-35f0-4356-a82d-7ab435e9afd1 spans 19–22 November.
- Activity 03e8f2bf-0983-46b0-85f2-463e9704b114: published Stage, Kvartsfinal, 19 November,
  20:20–22:20 Europe/Stockholm on TV4 Play.
- Broadcast 6d74ab41-5f21-4b3f-a639-59f3c11ff314 is linked, processed, hidden, and assigned to
  Global Champions Tour.
- Cannes Stars entity 0b89179c-dd44-44a9-aaa2-63add45bfbfe was created with its official team
  page and France as its sourced country.
- Angelica Augustsson Zanotelli is linked as an active Person participant, represented by Cannes
  Stars. The team link is 5cb4823c-6df4-4a4e-9dd5-407767815cb6; the activity link is
  fb06cf91-c720-46e3-ba6e-a8d8b0d92631.
- GCL lists Angelica on the 2026 team roster, and Cannes Stars qualified for the quarter-final.
  The event-day rider order was not available, so no rider-specific detail activity was added.
- Her profile URL now points to the working official GCL rider profile. Her birth date and
  formative club were reused; the hosted card shows age 39 and Nordhallands Ridklubb.
- Saved GCL schedule, standings, team, rider, TV.nu, and direct TV4 Play evidence.

## Other data corrections

- LPGA Tour was assigned to both visible CME Group Championship broadcasts.
- PDC / WDF was assigned to the two Grand Slam of Darts broadcasts that lacked an organization:
  - 8577a8cd-c9b0-eb33-969c-5203b1e8389d
  - d0a19be9-b371-973a-90b9-308bcb501cb6

## Rejected broadcasts

All 49 were classified as outside scope. Existing rows were changed only by setting hidden_at. Rows
labelled absent had already disappeared and could not be hidden.

### Domestic ATP Stockholm Open (5)

Domestic competition in Sweden is out of scope.

- 10d762b0-935d-b13d-a82c-7495ab5d3df4 — absent; not hidden
- 4370a76d-6c53-453a-86ec-22a891b47740 — absent; not hidden
- f1c00d23-5f4f-0539-804b-116f88c9d064 — absent; not hidden
- 4d5541f7-4e64-9631-ab9f-f08fff0fafd5 — absent; not hidden
- b2554e41-c451-f53e-864c-678d169ca14f — absent; not hidden

### Foreign national-team fixtures (11)

Fixtures between foreign national teams are out of scope, even when Swedish athletes are involved.

- 00da3809-a528-eb3e-8370-2fb8541290a7 — absent; not hidden
- 9cacaa00-2275-3f31-b378-df908db1a90d — absent; not hidden
- fea43a2e-ca2c-913a-9069-eccb879cfb65 — absent; not hidden
- ba5bf1da-d998-f737-8f95-adabbe34c0e9 — absent; not hidden
- 91ecf427-b016-f535-bdca-dac0e52a2d89 — absent; not hidden
- 2d83b0b4-6985-6c32-aad7-5316a2a1a24b — absent; not hidden
- 388daa8b-885d-4135-b0cd-a9926beecf1a — absent; not hidden
- 7bd3acaa-d90c-f53a-a8d6-dec85b14cbb2 — absent; not hidden
- b6b22238-0292-ed31-a33d-6a1309991dcc — absent; not hidden
- 118a744d-f79b-a431-8092-d1eaee1908e8 — absent; not hidden
- c492f94a-0928-bb32-83a5-10aa775e4b39 — absent; not hidden

### Domestic Swedish leagues (4)

Domestic Swedish league broadcasts are out of scope.

- 888897c5-9819-ee3a-bb2a-a9995fa6d040 — absent; not hidden
- d20869b5-cf86-1c33-a764-02be01dec1ec — absent; not hidden
- 505be086-f4a8-143c-acc6-688d2b2d0199 — hidden_at set
- 714ddb02-f9b0-c03c-ae04-5dcabc67e385 — hidden_at set

### Studio, highlights or recap (4)

Generic studio, highlights, and recap programmes are excluded.

- 950ad8e5-a08e-9e30-a8e3-5d3e77095c25 — hidden_at set
- e634f10f-b0ef-fa31-b563-1b276989ad75 — hidden_at set
- 2612929c-cf8a-ea35-afef-398e9fac8686 — hidden_at set
- 72ff237d-175e-7c3d-b8dc-59d0bd932fe5 — hidden_at set

### Unidentified billiards rounds (2)

The round labels do not identify a competition or event.

- de3d0caa-2a30-983e-8d07-dae41fbdc8b5 — hidden_at set
- ebba1f21-305b-643a-969f-4a24e3ff4cf5 — hidden_at set

### Foreign Finnish Liiga fixture (1)

This is a foreign-team league fixture.

- 5c89395e-f6f1-a037-9d86-088eea802c8d — hidden_at set

### Complete SOK roster has no relevant teams (2)

The official SOK roster showed no Swedish team in the listed beach handball or futsal events.

- f23b468c-7477-f937-bec0-06b674f1cee9 — absent; not hidden
- 54264ca3-f297-1a33-8347-84386642c9ff — absent; not hidden

### Complete Champion of Champions field has no Swedes (2)

The official 16-player field contains no Swedish players.

- f1424310-e962-5135-bfb2-7d059592e848 — absent; not hidden
- 41d39729-707a-bc36-bf8f-c5bebec83a5c — absent; not hidden

### Complete ATP Finals fields have no Swedes (8)

The complete official singles and doubles fields contain no Swedish players.

- 8c168cf2-80ad-7331-b731-7977f40cfab5 — hidden_at set
- 98e5cf52-8929-033a-98ce-6d033ac77b9f — hidden_at set
- 37b2f183-1fd3-5535-8359-6ab2e9d5b951 — hidden_at set
- a9319298-7279-6635-9731-a3cde1b3b336 — hidden_at set
- 326ff368-b6f6-0e37-a2ee-817de10e0b73 — hidden_at set
- 8dcb4d9b-fe3c-c834-9774-01ab78b35385 — hidden_at set
- ab734f15-c639-6938-907c-477f3a032d10 — hidden_at set
- f1edecf5-d42e-f93f-b88b-a34b39931bbd — hidden_at set

### Complete WST qualifying draw has no Swedes (1)

The official German Masters qualifying draw contains no Swedish entrant.

- 685bdceb-9cb2-c33e-8f8d-9e6495dd589f — hidden_at set

### Complete Formula 1 grid has no Swedish driver (4)

The official 2026 Formula 1 driver list contains no Swedish driver.

- 29419b9f-8e12-ea39-9c44-5fababefc4b5 — hidden_at set
- 9a83c81a-ec78-c836-bd74-a98d40624304 — hidden_at set
- d4327362-2b92-6b32-9444-3178b525b315 — hidden_at set
- d793ed3e-0961-a630-ba9f-77d5ddf600b6 — hidden_at set

### Complete Hamme elite lists have no Swedes (5)

The complete official elite women and men start lists contain no Swedish rider.

- 0c97916b-3505-093d-beec-26b1f21afeef — hidden_at set
- e95f0a0e-e407-093b-a0d6-8ec1d8ea3611 — hidden_at set
- b0c6af74-573d-9c3b-a114-a1ad0d7cb343 — hidden_at set
- b1e783ae-93bc-453b-9b8e-50998c9b286a — hidden_at set
- 2d078751-89ae-7735-a1e8-17ea76042178 — hidden_at set

## Unresolved broadcasts

All 33 remain visible and unprocessed. Each retains a reason and the known organization.
Organization not tracked means no matching organization entity exists in SESport.

### Dubai T100 wildcard slots remain unnamed (2)

- Organization: T100 Triathlon World Tour. The official lists still show two unnamed wildcard slots
  per gender, so Swedish participation cannot be confirmed.
- 4a09555c-a465-723f-a79a-ca449d234dc2
- fb15c1c6-6162-0a37-8621-82077484f1d8

### Merksplas has no complete current entrant lists (2)

- Organization: Cycling. The event page does not provide complete current entrant lists.
- 48b30142-56e0-0d38-95a6-d130f104ee66
- daf54d93-a7c1-d73b-bf27-942f585dfdfd

### Daytona entry list gives hometowns, not nationality (2)

- Organization: Organization not tracked. The 160-entry list gives names and hometowns but no
  nationality; no organization entity is tracked.
- 24cf4bbb-d485-0836-bbb7-d7e8cad9d949
- e6933904-3066-6536-b629-628a25b3c29a

### IIHF rounds depend on unplayed Sweden–Switzerland QF (12)

- Organization: Ishockeylandslaget (D). Sweden–Switzerland had not played its quarter-final at
  snapshot time; later semifinal and final teams were not known.
- e87ee685-f224-b53f-86f9-810730ddb329
- 2aedd7f7-c769-c93d-9d90-f6d37c57da2c
- 79c12ace-35d2-9f30-9912-08e1eae41f1d
- bc194878-9a2a-563f-9857-de5d8d317e2c
- 14f0f9a6-ee62-d93c-9a97-fb5006909ebd
- 9986469d-9dee-d33b-b3ec-e04c17bc90e4
- f3840800-f4ca-1a38-b6d5-ec7f0849cc3c
- d663b80c-5c25-9e3c-a018-938198553550
- a41d4795-75ad-c234-a53e-75e4456b595a
- da878fa6-b3dc-1a39-bc18-b22817834498
- dfb866d9-19f7-d83b-bab7-95bbc47021df
- 5895be4b-453f-b13c-a1ce-2d4b76574b4d

### Sweden–Belgium matchday roster is unconfirmed (3)

- Organization: Basketlandslaget (dam). The 14-player preliminary squad does not confirm the
  12-player matchday roster.
- f3569876-e1a0-da37-9d70-b55cfb595f43
- 6e42c75f-d0b7-1934-9cc6-b0ba283e0977
- 50bd5925-7c2f-0535-a2b5-8cb01461f23b

### Darts matches depend on pending group results (6)

- Organization: PDC / WDF. Sweden has an entrant, but later matchups depend on pending group
  results.
- 6b67db15-d728-7b38-97b4-d0abeca27668
- de148822-348c-7530-8184-3744ba8efb51
- bf65a69d-a175-d437-9773-f0b796e80bf3
- c69df824-1d12-423d-8148-cc5f515e2f35
- 8577a8cd-c9b0-eb33-969c-5203b1e8389d
- d0a19be9-b371-973a-90b9-308bcb501cb6

### Nov 18 GCL broadcast has no confirmed round mapping (1)

- Organization: Global Champions Tour. The TV listing says 18 November, while the official guide
  places the quarter-final on 19 November; no specific round mapping is supported.
- d2eb2fc7-5f7a-723a-a3ff-68a8a0a440c8

### Ferrari Challenge lacks complete nationality-marked entries (3)

- Organization: Organization not tracked. The event page confirms the event but no complete
  nationality-marked driver list was found; no organization entity is tracked.
- e6622f4d-a08d-d239-89eb-3c268f9c5ad9
- 14d51f7b-f81f-1932-bc45-245fa010c850
- f937e3b5-97bc-7838-b980-c6eed8bf6003

### CME field depends on The ANNIKA, which is still underway (2)

- Organization: LPGA Tour. The CME field depends on The ANNIKA results and was not final at snapshot
  time.
- 022561ef-cb25-8c3c-9351-15924eada4f6
- d24eadb1-37d0-163a-8612-24f1e9f299f6

## Dashboard review

The hosted dashboard showed one issue: activity 7813a903-1c70-4587-abec-4c2be20cb584, DP World Tour
Championship — Runda 3, on 14 November. The sibling Viaplay activity is
f9cf9801-d484-4b1a-bef7-14ff5bae5fb2. Both include Mikael Lindberg but have no participant
start-time result.

The DP World Tour tee-times page did not list individual pairings when checked. Its official week
schedule says Round 3 is on 14 November and gives only an approximate first tee time. Round 2 was
still underway, so no sourced Mikael start time could be added. The dashboard issue remains
unresolved for a later run; no guessed time or activity change was made.

## Hosted presentation and card counts

Only 19 November received new activities. The hosted page shows two separate cards with clear
Swedish segment titles, participant age and club, and linked channels and profiles. RSM shows HBO
Max and Eurosport 2; GCL shows TV4 Play. The stream links and country flag SVGs used by the cards
loaded successfully.

| Public date | Before | After | Explanation |
| --- | ---: | ---: | --- |
| 2026-11-14 | 10 | 10 | Dashboard item only; no data change. |
| 2026-11-19 | 0 | 2 | One RSM Classic card and one GCL Super Cup card. |

No activity cards were moved. The 22 absent 12 November broadcasts were no longer present in the
database and therefore did not create hosted cards.

The hosted cards rendered all provider links. Automated checks returned HTTP 403 from the HBO Max
event and Max catalog URLs. The TV4 Play URL redirected to a program page with HTTP 200. The
country-flag SVGs returned HTTP 200.

## Evidence links

Typed ActivityEvidence, ParticipationEvidence, PersonFacts, and StreamLink rows are attached to the
related broadcasts, groups, activities, and Angelica record.

- RSM organizer release:
  https://rsmclassic.com/media/press-releases/eight-winners-this-season-coming-to-sea-island/

- TV.nu HBO Max:
  https://www.tv.nu/s/p_1637074_20261119

- TV.nu Eurosport 2:
  https://www.tv.nu/s/e_4991654_20261119

- GCL guide:
  https://www.gcglobalchampions.com/news/1184161

- GCL final standings:
  https://www.gcglobalchampions.com/news/1182513

- Cannes Stars 2026 team page:
  https://www.gcglobalchampions.com/gcl/teams/2026/cannes-stars

- Angelica GCL rider profile:
  https://www.gcglobalchampions.com/gcl/riders/angelica-augustsson-zanotelli

- TV.nu GCL quarter-final:
  https://www.tv.nu/s/p_1627949_20261119

- SOK Dakar roster:
  https://sok.se/olympiska-spel/tavlingar/yog/dakar-2026.html

- Official Champion of Champions field:
  https://championofchampionssnooker.co.uk/

- ATP singles Race to Turin:
  https://www.atptour.com/en/rankings/singles-race-to-turin

- ATP doubles Race to Turin:
  https://www.atptour.com/en/rankings/doubles-race-to-turin

- German Masters qualifying draw:
  https://www.wst.tv/matches/1ec300d9-da1c-4b16-95e7-d296c399255b

- Formula 1 drivers:
  https://www.formula1.com/en/drivers

- Hamme official entrant list:
  https://www.bollekescross.be/info/deelnemers

- Dubai T100 participant list:
  https://stats.protriathletes.org/race/dubai-t100/2026/participants

- Merksplas official event page:
  https://www.superprestigecyclocross.be/nl/kalender/2026-2027/merksplas

- Daytona event page:
  https://www.hsrrace.com/classic-24-hour-at-daytona-daytona-historics/

- IIHF schedule:
  https://www.iihf.com/en/events/2026/ww/schedule

- FIBA qualifier schedule:
  https://www.fiba.basketball/en/events/fiba-womens-eurobasket-2027-qualifiers/games

- PDC Grand Slam draw:
  https://ui.pdc.tv/news/2026/november/09/littler-drawn-greaves-zonneveld-at-mr-vegas-grand-slam/

- LPGA CME entries:
  https://www.lpga.com/tournaments/cme-group-tour-championship/entries

- Ferrari Finali Mondiali event:
  https://www.circuitcat.com/es/eventos/ferrari-finali-mondiali-2026/

- DP World Tour tee-times page:
  https://www.europeantour.com/dpworld-tour/dp-world-tour-championship-dubai-2026/tee-times
