# Activity verification report: 2026-09-12

## Execution

- Scope: SESport SportsDay 2026-09-12.
- Public view: <https://sesport.se/?date=2026-09-12>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- The current `jobs/verify-activities.md` was reread, including the rule
  that public content should stay concise on small portrait screens and that
  participant start-time links on `/Index` should be human-readable.
- Step 1 public view: 38 activity cards, 45 published activity rows, and
  126 visible participant names.
- Seven row reductions are expected from grouped cards: Hungaroring,
  Flanders Darts Trophy, Vojens GP, Rally Chile, and the two Volleyboll-EM
  channel variants.
- The operator approved the recommendations. All applicable changes were
  applied in one manual PostgreSQL transaction.
- Step 7 was a cache-busted public refetch after the changes.

## Applied changes

### WTT Champions Macao

The existing activity group is
`5b7b82f7-4ffd-40e7-bb1b-c9032978abdf`.

- Changed the group `end_date` to 2026-09-13.
- Created and published `Truls Möregårdh - Darko Jorgic`
  (`b28b9922-9866-4634-9d62-8a5a964fb57e`):
  - Date and time: 2026-09-12, 07:00-09:00 Europe/Stockholm.
  - Sport and type: table-tennis, Match.
  - Channel: SVT Play.
  - Participant: the existing Person entity `Truls Möregårdh`.
  - Organization: the existing `Table Tennis` entity.
  - Linked it to the 07:00 SVT Play broadcast
    `96b2b665-a182-e737-af19-3b6216ca5c55`.
  - Used the direct provider URL
    <https://www.svtplay.se/video/jGVVBm9> as `StreamLink`.
  - Used the concise description `Från Macao.`.
- Created and published `Anton Källberg - Alexis Lebrun`
  (`e73bec1a-23fc-4e11-a361-3e35711c7ae1`):
  - Date and time: 2026-09-12, 12:30-16:30 Europe/Stockholm.
  - Sport, type, channel, organization, and description as above.
  - Participant: the existing Person entity `Anton Källberg`.
  - Linked it to the 12:30 SVT Play broadcast
    `291a1a29-aeb6-fb3d-8161-121692f14f13`.
  - Used the direct provider URL
    <https://www.svtplay.se/video/jAMMprV> as `StreamLink`.
- Saved the SVT guide as activity evidence for the group and both new
  activities. The activity windows follow the supplied SVT guide;
  TV.nu currently has shorter generic rows, 07:00-09:30 and 12:30-14:00.

Source: <https://www.svt.se/sport/bordtennis/sa-sander-svt-bordtennis-2026>.

### Golf participant start times

- `Solheim Cup: Dag 2`
  (`3e2b6585-88bc-4b1a-a1f5-b54b1a6793d8`): corrected both Linn Grant's
  and Maja Stark's start time to 07:40. The displayed links use a
  human-readable Saturday pairings page, with the official LPGA pairings
  page saved as supporting evidence.
- `Amgen Irish Open: Dag 3`
  (`3d2435fb-56e7-4c7f-a85d-39cffcc49ff6`): saved the rounded displayed
  times below and used the human-readable Round 3 page for the links:

  | Participant | Official tee time | Displayed time |
  | --- | ---: | ---: |
  | Niklas Lemke | 12:07 | 12:05 |
  | Sebastian Söderberg | 12:07 | 12:05 |
  | Jens Dantorp | 13:41 | 13:40 |

- Set Joakim Lagergren and Mikael Lindberg to `is_active = false` on Amgen
  Day 3 and the published final-round activity. They missed the cut and
  remain linked, but are no longer shown as active participants.
- Saved the official Round 3 page as `ParticipantStartEvidence` and retained
  the raw API as supporting evidence. Do not add Round 4 times; the official
  final-round list is not published.

Sources:

- <https://www.europeantour.com/dpworld-tour/amgen-irish-open-2026/tee-times?round=3>
- <https://www.europeantour.com/api/sportdata/Teetimes/Event/2026135/Round/3>
- <https://www.lpga.com/tournaments/the-solheim-cup/pairings>
- <https://golf.com/news/solheim-cup-saturday-pairings-tee-times-tv-2026/?amp=1>

### Stream links and broadcast associations

- `Rally Chile: Live Stage 1`
  (`59712ffd-b7ac-46c0-9711-45802bcc8962`): the 16:00 TV4 Play and
  TV4 Sportkanalen broadcasts were retained, the two 22:00 broadcasts were
  moved to Stage 2, and the activity-specific TV4 Play URL was changed to
  `https://l.tv4play.se/ext-live/5c439454-05a8-4533-8d88-ca31a30ae962`.
  The URL above is the current TV4 Play link for Stage 1.
- `Rally Chile: Live Stage 2`
  (`590ef9e2-e151-4813-a295-d42237624d4b`): linked the two 22:00 broadcasts
  (TV12 and TV4 Play), and added the current TV4 Play URL
  `https://l.tv4play.se/ext-live/ac62e25d-6c64-4bff-80f4-05a04d2859cb`.
  The resulting channel list is TV12, TV4 Play.
- `Racing Santander - Deportivo Alaves`
  (`9631b436-6d9f-4559-8301-feb583d02a15`): removed the activity-specific
  Disney+ homepage `StreamLink`. The central Disney+ fallback remains; no
  event-specific provider URL was available.

### Participant representation and title

- In `SaiPa - HK Nitra`
  (`451e3791-2cf2-43ad-8ae6-bbf229a6b035`), changed Einar Emanuelsson's
  `represented_entity_id` from Frölunda HC to the existing SaiPa Team
  entity `6d3c96af-02fb-604a-c5c7-94a9373531b5`.
- Corrected the title `SC Freiburg - Boussia Mönchengladbach` to
  `SC Freiburg - Borussia Mönchengladbach`, including its slug.

## Unresolved items

- Sanford International: Day 2 has no published official Round 2 tee-time
  list. Freddie Jacobson, Henrik Stenson, and Robert Karlsson remain active,
  with start times blank until the official list is available.
- Amgen Irish Open: Day 3 now follows the listed Viaplay stream through
  21:30. The official linear DP World Tour TV schedule ends at 20:30, so
  this remains a documented linear-versus-stream source difference.
- WTT activity windows intentionally follow SVT rather than the shorter
  current TV.nu generic broadcast rows described above.

## Evidence saved

- SVT's WTT guide:
  <https://www.svt.se/sport/bordtennis/sa-sander-svt-bordtennis-2026>
- Golf start-time evidence:
  <https://www.lpga.com/tournaments/the-solheim-cup/pairings>,
  <https://golf.com/news/solheim-cup-saturday-pairings-tee-times-tv-2026/?amp=1>
- Amgen Round 3 start-time evidence:
  <https://www.europeantour.com/dpworld-tour/amgen-irish-open-2026/tee-times?round=3>
  and <https://www.europeantour.com/api/sportdata/Teetimes/Event/2026135/Round/3>
- TV.nu WTT details:
  <https://www.tv.nu/s/p_1611390_20260912> and
  <https://www.tv.nu/s/p_1611389_20260912>
- TV.nu Rally details:
  <https://www.tv.nu/s/e_4929222_20260912> and
  <https://www.tv.nu/s/e_4938439_20260912>
- Official Bundesliga fixture listing:
  <https://products.bundesliga.com/fixtures>

## Public card counts

- Step 1, initial public view: 38 activity cards, 45 published rows, and
  126 visible participant names.
- Step 7, final public view: 39 activity cards, 47 published rows, and
  128 visible participant names.
- Difference: one additional grouped WTT card and two additional published
  activity rows. The two new WTT participant rows account for the additional
  names; inactive golf participants remain rendered in the collapsed table.
