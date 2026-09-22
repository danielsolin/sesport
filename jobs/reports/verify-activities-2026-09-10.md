# Activity verification report: 2026-09-10

## Execution

- Scope: SESport SportsDay 2026-09-10.
- Public view: <https://sesport.se/?date=2026-09-10>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 11 activity cards, 11 published activity rows, and
  82 visible participant names.
- No grouped card contained multiple activity rows today.
- The approved WTT activity and participant-representation changes were
  applied.
- Step 7 final public refetch after the approved change: 12 activity cards,
  12 published activity rows, and 83 visible participant names.

## Applied changes

### WTT Champions Macao

- The existing activity group's `end_date` is 2026-09-13.
- Created and published one activity in the existing group:
  - Title: `Flavien Coton - Anton Källberg`.
  - Date and time: 2026-09-10, 12:30-16:30 Europe/Stockholm.
  - Sport and type: table-tennis, Match.
  - Description: `Åttondelsfinal från Macao.`
  - TV channel: `SVT Play`.
  - Participant: the existing Person entity `Anton Källberg`.
  - StreamLink: <https://www.svtplay.se/video/e3dd5Nr>.
  - Link the activity to the existing 12:30-16:30 SVT Play broadcast
    `b039550c-f4c1-7b33-9ca8-2512005eac66`.
- The official spelling `Coton` was used; the SVT article spells the surname
  `Cotton`.
- Activities were not created for `Tomokazu Harimoto - Anders Lind` or
  `Miwa Harimoto - Anna Hursey`; neither match has a Swedish participant.

### Participant representation

- In `SaiPa - HC Pardubice`
  (`7708abed-ecea-476f-8b65-930e1706a4b7`), changed Einar Emanuelsson's
  `represented_entity_id` from Frölunda HC to the existing SaiPa Team
  entity `6d3c96af-02fb-604a-c5c7-94a9373531b5`.
- Einar remains an active participant. The current SaiPa roster confirms
  that he belongs to SaiPa for this match.

### Evidence saved

The current TV.nu event pages were saved as `ActivityEvidence` for the
relevant activities. These pages were used to verify today's broadcast times
and providers:

- <https://www.tv.nu/s/e_4928231_20260910>
- <https://www.tv.nu/s/s_4663375_20260910>
- <https://www.tv.nu/s/s_4663376_20260910>
- <https://www.tv.nu/s/s_4663380_20260910>
- <https://www.tv.nu/s/s_4663387_20260910>
- <https://www.tv.nu/s/s_4663388_20260910>
- <https://www.tv.nu/s/s_4663418_20260910>
- <https://www.tv.nu/s/s_4663390_20260910>
- <https://www.tv.nu/s/s_4664972_20260910>
- <https://www.tv.nu/s/e_4942428_20260910>
- <https://www.tv.nu/s/s_4643486_20260910>

The following sources were saved for the new table-tennis activity:

- SVT's guide as `ParticipationEvidence`:
  <https://www.svt.se/sport/bordtennis/sa-sander-svt-bordtennis-2026>
- The official WTT event page as `ActivityEvidence`:
  <https://www.worldtabletennis.com/eventInfo?eventId=3248&selectedTab=Overview>
- The official WTT schedule as `ActivityEvidence`:
  <https://wtt-web-frontdoor-cthahjeqhbh6aqe3.a01.azurefd.net/websitecacheddata/3248/schedule/schedule.json?q=2026090923>
- The SVT Play URL above as `StreamLink`.

For the Einar relation correction, the current SaiPa announcement was saved
as
`ParticipationEvidence`:

- <https://saipa.fi/fi-fi/article/uutiset/hyokkaaja-einar-emanuelsson-saipaan/9338/>

## Unresolved items

- None. The individual match time was intentionally omitted, and the
  activity uses the full 12:30-16:30 SVT Play broadcast window.

## Public card counts

- Step 1, initial public view: 11 activity cards.
- Step 7, final public view: 12 activity cards, 12 published activity rows,
  and 83 visible participant names.
- Difference: one new published table-tennis activity.
