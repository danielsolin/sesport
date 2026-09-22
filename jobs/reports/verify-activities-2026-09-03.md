# Activity verification report: 2026-09-03

## Execution

- Scope: SESport SportsDay 2026-09-03.
- Public view: <https://sesport.se/?date=2026-09-03>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 11 activity cards.
- The 13 published activity rows render as 11 cards because the Omega
  European Masters and HC Plzen - Rögle BK broadcasts are grouped by
  activity.
- The approved changes were applied manually in one PostgreSQL transaction.
- 18 evidence records were saved or refreshed: 13 `ActivityEvidence`,
  3 `ParticipationEvidence`, and 2 `ParticipantStartEvidence` records.
- The broadcast import command was not run.

## Applied changes

### Primary-country participation exclusions

Deleted the following activity-participant links:

- `Felix Nilsson` from `HC Plzen - Rögle BK`: link IDs
  `3e2480cd-4bc5-486e-8343-e39db209d6b2` and
  `4983a03b-4d6b-4dfd-8516-c1a2893379c7`.
  They belong to activity IDs `a6572842-45ec-4d47-87da-972836655841` and
  `c6f7a913-d16d-48b1-ae70-7be8af394273`.
  The current Rögle roster places Felix Nilsson under the players who are
  leaving, not in the 2026-27 roster:
  <https://www.roglebk.se/lagbyggetherr>.
- `Ludvig Johnson` from `EC Graz 99Ers - HC Fribourg-Gotteron`: link ID
  `392a9983-85cb-47e5-8d31-61037e66e06b` on activity ID
  `91ccb05f-33b5-41ec-af6f-47170ca5c4cc`. He is still listed by the
  current Fribourg roster, so the Person entity remains. Its existing
  `RepresentsOtherCountry` status and reason `Represents Switzerland.`
  also remain; only the Swedish activity link was removed:
  <https://www.gotteron.ch/fr/Principale/Equipe-26-27>.

### Omega European Masters participant start times

Both channel variants had seven blank `start_time` result values. The
following Europe/Stockholm times were set for each golfer in both activities:

| Golfer | Round 1 start |
| --- | ---: |
| Jens Dantorp | 13:45 |
| Jesper Svensson | 07:30 |
| Joakim Lagergren | 12:55 |
| Marcus Kinhult | 14:25 |
| Mikael Lindberg | 07:50 |
| Niklas Lemke | 13:25 |
| Sebastian Söderberg | 14:05 |

This updated 14 participant start-time result rows in total, covering
activity IDs `39b40404-c680-495e-9ea9-d1e2c921f1df` and
`55ce6b71-5ee5-46a1-a8af-7d29d186179c`. A
`ParticipantStartEvidence` source for both activities was added using the
official tee-time page:
<https://www.omegaeuropeanmasters.com/tournament/tee-times>.

### Real Sociedad - Celta de Vigo stream source

Removed the activity-specific `StreamLink` source
`70926b60-ae62-4b71-9c0d-9600d3ae1f6c`, which points to the generic
`https://www.disneyplus.com/sv-se` homepage. TV.nu does not expose an
event-specific Disney+ URL for this match. The central Disney+ channel
mapping remains the presentation fallback. The missing event-specific URL
remains unresolved:
<https://www.tv.nu/s/s_4615675_20260903>.

## Unresolved items

- No event-specific Disney+ stream URL could be verified. The public
  activity can still use the existing central Disney+ mapping, but it is a
  generic provider homepage rather than a match-specific destination.

## Evidence

The following sources support the applied changes and were saved or refreshed
with the indicated kinds:

- `ActivityEvidence`: the relevant TV.nu event pages for the verified
  broadcast windows and providers:
  <https://www.tv.nu/s/p_1609912_20260903>,
  <https://www.tv.nu/s/p_1610474_20260903>,
  <https://www.tv.nu/s/e_4922307_20260903>,
  <https://www.tv.nu/s/p_1610476_20260903>,
  <https://www.tv.nu/s/s_4656068_20260903>,
  <https://www.tv.nu/s/s_4656067_20260903>,
  <https://www.tv.nu/s/s_4656074_20260903>,
  <https://www.tv.nu/s/s_4655925_20260903>,
  <https://www.tv.nu/s/s_4656077_20260903>,
  <https://www.tv.nu/s/s_4656078_20260903>,
  <https://www.tv.nu/s/s_4656106_20260903>,
  <https://www.tv.nu/s/p_1611349_20260903>,
  <https://www.tv.nu/s/s_4615675_20260903>.
- `ParticipationEvidence`: the current Rögle roster and current Fribourg
  roster listed above. Existing activity and participation evidence for the
  other activities was reused.
- `ParticipantStartEvidence`: the official Omega European Masters tee-time
  page listed above and the existing European Tour tee-time source:
  <https://www.europeantour.com/dpworld-tour/omega-european-masters-2026/tee-times>.
- No new `ParticipantStarEvidence` is required. The existing evidence for
  Albin Lagergren, Daniel Pettersson, Felix Claar, and Oscar Bergendahl
  remains current.

## Public card counts

- Step 1, initial public view: 11 activity cards and 52 visible participant
  names.
- Step 7, final public view: 11 activity cards and 50 visible participant
  names.
- Difference in card count: none. The participant count fell by two because
  Felix Nilsson's two channel links represented one visible person, and
  Ludvig Johnson had one visible link.
