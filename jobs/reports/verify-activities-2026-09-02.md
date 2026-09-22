# Activity verification report: 2026-09-02

## Execution

- Scope: SESport SportsDay 2026-09-02.
- Public view: <https://sesport.se/?date=2026-09-02>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 6 activity cards.
- The 8 published activity rows render as 6 cards because the QPR and
  Burnley broadcasts are grouped by activity.
- The public page, database rows, TV.nu listings, provider pages, official
  schedules, and current roster sources were checked.
- The operator approved the report and the changes were applied manually in
  one PostgreSQL transaction.
- Four evidence sources were saved with their corresponding activity and
  evidence kind.
- The broadcast import command was not run.

## Applied changes

### Tour of Britain: Etapp 1 broadcast window

Activity `561ce4f7-8440-4ee9-8c5b-a4c025229c99` was changed from a local
broadcast window of 11:55-16:30 to 11:20-17:00, including the corresponding
`starts_at` and `ends_at` values. `HBO Max` and the existing direct
activity-specific stream link were retained.

The TV.nu detail page lists the HBO Max broadcast from 11:20, and its
stream metadata ends at 17:00. The official Lincoln stage information also
places the stage start at approximately 10:30 UK time and the finish at
approximately 15:05-15:30 UK time, which is consistent with this coverage
window.

The following sources were saved as `ActivityEvidence`:

- <https://www.tv.nu/s/p_1609430_20260902>
- <https://www.lincoln.gov.uk/news/article/495/tour-of-britain-men-stage-one-everything-you-need-to-know>

### Italien-Sverige: remove injured participant

The activity `069bd452-eee8-461b-8d76-09164062ec86` no longer lists
`Gun Hindgren` (`661adfa1-7f0c-4446-9856-c85dacb88b20`). Her
activity-participant link was deleted. `Erica Muhizi` was retained as her
replacement. No new Person entity was required.

The Italian federation's current quarter-final preview lists the 14-player
Swedish roster without Hindgren and with Muhizi. CEV separately confirms
that Hindgren is out injured and Muhizi was called up.

The following sources were saved as `ParticipationEvidence`:

- <https://www.federvolley.it/eurovolley-2026-azzurre-verso-i-quarti-domani-italia-svezia>
- <https://www.cev.eu/articles/volleyball/injured-hindgren-out-of-eurovolley-erica-muhizi-called-up/>

## Unresolved items

- None. The remaining activity times, provider links, participant links,
  star status, and detail-activity selections remain supported by the
  sources checked.

## Public card counts

- Step 1, initial public view: 6 activity cards.
- Step 7, final public view: 6 activity cards.
- Difference in card count: none. The final public response shows 23 Swedish
  participants, including 14 in volleyball, after the approved removal.
