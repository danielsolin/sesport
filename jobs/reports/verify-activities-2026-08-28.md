# Activity verification report: 2026-08-28

## Execution

- Scope: SESport SportsDay 2026-08-28.
- Public view: <https://sesport.se/?date=2026-08-28>
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 10 activity cards and 38 visible participant names.
- The database scope contains 13 published activity rows. The Ally
  Challenge row has calendar date 2026-08-29 but starts at 01:00 CEST and
  is therefore rendered on SportsDay 2026-08-28.
- The 13 rows belong to 10 activity groups, which render as 10 cards.
- The operator authorized the reported corrections. They were applied in
  one manual PostgreSQL transaction.
- The broadcast import command was not run.

## Applied changes

### Alliansloppet: Sprint

- Activity ID: `d52c2ab4-cd42-42da-9e70-67e1aa32d996`.
- Updated the public activity end from 18:00 to 19:00 on TV12.
- TV.nu and the official Alliansloppet broadcast information support a
  16:00-19:00 TV window.
- `local_end_time` is now `19:00` and `ends_at` is
  `2026-08-28 17:00:00+00`; the 16:00 start is unchanged.
- The five participant links were retained; all are present in the official
  sprint start list.

Evidence:

- <https://www.tv.nu/s/e_4926087_20260828>
- <https://www.alliansloppet.se/information/2026/trollhattan-action-week-sands-i-tv4-och-tv12-varldscupen-tillbaka-2026/>
- <https://www.alliansloppet.se/wp-content/uploads/2026/08/Startlista_Sprint_TAW_Friday.pdf>

### The Ally Challenge: Day 1

- Activity ID: `8035463d-d6ec-461c-95b4-7cb9cb961a71`.
- Henrik Stenson and Robert Karlsson were retained.
- Deleted the activity link for Jesper Parnevik. The current official event
  field, dated 2026-08-28, contains Stenson and Karlsson but not Parnevik.
  The current PGA TOUR Champions event field also omits Parnevik. This is
  a pre-event field correction, so the link should be deleted rather than
  marked inactive as an eliminated participant.

Evidence:

- <https://theallychallenge.com/tournament-info/field-and-pairings/>
- <https://www.pgatour.com/pgatour-champions/tournaments/2026/the-ally-challenge/S2026022/field>
- <https://www.tv.nu/s/p_1608169_20260828>

### Racing Santander - Elche CF

- Activity ID: `661396f6-60cc-495d-a61f-84e916021760`.
- Removed the activity-specific StreamLink
  `https://www.disneyplus.com/sv-se`.
- The URL is the same generic URL as the active central `Disney+` channel
  mapping. The `Disney+` channel remains on the activity and continues to
  resolve through the central mapping. The public page now renders exactly
  one Disney+ link, as intended.

Evidence: <https://www.tv.nu/s/s_4615642_20260828>

### Henrik Stenson entity records

- Merged the old Person entity
  `274fcbd9-a674-438c-9d3f-6bd7e7cb886a` into the surviving entity
  `82736cb0-e1e6-417d-88b7-4018f435d2bb`.
- All 12 historical activity links from the old entity were moved to the
  survivor, including active and inactive links. The survivor now has 19
  activity links in total, including both Ally Challenge days and the
  earlier senior tournaments.
- The merge also moved the two old participant start-time result rows and
  the PGA Tour Champions relationship. The identical image and duplicate
  English Wikipedia source were de-duplicated.
- Henrik Stenson meets the star criteria through major, Olympic, FedExCup,
  and Ryder Cup-level results with broad Swedish sports visibility. The
  surviving Person entity is now `tier_0`.
- Robert Karlsson was `tier_2`, but his official European Tour profile
  records 11 wins, an Order of Merit title, and two Ryder Cup appearances.
  His Person entity is now `tier_0` as well.

## Broadcast and evidence notes

- The Women’s Irish Open activity was already correct at 16:00-19:30. Its
  linked broadcast row `749467af-ac7b-fc35-b4a9-3c8706363922` was corrected
  from 15:00-18:30 to `2026-08-28 14:00:00+00` through
  `2026-08-28 17:30:00+00`.
- Evidence: <https://www.tv.nu/s/p_1608950_20260828>
- All 10 original activity-specific StreamLinks were checked and returned
  HTTP 200. After removing Racing–Elche’s duplicate generic row, 9 remain.
  The British Masters, Alliansloppet, and Eurosport 2 rows use valid central
  fixed-channel mappings and do not need duplicate links.
- Evidence was saved for the star review. The final SportsDay inventory
  contains 29 ActivityEvidence rows, 9 ParticipantStarEvidence rows, 6
  ParticipantStartEvidence rows, 27 ParticipationEvidence rows, and 9
  activity-specific StreamLink rows.
- Relevant star sources include the official Henrik Stenson profile:
  <https://www.pgatour.com/player/21528/Henrik-Stenson/career>, the official
  Robert Karlsson profile:
  <https://www.europeantour.com/players/robert-karlsson-4573/>, together with
  the official WRC, LPGA, PGA Tour, and Alliansloppet event sources recorded
  in the database.

## Participants, details, and unresolved items

- The final scope contains 42 participant links, all active Person links.
  All reviewed entities have birth dates and formative clubs. No other
  participant, representation, or organization correction was needed.
- No detail activities were created or recommended. Golf already exposes
  participant start times. The Alliansloppet qualification starts are
  outside the TV12 window, and later heat assignments are not sufficiently
  deterministic for person-specific detail activities. Rally coverage is
  already split into broadcast segments rather than deterministic
  participant start slots.
- No unresolved correction remains from this run. The requested restriction
  on creating detail activities was respected.

## Public card counts

- Step 1, initial public view: 10 activity cards and 38 visible participant
  names.
- Step 7, final public view: 10 activity cards and 37 visible participant
  names. Henrik and Robert render with the highest watch-priority badge,
  Jesper is absent, and Racing–Elche renders one Disney+ link.
- Difference in card count: none. The participant count decreased by one
  because Jesper Parnevik was removed.
