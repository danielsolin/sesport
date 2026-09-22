# Activity verification report: 2026-09-11

## Execution

- Scope: SESport SportsDay 2026-09-11.
- Public view: <https://sesport.se/?date=2026-09-11>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- The current `jobs/verify-activities.md` was reread, including the rule
  that participant start-time links on `/Index` should be human-readable.
- Step 1 public view: 14 activity cards, 17 published activity rows, and
  57 visible participant names.
- The 17 rows become 14 cards through the intended grouping of Flanders
  Darts Trophy and Rally Chile, plus the incorrect Amgen merge described
  below.
- The operator approved the data changes and explicitly deferred the Amgen
  grouping bug. The approved changes were applied in one manual PostgreSQL
  transaction.
- Step 7 was a cache-busted public refetch after the changes.

## Applied changes

### Amgen Irish Open

Activity IDs:

- Day 1: `49fc4ecf-60af-44bd-beb6-53e60866da2a`
- Day 2, morning: `0533e39d-741d-46bb-b7e7-f8c146ee740c`
- Day 2, evening: `ce8166a2-da7a-44a4-919d-aa3e5308c70f`
- Day 3: `3d2435fb-56e7-4c7f-a85d-39cffcc49ff6`
- Final round: `2240a3ea-a2f1-4948-8c5f-4386c8d2e9de`

The following changes were applied:

- Moved the V Sport Golf broadcast (19:00–20:00) from the morning Day 2
  activity to the evening Day 2 activity. The activity channel names were
  synchronized as well.
- Added Niklas Lemke to all five published round activities. The current
  official entry list marks him above the cut, and the official first- and
  second-round tee-time lists confirm that he played both rounds.
- Added Niklas Lemke's first-round start time, 08:25, to Day 1.
- Added the following second-round start times to both Day 2 activities:

  | Participant | Start |
  | --- | ---: |
  | Sebastian Söderberg | 12:20 |
  | Niklas Lemke | 12:30 |
  | Mikael Lindberg | 14:55 |
  | Joakim Lagergren | 15:30 |
  | Jens Dantorp | 15:40 |

- Used the human-readable page
  <https://www.europeantour.com/dpworld-tour/amgen-irish-open-2026/tee-times?round=2>
  for the displayed Day 2 start-time links. The raw second-round API was
  saved as supporting evidence only. The existing human-readable Round 1
  page remains suitable for Day 1.
- Left Day 3 and final-round start times blank because their official tee-time
  lists were not published at execution time.

The two Day 2 rows currently have non-overlapping windows, 10:30–16:30 and
18:00–20:30, but they render as one card. `PublicActivityTimelineBuilder`
classifies them as invisible channel variants because they have the same
identity and participants but different channels. That check does not test
whether their time windows overlap. The grouping logic should require overlap
before hiding a row as a channel variant. No database workaround is included
in this report.

### Sanford International: Day 1

Activity: `e1197a14-c611-41d8-9f41-b7578ed488b3`.

Added the official first-round tee times, converted from America/Chicago to
Europe/Stockholm:

| Participant | Start |
| --- | ---: |
| Freddie Jacobson | 17:18 |
| Henrik Stenson | 17:39 |
| Robert Karlsson | 18:21 |

The human-readable PGA Tour page
<https://www.pgatour.com/pgatour-champions/tournaments/2026/sanford-international/S2026023/tee-times>
is now the Day 1 start-time source. The existing generic
`sanfordinternational.com` player-page source was removed from the current
start-time results. The activity's broadcast window, 20:00–23:00, remains
unchanged.

The 2024 Sanford final-round PDF is not used for the current tournament
verification. It is retained because it supports an existing historical
fact about Steve Stricker; removing it would leave that fact without its
source. The current 2026 PGA Tour information source is used for the new
start-time data.

### 1. FC Nürnberg - Hannover 96

Activity: `b2bd34ee-bf9c-4387-bdd7-aba3b66b5c0b`.

- Changed the local start from 18:20 to 18:25.
- Retained the 21:00 end.

The linked Viaplay stream begins at 18:25. The 18:30 time on the event page
is the match start, whereas this activity follows the broadcast window.

### Stars and detail activities

No star changes or detail activities were needed. The current participant and
star selections otherwise pass the verification checks.

## Unresolved items

- The Amgen Day 2 card requires a code change in the grouping logic described
  above so that the 16:30–18:00 gap is visible.
- Official Day 3 and final-round Amgen tee times were not published at
  execution time.
- Official match lineups were not available for the relevant team activities;
  the existing roster evidence was retained.

## Evidence saved or updated

The following official sources were saved for the Amgen changes:

- <https://www.europeantour.com/dpworld-tour/amgen-irish-open-2026/entry-list>
- <https://www.europeantour.com/api/sportdata/EntryList/TourId/1/Event/2026135>
- <https://www.europeantour.com/dpworld-tour/amgen-irish-open-2026/tee-times?round=1>
- <https://www.europeantour.com/dpworld-tour/amgen-irish-open-2026/tee-times?round=2>
- <https://www.europeantour.com/api/sportdata/Teetimes/Event/2026135/Round/1>
- <https://www.europeantour.com/api/sportdata/Teetimes/Event/2026135/Round/2>
- <https://www.europeantour.com/dpworld-tour/amgen-irish-open-2026/tv-schedule>

The following 21 current TV.nu detail pages were checked and stored as
`ActivityEvidence` for their associated activity groups:

- <https://www.tv.nu/s/e_4928233_20260911>
- <https://www.tv.nu/s/e_4929023_20260911>
- <https://www.tv.nu/s/e_4935208_20260911>
- <https://www.tv.nu/s/e_4935232_20260911>
- <https://www.tv.nu/s/e_4941242_20260911>
- <https://www.tv.nu/s/e_4941243_20260911>
- <https://www.tv.nu/s/e_4942570_20260911>
- <https://www.tv.nu/s/p_1612321_20260911>
- <https://www.tv.nu/s/p_1613014_20260911>
- <https://www.tv.nu/s/p_1613015_20260911>
- <https://www.tv.nu/s/p_1613336_20260911>
- <https://www.tv.nu/s/p_1613499_20260911>
- <https://www.tv.nu/s/p_1613539_20260911>
- <https://www.tv.nu/s/p_1613585_20260911>
- <https://www.tv.nu/s/p_1613768_20260911>
- <https://www.tv.nu/s/s_4601156_20260911>
- <https://www.tv.nu/s/s_4663931_20260911>
- <https://www.tv.nu/s/s_4664667_20260911>
- <https://www.tv.nu/s/s_4664684_20260911>
- <https://www.tv.nu/s/s_4664685_20260911>
- <https://www.tv.nu/s/s_4664690_20260911>

The following additional sources were saved or reused:

- <https://www.pgatour.com/pgatour-champions/tournaments/2026/sanford-international/S2026023/tee-times>
- <https://datencenter.dfb.de/global_referee_schedule>

## Public card counts

- Step 1, initial public view: 14 activity cards, 17 published rows, and
  57 visible participant names.
- Step 7, cache-busted public refetch: 14 activity cards, 17 published rows,
  and 58 visible participant names.
- Difference: one additional visible participant, Niklas Lemke. The card
  count remains 14 because the deferred grouping bug is unchanged.
