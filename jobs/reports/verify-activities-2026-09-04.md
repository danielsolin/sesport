# Activity verification report: 2026-09-04

## Execution

- Scope: SESport SportsDay 2026-09-04.
- Public view: <https://sesport.se/?date=2026-09-04>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 19 activity cards and 126 visible participant names.
- The 21 published activity rows render as 19 cards. The Czech Darts Open
  and Valkenswaard rows are grouped by activity.
- The operator approved all recommendations except the four detail
  activities in the Diamond League activity group.
- The approved changes were applied in one manual PostgreSQL transaction.
- 32 evidence source records were saved or refreshed. Three stale Stage One
  evidence records and their three linked obsolete facts were removed.
- A follow-up correction added the missing Tudor Pro Cycling Team relation
  for Jacob Eriksson in a second manual PostgreSQL transaction.
- The broadcast import command was not run.

## Applied changes

### Tour of Britain

- `Tour of Britain: Etapp 3`
  (`fe680253-873c-4863-81c3-e4c806e825b8`): changed the local broadcast
  start from 11:15 to 11:20. The 17:15 end was retained.
- Removed the three Stage One evidence records and their obsolete linked
  facts from the `Tour of Britain` activity group. Added current Stage Three
  route evidence.
- Added the existing Person entity `Jacob Eriksson` to the activity with
  the existing `UCI Pro Series` organization link.
- Created `Tudor Pro Cycling Team` as a Swiss cycling Team entity, linked
  Jacob Eriksson to it, and set it as his represented team on the activity.
  Jacob was not linked to Lucky Sport Cycling Team.
- Axel Källberg was not added because the reviewed start-list sources
  identify him as Finnish and outside the primary-country scope.

Evidence:

- TV.nu: <https://www.tv.nu/s/p_1610107_20260904>
- British Cycling Stage Three:
  <https://www.britishcycling.org.uk/tourofbritain/men/stagethree>
- East Riding route announcement:
  <https://www.eastriding.gov.uk/news/article/?entry=east_yorkshire_route_details_go_live_for_2026_lloyds_tour_of_britain_men>
- Stage Three start list:
  <https://www.ciclo21.com/3a-tour-gran-bretana-2026-directo-tv/>
- Tudor Pro Cycling roster:
  <https://www.tudorprocycling.com/pro-team>

### Omega European Masters

- `Omega European Masters: Dag 2`
  (`1913b0cb-23fa-4654-8e42-6ef48a9e7456`): added five active Round 2
  starters with the existing `DP World Tour` organization link.

  | Participant | Round 2 start |
  | --- | ---: |
  | Hugo Townsend | 09:10 |
  | Tobias Jonsson | 09:20 |
  | Anton Moström | 09:20 |
  | Albin Bergström | 14:05 |
  | Simon Forsström | 14:25 |

- Created the missing Person entity `Anton Moström` with sport `golf`,
  primary country Sweden, gender male, birth date 1995-03-11, and
  formative club `Örebro City Golf & Country Club`.
- Added the `DP World Tour` organization relationship for Anton Moström.
- Replaced the older tee-time source with the official Omega source and
  added the DP World Tour tee-time source. Existing seven Round 2 start
  times were retained.

Evidence:

- Official tee times:
  <https://www.omegaeuropeanmasters.com/tournament/tee-times>
- DP World Tour tee times:
  <https://www.europeantour.com/dpworld-tour/omega-european-masters-2026/tee-times>
- SGF player profile for Anton Moström:
  <https://golfdata.se/sgfranking/Players_statistics?PlayerID=53210>

### Star review

- Demoted `Dino Beganovic`
  (`b154fd9b-e5ab-498e-9499-88f8eb9772a9`) from `tier_0` to `tier_1`.
- Saved current FIA Formula 2 profile and standings as
  `ParticipantStarEvidence`. Current developmental F2 competition does not
  qualify for a star under the job rules without absolute senior-level
  evidence.

Evidence:

- FIA Formula 2 driver profile:
  <https://www.fiaformula2.com/en/drivers/dino-beganovic>
- FIA Formula 2 2026 standings:
  <https://www.fiaformula2.com/en/standings/2026/drivers>

### Stream links

- No StreamLink correction was applied. Existing activity-specific URLs are
  direct provider URLs without affiliate wrappers or tracking parameters,
  and returned HTTP 200 during verification. Fixed channel catalog links
  remain presentation fallbacks.

## Not applied by operator instruction

The following four published Diamond League detail activities were not
created:

- `Kula - final` for Fanny Roos.
- `Diskus - final (damer)` for Vanessa Kamga.
- `Stavhopp - final` for Armand Duplantis.
- `Diskus - final (herrar)` for Daniel Ståhl.

The SVT guide was saved as `ActivityEvidence` on the `Bryssel` activity
group for future use.

## Unresolved items

- `Ipswich Town - Liverpool FC` remains unresolved. TV.nu exposes two
  Viaplay entries, at 20:40 and 20:55. The activity uses the direct 20:55
  URL while its normalized Viaplay broadcast begins at 20:50. There is no
  unambiguous mapping between the two provider entries, so no stream or
  time change was applied.

Evidence:

- TV.nu Ipswich detail:
  <https://www.tv.nu/s/s_4609316_20260904>

## Activity evidence saved

The following 21 TV.nu detail pages were saved as `ActivityEvidence` for the
published activity rows:

- <https://www.tv.nu/s/p_1610107_20260904>
- <https://www.tv.nu/s/p_1610693_20260904>
- <https://www.tv.nu/s/p_1610961_20260904>
- <https://www.tv.nu/s/e_4922955_20260904>
- <https://www.tv.nu/s/p_1610765_20260904>
- <https://www.tv.nu/s/p_1610763_20260904>
- <https://www.tv.nu/s/e_4922969_20260904>
- <https://www.tv.nu/s/p_1612855_20260904>
- <https://www.tv.nu/s/p_1612858_20260904>
- <https://www.tv.nu/s/s_4657260_20260904>
- <https://www.tv.nu/s/s_4657263_20260904>
- <https://www.tv.nu/s/p_1612854_20260904>
- <https://www.tv.nu/s/e_4937894_20260904>
- <https://www.tv.nu/s/s_4657276_20260904>
- <https://www.tv.nu/s/s_4657275_20260904>
- <https://www.tv.nu/s/s_4657280_20260904>
- <https://www.tv.nu/s/s_4657282_20260904>
- <https://www.tv.nu/s/s_4638966_20260904>
- <https://www.tv.nu/s/p_1610221_20260904>
- <https://www.tv.nu/s/s_4601141_20260904>
- <https://www.tv.nu/s/s_4609316_20260904>

## Public card counts

- Step 1, initial public view: 19 activity cards and 126 visible participant
  names.
- Step 7, final cache-busted public view: 19 activity cards and 132 visible
  participant names.
- Difference in card count: none. The participant count increased by six
  from Jacob Eriksson and the five added Omega European Masters starters.
