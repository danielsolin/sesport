# Activity verification report: 2026-09-17

## Execution

- Scope: SESport SportsDay 2026-09-17.
- Public view: `https://sesport.se/?date=2026-09-17`.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- The current `jobs/verify-activities.md` was reread before verification.
- Step 1 public view: 10 activity cards, 11 published activity rows, and
  47 visible participant entries.
- The Tour of Abruzzo card contains two channel-specific activity rows.
- The operator approved the recommendations. The changes were applied in one
  manual PostgreSQL transaction.
- Twenty-two new evidence sources were saved. Existing BMW and La Sella
  participant-start sources were refreshed.

## Applied changes

### Broadcast windows

- Extended `Tour of Abruzzo: Etapp 3` Eurosport activity
  (`c99cfbc5-ccb8-4971-a76c-612b2bed7fd0`) from 15:30 to 15:55.
  The current Eurosport broadcast is 13:35-15:55.
- Extended `La Sella Open: Dag 1`
  (`c3378448-b4aa-4dd7-9a35-93dbf1a6669f`) from 19:30 to 20:00.
  The current Viaplay broadcast is 16:00-20:00.

### BMW PGA Championship participants and start times

- Deleted the activity links for these five people. They have no
  Round 1 tee time in the complete official tee-time data:
  `Albin Bergström`, `Hugo Townsend`, `Per Längfors`, `Simon Forsström`, and
  `Tobias Jonsson`.
- Kept the other eight people and stored these Stockholm start times:

| Participant | Start |
| --- | ---: |
| Jens Dantorp | 08:20 |
| Joakim Lagergren | 09:20 |
| Mikael Lindberg | 09:30 |
| Niklas Lemke | 09:40 |
| Sebastian Söderberg | 09:40 |
| Marcus Kinhult | 09:50 |
| Alex Norén | 14:00 |
| Ludvig Åberg | 14:00 |

- The human-readable DP World Tour tee-time page is used for the rendered
  start-time links. The official tee-time API was retained as evidence only.

### Names and generic stream source

- Corrected the Vejle activity and group title to `Vejle BK - Brøndby IF`.
- Corrected the handball activity title to
  `FRISCH AUF! Göppingen - Rhein-Neckar Löwen` and the group title to
  `Göppingen - Rhein-Neckar Löwen`. Preserve the intentionally concise
  `FRISCH AUF!` form for portrait screens.
- Corrected the Málaga description to `Från La Rosaleda, Málaga.`
- Deleted the generic Disney+ homepage `StreamLink` from `Málaga CF - Villarreal
  CF`. The central Disney+ fixed-channel fallback remains.

## No change needed

- The official Giro d'Abruzzo entry list contains Jacob Eriksson, and the
  official Stage 3 page confirms the 17 September route and date.
- All seven listed Swedish La Sella Open participants are entered and appear
  in the official Round 1 draw. Their stored start times are:
  Corinne Viden 08:25, Louise Rydqvist 08:47, Andrea Lignell 08:58,
  Caroline Hedwall 13:10, Kajsa Arwefjäll 13:32, Moa Folke 13:32, and
  Lisa Pettersson 13:54.
- The current activity-specific stream links resolve to provider pages. The
  fixed catalog correctly covers Eurosport 1, V Sport Golf, and V Sport Extra.
- Keep the shorter linear windows for BMW PGA Championship and World Series
  of Darts Finals when their Viaplay streams run longer.
- No watch-priority or participant-star change is recommended.
- No detail activity has sufficiently precise participant-specific timing to
  create one today.

## Unresolved items

- None. The BMW discrepancy is handled using the official Round 1 tee-time
  data rather than the broader entry list.

## Evidence saved

- Current TV.nu detail pages were saved for all 13 broadcast rows, including the
  Eurosport, HBO Max, and Viaplay pages for the two affected golf/cycling
  windows.
- The official DP World Tour tee-time page and Round 1 API were saved as
  `ParticipantStartEvidence` for BMW PGA Championship.
- The official Il Giro d'Abruzzo entry-list PDF and Stage 3 page were saved as
  participation and activity evidence.
- The current OCS/LET Round 1 draw and entry list were retained as La Sella
  participation and participant-start evidence.
- Current official fixture/team pages used for the Brøndby, Göppingen,
  Rhein-Neckar Löwen, and Málaga corrections were saved as activity evidence.

## Public card counts

- Step 1, initial public view: 10 activity cards, 11 published rows, and
  47 visible participant entries.
- Step 7, final public refetch: 10 activity cards, 11 published rows, and
  42 visible participant entries.
- Difference: the card and activity-row counts are unchanged. Five BMW
  participant entries were removed.
