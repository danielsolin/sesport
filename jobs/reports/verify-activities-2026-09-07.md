# Activity verification report: 2026-09-07

## Execution

- Scope: SESport SportsDay 2026-09-07.
- Public view: <https://sesport.se/?date=2026-09-07>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 6 activity cards, 6 published activity rows, and
  18 visible participant names.
- No grouped card contains multiple activity rows today.
- The operator approved the changes. They were applied in one transaction.
- Step 7 final public view: 5 activity cards, 5 published activity rows, and
  17 visible participant names.

## Applied changes

### Broadcast time

- `Union Berlin - Frankfurt`
  (`c3f27ccd-3e73-41dd-a1bd-109c19fe8964`): changed the local start from
  17:50 to 17:55. Retain the 20:30 end.

The current TV.nu event page lists the Viaplay stream as 17:55–20:30. The
official match page confirms the 18:00 kickoff:

- <https://www.tv.nu/s/s_4661407_20260907>
- <https://www.fc-union-berlin.de/de-e/alle-neuigkeiten/union-frauen-spielen-zu-hause-gegen-sge-xioWkE>

No other activity time change is recommended. The other current TV.nu
stream windows match the published activity windows.

### Participants

- `FC Nantes - AS Nancy`
  (`8758f3a7-eb3d-4c4a-88b7-c7881c60e5ac`) had Patrik Carlgren as its only
  participant. Delete the activity, its single-activity group, and its
  activity-specific facts and sources.
- Keep the existing person-to-team relation between Patrik Carlgren and
  FC Nantes. No new club relation was added because current evidence lists
  him as without a club; add the next club relation when a new club is
  confirmed.

Evidence:

- <https://www.fcnantes.com/articles/article2809.php?num=49537>
- <https://rwe-portal.de/spieler/patrik-carlgren>

The remaining participant links are supported by current squad, call-up, or
official event evidence. In particular, Amar Fatah is included in Lecce's
current match squad for Cagliari–Lecce.

### Stream links

- Removed activity-specific `StreamLink` source
  `661e724e-a593-4b6e-a41f-866176d25399` from `Getafe CF - Celta de Vigo`
  (`616846dc-daf6-438f-bc8c-d7a221bfcee7`). Its URL is only the generic
  <https://www.disneyplus.com/sv-se> homepage. Retain the central Disney+
  provider fallback.
- All other current activity stream links resolve and point to
  event-specific provider pages. No other stream change is recommended.

### Stars and detail activities

- No star change is recommended.
- No detail activity has enough precise participant-specific information to
  create one today.

## Unresolved items

- Cagliari–Lecce and Udinese–Lazio have longer linear TV4 Sport Live 2
  programme blocks than their event-specific TV4 Play streams. The current
  activity windows follow the event-specific streams. No change is
  recommended without a policy decision for multi-window broadcasts.

## Evidence to save

The following four current TV.nu event-detail pages were saved as
`ActivityEvidence` for the relevant activity group:

- <https://www.tv.nu/s/s_4661407_20260907>
- <https://www.tv.nu/s/s_4601138_20260907>
- <https://www.tv.nu/s/s_4615651_20260907>
- <https://www.tv.nu/s/s_4601146_20260907>

The following current official sources were saved as
`ParticipationEvidence`:

- <https://otilloswimrun.com/news/startlist-otillo-swimrun-world-championship-2026/>
- <https://oo.uslecce.it/news/64757337606/i-giallorossi-convocati-per-cagliari-lecce-uslecce>
- <https://www.udinese.it/squadre/prima-squadra>

The current LaLiga match source was already present. The FC Nantes transfer
summary and the current club-status profile were saved as
`ParticipationEvidence` for Patrik Carlgren's entity:

- <https://www.fcnantes.com/articles/article2809.php?num=49537>
- <https://rwe-portal.de/spieler/patrik-carlgren>

The existing ÖTILLÖ, Union, and Eintracht evidence can otherwise be reused.
No new `ParticipantStartEvidence` or `ParticipantStarEvidence` is required.

## Public card counts

- Step 1, initial public view: 6 activity cards.
- Step 7, final cache-busted public view: 5 activity cards.
