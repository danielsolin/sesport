# Activity verification report: 2026-08-31

## Execution

- Scope: SESport SportsDay 2026-08-31.
- Public view: <https://sesport.se/?date=2026-08-31>.
- Time zone: Europe/Stockholm, with the 04:01 SportsDay cutoff.
- Step 1 public view: 5 activity cards and 19 visible participant names.
- The six published activity rows render as five cards. The two Atalanta
  rows share one card.
- The operator approved the recommended changes. They were applied manually
  in one PostgreSQL transaction and verified in the database and public view.
- The production web service was restarted to reload the central channel
  catalog after the database update.
- The broadcast import command was not run.

## Recommended activity and broadcast corrections

### Atalanta BC - Bologna FC

- Activity `f0bfe0a7-93cb-4000-8196-6fd5c71f2473`: change the local end
  time from 23:15 to 23:00. Set `ends_at` to
  `2026-08-31 21:00:00+00`.
- Change the activity channel from `TV4 Sport Live 2` to
  `TV4 Sport Live 3`.
- Replace the linked broadcast
  `6309196d-9339-5a36-afb7-12f676820589`, which is the stale
  `TV4 Sport Live 2` row ending at 23:15, with the existing broadcast
  `ef7f38bf-48a2-583b-8854-882f9757947b`. The replacement is the current
  `TV4 Sport Live 3` row from 20:40 to 23:00.
- Show the replacement broadcast and hide the obsolete unlinked
  `TV4 Sport Live 2` row.
- Add the central fixed-channel mapping
  `TV4 Sport Live 3 -> https://www.tv4play.se/kanaler`, so the corrected
  channel has a direct public link.

Evidence:

- <https://www.tv.nu/s/s_4601127_20260831>
- <https://en.legaseriea.it/serie-a/news/the-referees-for-the-2nd-round-x9129>

### Tjeckien-Sverige

- Delete the active activity link for `Gun Hindgren` from
  `00f5e92f-8b65-4f53-837a-de5f2457ca50`.
- Add the existing Person entity `Erica Muhizi` as an active participant,
  represented by `Volleybollandslaget (dam)`.
- The other 13 participants in the current CEV match roster remain
  unchanged. No individual start times are required for this activity.
- Save the CEV replacement notice and the official CEV match roster as
  `ParticipationEvidence`.

Evidence:

- <https://www.cev.eu/articles/volleyball/injured-hindgren-out-of-eurovolley-erica-muhizi-called-up/>
- <https://www-old.cev.eu/Competition-Area/MatchPage.aspx?ID=1573&mID=85094>

## Other review results

- No other broadcast-time or activity-broadcast correction is recommended.
  The current direct provider links were checked and returned HTTP 200.
- No StreamLink correction is recommended. Existing activity-specific links
  are direct provider URLs without TV.nu affiliate wrappers or tracking
  parameters; the remaining displayed channels resolve through the central
  fixed-channel catalog.
- No star changes are recommended. Isabelle Haak, Victor Lindelöf, and
  Viktor Gyökeres remain the qualifying `tier_0` participants in this scope.
- No detail activities are recommended. Football and volleyball do not
  provide a deterministic person-specific broadcast segment here.

## Unresolved items

- No unresolved participant or stream item remains after the proposed
  Atalanta channel mapping and the CEV participant replacement.
- TV.nu lists an earlier 20:50 Viaplay coverage entry for Aston Villa -
  Arsenal FC in addition to the 20:55 event-specific entry. SESport uses the
  direct 20:55 link, which matches the V Sport Premium broadcast start, so
  no correction is recommended.

## Public card counts

- Step 1, initial public view: 5 activity cards and 19 visible participant
  names.
- Step 7, final cache-busted public view: 5 activity cards and 19 visible
  participant names.
- Difference in card count: none. The final view was unchanged because the
  database was intentionally left untouched pending approval.
