# Activity verification report - 2026-09-22

## Scope and public-view counts

- Scope: the public SportsDay view for 2026-09-22 in Europe/Stockholm.
- Public view: `https://sesport.se/?date=2026-09-22`.
- Step 1 count: 1 activity card, `Inter - BK Häcken`.
- Step 7 count: 1 activity card, `Inter - BK Häcken`.
- No database changes were made. This report is pending operator approval.

## Recommended changes

None.

The published activity, its participant list, and its provider-specific stream link do not require
a confirmed correction.

## Unresolved items

- The current broadcast end time is 20:45. TV.nu confirms the TV4 Play broadcast starts at 18:40,
  but does not publish an end time on the event detail page. UEFA and BK Häcken confirm the 18:45
  match kickoff, which is not the broadcast start or end time. No safe replacement end time was
  found, so the current value is left unchanged.

## Evidence sources used

- `ActivityEvidence`: UEFA match facts. Existing activity-group evidence; confirms the match and
  Women's Champions League context.
  - Host: `https://www.uefa.com`
  - Path part 1:
    `/womenschampionsleague/news/02a9-219f2f47e5e7-899c4e129b54-1000--women-s-champions-league-`
  - Path part 2: `matchday-1-inter-vs-hacken-facts/`
- `ActivityEvidence`: UEFA 2026/27 fixture list. Confirms 22 September and the 18:45 kickoff.
  - Host: `https://www.uefa.com`
  - Path part 1:
    `/womenschampionsleague/news/02a9-2184a4a3a3d8-3d2bf80e5c0e-1000--women-s-champions-league-`
  - Path part 2: `2026-27-league-phase-fixtures-by/`
- `ActivityEvidence`: BK Häcken official fixture announcement. Confirms the opponent, date, and
  18:45 kickoff.
  - Host: `https://bkhacken.se`
  - Path: `/nyhet/inter-pa-bortaplan-inleder-bk-hackens-champions-league-resa`
- `ParticipationEvidence`: UEFA official match squad list. Its 14 Swedish players match the
  published participant list exactly.
  - Host: `https://www.uefa.com`
  - Path: `/womenschampionsleague/match/2050551--inter-vs-hacken/lineups/`
- `ActivityEvidence`: TV.nu event detail. Confirms the 18:40 TV4 Play broadcast listing and is
  the source used to review the unresolved end time.
  - Host: `https://www.tv.nu`
  - Path: `/s/p_1615120_20260922`
- `StreamLink`: TV.nu's provider link resolves to the direct TV4 Play URL already published for the
  activity. The tracking wrapper was not used as the stored link.
  - Host: `https://l.tv4play.se`
  - Path: `/ext-live/e09d9a79-45d9-4eba-8332-ac819985fa7f`
