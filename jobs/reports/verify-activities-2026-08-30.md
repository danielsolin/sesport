# Activity verification report: 2026-08-30

## Execution

- Scope: SESport SportsDay 2026-08-30.
- Public view: <https://sesport.se/?date=2026-08-30>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 22 activity cards and 84 visible participant names.
- The operator approved the report. The approved recommendations were
  applied in one manual PostgreSQL transaction and verified afterward.
- A later refresh of the official DP World Tour data published the British
  Masters final-round pairings. That follow-up correction was applied in a
  second manual PostgreSQL transaction and verified afterward.
- The broadcast import command was not run.

## Applied broadcast-time correction

- `Val di Sole: Cross Country Olympic`
  (`d2072b33-870c-4edc-bdd4-95f0bae7125e`): changed the Eurosport 2 row's
  local end time from 14:40 to 14:35. The linked TV.nu broadcast and the
  current TV.nu detail page both end at 14:35.

Evidence:

- <https://www.tv.nu/s/e_4918031_20260830>

## Applied activity-broadcast relation correction

- `Road America: Race`
  (`0dad5d69-18d9-4f27-a331-cad569ce6993`): replaced the stale linked
  broadcast `p_1609562_20260830:stream:VIAPLAY:20260830174500`, which is
  19:45-00:20 in Swedish time, with the current
  `p_1609562_20260830:stream:VIAPLAY:20260830183000` record, which is
  20:30-00:55. The activity's displayed 20:30-00:55 time is already
  correct.

Evidence:

- <https://www.tv.nu/s/p_1609562_20260830>

## Applied stream-source cleanup

- `Celta de Vigo - Athletic Bilbao`
  (`89e93f42-d70d-4e66-812a-24b61dbae6cd`): removed the activity-specific
  generic Disney+ `StreamLink`
  (`8e2433d1-81ba-4a39-96b0-8c62bf45a5c3`). The central fixed-channel
  catalog already resolves Disney+ to the same generic provider URL, and
  the TV.nu detail does not expose an event-specific provider URL.

Evidence:

- <https://www.tv.nu/s/s_4615637_20260830>

## Applied participant corrections

- `Finnkampen: 200 meter`
  (`335e8ccc-0d0f-4428-979c-6d78349f7adb`): deleted the active
  `Henrik Larsson` link and added the existing Person entity
  `Zion Eriksson` (`47d131a0-036a-d114-b4ae-b06f62d2ce33`). Henrik
  withdrew after the original squad list was published; Zion replaced him
  in the 200 metres. Kept the original complete squad source for the rest
  of the event and saved the updated withdrawal/replacement article as
  participation evidence.

Evidence:

- <https://www.svt.se/sport/friidrott/andreas-almgren-och-samuel-pihlstrom-till-finnkampen>
- <https://www.svt.se/sport/friidrott/henrik-larsson-missar-finnkampen>

## Applied participant start-time updates

- `Women’s Irish Open: Dag 4`
  (`8933c73c-b375-4f23-bb42-e4b78701bb3d`): replaced the stale round-3
  start-time evidence with the official round-4 order of play and set the
  following Europe/Stockholm times for active participants. The source
  times are in Ireland local time and have been converted by +1 hour:
  Andrea Lignell 09:58, Moa Svedenskiold 09:58, Moa Folke 11:46,
  Lisa Pettersson 12:22, Louise Rydqvist 12:46, Corinne Viden 12:46,
  and Kajsa Arwefjäll 12:58. Caroline Hedwall and Louisa Carlbom remain
  inactive and receive no start time.
- `FM Championship: Dag 4` on both activity rows
  (`5281e3f4-a861-445a-8b30-7436c2e3b9b5` and
  `39252fc0-20b9-42e0-8946-ed5336f7c617`): set Anna Nordqvist to 15:04
  and Linnea Ström to 15:16 in Europe/Stockholm time, using the official
  round-4 pairings. The three inactive Swedish players receive no start
  time.
- `Tour Championship: Dag 4` on both activity rows
  (`1aa79ee0-2be0-4fc8-ab78-fc88b47dfc48` and
  `7d4f01e9-56e1-4cb6-b3db-8ac448f750f4`): set Ludvig Åberg to 19:38
  Europe/Stockholm time from the official PGA Tour round-4 tee times.
  The official PGA Tour source is now used for both rows instead of the
  secondary Golf.com source on the HBO Max row.

Evidence:

- <https://ocs-let.com/tic/tmdraw.cgi?tourn=2028~round=4~cardlink=Y~~season=2026~style=20~groupties=Y~pr=N~textout=N~bordersize=1~fontsize=M~winfocus=Y~>
- <https://www.lpga.com/tournaments/fm-championship/pairings>
- <https://www.pgatour.com/tournaments/2026/tour-championship/R2026060/tee-times>

## Applied star updates

Promoted these Person entities from `tier_1` to `tier_0` and saved the
listed evidence as `ParticipantStarEvidence`:

- Felix Rosenqvist
- Marcus Ericsson
- Albin Lagergren
- Daniel Pettersson
- Felix Claar
- Oscar Bergendahl

Rosenqvist won the 2026 Indianapolis 500, while Ericsson has a 2026
IndyCar win and both remain near the top of the current IndyCar standings.
The four Magdeburg players are current Swedish national-team players in a
defending EHF Champions League team, with meaningful Swedish media coverage.
The central source records are attached to the corresponding Person
entities, while the existing activity evidence remains unchanged.

Evidence:

- <https://www.indycar.com/news/2026/05/05-24-500-race-lead>
- <https://www.indycar.com/standings>
- <https://www.svt.se/sport/motorsport/marcus-ericsson-vinner-sitt-forsta-lopp-for-aret-efter-kritisk-omkorning>
- <https://ehfec.eurohandball.com/men/2026-27/news/en/magdeburgs-felix-claar-on-the-pressure-of-defending-the-champions-league-title/>
- <https://www.scm-handball.de/en/match-center/team/>
- <https://www.aftonbladet.se/sportbladet/a/167nVq/sanslosa-facit-for-svenskarna-i-magdeburg>

## Applied British Masters correction

- `British Masters: Dag 4`
  (`551c7e8e-902d-4eae-a41b-065a0738b808`): replaced the obsolete
  `/teetimes` source with the current `/tee-times` page. The official
  round-four data contains 24 matches, starts at 08:55 UK local time, and
  has no redraw of matches.
- Set Albin Bergström to 09:55 and Joakim Lagergren to 10:28 in
  Europe/Stockholm time. Marked Hugo Townsend, Jens Dantorp, Marcus Kinhult,
  Mikael Lindberg, Niklas Lemke, Sebastian Söderberg, and Tobias Jonsson
  inactive after the official leaderboard showed that they missed the cut.
  Per Längfors was already inactive.

Evidence:

- <https://www.europeantour.com/dpworld-tour/husqvarna-british-masters-hosted-by-sir-nick-faldo-2026/tee-times>
- <https://www.europeantour.com/api/sportdata/Teetimes/Event/2026133/Round/4>
- <https://www.europeantour.com/api/sportdata/Leaderboard/Strokeplay/2026133>

## Public card counts

- Step 1, initial public view: 22 activity cards and 84 visible participant
  names.
- Step 7, final cache-busted public view: 22 activity cards and
  `Svenskar: 77`. It shows Zion Eriksson in the 200 metres, Val di Sole
  ending at approximately 14:35, the approved participant start times, the
  six promoted stars, and only Albin Bergström and Joakim Lagergren active
  for British Masters day 4.
- Difference in card count: none. The participant count decreased by seven
  because the missed-cut players are no longer active for the final round.
