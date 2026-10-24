# Activities job — 2026-10-23

Started: 2026-10-23 07:26:39 UTC.

Snapshot: 115 broadcasts had hidden_at unset, across dates and process states. All were accounted
for: 65 irrelevant feeds were hidden, 8 confirmed feeds were linked, processed, and hidden, and
42 uncertain feeds remain visible and unprocessed with an organization and ActivityEvidence.
No broadcast was left unattended.

## Created and changed activities

All times are Europe/Stockholm. The four new activities are published for 2026-10-29.
The existing Curling-EM group 03733f6c-77a0-476a-820a-c69ac24aa590 was reused. A
UEFA Women's Champions League group was created: 0114eabb-fe44-4af2-b45d-851d3d0ac468.

- 09:20–11:50, Skottland – Sverige, SVT2 and SVT Play.
  Activity 3d90f838-79e5-47ce-bcef-8dde4613fad9.
  Broadcasts 2a6518b8-07a3-d137-a295-86dfad4389cc and
  46ac6476-4f55-b935-86e9-ed177e08f8c8.
  Direct stream: https://www.svtplay.se/video/K1q1YAk
- 13:50–16:20, Schweiz – Sverige, SVT2 and SVT Play.
  Activity df1e1ad2-ae6f-4607-9407-d3c915637d48.
  Broadcasts 0e777607-5484-f738-aa07-cf9ea765f26d and
  02c364ac-432f-4c39-a9c3-c727970d9a4b.
  Direct stream: https://www.svtplay.se/video/j3dwkYd
- 18:20–20:50, Sverige – Litauen, Kunskapskanalen and SVT Play.
  Activity a0e19518-b750-4fe5-97b3-e032fa3d637d.
  Broadcasts 3bfc5a1c-fae7-5137-ac30-3f6e54510816 and
  8a48cc0a-51cf-1e3b-8d70-605f4366582e.
  Direct stream: https://www.svtplay.se/video/jgWY31z
- 18:40–20:45, Paris FC – BK Häcken, TV4 Play.
  Activity d160186b-38e0-48c6-8953-878e80f61208.
  Broadcast 066f28c7-70c3-7233-a169-8c9bc10738ad.
  Direct stream: https://www.tv4play.se/program/3ae301de979ec7fc0909/paris-fc-bk-hacken

Reused Rally activity ba3a39b2-b425-449e-bff9-9d73db8f18ce for 2026-10-23 and linked
broadcast a87b78b5-0c58-113e-ad9f-533942f3ed2c. Its TV4 Play source was changed from a
wrapper to the direct event page:
https://www.tv4play.se/program/64e53f19af3f188979bf/rally-em-portugal
All eight linked broadcasts have the activity organization, processed_at set, and hidden_at set.

Evidence saved: SVT and Swedish Curling Federation schedule/roster evidence; UEFA squad evidence;
BK Häcken fixtures and TV.nu match schedules; FPAK schedule and entry-list evidence; and direct
StreamLink sources. The four new direct streams returned HTTP 200. TV4 redirects resolved to
canonical event URLs. SVT2, Kunskapskanalen, SVT Play, and TV4 Play resolve through the existing
channel catalog or the activity's direct stream.

The three curling activities link five women from Lag Wranå or four men from Lag Eriksson.
The Häcken activity links 18 players from the official UEFA squad. Public cards show ages,
formative clubs, and profile links for these participants. Existing tier_0 priority and
ParticipantStarEvidence were checked; no star change was supported.

## Dashboard corrections

Added seven sourced participant start times, stored as ParticipantStartEvidence and displayed on
the hosted activity cards. Times below are local:

- Butterfield Bermuda Championship, round 2, activity
  86841011-0000-4000-8000-000000000111: Henrik Norlander 13:08, Pontus Nyholm 14:03,
  David Lingmerth 17:16, Jesper Svensson 18:22.
- Stifel Charity Classic, round 1, activity 0f152353-07b0-4d06-a0f4-9ac6be978034:
  Robert Karlsson 17:36, Freddie Jacobson 18:00, Henrik Stenson 18:12.
- Sources are the official PGA TOUR round 2 and PGA TOUR Champions round 1 tee sheets.

The Dashboard now has 10 activity issue rows. Nine are rally activities with the same missing
birthdate issue for Adam Grahn and Christoffer Bäck:

- 2026-10-23, Sträcka 1: ba3a39b2-b425-449e-bff9-9d73db8f18ce.
- 2026-10-24, Sträcka 2: 4b3ebbf0-be1f-47d4-b812-4658840e65c3.
- 2026-10-24, Sträcka 6: 8d782a62-ffac-4c73-bbc6-5e27138797cd.
- 2026-10-24, Sträckor 3–5: 3c662c26-8568-4d87-8fb6-ef4f5dffe7ae.
- 2026-10-24, Sträckor 7–9: 444ca6ab-8607-47a3-b224-77c3f8e8b9c0.
- 2026-10-25, Live-sträcka 1: 34384aa2-c173-4ca1-8c26-50ee17535968.
- 2026-10-25, Live-sträcka 2: 63678c84-e9f8-4a42-a129-c2a8a403cacf.
- 2026-10-25, Sträckor 10–12: 4add2d69-c442-4964-a352-8fa29c16ad93.
- 2026-10-25, Sträckor 14–16: 56112cac-8b22-4273-aed9-9c4388449184.

Their formative clubs remain linked. Sources support only birth years, not exact dates, so false
2005-01-01 and 1990-01-01 birthdates were cleared and the public ages are blank. PersonFacts
evidence records the correction. Exact birthdays remain unresolved.

The remaining Dashboard issue is Genesis Championship – Runda 3, activity
086c5d6e-07e3-4ac6-b567-efe2be952ed4, on 2026-10-24. Third-round tee groups were not published
at the run snapshot; official tee-time groups are set after the cut. No start times were guessed.

The existing rally activity remains 19:45–21:05. Listings conflict: TV.nu gives a 19:45 start
and a 65-minute episode (20:50 end), the TV4 Motor listing spans 19:45–21:30, and the linked
broadcast range is 20:00–21:10. The activity time remains unresolved. TV.nu and TV4 schedule
evidence are saved with the activity.

## Hidden broadcasts

Changed only hidden_at for these 65 broadcasts. The 37 competition-feed exclusions have
ParticipationEvidence. Complete official entries showed no Swedish participants for the
international events; other exclusions follow the broadcast metadata and linked context.

Domestic TV-pucken (5), out of scope:

- 7cefcbab-6201-0030-8693-813b1c0b1cb2
- 11a88020-82f9-ec3a-9847-d8dd98bafd17
- ca1c0b67-8652-f93d-8f02-96282f21f230
- f85bdc84-e79a-c332-bb7f-85adee9ce98e
- 1ed061da-c615-5d36-b4dd-9deb90c53de2

Domestic SHL fixtures (3), out of scope:

- 1f4c1d7f-ba50-f839-a8e4-fd342135c18e
- 9adfdf19-5f26-9632-b947-289ffc955ef2
- a5506c97-af78-1735-9c88-49e325e1f6d3

Domestic Handbollsligan fixtures (3), out of scope:

- 2518a40b-857e-0f37-9e9f-97789a114ac3
- 6b89a3b1-7d10-1234-b52c-64eb8ba1d521
- f008fae8-443f-4a36-9233-2fbdd82ce6f8

Studio, highlights, and recap programmes (5):

- 93903141-4519-bf39-af8e-70357587339c
- 4a7d9161-4ee5-dd39-be7c-2f9e7537f0a4
- c959e670-c44d-2d3b-927e-16dcbebb5181
- a748f82c-36a8-5334-ad10-f270a6bf07b7
- 95d28bab-be0c-ec31-a3a8-0de43c303a2a

Foreign fixtures with no Swedish club or national team (10):

- c4d5bc07-498b-2f36-b5b3-57ce9735c55a
- babc7bac-e39d-f639-b33b-dc82c322e839
- a97ab727-0e92-6430-b6e5-206dbfccf498
- 529a0dba-5245-d73a-b690-58ce4462efc6
- 872f3243-a508-043a-9904-322bf6d25235
- 1e54080a-c4e1-fd36-8a59-20d3fd475031
- c36f385e-a68d-353e-add9-cbdd31b27e61
- 3a14c043-b65f-4a36-b323-692943091893
- d8ef4c61-d9db-a935-9902-4be437ebf8c5
- 2b6b7ff3-d618-c634-8306-08d8e4ecf0dd

Unidentified broadcasts (2), with no event context beyond a generic title or date:

- 09105e78-0db7-a13a-9567-b7ecc2e749e1
- f109d96e-4347-c138-bec8-98e0135f2d5d

Formula 1 United States GP feeds (16), no Swedish driver in the FIA 2026 entry list:

- 6b0e004f-82cf-cf3d-ad82-1c0f01e1aedf
- 50565f42-f6dd-ad35-aceb-6a63766d5b0d
- 9ef23492-0fb6-c536-b93c-a3f28cdef3fe
- 62670b41-bd17-9231-9b85-20e82df85d82
- 09b8c2a3-1ede-523d-8f6e-c1175e2a5138
- f32af352-c869-6c35-8a82-3c1983fa41ab
- 46da01cf-aa8b-2e35-968c-620fa91fd6f6
- c48bf26f-43be-dc32-8cf7-6c0315260983
- fca04546-536c-8032-a94b-967beb159db6
- f52f87eb-e2a0-1c31-bc98-1722a9571ad5
- 43d67e3e-12f1-5631-882e-dc00e1ca3060
- 602f2867-5a6a-863e-941c-e540d378e753
- 15f02bd9-d4d0-093a-bca8-0901530109b4
- 95beeeb0-a199-db33-8ca0-9cd6f870e449
- 951cac33-daf7-c232-8bae-ec18f7adc053
- 2a691bce-02de-4c34-a356-fb5208878215

MotoGP Australia and Malaysia feeds (18), complete official grids had no Swedish riders:

- 3234cb5f-8700-8d34-a44b-e6201f922daa
- 293bd103-7df1-f336-8642-e748d32269cc
- 2d57ded2-4202-0f35-8c85-af32ef627165
- ced0d20e-c7ad-7639-a715-4ef92a20d787
- 259d7764-ed01-233f-beed-658fc9307127
- 2c0c58eb-0704-9f36-9fb1-eb1edf854523
- 0a479599-0010-2833-8899-de9816f8791c
- 749bf997-aa1b-593f-a63f-24d9961ef7d9
- 15eae269-ef70-5630-86eb-c1f27fcb9aea
- 40f55d3d-c475-8b36-85d3-03c7734fa5be
- 24e0dee0-6c44-d637-921f-e86989bd3f06
- 567b6ec2-9e5c-8530-a07a-2a70d2657338
- 9f945ce3-a4ab-ca3c-9158-98d42a6ec71e
- 9274cdf5-b99d-543c-9fd3-d06ae0f35770
- d056e142-380e-2733-9f5d-772566363523
- 4cff8f0d-2d36-163e-8709-f4be842bc757
- 1be94f34-9100-473e-91a9-bb27333f6c16
- 9b32c13c-75be-c635-9285-f243244c6959

European Darts Championship (1), official PDC Europe top 32 had no Swede:

- 981c93ce-879d-8631-80fb-e2feb57a2045

Wistron Ladies Open (2), complete TLPGA final field had no Swedish player:

- eab20bd0-21aa-6339-903f-6dbafe5f781f
- de40ccb8-8303-b439-b120-d836f58c1f09

Evidence sources for completed no-Swede checks:

- FIA 2026 entry list: https://www.fia.com/sites/default/files/final_2026_media_kit.pdf
- MotoGP rider lists: https://www.motogp.com/en/riders/motogp
- Moto2 rider list: https://www.motogp.com/en/riders/moto2
- Moto3 rider list: https://www.motogp.com/en/riders/moto3
- TLPGA Wistron final field: https://www.tlpga.org.tw/en/Games_players.php?gno=545
- PDC Europe 2026 European Championship standings are saved as ParticipationEvidence.

## Visible unresolved broadcasts

All 42 remain visible and unprocessed. Every row has its assigned organization and
ActivityEvidence. Sources include the official event pages below; complete participation lists or
an exact activity mapping were unavailable.

World Nineball Tour, Philippines Open (3): full draw requires live-scores login.

- 1a77e776-5794-f639-87f9-9317f591f37e
- 48eeb916-021a-4937-b7cc-c3e379f38778
- 87334024-ebaa-c136-875d-46f7e6f6461f
  Source: https://worldnineballtour.com/events/2026-philippines-open-pool-championship/

Golden Trail World Series, Muju (4): complete elite start fields were unavailable.

- 2ecd6d1e-1b5e-4c33-b96a-58c5b9ac8549
- 9523721b-fc14-0b31-aa07-f9204d856815
- 9f6d6a22-f7eb-e73d-a6a8-5adf31be4db3
- fb272e53-b3d2-bd35-8f98-712c68200ea9
  Source: https://goldentrailseries.com/race/muju-trail/

Cycling (5): Heerde lacks a complete entry list; Superprestige Overijse lacks full start lists.

- Heerderstrand: 144ad12a-d060-0b34-b462-110b32a42df3
- Overijse: 98a8ed58-106d-e43d-ad78-0d87e022e25d
- Overijse: 6a90d0ec-2618-613a-8360-fc1db48d0762
- Overijse: 82167e54-e1bf-3c32-82b6-58ebb697babe
- Overijse: 144a32fe-0843-6a39-9c72-7dda120c5a03
  Sources: https://www.visitheerde.com/evenementen/cyclocross-heerderstrand?occ=1919240027
  and https://www.superprestigecyclocross.be/en/calendar/2026-2027/overijse

Climbing, Santiago (3): no complete start list was available; the broadcast mapping was unclear.

- c50665d2-7277-5131-a379-95951de3a0b5
- 417bea28-3fc1-4631-a005-f26e858088f7
- 71f4494d-6c04-4335-a84f-094ec8bbcf99
  Source: https://www.worldclimbingsantiago.cl/la-copa/

Global Champions Tour, Cairo (1): full field unavailable.

- ecc663d7-98f1-0d39-8fca-18f71a9657ea
  Source: https://www.fei.org/events/2026_CI_1513/

ATP Vienna and Basel (16): 2026 draws were not published.

- 04912ec1-ae53-f731-8882-7015d500b361
- 081be24b-1fe1-fc34-ae47-47db6f56dbc1
- 0bfa6e79-21e1-e63b-a5a7-70e80c91bd8b
- 2a69e684-949c-ca38-84c8-d05694437e4d
- 491b4c64-21b7-d33b-b9ed-363e6c8eb23d
- 4ec34893-b29a-9639-ad05-4bb81c07b307
- 7c594318-bb24-b73d-a450-208ffbc6416c
- 8169f842-8be2-9a30-8450-367a3edf810e
- 8c14d0a7-9500-f739-8542-c73805d2c1ab
- 99c6fc7a-5b77-eb3c-9063-fa9b5cebc28c
- a2901efc-418d-e63a-9529-be92532a3d54
- b51a703f-b53f-ca32-a516-172286c93996
- b7ca1054-077b-3430-9e66-7fa649760eed
- c35b46f7-07d0-a934-b50c-aeb28f6bb51b
- d50be9d7-193b-5932-beef-5a0508a4fdba
- eaba3cbd-5cdd-7e38-8984-dac923fdf4a1
  Sources: https://www.atptour.com/en/tournaments/vienna-2/337/overview
  and https://www.atptour.com/en/tournaments/reinach/328/overview

Genesis Championship (1): broadcast is confirmed, but round 3 participant tee groups were not
available at the run snapshot.

- a0145beb-bdfe-6635-82ca-fd8a8ff6e49b
  Source: https://www.europeantour.com/dpworld-tour/genesis-championship-2026/tv-schedule

LPGA Maybank Championship (4): official field was incomplete and still changing.

- 0aea8cf6-568e-a93c-b52e-9df05a8fa52d
- 98d041b0-f9a9-3339-9515-48fceb3de7db
- ca4e3f29-17ca-0e3b-8007-5b8b7a497280
- d134b4c7-59e3-ba35-bdd5-c9b57a0e4e0b
  Source: https://www.lpga.com/tournaments/maybank-championship/entries

HotelPlanner Tour Grand Final (2): official entry page showed no names. Both feeds retain the
DP World Tour organization because no separate organization entity exists.

- a0c0b036-1141-1034-a15c-babc351b196f
- dec70ddc-3a10-5b3d-a010-73c6985307c7
  Source: https://www.europeantour.com/hotelplanner-tour/grand-final-2026/entry-list

PGA Tour Mexico Open (2): dates were confirmed, but the complete field was unavailable.

- 4fe1e8dc-be0c-053e-8ab9-0bd85cc2f0ca
- 52506a12-fbd6-7736-9b66-84762d0cea91
  Source: https://pgatourmedia.pgatourhq.com/tours/2026/pgatour/vidantaworldmexicoopen

Generic Ostrava curling listing (1): it cannot be mapped to one Sweden match and time segment.

- 4eaab9da-4d5b-1c35-a7da-ce9792411992
  Source: https://www.svt.se/sport/curling/guide-curling-em-2026

## Public verification and counts

Only https://sesport.se was used for public activity checks. All eight date pages returned HTTP
200. The three new Curling-EM rows and one Häcken row render as four separate cards on 2026-10-29.
The Rally card shows its direct TV4 Play link. All three hosted flag SVGs checked (se, eu, int)
returned HTTP 200.

| Public date | Before | After | Explanation |
| --- | --: | --: | --- |
| 2026-10-23 | 5 | 5 | Existing card count; tee times and Rally stream metadata changed. |
| 2026-10-24 | 8 | 8 | Existing cards; Genesis round 3 still lacks start times. |
| 2026-10-25 | 6 | 6 | No card-count change. |
| 2026-10-26 | 0 | 0 | No card-count change. |
| 2026-10-27 | 2 | 2 | No card-count change. |
| 2026-10-28 | 4 | 4 | No card-count change. |
| 2026-10-29 | 0 | 4 | Three curling matches and one Women's Champions League match. |
| 2026-10-30 | 0 | 0 | No card-count change. |

The remaining Dashboard items are the Genesis round 3 start times and the rally participants'
unknown exact birthdays. The Rally broadcast time range also conflicts with the saved activity
window. All other reviewed changes passed database readback and public-page verification.

Validation: git diff --check passed. No automated tests or builds were run, as directed.
