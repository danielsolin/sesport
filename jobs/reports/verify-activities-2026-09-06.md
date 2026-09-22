# Activity verification report: 2026-09-06

## Execution

- Scope: SESport SportsDay 2026-09-06.
- Public view: <https://sesport.se/?date=2026-09-06>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 27 activity cards, 27 published activity rows, and
  113 visible participant names.
- No grouped card contains multiple activity rows today.
- The operator approved the changes except for expanding the Göppingen
  activity title. All approved changes were applied in one manual PostgreSQL
  transaction.
- Step 7 was a cache-busted public refetch after the changes.

## Applied changes

### Broadcast time

- `Tour of Britain: Etapp 5`
  (`6e0f6d1c-0a0d-4c41-a788-8af767bcc6b3`): changed the local start from
  11:45 to 11:50. The 16:45 end was retained.

The current TV.nu listing is 11:50–16:45. The current official stage page
was also saved as fresh participant evidence:

- <https://www.tv.nu/s/p_1610376_20260906>
- <https://www.britishcycling.org.uk/tourofbritain/men/stagefive>

The three currently linked Swedish participants were retained. No additional
Swedish participant was confirmed for this activity.

### Omega European Masters: Day 4 participants

Activity: `236a67ec-be80-454b-b61f-c3cc12d538c7`.

These linked participants were set inactive because they are not in the official
Round 4 tee-time list:

- Jens Dantorp
- Jesper Svensson
- Marcus Kinhult
- Mikael Lindberg

These participants remain active with the following local start times:

| Participant | Start |
| --- | ---: |
| Sebastian Söderberg | 07:54 |
| Niklas Lemke | 08:38 |
| Joakim Lagergren | 10:49 |

The activity remains 12:00–17:00. The linear V Sport Golf broadcast ends
at 17:00; the longer Viaplay slot is not used to extend the activity.

Evidence:

- <https://www.europeantour.com/dpworld-tour/omega-european-masters-2026/tee-times>
- <https://www.europeantour.com/api/sportdata/Teetimes/Event/2026134/Round/4>
- <https://www.tv.nu/s/e_4923989_20260906>

### Stream links

- Removed activity-specific `StreamLink` source
  `ce8203d1-b2f5-4a21-aa68-03a6f6dbf1b5` from `Valencia CF - FC Barcelona`
  (`e74dcd41-1d54-479d-a8bb-4277f608d845`). Its URL is only the generic
  <https://www.disneyplus.com/sv-se> homepage. Retain the central Disney+
  provider fallback.
- All other current activity stream links resolve and point to
  event-specific provider pages. No other stream change was needed.

### Activity title

- `FA Goppingen - THW Kiel`
  (`6db94774-d321-42bb-ac77-a70532a7f295`) was intentionally retained.
  The longer `FRISCH AUF!` form was not added because public content should
  stay concise for small screens in portrait orientation.

### Stars and detail activities

- No star change was needed.
- No detail activity had enough precise participant-specific information to
  create one today.

## Unresolved items

- `Czech Darts Open` has separate day and evening TV entries:
  12:55–17:30 and 19:00–23:30. They are currently represented by one
  activity/card, so the evening session is not represented correctly. This
  needs the activity/session grouping logic reviewed; it is not changed in
  this run.
- `HK Nitra - Växjö Lakers` is shown at 16:55, while one linked TV10 block
  begins at 14:55. The Viaplay and V Sport entries align with 16:55. No
  change is recommended without clarifying whether the TV10 block includes
  an earlier programme segment.

## Evidence saved

The existing current-day activities mostly still reference 2026-09-05
TV.nu pages. The following 30 current TV.nu pages were saved as
`ActivityEvidence` for the relevant activity or activity group:

- <https://www.tv.nu/s/e_4923648_20260906>
- <https://www.tv.nu/s/p_1610376_20260906>
- <https://www.tv.nu/s/e_4923989_20260906>
- <https://www.tv.nu/s/p_1611825_20260906>
- <https://www.tv.nu/s/p_1611940_20260906>
- <https://www.tv.nu/s/e_4924081_20260906>
- <https://www.tv.nu/s/s_4638968_20260906>
- <https://www.tv.nu/s/e_4923979_20260906>
- <https://www.tv.nu/s/p_1568162_20260906>
- <https://www.tv.nu/s/p_1611944_20260906>
- <https://www.tv.nu/s/s_4661128_20260906>
- <https://www.tv.nu/s/s_4638970_20260906>
- <https://www.tv.nu/s/s_4638971_20260906>
- <https://www.tv.nu/s/s_4661162_20260906>
- <https://www.tv.nu/s/s_4661130_20260906>
- <https://www.tv.nu/s/s_4661131_20260906>
- <https://www.tv.nu/s/s_4601144_20260906>
- <https://www.tv.nu/s/s_4638972_20260906>
- <https://www.tv.nu/s/s_4661133_20260906>
- <https://www.tv.nu/s/s_4660021_20260906>
- <https://www.tv.nu/s/s_4615654_20260906>
- <https://www.tv.nu/s/s_4661142_20260906>
- <https://www.tv.nu/s/s_4661141_20260906>
- <https://www.tv.nu/s/e_4926466_20260906>
- <https://www.tv.nu/s/s_4616900_20260906>
- <https://www.tv.nu/s/s_4609310_20260906>
- <https://www.tv.nu/s/s_4601137_20260906>
- <https://www.tv.nu/s/s_4660249_20260906>
- <https://www.tv.nu/s/p_1610384_20260906>
- <https://www.tv.nu/s/e_4923651_20260906>

The current British Cycling Stage Five page was saved as
`ParticipationEvidence`, and the official DP World Tour Round 4 API was
saved as `ParticipantStartEvidence`. Existing participation and star evidence
was otherwise reused. No new `ParticipantStarEvidence` was required.

## Public card counts

- Step 1, initial public view: 27 activity cards.
- Step 7, final cache-busted public view: 27 activity cards, 27 rows, and
  113 visible participant names.
- Difference: none. Inactive golf participants remain visible with the
  public `UTE` status, so the participant-name count is unchanged.
