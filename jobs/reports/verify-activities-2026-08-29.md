# Activity verification report: 2026-08-29

## Execution

- Scope: SESport SportsDay 2026-08-29.
- Public view: <https://sesport.se/?date=2026-08-29>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 26 activity cards and 81 visible participant names.
- The operator approved the Broadcast times, Activity metadata,
  Activity-broadcast relations, and four participant sections. All approved
  changes were applied in manual PostgreSQL transactions.
- Nine broadcast-time rows were updated using five-minute rounding. The
  Derby V Sport Extra row already had the rounded value 13:25-15:25 and
  required no database change.
- ActivityEvidence, ParticipationEvidence, and ParticipantStartEvidence were
  saved for the approved changes. Twelve participant start-time result rows
  were updated.
- No changes were made to stars, StreamLinks, or detail activities.
- The broadcast import command was not run.
- No new detail activities were created or recommended.

## Broadcast-time corrections applied

### Broadcast times

- `Rally Paraguay: Sträcka 9-10` (`734f5cb8-c0c1-4dae-a251-84bb9e7f5dc8`):
  changed 13:15-14:45 to 13:00-14:30, matching the current TV4 Play
  stream.
- `Derby County - Swansea City`: changed the Viaplay activity row
  (`d2923334-ab03-49bd-a8b6-096d60d24008`) from 13:25-16:00 to
  13:20-16:00. The V Sport Extra row
  (`71652c2d-843e-450a-bbb0-42f500e8bd7c`) already matched the rounded
  13:25-15:25 value.
- `Wolverhampton Wanderers FC - Stoke City`
  (`d736e463-b76c-4dea-902d-387e10dd4fb2`): changed 13:25-15:30 to
  13:25-15:55.
- `Blackburn Rovers - Queens Park Rangers`, `Bristol City - Portsmouth
  FC`, `Cardiff City - Sheffield United FC`, and `Norwich City FC - Burnley
  FC`: changed the start from 15:55 to 15:50. The 18:30 end remains.
- `Sassuolo Calcio - Torino FC`
  (`3b1080df-3c21-4193-8ac7-4c20aa76f964`): changed the end from 21:00 to
  20:30.
- `Rally Paraguay: Sträcka 16-17`
  (`e4218b0a-edd2-41d7-a8a0-7dc9948b99fc`): changed the end from 00:15 to
  00:30.

Evidence:

- <https://www.tv.nu/s/e_4923547_20260829>
- <https://www.tv.nu/s/e_4923550_20260829>
- <https://www.tv.nu/s/s_4612331_20260829>
- <https://www.tv.nu/s/s_4612336_20260829>
- <https://www.tv.nu/s/s_4612326_20260829>
- <https://www.tv.nu/s/s_4612328_20260829>
- <https://www.tv.nu/s/s_4612329_20260829>
- <https://www.tv.nu/s/s_4612333_20260829>
- <https://www.tv.nu/s/s_4601136_20260829>

## Activity metadata changes applied

### Activity metadata

- `Coventry City - Hull City`
  (`b367cdb5-d3bb-41a7-9816-6bcdd102ab9e`): renamed the displayed channel
  from `V Sport Football` to `V Sport Premium`.
- Renamed `RB Leipzig - Boussia Mönchengladbach` to
  `RB Leipzig - Borussia Mönchengladbach`.

Evidence:

- <https://www.tv.nu/s/s_4609303_20260829>
- <https://www.tv.nu/s/s_4616890_20260829>

## Activity-broadcast relations applied

- `Rally Paraguay: Sträcka 13-14` was relinked from the stale rendered
  TV4 Play record `s/e_4923549_20260829:rendered:TV4-PLAY:20260829164500`,
  which covers only 18:45-18:55, to the current 19:15-21:15 TV4 Play record
  `e_4923549_20260829:stream:TV4-PLAY:20260829171500`.
- `FM Championship: Dag 3` was relinked from the stale V Sport Golf record
  `e_4917444_20260829:broadcast:V-SPORT-GOLF:20260829210000`, which starts
  at 23:00, to the 21:00-01:00 record
  `e_4917444_20260829:broadcast:V-SPORT-GOLF:20260829190000`.

Evidence:

- <https://www.tv.nu/s/e_4923549_20260829>
- <https://www.tv.nu/s/e_4917444_20260829>

## Participant corrections applied

### British Masters: Day 3

- Deleted the activity links for Alex Norén and Simon Forsström. The
  complete official entry list omits both players.
- Kept Per Längfors as a historical entry and set his activity link to
  inactive. The official entry list marks him below the cut, and he is absent
  from the current official leaderboard.
- Added the following Swedish local start times to the active participants:
  Albin Bergström 11:25, Hugo Townsend 09:45, Jens Dantorp 18:06,
  Joakim Lagergren 15:05, Marcus Kinhult 18:36, Mikael Lindberg 17:46,
  Niklas Lemke 11:15, Sebastian Söderberg 09:55, and Tobias Jonsson 11:35.

Evidence:

- <https://www.europeantour.com/api/sportdata/EntryList/TourId/1/Event/2026133>
- <https://www.europeantour.com/api/sportdata/Leaderboard/Strokeplay/2026133>

### FM Championship: Day 3

- Set Frida Kinhult, Ingrid Lindblad, and Linn Grant inactive. None appears
  in the complete official third-round pairings.
- Corrected Linnea Ström's start time from 19:35 to 19:25 Swedish time.

Evidence:

- <https://www.lpga.com/tournaments/fm-championship/pairings>

### Women's Irish Open: Day 3

- Set Caroline Hedwall and Louisa Carlbom inactive. Neither appears in the
  complete official third-round order of play.

Evidence:

- <https://ocs-let.com/tic/tmdraw.cgi?tourn=2028~round=3~cardlink=Y~~season=2026~style=20~groupties=Y~pr=N~textout=N~bordersize=1~fontsize=M~winfocus=Y~>

### The Ally Challenge: Day 2

- Deleted the activity link for Jesper Parnevik. The complete official
  current event field and tee-time list omit him.
- Added Henrik Stenson's start time as 17:45 and Robert Karlsson's as 18:30
  Swedish time.

Evidence:

- <https://www.pgatour.com/pgatour-champions/tournaments/2026/the-ally-challenge/S2026022/tee-times>
- <https://theallychallenge.com/tournament-info/field-and-pairings/>

## Stars, streams, and unresolved items

- No star changes were made or recommended. Henrik Stenson and Robert
  Karlsson remain non-star `tier_1` entities under the current-level rule;
  senior-tour participation alone does not justify a star.
- No StreamLink changes were made or recommended. All displayed channels
  resolved via a direct activity-specific provider URL or the central
  fixed-channel catalog, and the checked activity-specific URLs returned
  HTTP 200.
- No unresolved participant or stream item remains. The reported participant
  corrections are based on complete official lists or current official
  leaderboard/pairing data.

## Public card counts

- Step 1, initial public view: 26 activity cards and 81 visible participant
  names.
- Step 7, final public view: 26 activity cards and 78 visible participant
  names.
- Difference in card count: none. Three incorrectly listed participants were
  removed; eliminated participants remain visible with the `UTE` status.

## Approved next-day participant propagation

- The same verified status was applied to the corresponding activities for
  SportsDay 2026-08-30.
- Alex Norén and Simon Forsström were removed from `British Masters: Dag 4`.
- Per Längfors was set inactive on `British Masters: Dag 4`.
- Frida Kinhult, Ingrid Lindblad, and Linn Grant were set inactive on both
  `FM Championship: Dag 4` activity rows.
- Caroline Hedwall and Louisa Carlbom were set inactive on
  `Women’s Irish Open: Dag 4`.
- No `The Ally Challenge: Dag 3` activity exists yet, so no Ally change was
  possible.
- Five ParticipationEvidence sources were saved for the affected
  tomorrow activities.
- Targeted public check: 22 activity cards and 92 visible participant names
  before the propagation; 22 cards and 90 names afterwards. The two deleted
  participants disappeared, while inactive participants remain shown as
  `UTE`.
