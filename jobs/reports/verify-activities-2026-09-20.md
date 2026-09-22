# Activity verification report: 2026-09-20

## Execution

- Scope: SESport SportsDay 2026-09-20.
- Public view: `https://sesport.se/?date=2026-09-20`.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 26 activity cards, 29 published activity rows, and
  68 visible participant entries.
- Zandvoort, SailGP, and World Series of Darts Finals each contain two grouped
  activity rows.
- The operator approved the report before database changes.
- All approved changes were applied in one PostgreSQL transaction.

## Applied changes

### Broadcast windows

- Change `BMW PGA Championship: Dag 4`
  (`72ce0480-0f6b-49fb-ad20-62135a878cb0`) from 13:00-18:30 to 13:00-19:35.
  The Viaplay stream runs until 19:35; V Sport Golf runs until 18:30.
  Source: [TV.nu Viaplay](https://www.tv.nu/s/p_1615788_20260920).
- Change `World Series of Darts Finals: Kvartsfinaler`
  (`5f272f44-c428-47fd-a22c-d6f7b64921f4`) from 13:00-17:00 to 13:00-17:30.
  Source: [TV.nu detail](https://www.tv.nu/s/p_1620382_20260920).
- Change `La Sella Open: Dag 4`
  (`f82a889a-9f36-4f30-b3a9-81f75a1af301`) from 14:00-17:30 to 15:00-18:30.
  Source: [TV.nu detail](https://www.tv.nu/s/p_1616837_20260920).
- Change `Manchester City FC - Sunderland AFC`
  (`9e620659-6ae6-476d-b488-c5401f83f86b`) from 14:50-17:30 to 14:50-18:00.
  Source: [TV.nu detail](https://www.tv.nu/s/s_4609336_20260920).
- Change `World Series of Darts Finals: Semifinaler & Final`
  (`f8ea3293-1964-4244-82ff-d5b197b5af0f`) from 19:00-23:00 to 19:00-23:30.
  Source: [TV.nu detail](https://www.tv.nu/s/e_4944407_20260920).
- Change `Slovakien Runt` (`8377e6b5-beac-4bab-aa74-ddd8a475c5f0`)
  from 12:30-13:20 to 12:30-13:25.
  Source: [TV.nu detail](https://www.tv.nu/s/e_4941669_20260920).
- Change `VM Montreal` (`9dae8bc1-b58a-4996-af6e-4048e374a803`)
  from 18:30-21:30 to 18:35-21:30.
  Source: [TV.nu detail](https://www.tv.nu/s/e_4952486_20260920).
- Change `Soldier Hollow: Cross Country Olympic`
  (`f2a89c96-8d42-4bd2-a2c5-263b1216f173`) from 21:30-23:15 to 21:15-23:15.
  Source: [TV.nu detail](https://www.tv.nu/s/e_4941672_20260920).

### Stream link

- Delete the `StreamLink` source
  (`116fee46-c2cd-4e0b-a6ec-5d76f73d5e53`) from `Getafe CF - Málaga CF`.
  It is the generic Disney+ homepage. Keep the central Disney+ channel mapping.
- No other activity-specific stream-link replacement is needed. The checked
  direct links returned HTTP 200 and matched their published providers.

### Participants

- Delete the `Alieu Njie` link
  (`c9dd033c-ff9d-47c2-b1bd-cc0dc8705ba8`) from `ACF Fiorentina - SSC Napoli`.
  He is represented by Torino FC and is not a Fiorentina or Napoli participant.
- Delete the `Oliver Söderström` and `Robin Knutsson` links
  (`2a6aa233-1832-43f4-bc4f-8d7cfff9478b` and
  `540c6916-6585-45c6-b242-93ffdf62bd05`) from `Zandvoort: Race 2`.
  Retain them on `Zandvoort: Sprintrace 2`; the former is GT4 and the latter is
  GT World Challenge.
- Mark `Viktor Tingström` inactive on
  `World Series of Darts Finals: Semifinaler & Final`
  (`f8ce5f19-aac9-467e-a0dc-4fef829a936d`). The initial verification treated him
  as eliminated in the quarterfinal and kept him active in that activity.

### BMW PGA Championship: Dag 4

- Set official Round 4 start times in Europe/Stockholm:

| Participant | Start |
| --- | ---: |
| Marcus Kinhult | 08:20 |
| Niklas Lemke | 11:10 |
| Joakim Lagergren | 11:30 |
| Alex Norén | 12:45 |
| Ludvig Åberg | 13:05 |

- Mark Jens Dantorp, Mikael Lindberg, and Sebastian Söderberg inactive after
  they were absent from the official Round 4 draw. Retain their participant links.

### Participants, stars, and detail activities

- No other participant, discipline, or represented-organization changes were
  applied.
- No watch-priority or star changes were applied.
- No detail activities are needed for precise participant timing.

## Evidence saved

- Saved 30 checked TV.nu pages as `ActivityEvidence` for the 29 published rows.
  BMW has two pages because its Viaplay and V Sport Golf listings are separate.
- Saved the BMW Round 4 API as `ParticipantStartEvidence`; retained the
  human-readable DP World Tour tee-time page for the public start-time link.
- Saved the official GT4 Zandvoort entry list and retained the GT World
  Challenge entry list as `ParticipationEvidence`.
- Saved the Sky Sports Darts result evidence supporting Viktor Tingström's
  inactive final-activity status.
- Retain the current official football, athletics, cycling, equestrian, hockey,
  and golf participation evidence.

## Unresolved items

- La Sella Open: Dag 4 has no published official Round 4 draw at verification
  time. Keep all seven current Swedish participants active without start times.
  Rerun when the official draw becomes available.

## Post-verification correction

- The initial verification removed `Alieu Njie` from `ACF Fiorentina - SSC Napoli`
  because the stored relationship represented Torino FC.
- After operator follow-up, restore the activity link with ACF Fiorentina as the
  represented team. Keep the existing Torino FC relationship as well.
- Add the official Torino FC transfer announcement and the current Lega Serie A
  Fiorentina squad as participation evidence.
- After a result review, deactivate `Viktor Tingström` in the quarterfinal
  activity as well. The result evidence shows that he lost in round one, so the
  link remains as inactive history rather than being deleted.
- Save the Sky Sports result page as participation evidence for the quarterfinal
  activity.
- The correction was applied in a separate PostgreSQL transaction after the
  initial report transaction.

## Public card counts

- Step 1, initial public view: 26 activity cards, 29 published rows, and
  68 visible participant entries.
- Step 7, final read-only refetch: 26 activity cards, 29 published rows, and
  67 visible participant entries.
- The card and row counts are unchanged. The participant count decreased by one
  because Alieu Njie was removed; Oliver Söderström and Robin Knutsson remain
  visible through the grouped Sprint 2 activity.
- Post-correction refetch: 26 activity cards, 29 published rows, and 68 visible
  participant entries. Alieu Njie is visible on the Fiorentina-Napoli activity.
- After the Viktor correction, the same counts remain; Viktor Tingström is shown
  with the public `UTE` marker in the quarterfinal activity.
