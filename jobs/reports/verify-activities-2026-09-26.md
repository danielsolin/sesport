# Activity verification report - 2026-09-26

## Scope and public-view counts

- Scope: public SportsDay view for 2026-09-26 in Europe/Stockholm.
- Step 1 count: 11 activity cards.
- Step 7 count: 11 activity cards after the approved changes.
- The public view still shows 11 cards; the card count did not change.

## Approved changes applied

### NW Arkansas Championship: Dag 2

- Updated the five Round 2 start times, converted from CDT to CEST and rounded
  to the nearest five minutes:
  - Daniela Holmqvist: 19:15.
  - Linn Grant: 19:50.
  - Ingrid Lindblad: 20:20.
  - Frida Kinhult: 20:55.
  - Linnea Ström: 21:15.
- Linked each participant's start time to the official LPGA leaderboard.
- Removed the obsolete GolfPost start-time source.

### FedEx Open de France: Dag 3

- Deleted the participant links for Per Längfors and Sebastian Söderberg.
- Set Hugo Townsend and Jens Dantorp to `is_active=false` after they missed
  the cut.
- Saved the leaderboard, cut, complete score, and Round 2 tee-sheet sources as
  `ParticipationEvidence`.
- The Round 3 tee-time page remains saved as `ParticipantStartEvidence`; it did
  not expose the current round's player list.

### Speedway GP: Torun

- Renamed the two grouped activity slots to `Kval` and `Grand Prix`.
- Saved the WBD schedule and Swedish TV.nu race listing as
  `ActivityEvidence` for the activity group.

### Hoppning

- Corrected the title from `Hoppning 1, 50` to `Hoppning 1,50 m`.
- The FEI event schedule is saved as `ActivityEvidence`.

### Participant stars

- Changed Linn Grant and Peder Fredricson from `tier_0` to `tier_1`.
- Saved current LPGA and FEI profiles for Linn and Peder as
  `ParticipantStarEvidence`.
- Saved current evidence for Tove Alexandersson, Ludvig Åberg, and Henrik von
  Eckermann, who remain at `tier_0`.
- Saved the current-season source for Caroline Andersson; her priority remains
  unchanged while her status is unresolved.

## Unresolved items

- Exact Open de France Round 3 starts remain unresolved for Albin Bergström,
  Ludvig Åberg, Marcus Kinhult, Niklas Lemke, Simon Forsström, and Tobias
  Jonsson. The official tee-times page did not expose the current player list.
  No Round 2 times were reused.
- Barber Motorsports Park has an approximate public window of 23:35–01:25 CEST.
  The official GT America timetable lists Race 1 at 19:00 CEST, but no Swedish
  Saturday broadcast listing was found. The activity remains unchanged.
- IOF links to separate A, B, and C final start lists were inaccessible. The 16
  listed participants match the Swedish federation's squad; exact start slots
  remain unresolved, so no detail activities were added.
- Caroline Andersson's `tier_0` status remains uncertain. The Swedish Cycling
  Federation reports a 2026 fourth place in a C1.1 race and a 2025 World Tour
  runner-up finish; this does not resolve current-star status by itself.

## Evidence references

Sources used for the applied changes and unresolved items are saved under the
appropriate evidence types. Existing matching evidence was retained.

### ActivityEvidence

- WBD Poland Speedway schedule:
  Host: `https://prasa.wbdpoland.pl`
  Path: `/post/final-jakiego-jeszcze-nie-bylo-speedway-grand-prix-w-s`
- TV.nu Speedway race: `https://www.tv.nu/p/1XavY6-15-kKH9`
- FEI jumping schedule: `https://www.fei.org/events/2026_CI_0242`
- IOF middle distance day and broadcast schedule:
  `https://orienteering.sport/event/european-orienteering-championships-4/middle/`
- GT America Barber timetable: `https://www.gtamerica.us/event/112/barber-motorsports-park`
- TV.nu Barber Sunday listing: `https://www.tv.nu/s/p_1620316_20260927`

### ParticipationEvidence

- DP World Tour leaderboard:
  Host: `https://www.europeantour.com`
  Path: `/dpworld-tour/fedex-open-de-france-2026/leaderboard`
- DP World Tour Round 2 digest and cut line:
  Host: `https://www.europeantour.com`
  Path: `/dpworld-tour/news/articles/detail/fedex-open-de-france-day-two-digest-x0446/`
- Complete Round 2 score table:
  `https://major-cut.com/leaderboard?t=open-de-france-2026`
- Complete Round 2 tee sheet:
  `https://goandplay.eu/en/2026-fedex-open-de-france-tee-times/`
- Swedish EOC squad information:
  Host: `https://www.orientering.se`
  Path: `/utova-och-folj/nyheter/allt-du-behover-veta-infor-orienterings-em-i-litauen/`
- IOF Eventor middle distance start-list route:
  `https://eventor.orienteering.sport/Events/StartList?eventId=8773&groupBy=EventClass`

### ParticipantStartEvidence

- Official LPGA leaderboard for all five NW Arkansas start times:
  `https://www.lpga.com/tournaments/walmartnwarkansaschampionshippresentedbypg/leaderboard`
- Official DP World Tour tee-times page for unresolved Round 3 starts:
  `https://www.europeantour.com/dpworld-tour/fedex-open-de-france-2026/tee-times`

### ParticipantStarEvidence

- Linn Grant current LPGA profile:
  `https://www.lpga.com/athletes/linn-grant/101583/results`
- Peder Fredricson current FEI profile: `https://www.fei.org/athlete/10002504`
- Henrik von Eckermann current FEI profile: `https://www.fei.org/athlete/10001601`
- Ludvig Åberg 2026 PGA TOUR stats:
  `https://www.pgatour.com/player/52955/ludvig-aberg/stats`
- Tove Alexandersson's current IOF championship result:
  `https://orienteering.sport/eoc-alexanderssons-comeback-gold-fosser-defends-mens-title/`
- Caroline Andersson 2026 season note:
  Host: `https://scf.se`
  Path: `/landsvag/bra-sasongsstart-for-flera-svenska-landsvagscyklister-i-utlandskateam/`
