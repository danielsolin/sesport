# Activity verification report: 2026-09-09

## Execution

- Scope: SESport SportsDay 2026-09-09.
- Public view: <https://sesport.se/?date=2026-09-09>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 16 activity cards, 16 published activity rows, and
  46 visible participant names.
- No grouped card contains multiple activity rows today.
- The operator approved the recommendations. They were applied in one
  transaction.
- Step 7 final public refetch: 16 activity cards, 16 published activity
  rows, and 46 visible participant names.

## Applied changes

### Stream links

- Removed activity-specific `StreamLink` source
  `c74450c3-b196-451e-81b7-61331388cfa0` from `Moreirense FC - SL Benfica`
  (`3df49e46-47c7-4811-8312-df20459fb904`). Its TV4 Play URL redirects to
  the generic TV4 Play homepage instead of the event. The central TV4 Play
  channel fallback remains active.
- No other stream-link changes were needed. The remaining links resolve
  to provider-specific pages or use the central fixed-channel catalog.

### Participants, stars, and detail activities

- No participant-link changes were needed. Current official team-roster
  sources support the listed participants. Match lineups were not published
  at 06:41 CEST, so roster evidence was used as permitted by the job rules.
- No watch-priority changes were needed.
- No detail activity has enough precise participant-specific information to
  create one today.

## Unresolved items

- The TV.nu Championship league view labels `Derby - West Brom` as lacking
  broadcast information, while the general football schedule lists Viaplay
  from 20:35 and the provider-specific Viaplay URL resolves. The existing
  20:35–23:15 activity window is retained pending a clearer source.
- Official match lineups were unavailable at execution time for the evening
  football and hockey matches. The confirmed current rosters were retained;
  this should not be interpreted as a confirmed starting lineup.

## Evidence saved

The following current TV.nu event pages were saved as
`ActivityEvidence` for the relevant activity or activity group:

- <https://www.tv.nu/s/p_1611393_20260909>
- <https://www.tv.nu/s/p_1611887_20260909>
- <https://www.tv.nu/s/p_1613310_20260909>
- <https://www.tv.nu/s/p_1613312_20260909>
- <https://www.tv.nu/s/p_1613314_20260909>
- <https://www.tv.nu/s/p_1613315_20260909>
- <https://www.tv.nu/s/s_4662870_20260909>
- <https://www.tv.nu/s/s_4662872_20260909>
- <https://www.tv.nu/s/s_4664957_20260909>
- <https://www.tv.nu/s/p_1614198_20260909>
- <https://www.tv.nu/s/p_1613493_20260909>
- <https://www.tv.nu/s/s_4612370_20260909>
- <https://www.tv.nu/s/s_4664962_20260909>
- <https://www.tv.nu/s/s_4664965_20260909>
- <https://www.tv.nu/s/s_4664961_20260909>
- <https://www.tv.nu/s/s_4643488_20260909>

The current official roster sources already stored for the activities and
related activity groups were reused. The current official KooKoo roster and
QPR source were associated with the relevant current activity groups as
`ParticipationEvidence`:

- <https://www.kookoo.fi/joukkue/>
- <https://www.qpr.co.uk/news/2026/may/02/we-have-ambitious-mentality/>

No new `ParticipantStartEvidence` or `ParticipantStarEvidence` is required.

## Public card counts

- Step 1, initial public view: 16 activity cards.
- Step 7, final public view: 16 activity cards, 16 rows, and 46 visible
  participant names.
- Difference: none; only source evidence and the stale StreamLink changed.
