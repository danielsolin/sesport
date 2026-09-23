# Activity verification report - 2026-09-23

## Scope and public-view counts

- Scope: the public SportsDay view for 2026-09-23 in Europe/Stockholm.
- Public view: `https://sesport.se/?date=2026-09-23`.
- Step 1 count: 2 activity cards: `Hammarby – Rangers` and
  `St Pölten – Malmö FF`.
- Step 7 count: 2 activity cards, unchanged.
- The recommended changes were applied after operator approval.

## Applied changes

- Changed the `St Pölten – Malmö FF` broadcast start from 18:50 to 18:35 local
  time in both `local_start_time` and `starts_at`.
- TV.nu's exact event page lists Sportbladet Plus at 18:35. The Aftonbladet
  asset metadata also starts at 18:35. The official match kickoff is 18:45,
  so this is a broadcast-start correction, not a kickoff correction.
- Changed `Zecira Musovic` from watch priority `tier_2` to `tier_0`, which shows
  a star. She is active in Malmö FF's current squad, returned to the Swedish
  national-team squad in 2026, and has a recent World Cup bronze and standout
  performance against the USA.
- No participant, stream-link, or detail-activity changes were needed.
  Both activities have 15 active Swedish participants; the other 29 people
  remain at watch priority `tier_2`.

## Unresolved items

- No sourced broadcast end time was found for `St Pölten – Malmö FF`. Keep the
  current 20:50 end time until stronger evidence is available.
- ORF SPORT+ also lists an Austrian broadcast at 18:40 with a 49-minute slot.
  It is not the Swedish provider shown on the public activity, and no verified
  direct Swedish-accessible stream link was found, so it was not added.

## Evidence sources used

### ActivityEvidence

- UEFA's qualifying schedule confirms the date and match kickoffs, 18:45 and
  19:00.
  - Host: `https://www.uefa.com`
  - Path part 1:
    `/womenseuropacup/news/02a9-2182e01492df-71327b40bf6b-1000--uefa-women-s-europa-`
  - Path part 2: `cup-second-qualifying-round-starts-23/`
- Hammarby Fotboll's official match listing confirms Hammarby–Rangers at 19:00.
  - Host: `https://hammarbyfotboll.se`
  - Path: `/`
- Malmö FF's official matchday guide confirms the 18:45 kickoff and Aftonbladet
  TV broadcast.
  - Host: `https://www.mff.se`
  - Path: `/nyheter/matchdagsguide-st-polten-borta-2026-09-21/`
- TV.nu's Hammarby event page confirms Sportbladet Plus at 18:50.
  - Host: `https://www.tv.nu`
  - Path: `/s/p_1615035_20260923`
- TV.nu's St Pölten event page confirms Sportbladet Plus at 18:35.
  - Host: `https://www.tv.nu`
  - Path: `/s/p_1619903_20260923`
- ORF SPORT+'s event page was checked as an alternative-provider listing.
  - Host: `https://tv.orf.at`
  - Path: `/livefussba1758.html`

### ParticipationEvidence

- UEFA's current Hammarby squad page matches all 15 published Swedish
  participants.
  - Host: `https://www.uefa.com`
  - Path: `/womenschampionsleague/clubs/2600837--hammarby/squad/`
- Malmö FF's current women's squad page matches all 15 published Swedish
  participants.
  - Host: `https://www.mff.se`
  - Path: `/lag/dam/spelare/`

### ParticipantStarEvidence

- Malmö FF's current squad page confirms that Zecira Musovic is an active
  player.
  - Host: `https://www.mff.se`
  - Path: `/lag/dam/spelare/`
- Omni reports Musovic's return to the Swedish national-team squad for 2026
  World Cup qualifying.
  - Host: `https://omni.se`
  - Path part 1: `/zecira-musovic-tillbaka-i-landslaget-infor-vm-kvalet/a/`
  - Path part 2: `lnnjQ7`
- The Swedish Football Association describes Musovic's decisive performance
  against the USA at the 2023 World Cup.
  - Host: `https://www.svenskfotboll.se`
  - Path: `/nyheter/landslag/2023/08/vm-swe-usa/`

### StreamLink

- Hammarby's existing direct Sportbladet Plus link was verified and returned
  HTTP 200: `https://tv.aftonbladet.se/video/406869`.
- St Pölten's existing direct Sportbladet Plus link was verified and returned
  HTTP 200: `https://tv.aftonbladet.se/video/407296`.
