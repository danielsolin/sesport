# Activity verification report: 2026-09-16

## Execution

- Scope: SESport SportsDay 2026-09-16.
- Public view: <https://sesport.se/?date=2026-09-16>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- The current `jobs/verify-activities.md` was reread before verification.
- Step 1 public view: 10 activity cards, 11 published activity rows, and
  32 visible participant entries.
- The Tour of Abruzzo card contains two channel-specific activity rows.
- The operator approved the recommended changes. They were applied in one
  manual PostgreSQL transaction.
- Thirteen new `ActivityEvidence` records were saved.
- The broadcast import command was not run.

## Applied changes

### Activity window

- Extended `Sverige-Slovakien`
  (`6d82a1c0-3b63-4bbf-a5af-88e288464ddf`) from 17:20 to 18:00.
  SVT2 ends at 17:20, while the continued SVT Play broadcast and the
  Kunskapskanalen segment run until 18:00.

### Participant cleanup

- Deleted `Grand Prix de Wallonie`
  (`303680b2-262f-4dbd-afd4-57ca03008cd3`) and its single-use activity group
  (`5df4f673-fbfc-4720-ba03-96867d29e9b9`). Current startlists from
  CyclingStartlist and Cyclingflash list neither Jacob Eriksson nor Jakob
  Söderqvist for today's race. Both Person entities and Jacob's unrelated Tour
  of Abruzzo participation were retained. Activity and group evidence sources
  were removed with the deleted records.

### Activity title and stream source

- Changed `Vatanspor - FC Nordsjaelland`
  (`c05b0743-c345-438e-91f2-45630c6a6333`) to
  `Vatanspor - FC Nordsjælland`, including the activity-group title and
  normalized slug. The club's official site uses the latter spelling.
- Deleted the activity-specific `Disney+` StreamLink source
  (`3c05241a-bff6-417c-a712-fa3a5a3773a0`) for
  `FC Barcelona - Racing Santander`. It is only the generic Disney+ homepage;
  retain the central fixed-channel catalog fallback.

## No change needed

- The Tour of Abruzzo window and description are correct. Jacob Eriksson is
  present in the official race entry list and remains in the race after stage
  1.
- The Grand Prix de Wallonie broadcast window and provider links were correct;
  the activity was deleted because its Swedish participant data was stale.
- The BMW PGA Championship: Celebrity Pro-Am entry for Alex Norén is
  supported by the official DP World Tour tee-time information. Keep the
  16:00-19:00 activity window: the linear Viaplay Sport broadcast ends at
  19:00 even though the Viaplay stream continues longer.
- All 14 Sweden participants in `Sverige-Slovakien` match the current SVT
  squad list. No start-time or star changes are needed.
- The eight hockey participants in `Oulun Kärpät - Ilves` match the current
  team information, and their birthdates and formative clubs are present.
- The football participant lists, broadcast windows, and provider links are
  otherwise supported by current fixture and squad sources.
- No detail activity has sufficiently precise participant-specific timing to
  create one today.
- No watch-priority or participant-star changes are recommended. Alex Norén's
  current top-level senior status remains the only `tier_0` entry today.

## Unresolved items

- Niklas Rubin's entity stores Finland as its country with the explicit
  `SwedesInOrganization` relevance kind. Current authoritative player data
  identifies him as Swedish, and the public participant row is correctly
  rendered with a Swedish flag. This internal classification was left
  untouched because it is the existing mechanism for including him here.

## Evidence saved

The following sources were saved as `ActivityEvidence` for the relevant
activities:

- <https://www.svt.se/sport/volleyboll/guide-herrarnas-volleyboll-em-2026>
- <https://www.svtplay.se/video/KGVdYdz/volleyboll-em/sverige-slovakien>
- <https://www.tv.nu/kanal/svt2/2026-09-16>
- <https://www.tv.nu/kanal/kunskapskanalen/2026-09-16>
- <https://cyclingstartlist.com/event/GrandPrixdeWallonie>
- <https://cyclingflash.com/race/grand-prix-de-wallonie-2026/startlist>
- <https://lottograndprixdewallonie.be/un-plateau-masculin-international-de-tres-haut-niveau/>
- <https://www.fcn.dk/>
- <https://www.tv.nu/sport/fotboll?datum=onsdag>
- <https://www.tv.nu/s/e_4938090_20260916?broadcastId=1X6QHM-7U3-kICC>
- <https://www.europeantour.com/dpworld-tour/bmw-pga-championship-2026/spectator-info/>
- <https://www.karpat.fi/joukkueet/liiga>
- <https://www.eliteprospects.com/player/44007/niklas-rubin>

Existing official participation and stream sources were reused where they
remain current. No new `ParticipantStartEvidence` or
`ParticipantStarEvidence` is required.

## Public card counts

- Step 1, initial public view: 10 activity cards, 11 published rows, and
  32 visible participant entries.
- Step 7, final cache-busted public view: 9 activity cards, 10 published
  rows, and 30 visible participant entries.
- Difference: one card and one activity row were removed with the stale
  Grand Prix de Wallonie activity; two participant entries disappeared with
  that activity.
