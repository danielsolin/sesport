# Activity verification report: 2026-09-01

## Execution

- Scope: SESport SportsDay 2026-09-01.
- Public view: <https://sesport.se/?date=2026-09-01>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 13 activity cards.
- The 13 published activity rows render as 13 cards. No grouped card was
  present in this scope.
- The public page, database rows, provider pages, official club schedules,
  and current squad sources were checked.
- The operator approved the report and requested that the Blackburn activity
  be deleted once its only Swedish participant was removed. The approved
  changes were applied manually in one PostgreSQL transaction.
- Evidence sources were saved with the corresponding activity or entity.
- The operator created `Torino FC - AC Monza` after the initial public-view
  count. That new activity was outside the verification change set and was
  left untouched.
- The broadcast import command was not run.

## Applied activity and broadcast corrections

### Liiga: six TV4 Play broadcasts

The current local window is 15:30-19:00 for all six activities. The
title-specific TV4 Play event pages expose a 16:30-23:30 local broadcast
window. The Finnish club schedules list the matches at 18:30 Finnish time,
which is 17:30 in Sweden; the earlier 16:30 value is the provider's
pre-match broadcast start.

For each activity below, change the local start from 15:30 to 16:30 and the
local end from 19:00 to 23:30. Keep the channel as `TV4 Play` and keep the
existing activity-specific stream link.

- `c63732d8-4174-4680-987a-2d18adc6355b` — Jukurit - HPK.
- `ea55c35a-4e2e-4aba-8398-8e14687cbaa9` — Kiekko-Espoo - KalPa.
- `3309e359-90fa-4798-939a-cb4768f4a311` — Lukko - Ilves.
- `5b8491b0-1767-4263-b980-24d566284f8b` — SaiPa - Tappara.
- `c337cd73-8007-4b43-a911-152489159c35` — Sport - Jokerit.
- `2b33688e-0fd1-472d-a8a3-8fccfb081dfa` — TPS - KooKoo.

Evidence:

- <https://www.tv4play.se/program/3fdc016e9dd943d3f8c4/jukurit-hpk>
- <https://www.tv4play.se/program/9ad3f8ed8ae204bda432/kiekko-espoo-kalpa>
- <https://www.tv4play.se/program/8ad2d209d6626ea095f2/lukko-ilves>
- <https://www.tv4play.se/program/7b2f766af10b2d00dfa4/saipa-tappara>
- <https://www.tv4play.se/program/afdfe5fec8c6b1eea67a/sport-jokerit>
- <https://www.tv4play.se/program/3a65176016609a297314/tps-kookoo>
- <https://hpk.fi/biorex-otteluennakko-jukurit-hpk-1-9-kausi-kayntiin-vieraissa/>
- <https://kiekko-espoo.fi/ottelut-ja-liput/>
- <https://www.ilves.com/uutinen/liiga-kauden-2026-2027-otteluohjelma-on-julkaistu/>
- <https://www.raumanlukko.fi/uutiset/tapahtumainfo-lukko-ilves-1-dot-9-klo-18-dot-30>
- <https://saipa.fi/fi-fi/article/uutiset/otteluohjelma-2026-2027/9361/>
- <https://vaasansport.fi/liiga-joukkue/>
- <https://hc.tps.fi/fi-fi/article/etusivu/joukkue-tilanne/1776/>

### Championship: six Viaplay broadcasts

The current channel names are correct. The provider and TV.nu schedules
show that Viaplay coverage begins at 20:35 for the first five matches and
at 20:50 for Stoke - Norwich. Keep all current end times.

- `ba1bd6e5-6d15-43b7-9a88-711fd704d158` — Lincoln - Blackburn:
  change 20:40 to 20:35; keep 23:15 and `Viaplay`.
- `b2cbf502-18cd-4dfb-b7fa-df83a2b5dcd4` — Portsmouth - Derby:
  change 20:40 to 20:35; keep 23:15 and `V Sport Football, Viaplay`.
- `ccaca094-88ce-44fb-9a91-e231b8706fd4` — Preston - Bristol City:
  change 20:40 to 20:35; keep 23:15 and `V Sport 1, Viaplay`.
- `60b1ca21-a4ee-4d36-85a6-b85b35b30c10` — Sheffield United - Bolton:
  change 20:40 to 20:35; keep 23:15 and `Viaplay`.
- `d744245e-8997-42fc-ae50-30eda6926ade` — Swansea - Watford:
  change 20:40 to 20:35; keep 23:15 and `Viaplay`.
- `431bbbe4-570b-4f87-a775-b3403d241e11` — Stoke - Norwich:
  change 20:55 to 20:50; keep 23:30 and
  `V Sport Premium, Viaplay`.

Evidence:

- <https://www.tv.nu/sport/fotboll/liga/championship?datum=tisdag>
- <https://viaplay.se/sport/fotboll/championship/lincoln-blackburn/s26082488065972841>
- <https://viaplay.se/sport/fotboll/championship/portsmouth-derby/s26082472772802039>
- <https://viaplay.se/sport/fotboll/championship/preston-bristol-city/s26082401575135450>
- <https://viaplay.se/sport/fotboll/championship/sheffield-united-bolton/s26082425442494229>
- <https://viaplay.se/sport/fotboll/championship/swansea-watford/s26082415596212730>
- <https://viaplay.se/sport/fotboll/championship/stoke-norwich/s26082401143818912>

### Lincoln - Blackburn activity removal and Axel Henriksson links

- Activity `ba1bd6e5-6d15-43b7-9a88-711fd704d158` was deleted because its
  only active Swedish participant was `Axel Henriksson`.
- The `Axel Henriksson` Person entity was retained.
- The existing `Axel Henriksson` -> `Blackburn Rovers` entity link was
  retained.
- A separate `Axel Henriksson` -> `Randers FC` Team link was added.

Evidence:

- <https://randersfc.dk/nyheder-superliga/2026/august/randers-fc-henter-axel-henriksson>

## Other review results

- The Parma - Cremonese activity is correct at 17:45-19:45 with
  `Sportbladet Plus`. The direct Aftonbladet TV page confirms a 17:45
  stream opening and an 18:00 kickoff. No correction is recommended.
  Evidence: <https://tv.aftonbladet.se/video/406269>
- No other participant deletion, addition, or represented-team correction
  is recommended for the originally verified activities. Their remaining
  participant links are supported by current club or league roster
  information. This includes the current 2026-27 Swansea retained list,
  which includes Melker Widell and Zeidane Inoussa.
- No StreamLink correction is recommended. Existing activity-specific links
  are direct provider URLs without TV.nu affiliate wrappers or tracking
  parameters; displayed fixed channels use the central catalog.
- No star changes are recommended. No participant in this scope currently
  has `watch_priority=tier_0`; the three current `tier_1` football players
  remain unchanged.
- No detail activities are recommended. The listed broadcasts do not expose
  deterministic person-specific segments.

## Unresolved items

- The Sportbladet Plus live page does not publish a reliable end time. The
  current 19:45 value remains the best duration estimate and should be
  rechecked if an exact schedule becomes available.
- Finnish-market Liiga material refers to MTV Katsomo, while the Swedish
  TV4 Play pages contain title-specific streams for all six listed matches.
  The Swedish provider evidence is sufficient to retain `TV4 Play`, but no
  broader rights conclusion is made here.
- Final starting lineups were not published at verification time. Current
  roster memberships were retained except for the confirmed Blackburn-to-
  Randers transfer above.

## Public card counts

- Step 1, initial public view: 13 activity cards.
- Step 7, final cache-busted public view: 13 activity cards.
- Difference in card count: none. The approved Lincoln - Blackburn card was
  deleted, and the operator created the Torino FC - AC Monza card after the
  initial count, leaving the total unchanged.
