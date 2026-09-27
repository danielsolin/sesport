# Activity verification report - 2026-09-27

## Scope and public-view counts

- Scope: the public SportsDay view for 2026-09-27 in Europe/Stockholm.
- Public view: `https://sesport.se/?date=2026-09-27`.
- Step 1 count: 7 activity cards.
- Step 7 count: 5 activity cards after changes.
- The two-card decrease is from removing Fuji and Barber, which are out of scope.

## Changes applied

### 6 Hours of Fuji

- Unpublished all three grouped Fuji activities and removed Alexander West's
  participant links.
- West is Swedish but drives for British team Garage 59. Nationality alone does
  not qualify this international club activity.

### Barber Motorsports Park

- Unpublished the race activity and removed Hampus Eriksson's participant link.
- The complete event entry list places him in US team Random Vandals Racing.

### NW Arkansas Championship

- Changed the activity title to `NW Arkansas Championship: Slutrundan`.
- Set Ingrid Lindblad and Daniela Holmqvist inactive after the cut.
- Updated the final-round starts: Frida Kinhult 16:05, Linnea Ström 16:50,
  and Linn Grant 17:30 CEST.

### GCT Wien Grand Prix

- Changed both grouped activity titles to `GCT Wien: Grand Prix 1,60`.
- Removed Karoline Goldberg and Sofia Westborg from both rows. The GCT roster
  and complete Vienna entry list place them in CSI2*, not the CSI5* Grand Prix.
- Changed Karoline Goldberg's Watch Priority from `tier_0` to `tier_1`.
- The three remaining riders already had birthdates. Their club links were
  checked: the earliest records found support Mölndals Ridklubb for Antonia
  Pettersson Häggström and Söderköpings Ryttarsällskap for Henrik von Eckermann.
- Replaced Peder Fredricson's unsupported Uppsala Ponnyklubb link with a new
  `Flyinge Ryttarförening` Club entity (`Flyinge RF`). The Swedish Olympic
  Committee lists Flyinge RF in 1992, his earliest club affiliation found.

### VM Montreal

- Changed Jakob Söderqvist's Watch Priority from `tier_3` to `tier_0`.
- His fifth place in the 2026 senior World Championship time trial and the
  Swedish Cycling Federation's road-race roster support star status.

## Unresolved items

- **Orienterings-EM relay:** the 18 listed Swedish athletes are a squad that
  includes reserves. Eventor returned 403 for both complete relay start lists.
  The participant list remains unchanged pending verification.
- **Open de France final round:** official tee-time and leaderboard pages did
  not expose round-four data. The Sunday field and starts remain unverified.
- **Vienna Grand Prix:** TV.nu confirms a 20:05 broadcast start and FEI lists
  the 1.60 m Grand Prix at 20:15. No reviewed source confirms the 22:45 end.

## Evidence saved

### ActivityEvidence

- NW Arkansas schedule: `https://nwachampionship.com/tournament-information`
- Vienna TV.nu programme: `https://www.tv.nu/s/e_4954721_20260927`
- FEI Vienna schedule: `https://www.fei.org/events/2026_CI_0242`

### ParticipationEvidence

- FIA WEC Fuji entry-list notice:
  `https://www.fiawec.com/en/news/entry-list-released-for-6-hours-of-fuji/13795`
- FIA WEC provisional entry-list PDF, URL parts:
  `https://www.fiawec.com/umbrella_media/` +
  `2026-fia-wec-6-hours-of-fuji-provisional-entry-list-v1-` +
  `6aa7d62e17ed5145302128.pdf`
- Garage 59 car 58: `https://www.fiawec.com/en/car/2026/58`
- Barber entry list:
  `https://www.gt-world-challenge-america.com/entry-list/2026/barber-motorsports-park`
- Random Vandals Racing contact: `https://therandomvandals.com/contact/`
- NW Arkansas scoring: `https://nwachampionship.com/scoring`
- GCT Vienna rider roster:
  `https://www.gcglobalchampions.com/schedule/2026/vienna/riders`
- Vienna CSI5* and CSI2* entries:
  `https://www.hippobase.com/EventInfo/Entries/CompetitorHorse.aspx?EventID=805`
- Montreal road-race roster, URL parts:
  `https://scf.se/landsvag/` +
  `sverige-skickar-atta-cyklister-till-ett-mycket-intressant-vm-i-montreal/`
- Eventor men's relay list (403):
  `https://eventor.orienteering.sport/Events/StartList?eventClassId=17545&eventId=8774`
- Eventor women's relay list (403):
  `https://eventor.orienteering.sport/Events/StartList?eventClassId=17546&eventId=8774`
- Open de France leaderboard:
  `https://www.europeantour.com/dpworld-tour/fedex-open-de-france-2026/leaderboard`

### ParticipantStartEvidence

- NW Arkansas final-round pairings:
  `https://documents.directlync.com/501034/Final%20Round.pdf`
- Open de France tee times:
  `https://www.europeantour.com/dpworld-tour/fedex-open-de-france-2026/tee-times`

### ParticipantStarEvidence

- Jakob Söderqvist World Championship report:
  `https://www.svt.se/sport/cykel/pa-vag-mot-medalj-i-cykel-vm-da-vurpar-jakob-soderqvist`
- Jakob's Montreal road-race roster is the SCF URL listed above.
- Karoline Goldberg's 2025 results:
  `https://www.rimondo.com/en/tournament-details/133233/equestrian-tournament-kottingbrunn-2025`
- Karoline's Vienna entries are the Hippobase URL listed above.

### PersonFacts

- Antonia Pettersson Häggström birth date:
  `https://www.eniro.se/` +
  `antonia%2Bpettersson%2Bh%C3%A4ggstr%C3%B6m%2Bkungsbacka/118688531/person`
- Antonia with Mölndals RK at Falsterbo:
  `https://falsterbohorseshow.se/` +
  `antonia-pettersson-haggstrom-segrade-i-moutain-horse-ponny-grand-prix/`
- Henrik von Eckermann profile:
  `https://sok.se/idrottare/idrottare/h/henrik-von-eckermann.html`
- Peder Fredricson profile:
  `https://sok.se/idrottare/idrottare/p/peder-fredricson.html`
- Flyinge RF club information: `https://flyingerf.se/foreningen/klubbinfo/`
