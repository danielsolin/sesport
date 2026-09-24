# Activity verification report - 2026-09-24

## Scope and public-view counts

- Scope: the public SportsDay view for 2026-09-24 in Europe/Stockholm.
- Public view: `https://sesport.se/?date=2026-09-24`.
- Step 1 count: 4 activity cards. The `VM Montreal` card contains separate
  women's U23 and men's junior road-race slots.
- Step 7 count: 4 activity cards. The F2 card contains the main broadcast and
  both qualifying slots; the Montreal card still contains two race slots.

## Applied changes

### Azerbaijan Grand Prix: qualifying

- Changed the main broadcast start from 12:30 to 11:55; its 13:20 end remains.
- Added `Kval 1` from 12:00 to 12:20 and `Kval 2` from 12:30 to 12:50 in the
  existing group, with Dino Beganovic linked to both.
- Kept Dino Beganovic's Watch Priority at `tier_0`, as requested by the
  operator. His Madrid Feature Race win on 13 September makes him a medal
  candidate for this purpose.

### Open De France: Dag 1

- Extended the activity end from 18:30 to 19:30 to include the Viaplay
  broadcast; V Sport Golf and the official global feed end at 18:30.
- Removed Per Längfors and Sebastian Söderberg. Neither appears in the current
  official field or complete first-round tee sheet. The other eight Swedish
  participants have first-round tee times.

## Unresolved items

- None. Every displayed provider resolves through an activity-specific link or
  the central fixed-channel catalog.

## Evidence sources used

### ActivityEvidence

- TV.nu's F2 event page lists the qualifying broadcast from 11:55.
  - Host: `https://www.tv.nu`
  - Path: `/s/e_4944767_20260924?broadcastId=1X9GH4-1W-kKmj`
- FIA Formula 2 confirms both Baku qualifying session times.
  - Host: `https://www.fiaformula2.com`
  - Path part 1: `/en/latest/article/how-to-watch-round-12-in-baku-`
  - Path part 2: `tv-session-times-and-more.5pVVVALjr3uo9hiwPWcNv6`
- DP World Tour's official broadcast schedule lists the golf world feed.
  - Host: `https://www.europeantour.com`
  - Path: `/dpworld-tour/news/articles/detail/how-to-watch-the-dp-world-tour/`
- TV.nu's golf event page lists the V Sport Golf and Viaplay time windows.
  - Host: `https://www.tv.nu`
  - Path: `/s/e_4944758_20260924?broadcastId=1X9Ib0-2f-kKma`
- SVT's guide confirms the orienteering broadcast time and channels.
  - Host: `https://www.svt.se`
  - Path: `/sport/orientering/guide-varldscupen-i-orientering-2026`
- TV.nu's Eurosport 1 schedule confirms both Montreal broadcast windows.
  - Host: `https://www.tv.nu`
  - Path: `/kanal/eurosport-1/2026-09-24`
- TV.nu's men's cycling event page lists HBO Max and Eurosport 1.
  - Host: `https://www.tv.nu`
  - Path: `/s/e_4944734_20260924`
- UCI's final Montreal schedule confirms the race sessions and local time zone.
  - Host: `https://assets.ctfassets.net`
  - Path part 1: `/761l7gh5x5an/wmgNMqwdpy020sBstc8f1/`
  - Path part 2: `/dda3011dfa9b3194a75ceb3e80c4810d/`
  - Path part 3: `2026_RWC_SPORT_COMPETITION_SCHEDULE_FINAL_ENG_03_DEC_2025.pdf`

### ParticipationEvidence

- The Swedish Orienteering Federation's squad update confirms replacements in
  the current European Championship team.
  - Host: `https://via.tt.se`
  - Path: `/pressmeddelande/4557781/andringar-i-truppen-till-em?lang=sv&publisherId=3235453`
- DP World Tour's official tee-times page was checked against the golf list.
  - Host: `https://www.europeantour.com`
  - Path: `/dpworld-tour/fedex-open-de-france-2026/tee-times`
- The Swedish Cycling Federation confirms Thursday's Swedish starters by race.
  - Host: `https://scf.se`
  - Path: `/landsvag/svenska-laget-laddade-i-montreal-infor-linjeloppen-torsdag-sondag/`

### ParticipantStartEvidence

- GoAndPlay's first-round tee sheet confirms the saved start times for the eight
  remaining Swedish golfers.
  - Host: `https://goandplay.eu`
  - Path: `/en/2026-fedex-open-de-france-tee-times/`

### ParticipantStarEvidence

- FIA's driver profile and 2026 standings confirm Dino Beganovic's current F2
  status and rank.
  - Host: `https://www.fiaformula2.com`
  - Paths: `/en/drivers/dino-beganovic` and `/en/standings/2026/drivers`
- FIA's Madrid race report confirms Dino Beganovic's Feature Race win on
  13 September 2026; this supported the operator's Watch Priority exception.
  - Host: `https://www.fiaformula2.com`
  - Path part 1: `/en/latest/article/feature-race-dominant-beganovic-takes-lights-to-`
  - Path part 2: `flag-victory-in-madrid.1hqA4akOQzyJq8BbVfeS0S`
- The IOF confirms Tove Alexandersson's 2026 World Cup qualifying win; SVT
  reports her return to international orienteering and her 2025 World titles.
  - Host: `https://orienteering.sport`
  - Path: `/eoc/`
  - Host: `https://www.svt.se`
  - Path: `/sport/orientering/tove-alexandersson-lamnar-klartecken-till-em-bra-avstamp-till-vm`
- The PGA TOUR profile confirms Ludvig Åberg's current 2026 form and ranking.
  - Host: `https://www.pgatour.com`
  - Path: `/player/52955/ludvig-aberg/standings`

### StreamLink

- The existing direct Viaplay links for Formula 2 and golf were verified.
  - F2 host: `https://viaplay.se`
  - F2 path: `/sport/motorsport/formel-2/baku/s26091505200914232`
  - Golf host: `https://viaplay.se`
  - Golf path: `/sport/golf/dp-world-tour/open-de-france/s26091533511892087`
- The existing orienteering link is activity-specific.
  - Host: `https://www.svtplay.se`
  - Path: `/video/ed6oa13`
- Linear-channel fallback links are supplied by the fixed-channel catalog and
  were not duplicated.
