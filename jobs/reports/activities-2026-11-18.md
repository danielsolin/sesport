# Activity processing report — 2026-11-18

## Run

- Started: 2026-11-18 05:22:35 UTC.
- Snapshot: 133 visible broadcasts, captured 2026-11-18 05:24:04 UTC.
- Completed: 2026-11-18 06:09 UTC.
- Database: `sesport`, verified through `bin/dbc.sh` (PostgreSQL 17.10).
- Final outcomes: 2 Processed, 71 Rejected, 60 Unresolved.
- All 133 snapshot IDs have exactly one final outcome.
- All 60 unresolved broadcasts remain visible and unprocessed with an organization.
- No code, assets, schema, configuration, or deployment changed.

## Processed activities

### GCL Super Cup quarter-final — 19 November

- Created published Stage activity `82fccbf3-a7dd-4371-afa1-50f33cebf169`.
  Group: `3b95bcbf-35f0-4356-a82d-7ab435e9afd1` (existing GCL Super Cup).
- Broadcast `9c58bf54-2e19-b638-a7db-aeb2cfac5df1` is linked, processed, and hidden.
- Segment: 20:20–22:15 Europe/Stockholm on Eurosport 1.
- Participants: Peder Fredricson and Angelica Augustsson Zanotelli.
- ActivityEvidence: TV.nu segment listing and FEI Verona schedule.
- ParticipationEvidence: official GCL roster and 2026 team standings.

### LGCT Super Grand Prix final — 21 November

- Created published Event activity `8716957f-762d-4693-8953-88be3a6dabff`.
  Group: `fb0bee1f-e9be-4128-bcbf-605f87c646f2` (existing LGCT Super Grand Prix).
- Broadcast `2f90a4cb-88ff-e733-92ea-8bc9ae02bb34` is linked, processed, and hidden.
- Segment: 22:00–22:25 Europe/Stockholm on Eurosport 1.
- Participant: Angelica Augustsson Zanotelli.
- ActivityEvidence: TV.nu segment listing and FEI Verona schedule.
- ParticipationEvidence: official LGCT final field.

### Person profile correction

- Corrected Peder Fredricson (`60267f8f-7904-8738-a1c3-fdee507cf880`) to the
  official canonical FEI profile path:
  https://www.fei.org/athlete/10002504/Fredricson-Peder
- Updated the PersonFacts and ParticipantStarEvidence source URLs.
- Current FEI profile lists rank 102 and 2026 Worlds places 52 individual, 10 team.
- Kept Peder at `tier_1` and Angelica at `tier_2`; neither met both `tier_0` criteria.
- Existing birthdates, club links, and person profiles were retained. No detail
  activities were needed because no broadcast supplied a sourced rider start time.

## Rejected broadcasts (71)

### 4 ATP Finals broadcasts

Reason: The official 2026 singles and doubles fields contain no Swedish entrant.
- Sources: official Nitto ATP Finals singles and doubles field articles.
  https://www.nittoatpfinals.com/en/news/luz-matos-nitto-atp-finals-2026-qualification
- 013a8026-712f-0838-8ae1-b2e703ea786a
- 3bd540c1-cb2f-c735-b481-6fb6bae6aba1
- 59c8164f-5e49-4d37-b7d5-31fdd4124bc5
- b85448de-8b0e-4d37-beb5-6a2fa519d8bd

### 24 Formula 1 broadcasts

Reason: The complete 2026 driver grid contains no Swedish driver.
Source: [Formula 1 2026 driver list](https://www.formula1.com/en/drivers).
- 097711b1-33b6-e633-b74e-e94016573278
- 0a94ad90-8a83-e036-857c-de4cdebdbcdf
- 110eadd3-7d15-8a37-a8b0-ca3978d24160
- 2b507cda-34bb-3432-9610-fe7807382835
- 2e19956a-6dd7-af3a-8d13-1be0080cac8a
- 3abb9d77-d9e3-a13f-9669-6e4323e53cab
- 4a0d633e-e63a-1f38-8b56-e8387e953667
- 53f6ba65-e115-833e-b2df-ed0a2bd10722
- 55cd484c-ead3-0e35-aa06-c29474e13623
- 57b4e780-2b24-b83c-85d9-90af06310d5a
- 60549732-af76-f83e-9cd8-ecc755a88ac0
- 63bb74a1-b2d4-bb32-bbe0-3a28c776c417
- 67ec5963-938c-1b33-96d3-eb459f6cf962
- 729637c7-5ec6-2930-b68c-b41e3ed8a647
- 780ba254-4625-f83f-a9d1-765ba1fdf1d5
- 78e72af9-51a2-283f-848d-ddeb05fb80bc
- 8e169216-4112-ff31-969f-6005e658c6c3
- 94a6afc8-dd7b-4730-8dec-eebc718fface
- 9dedff97-60a3-7230-b888-312f0ff6cfc0
- b88a066e-df38-873f-b328-674d96ada2b7
- c23582ca-a2b1-1e34-8fb2-eedf2ac7239f
- cec31d11-95bb-db3c-a349-9ac970a1c877
- e0c5ea68-e61e-f435-864a-ddd05652a560
- f7f24fc8-c20b-7330-86c0-2b8f6e93b34a

### 15 Grand Slam of Darts broadcasts

Reason: Both Swedish entrants, Jeffrey de Graaf and Anton Östlund, were eliminated.
- Sources: Official PDC reports for 16 and 17 November record Anton Östlund
- and Jeffrey de Graaf losing their decisive matches; neither advanced.
- 023e29c6-9ea2-1c35-9927-5d86a0f04558
- 0f4a2b1f-ad91-b331-ac56-09844ae62a39
- 1cae2e68-5913-863f-8ce5-930d64e8fa93
- 283bc0b5-0160-ed37-b22f-b62f649d0388
- 42440a37-1c2f-b33d-b9c6-80587d231ad4
- 81b99e6d-d495-4532-9102-9cdf3b435505
- 8577a8cd-c9b0-eb33-969c-5203b1e8389d
- b04dd44e-4e3e-dd32-8a3b-43144c9f696d
- bf65a69d-a175-d437-9773-f0b796e80bf3
- c69df824-1d12-423d-8148-cc5f515e2f35
- d0a19be9-b371-973a-90b9-308bcb501cb6
- d3ccb004-aab2-143c-958d-dbed88c0a002
- e289c57b-31cd-2930-9686-5cbaccaf97f0
- e4e9b951-387b-0139-b1d5-aec88e7341dc
- ebe7f11f-5764-8a3a-b810-cc34ecd7348c

### 1 OKTAGON 98 broadcast

Reason: The official fight card lists no Swedish fighter.
Source: [Official OKTAGON 98 fight card](https://oktagonmma.com/de/events/oktagon-98-trinec/).
- 940bd6e3-f5e1-7638-afc1-13736143cace

### 1 squad announcement

Reason: The item announces a future roster and is not competition coverage.
Classification uses the broadcast title and description: a roster announcement.
- 27720b27-e875-3536-9dbf-e0f3d24b42dd

### 2 studio or highlights programmes

Reason: Generic NHL or Champions League studio/highlights content.
- 0f188eeb-7ae4-4d31-bcfc-0e70897885cb
- 6b6e4154-e9b0-cd38-a8bc-65e5dcee27b6

### 1 Bollklubben programme

Reason: A studio programme without a scheduled eligible event.
- 62be7514-6ffa-3835-9c16-2db2e8ffbe94

### 17 foreign-team fixtures

Reason: The named UEFA fixtures involve no Swedish club or team.
The named teams and UEFA competition categories identify foreign fixtures.
- 050738c4-5e34-1037-acb4-6769a64042d4
- 09dd4f13-eafe-313b-9403-f779ffa8493f
- 116056be-436e-1633-9db9-846c073392f0
- 12abf570-518d-a43a-9673-30e0891acf2f
- 18ed6e98-52c0-b532-9eac-0987ac27541c
- 236c75a2-ffa6-5438-8e19-1fa11c85fb0d
- 3f3345cd-565e-c333-b3a6-395c781a68f0
- 410cf6b6-497a-2033-b386-44d95af9330e
- 42b2082b-f208-8838-8453-400db018252b
- 438bb106-e3df-263f-9339-64c170911eaa
- 4ff335e6-414b-f53a-af03-a409c95893d8
- 648dd30f-d14b-193e-8d8a-6732a3d00f85
- 66f8c1a4-87c9-4831-b307-9a6d6a24f890
- 87546209-fd51-cb32-ae81-f73c7c415e4f
- af12c1ae-c489-ee3c-a145-db0e61d6758d
- b9734a9e-da03-853b-a77e-99f7b82503e8
- d9d27f8d-5dbb-583e-a410-e0fb1e309d66

### 2 foreign domestic-league fixtures

Reason: Domestic league matches are outside the international scope.
Titles identify Finnish and English domestic-league fixtures.
- b79d3695-ced5-7d3b-b1d0-8c282ad7f529
- cdedd303-54ee-7432-885b-918afc36c7af

### 4 Swedish domestic fixtures

Reason: Swedish domestic competitions are outside the international scope.
Titles/categories identify Swedish league and domestic cup fixtures.
- 02bf9ca4-8ebf-2539-8692-4443bf2500cc
- 5d113803-593a-4635-ba80-60c728ec759b
- 8501f791-78ab-183e-a530-bacdc93d7de4
- acf5ed64-6c00-d23f-959f-c90e76ce0bdc

## Unresolved broadcasts (60)

All remain visible and unprocessed. The stated organizations are set on the records.

### Backhoppningslandslaget — 6 broadcasts
Organization ID: c2f97b06-7e4b-3bf8-16b1-0c579df406e6
Reason: Lillehammer start lists are unpublished; no Swedish start is confirmed.
Status: visible and unprocessed.
- 0871bf32-0241-e335-b408-8eeba92491e8
- 098b6e2c-e41a-443c-99f6-911917c4be62
- 1a76f6a5-6ce0-d038-b57c-326ba86e2848
- b36fdc5c-d136-be33-80db-f5dbfa2622eb
- cc680dd5-1994-1b33-bac1-261438ba22e2
- d969e3ed-d925-0131-802d-752051541753

### Cycling — 6 broadcasts
Organization ID: 467a3c47-a54d-47ea-8aa4-a8742551ea4f
Reason: No complete authoritative event start list confirms Swedish participation.
Status: visible and unprocessed.
- 125e6de6-9201-c433-bdfd-5ce360432784
- 1d48b702-89f3-f13b-844a-178262190764
- 643e7a75-d19f-d230-a58a-c49b26e35491
- 89147eff-732f-4c3e-87ab-2c774c91e5a8
- a4d5ae9c-1776-e438-bb7e-10f4bed0b176
- ff391930-633b-6c37-98dc-cce6395363c0

### FIS Alpine Ski World Cup (Men) — 4 broadcasts
Organization ID: fed7e5e0-49d3-c461-829b-ecce1cbd616e
Reason: Race-specific Swedish start lists are not published; selection is made per race.
Status: visible and unprocessed.
- 66aa9475-16f8-a638-b88e-c556c65a5d13
- a71ff7d0-ca08-ef3e-a270-b366c9d31b8e
- bda966da-6823-8237-bcfb-b3e5019c81f3
- de15e723-6c54-ff3a-805c-d193e82831f0

### FIS Alpine Ski World Cup (Women) — 4 broadcasts
Organization ID: 915fad1f-c00a-6b78-b199-f5d6bf465970
Reason: Race-specific Swedish start lists are not published; selection is made per race.
Status: visible and unprocessed.
- 499d349a-8baf-2d35-9e53-4b9d8b131137
- 5ae964eb-184c-ad36-a5d6-76f5cddb39c7
- b0833cda-b9cf-c036-b4f0-20c35ceafc95
- dfa6c209-d79a-7237-9037-a53a88dc916d

### FIS Ski Jumping World Cup (Women) — 1 broadcasts
Organization ID: 2ef3408d-71a2-d0ae-c93e-6b13279058b2
Reason: The official Lillehammer start list does not confirm a Swedish starter.
Status: visible and unprocessed.
- 8f2d9979-e034-4e36-b72e-1ba6225a2671

### Ferrari Challenge Europe — 7 broadcasts
Organization ID: 5842c75a-6902-49ae-b6ea-c4733a0ff65f
Reason: No complete authoritative Finali Mondiali entry list confirms Swedish participation.
Status: visible and unprocessed.
- 0973f85e-409b-4d38-940c-04a7fa82a7aa
- 14d51f7b-f81f-1932-bc45-245fa010c850
- 491cb3f3-d987-1f31-a52e-301d9aeabb57
- 585ee2bd-28b9-e535-a672-2eee9246d420
- 8e169384-58c1-4e3c-9fe2-f23251075635
- b452083a-35f1-bc30-9efa-01d976fc552e
- f937e3b5-97bc-7838-b980-c6eed8bf6003

### Ferrari Challenge North America — 2 broadcasts
Organization ID: a58760e7-bf9d-49b9-a8a5-b30920991754
Reason: No complete authoritative Finali Mondiali entry list confirms Swedish participation.
Status: visible and unprocessed.
- 79234899-2f39-cd36-99b9-2907c03faa17
- e6622f4d-a08d-d239-89eb-3c268f9c5ad9

### Global Champions Tour — Sunday final progression — 3 broadcasts
Organization ID: 3237ea4c-5798-4cbf-9a0b-670b51b28a4b
Reason: Swedish team progression to the Sunday final is not yet confirmed.
Status: visible and unprocessed.
- 38964e6d-58ed-ce3a-bc6e-4ed95fd97245
- 4c9811b9-a5a4-c339-b8ce-2e8ed8b2f5c6
- 9290f5a4-1200-7a36-b820-6e845ffb255f

### Global Champions Tour — Walk of Champions listing — 1 broadcasts
Organization ID: 3237ea4c-5798-4cbf-9a0b-670b51b28a4b
Reason: The time aligns with the GCL Walk of Champions; Swedish rider presence is unconfirmed.
Status: visible and unprocessed.
- e0cf7cfc-ebab-6c32-b9d2-91c4ea031526

### International Skating Union — 3 broadcasts
Organization ID: 4e148819-d576-4981-a1a2-09b56625c02d
Reason: No complete official Obihiro speed-skating entry list confirms Swedish starters.
Status: visible and unprocessed.
- 37c9b20f-d041-f030-af24-234fd311d010
- 887e630d-67e3-5a39-ac6f-b3f47715796a
- a228274d-11c3-193b-a60c-bed35abbef56

### MotoGP — 22 broadcasts
Organization ID: 170549e3-17a2-4000-b80c-61b344d922c4
Reason: The official Portugal page says the entry list appears days before the GP.
Status: visible and unprocessed.
- 095ce56e-f7af-5338-b582-4ef567796f2e
- 096c43e3-b763-ef33-8dd4-16482832c474
- 0d34cc07-063e-d933-9284-a030816fd5a7
- 0dbe66a1-500e-6038-b908-0af045ac6bde
- 1698dc74-2e96-c03b-8fcf-4f939b77d6b1
- 21793271-3089-5d3c-9e99-178b4bf932ee
- 349f6641-3eb6-c33c-bfce-01a40d83bbe3
- 413ceedc-a50e-1c33-a9ea-3995ca61052a
- 4f03fcda-5c4f-cf33-aa57-e31ead76f6f1
- 591f0ce6-656d-9530-b586-ab699cfeb090
- 8b0fa140-6d5f-de3c-9b56-28b25677501a
- 8e301381-880d-e430-878d-35bc2df2a4fd
- 8f8459fa-e2e8-963e-9c68-e32c129c6e47
- 90c29842-7cb6-213e-b896-8d78ef64883f
- 973e7d81-b6a1-eb34-a7a8-3f1c10df2cc2
- bb7e134d-f916-f033-8233-75537963f4c3
- bc6b40ad-7636-5b37-ac14-6ab0a0870089
- d3d8fe7b-ccb9-4838-9883-96b850d30c8e
- d4332cbf-cb56-d23e-a378-3079f83ebe2d
- d474368c-aaa9-9a37-940f-bba9a46794f4
- def3cd60-a977-6132-b507-ec20a44f495a
- e2f6e7f9-183e-183e-be65-c8d1feab140d
### World Snooker Tour — 1 broadcasts
Organization ID: b3a41eb0-1d5c-4c4d-9386-1766ac97e2c5
Reason: The Nov 23 slot falls after the official qualifying dates; its event mapping is unclear.
Status: visible and unprocessed.
- d8c017ea-dbba-9536-99e7-c2258df1dfc1

## Research notes

Formula 1: the official driver list names the complete 2026 grid and no Swedish driver.
ATP Finals: official 2026 singles and doubles field releases list no Swedish entrant.
Grand Slam of Darts: PDC group results show Anton Östlund lost on 16 Nov and Jeffrey
de Graaf lost on 17 Nov; both were the Swedish entrants.
OKTAGON 98: the official fight card names its fighters and contains no Swedish fighter.
Ferrari Challenge: the official Nov 17 Finali Mondiali announcement names the event and
80-plus drivers from 21 countries but supplies no complete named entry list.
Cycling: Belgian Cycling confirms the Beringen calendar date; no complete event start
list confirming Swedish starters was found for the listed Beringen/Lokeren races.
FIS Alpine: the official Gurgl programme lists race dates and times, not Swedish starts.
FIS Ski Jumping: the official Lillehammer competition page had no results/start list.
ISU: the official Obihiro event schedule/announcement contained no complete entry list.
Global Champions: the official final guide limits Sunday places to progressing teams;
the Swedish team progression was not established at the snapshot date.
MotoGP: the official Portugal event page says the entry list appears days before the GP.
Snooker: WPBSA lists German Masters qualifying on 16–19 Nov; the Nov 23 TV slot remains
unclear. No event match could be supported.

Relevant official source links:
- [F1 2026 drivers](https://www.formula1.com/en/drivers).
- Nitto ATP Finals 2026 singles field:
  https://www.nittoatpfinals.com/en/news/nitto-atp-finals-2026-field-is-set
- OKTAGON 98 official event card: oktagonmma.com, 21 Nov 2026.
- Official PDC: Anton Östlund is Swedish and in the field; de Graaf and Östlund
  lost their final group matches.
  https://ui.pdc.tv/news/2026/november/14/mr-vegas-grand-slam-of-darts---stats-special/
  https://ui.pdc.tv/news/2026/november/16/Littler-progresses-despite-first-Grand-Slam-defeat/
  https://ui.pdc.tv/news/2026/november/17/wade-and-van-gerwen-set-up-blockbuster-grand-slam-clash/
- FIS official Gurgl programme: race schedule updated 11 Nov 2026.
- Belgian Cycling official calendar: X²O Badkamers Trofee, Beringen, 22 Nov 2026.
  https://www.belgiancycling.be/app/uploads/2026/11/CYCLO-CROSSKALENDER-2026-2027-261116.pdf
- Ferrari official/IMSA Finali Mondiali announcement: 17 Nov 2026.
- ISU official Obihiro World Cup event programme and schedule.
- FIS Ski Jumping: official Lillehammer competition page (no results/start list):
  https://www.fis-ski.com/DB/general/results.html?raceid=7797&sectorcode=JP
- MotoGP official Portugal event page:
  https://www.motogp.com/en/calendar/2026/event/portugal/c7f7626b-807f-4a6f-813e-7ad2c68302a3
- WPBSA German Masters qualifier dates, 14 Nov 2026:
  https://www.wpbsa.com/how-to-watch-the-machineseeker-german-masters-qualifiers-2/

## Public presentation

| Public date | Cards before | Cards after | Result |
| --- | ---: | ---: | --- |
| 2026-11-19 | 3 | 3 | GCL gained a segment row within its existing card. |
| 2026-11-21 | 5 | 5 | LGCT gained a segment row within its existing card. |

- Before: https://sesport.se/?date=2026-11-19 (3 cards).
- After: https://sesport.se/?date=2026-11-19 (3 cards).
- Before: https://sesport.se/?date=2026-11-21 (5 cards).
- After: https://sesport.se/?date=2026-11-21 (5 cards).
- The 19 Nov GCL card displays three rows, correct segment times and channels, and both
  Swedish riders with ages, club links, Swedish flags, and profile links.
- The 21 Nov LGCT card displays HBO Max, TV4 Play, and Eurosport 1 rows. Its Final row
  shows 22:00–22:25; the Eurosport link resolves through the active catalog to max.com.
- Peder Fredricson's canonical FEI profile and Angelica Augustsson Zanotelli's rider
  profile resolve. Displayed clubs also load successfully.
- Card counts stayed the same because both activities belong to existing event groups.

## Admin dashboard

- Checked https://sesport.se/Admin/Dashboard using `Admin__Password` from `.env`.
- “Activities needing attention” shows no upcoming activity issues; no open todos.
- No dashboard data correction was needed.

## Completion

- The 60 unresolved broadcasts have documented reasons and organizations. Revisit them
  when complete official start lists, entries, or team progression become available.
- No immediate operator action is required.
