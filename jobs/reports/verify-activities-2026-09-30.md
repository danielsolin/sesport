# Activity verification report - 2026-09-30

## Scope and public-view counts

- Scope: public SportsDay view for `2026-09-30`, Europe/Stockholm.
- Step 1 count: 4 activity cards.
- Step 7 count: 4 activity cards in the final public view.
- Counts match. No activity cards were added or removed.

## Approved changes applied

### Broadcast times and stream links

- `BK Häcken - Juventus`: changed `local_end_time` and `ends_at` from `20:50`
  to `20:45`. TV.nu lists the TV4 Play broadcast from 18:40 to 20:45;
  kickoff is 18:45.
- Replaced the TV4 Play redirect with the direct program page:
  `https://www.tv4play.se/program/eed7c0c776b1eb997c69/bk-hacken-juventus`
- Added the four TV.nu event pages below as `ActivityEvidence`.
- Added the fixed `Sjuan` mapping to the user-provided TV4 Play channels page:
  `https://www.tv4play.se/kanaler`
- No provider links remain unresolved.

### Participants

- `Sverige - Schweiz`: added Wilma Sundin and Ella Hellman from the official
  22-player federation roster.
- `Polen - Sverige`: added Bleon Kurtulus, Lukas Björklund, and Oscar Sjöstrand
  from the UEFA squad list.
- `BK Häcken - Juventus`: added Stella Söderbom, Lilly Mikaelsson, Svea Carlzon,
  Emma Lind, Livia Grou, and Erza Qela. Added Häcken's current UEFA squad page
  as `ParticipationEvidence`.
- `Rangers - Hammarby`: added Emma Holmgren, Alice Olsson, and Inga Ebba
  Kristina Hedlund. Removed Fanny Jurander and Hannah Sjödahl. Replaced the
  stale Hammarby Women's Champions League source with this match's UEFA
  Women's Europa Cup squad list.
- Created 14 Person entities and linked them to their activity organization and
  represented entity.
- Created the missing `IF Sundsvall Hockey` and `Gamla Upsala` Club entities to
  record verified formative or earliest documented club relationships.
- Verified birth dates and formative or earliest documented clubs:
  - Bleon Kurtulus: 2007-06-24; Halmstads BK.
  - Lukas Björklund: 2004-02-16; no earlier club verified.
  - Oscar Sjöstrand: 2004-11-08; Nacka FC.
  - Wilma Sundin: 2003-09-24; IF Sundsvall Hockey.
  - Ella Hellman: 2006-06-16; Hovås HC, earliest club found.
  - Stella Söderbom: 2007-04-11; no earlier club verified.
  - Lilly Mikaelsson: 2009-08-13; no earlier club verified.
  - Svea Carlzon: 2009-05-21; no earlier club verified.
  - Emma Lind: 2009-03-27; no earlier club verified.
  - Livia Grou: 2010-04-27; no earlier club verified.
  - Erza Qela: 2007-05-20; no earlier club verified.
  - Emma Holmgren: 1997-05-13; Gamla Upsala, earliest club in the source.
  - Alice Olsson: 2009-02-02; no earlier club verified.
  - Inga Ebba Kristina Hedlund: 2009-07-21; no earlier club verified.
- Left the formative Club unset where no earlier club is verified and recorded
  the data-quality exception below.

### Stars

- Changed Anna Anvegård and Jennifer Falk from `tier_2` to `tier_0`. Their Tokyo
  Olympic silver and current Sweden senior-team and UEFA squad status meet the
  star criteria. Saved the listed sources as `ParticipantStarEvidence` on each
  Person.

## Unresolved items

- Earlier clubs remain unverified for nine new Persons listed above. Their
  formative Club relationship is unset rather than inferred from a current
  team.

## Evidence saved

### ActivityEvidence

- Hockey broadcast: `https://www.tv.nu/s/s_4669784_20260930`
- U21 broadcast: `https://www.tv.nu/s/s_4685489_20260930`
- Women's Champions League broadcast: `https://www.tv.nu/s/s_4675495_20260930`
- Women's Europa Cup broadcast: `https://www.tv.nu/s/p_1620419_20260930`

### ParticipationEvidence

- Häcken's UEFA squad:
  `https://www.uefa.com/womenschampionsleague/clubs/2600867--hacken/squad/`
- Rangers-Hammarby UEFA Women's Europa Cup squad list:
  `https://www.uefa.com/womenseuropacup/match/2050527--rangers-vs-hammarby/lineups/`

### ParticipantStarEvidence

- Swedish Olympic Committee Paris media guide, Tokyo 2020 medal roster:
  `https://sok.se/download/18.66a35f951900c5bd64964658/1721143124617/2024_Mediaguide-Paris2024.pdf`
- SvFF's June 2026 senior-team guide and squad:
  `https://www.svenskfotboll.se/nyheter/landslag/2026/06/dam-matchguide-danmark-juni-2026/`
- Current UEFA Champions League squad:
  `https://www.uefa.com/womenschampionsleague/clubs/2600867--hacken/squad/`

### PersonFacts

- Bleon Kurtulus:
  `https://www.mff.se/lag/herr/spelare/bleon-kurtulus/`
- Oscar Sjöstrand:
  `https://www.mff.se/lag/herr/spelare/oscar-sjostrand/`
- Lukas Björklund:
  `https://www.uefa.com/under21/teams/players/250164109--lukas-bjorklund/`
- Wilma Sundin:
  `https://www.swehockey.se/media/ff4nn4ky/short_facts_dec_2019-1.pdf`
- Ella Hellman:
  `https://stats.swehockey.se/Teams/Info/TeamRoster/10398`
- Stella Söderbom:
  `https://www.zerozero.pt/jogador/stella-soderbom/3550623?epoca_id=155`
- Lilly Mikaelsson:
  `https://www.uefa.com/womenschampionsleague/clubs/players/250226334--lilly-mikaelsson/`
- Svea Carlzon:
  `https://www.uefa.com/womenschampionsleague/clubs/players/250226331--svea-carlzon/`
- Emma Lind:
  `https://www.uefa.com/womenschampionsleague/clubs/players/250226333--emma-lind/`
- Livia Grou:
  `https://www.uefa.com/womenschampionsleague/clubs/players/250226332--livia-grou/`
- Erza Qela:
  `https://www.uefa.com/womenschampionsleague/clubs/players/250217024--erza-qela/`
- Emma Holmgren:
  `https://www.levanteud.com/va/noticies/la-guardameta-emma-holmgren-nova-futbolista-del-levante-ud`
- Alice Olsson:
  `https://www.uefa.com/womenseuropacup/clubs/players/250208632--alice-olsson/`
- Inga Ebba Kristina Hedlund:
  `https://www.zerozero.pt/jogador/ebba-hedlund/3608973`
