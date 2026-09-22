# Activity verification report: 2026-09-05

## Execution

- Scope: SESport SportsDay 2026-09-05.
- Public view: <https://sesport.se/?date=2026-09-05>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 45 activity cards, 46 published activity rows, and
  118 visible participant names.
- Czech Darts Open has two activity rows in one grouped card.
- Step 7 was a cache-busted read-only refetch. It also showed 45 activity
  cards, 46 rows, and 118 visible participant names.
- The operator approved the report. All approved changes were applied in one
  manual PostgreSQL transaction.
- 50 evidence source records were saved or refreshed.

## Applied changes

### Broadcast times

- `Tour of Britain: Etapp 4`
  (`2a928a8d-66dc-41a2-9571-3506e8764b31`): changed the local start from
  11:45 to 11:50. The 17:15 end was retained.
- `TSG Hoffenheim - Borussia Dortmund`
  (`db64d0fc-ece4-4022-bac2-dc2cc32d1b9e`): changed the local start from
  15:25 to 15:20. The 18:00 end was retained.

Evidence:

- <https://www.tv.nu/s/p_1610223_20260905>
- <https://www.tv.nu/s/s_4616898_20260905>

### Activity titles

- `Boussia Mönchengladbach - SV 07 Elversberg`
  (`bbffb255-407e-4cf1-9bf0-51ae587cab3a`): renamed to
  `Borussia Mönchengladbach - SV 07 Elversberg`.
- `Tps - Hifk`
  (`f15d3ab1-b0f5-4a17-8c78-c674f27cf51e`): renamed to `TPS - HIFK`.

Existing official Borussia and TPS evidence supports these corrections.

### Omega European Masters: Day 3 participants

Activity:
`e47da7cf-f748-49af-b430-38a862700e1b`.

The following linked participants were set inactive because they are not in
the official Round 3 tee-time list:

- Jens Dantorp
- Jesper Svensson
- Marcus Kinhult
- Mikael Lindberg

These participants remain active with the following local Round 3 start times:

| Participant | Start |
| --- | ---: |
| Sebastian Söderberg | 09:25 |
| Niklas Lemke | 09:50 |
| Joakim Lagergren | 12:40 |

The four inactive golfers have no Round 3 start time because they did not
qualify for the round. No additional Swedish golfers should be added to this
activity.

Evidence:

https://www.europeantour.com/dpworld-tour/omega-european-masters-2026/tee-times

- <https://www.europeantour.com/api/sportdata/Teetimes/Event/2026134/Round/3>

### Stream links

- Removed the activity-specific `StreamLink` source
  `49e3b217-ca6a-4d42-9f55-06142b0e3201` from `Rayo Vallecano - Racing
  Santander` (`9a7e556f-b171-4ccf-88a8-91ffd046fa0a`). Its URL is only the
  generic Disney+ homepage. Retain the central Disney+ catalog fallback.

## Unresolved items

- No event-specific Disney+ URL could be verified for `Rayo Vallecano -
  Racing Santander`. The TV.nu provider link resolves to the generic
  provider homepage:
  <https://www.tv.nu/s/s_4615653_20260905>.

No other time, stream, participant, star, or detail-activity change was
needed. Existing organization relations are complete, including the current
Lucky Sport Cycling Team relations for the Tour of Britain participants.

## Evidence saved

The following 47 unique TV.nu event-detail pages were used for the broadcast
time and provider checks and saved as `ActivityEvidence` for the relevant
activity or activity group:

- <https://www.tv.nu/s/e_4923140_20260905>
- <https://www.tv.nu/s/e_4923341_20260905>
- <https://www.tv.nu/s/e_4923425_20260905>
- <https://www.tv.nu/s/e_4923436_20260905>
- <https://www.tv.nu/s/p_1610223_20260905>
- <https://www.tv.nu/s/p_1610226_20260905>
- <https://www.tv.nu/s/p_1610766_20260905>
- <https://www.tv.nu/s/p_1610970_20260905>
- <https://www.tv.nu/s/p_1611047_20260905>
- <https://www.tv.nu/s/p_1611052_20260905>
- <https://www.tv.nu/s/p_1611508_20260905>
- <https://www.tv.nu/s/p_1611513_20260905>
- <https://www.tv.nu/s/p_1612833_20260905>
- <https://www.tv.nu/s/p_1612835_20260905>
- <https://www.tv.nu/s/p_1612836_20260905>
- <https://www.tv.nu/s/p_1612837_20260905>
- <https://www.tv.nu/s/s_4601139_20260905>
- <https://www.tv.nu/s/s_4601142_20260905>
- <https://www.tv.nu/s/s_4601145_20260905>
- <https://www.tv.nu/s/s_4604523_20260905>
- <https://www.tv.nu/s/s_4609311_20260905>
- <https://www.tv.nu/s/s_4609312_20260905>
- <https://www.tv.nu/s/s_4609314_20260905>
- <https://www.tv.nu/s/s_4609315_20260905>
- <https://www.tv.nu/s/s_4609317_20260905>
- <https://www.tv.nu/s/s_4609318_20260905>
- <https://www.tv.nu/s/s_4609319_20260905>
- <https://www.tv.nu/s/s_4612351_20260905>
- <https://www.tv.nu/s/s_4612355_20260905>
- <https://www.tv.nu/s/s_4612356_20260905>
- <https://www.tv.nu/s/s_4612357_20260905>
- <https://www.tv.nu/s/s_4612358_20260905>
- <https://www.tv.nu/s/s_4612359_20260905>
- <https://www.tv.nu/s/s_4612361_20260905>
- <https://www.tv.nu/s/s_4615653_20260905>
- <https://www.tv.nu/s/s_4616898_20260905>
- <https://www.tv.nu/s/s_4616901_20260905>
- <https://www.tv.nu/s/s_4638967_20260905>
- <https://www.tv.nu/s/s_4657625_20260905>
- <https://www.tv.nu/s/s_4657638_20260905>
- <https://www.tv.nu/s/s_4658766_20260905>
- <https://www.tv.nu/s/s_4659115_20260905>
- <https://www.tv.nu/s/s_4659117_20260905>
- <https://www.tv.nu/s/s_4659140_20260905>
- <https://www.tv.nu/s/s_4659150_20260905>
- <https://www.tv.nu/s/s_4659151_20260905>
- <https://www.tv.nu/s/s_4659247_20260905>

The official DP World Tour page and its Round 3 API response were saved as
`ParticipantStartEvidence` on the Omega European Masters Day 3 activity. The
official Kärpät roster page used to validate Niklas Rubin was saved as
`ParticipationEvidence`:

- <https://www.karpat.fi/joukkueet/liiga>

Existing participation and star evidence was reused. No new
`ParticipantStarEvidence` is required.

## Public card counts

- Step 1, initial public view: 45 activity cards.
- Step 7, final cache-busted public view: 45 activity cards.
- Difference: none. The changes did not alter the number of cards.
