# Activity run — 2026-11-09

- Started: 2026-11-09 08:03:30 UTC.
- Database: `sesport`, verified through `bin/dbc.sh` at 172.18.0.2:5432.
- Starting snapshot: 82 visible broadcasts captured at 08:24:40 UTC.
- Final audit at 11:21 UTC: 38 rejected, 20 processed, 24 unresolved, zero invalid
  or missing outcomes.
- Final source review moved T100 Dubai from rejected to unresolved because two
  wildcard places on the official start list remain unnamed.
- Every unresolved broadcast remains visible and unprocessed, has an organization,
  and has at least two `ActivityEvidence` or `ParticipationEvidence` records.
- All 20 processed broadcasts are linked, processed, and hidden. Rejected broadcasts
  are hidden, unprocessed, and unlinked.

## Processed events

Times below are local Swedish broadcast times. All new activities are published.

### Japan–Sweden women’s hockey — 11 Nov

- Reused the Dam-VM group and the existing 23-player Swedish roster.
- Existing activity `b95919b3-62c7-4a85-a099-2c7331123d39`: SVT Play,
  19:00–21:30; broadcast `d2bc4d24-afc8-523b-9f68-0b74f5411846`.
- Created activity `4f88daea-5104-4c1f-87b3-6f4fcf6785ee`: SVT24,
  20:00–21:00; broadcast `54cddf8f-2416-d136-9989-45e1bdd74ef2`.
- Corrected the SVT24 end time to 21:00, replaced a stale football source on the
  SVT Play activity, and saved the direct Japan–Sverige SVT Play link.
- Both feeds have all 23 confirmed Swedish players. Verified profiles, birthdates,
  club/team links, ages, and flag. No player-specific start times were published.
- Public card count remained 3. The grouped row shows its overall 19:00–21:30 window
  and all channels, but hides the distinct segment windows. Its heading appends
  “Japan – Sverige” after the group title.
- Evidence: [SVT Text 399], [IIHF schedule], [SVT Play page], and TV.nu.

### DP World Tour Championship — 12–15 Nov

- Created group `ea6f5c51-f840-48b6-9fca-41c074e3b47c`, titled
  `DP World Tour Championship`, spanning 12–15 Nov.
- Created eight published activities, each linked to Mikael Lindberg and DP World
  Tour. The TV windows remain separate by channel:
  - 12 Nov, Viaplay 08:00–14:30: `7947cd73-4b91-4cc4-8e64-8790fa1f9f3e`,
    broadcast `a054b2d1-872e-7439-a4ad-485dc99f19ef`.
  - 12 Nov, V Sport Golf 08:00–14:00: `15f9eb7f-f91e-4f9d-8caa-36264caad1e9`,
    broadcast `b07b2fe1-f600-193a-befc-e0a069508b0e`.
  - 13 Nov, Viaplay 08:00–15:00: `d84a0dd9-4356-40a5-bc68-650091708dc2`,
    broadcast `85766d85-554b-cf39-ad44-74ad84716fa4`.
  - 13 Nov, V Sport Golf 08:00–14:00: `b440e4a4-e35c-4fb7-a2fe-97eedce4a746`,
    broadcast `6d3f4473-d073-f538-a1a5-b1594015b928`.
  - 14 Nov, Viaplay 08:00–15:00: `f9cf9801-d484-4b1a-bef7-14ff5bae5fb2`,
    broadcast `376543f6-099a-f635-9f12-2e9d40489bc1`.
  - 14 Nov, V Sport Golf 08:00–14:00: `7813a903-1c70-4587-abec-4c2be20cb584`,
    broadcast `5e4866f4-08b4-c53f-9ed9-2ed73e15c297`.
  - 15 Nov, Viaplay 07:30–14:30: `90c049eb-b7fe-46fd-aaf3-cdf36cc2fb07`,
    broadcast `a9828130-77b4-983c-a665-a8ca6f07ca7f`.
  - 15 Nov, V Sport Golf 07:30–13:30: `653436de-6a2b-44f7-b7cf-c41e656966c2`,
    broadcast `59e54a77-b253-0c39-b600-6e9219c7931e`.
- Lindberg qualified at 32 after Abu Dhabi. He remains active and linked to all rounds.
  No player-specific tee times were published, so no detail activities were added.
- Public card counts increased by one on each date, 12–15 Nov. Each grouped card shows
  both feed windows, participants, ages, clubs, profiles, and the Swedish flag.
- Evidence saved includes TV.nu listings, the official DP World Tour schedule, and
  the play-off qualification overview.

### The ANNIKA — final round, 15 Nov

- Reused group `2802ff15-5f6d-454a-a2df-1f8385dad269` and rounds 1–3.
- Created two published `Finalrundan` activities linked to Linn Grant, Frida Kinhult,
  Ingrid Lindblad, Linnea Ström, and LPGA Tour:
  - Viaplay 19:30–23:00: `9e28db90-cf10-4b19-b38b-1dcdcbdafa80`, broadcast
    `c99d88da-225b-5a3f-a127-ba2332974531`.
  - V Sport Golf 19:30–22:30: `6f4596b0-cee2-4836-8d28-31f20d76b168`, broadcast
    `8078932a-b22a-7636-b84b-e4b1be2ea195`.
- Verified all four profiles, birthdates, clubs, existing priorities, and public flags.
  No individual tee times were available; no detail activities were added.
- Added one grouped card on 15 Nov. It displays both windows and all four golfers.
- Evidence saved: LPGA event and watch pages, TV.nu, Sendeplan.dk, and the complete
  108-player field article.

### Speed skating World Cup, Beijing — 15 Nov

- Reused group `d54140c8-aeb0-467a-92f0-8f4f0acec8d1`.
- Created published activity `7d41b23e-b543-4e01-8b45-68fd8b56f59b`,
  `Dag 3`, 09:55–13:40 on Viaplay; broadcast
  `c99e9904-9ca2-1636-b712-50f78b039cd2`.
- Linked Sebastian Forsmark. Corrected his Sundsvalls SK club URL to
  `https://www.sundsvallsskridsko.se/` and verified his profile and club page.
- The official schedule had no discipline-specific start list for Forsmark; no detail
  activity was added. The public card omits the 13:40 end time.
- Added one card on 15 Nov. Evidence saved from ISU, KNSB, SweSports, and TV.nu.

### Grand Slam of Darts — 15 Nov sessions

- Reused group `799893d7-ab1a-4815-bbba-5ac75f3f9cb6`.
- Created four published Stage activities. Jeffrey de Graaf and Anton Östlund are
  active participants in each:
  - Viaplay 14:00–18:30: `b029a452-65ed-43a7-98a9-c591cee54307`, broadcast
    `e867103b-feaa-363d-a0a4-0cdc914bdd05`.
  - V Sport Extra 14:00–18:00: `d78d980a-ee1b-49b4-80a3-d22ab125ea9a`, broadcast
    `d12b2ac1-797e-cc3e-a8af-e4ca4ab660ab`.
  - Viaplay 20:30–01:00: `db9b41c0-598c-43cb-b1fc-b3dc42510b7d`, broadcast
    `71cf97e8-0c45-f03d-88cb-ab4245a38ccf`.
  - V Sport Extra 20:30–00:30: `4d6732d0-46cc-4c61-998e-a792d6f2e271`, broadcast
    `c35b0b20-cd14-2d32-b1ef-5e16e707a26e`.
- Added Anton to the four existing 14 Nov activities with Jeffrey:
  - `2c4ce6da-d9be-4aa4-a0d3-ea83736ac797`, broadcast
    `ba317efc-d9e3-413a-af21-3e1bd5ca9b28`.
  - `e9c77060-19f4-494f-a6d6-1fdeaf8cedca`, broadcast
    `932e59cd-4d30-623e-af00-f413c4cf20ab`.
  - `69da4079-628a-41a9-899f-4b12dc53fa0b`, broadcast
    `78c7c99a-c866-7331-9aab-be6fab350cb3`.
  - `f040ea02-e606-4544-ae6f-f5947dfff122`, broadcast
    `426cf597-4e52-7c30-9878-5296de1e054e`.
- Created person Anton Östlund `004fcdbb-ef9d-49c8-b9ef-518bc79b3b21`, team
  Nynäshamn `9271e2b9-6836-4987-b978-e0a60e247325`, and prior club
  Nynäshamns Dartförening `1fdce988-0c0c-405a-9c71-b6c81e046593`.
  His birthdate, profile, alias, and tier 3 priority were checked. No match-specific
  pairings or start times were published.
- Added one grouped card on 15 Nov; 14 Nov stayed at eight cards. The 15 Nov card
  shows session windows and both Swedish participants.
- Evidence saved: PDC calendar and announcements, Anton’s PDC Nordic & Baltic
  qualification, the PDC qualifier notice, StDF, and TV.nu.

### Stuttgart show jumping — 15 Nov

- Created group `0b35c0cf-1df8-4093-ab4e-7d916d1fa534`, titled
  `Världscupen i hoppning: Stuttgart`.
- Created two published activities, each linked to Peder Fredricson and Henrik von
  Eckermann:
  - Sjuan 14:45–17:50: `edf1a7c2-d9c8-40f7-be7b-24435254b1f2`, broadcast
    `4128c8ea-3357-333a-a9a6-930eb880eb4d`.
  - TV4 Play 14:45–17:40: `5cfc44ba-15ff-429c-a55f-b2586d9fe0db`, broadcast
    `5f58493c-c8a0-3333-846d-77d0ea285496`.
- FEI schedules the event 15:10–17:40; TV.nu and the Swedish federation support the
  feed-specific windows above. The official entry list includes both riders. No
  individual order or start time was published.
- Added one grouped card. Both channels, time windows, profiles, ages, clubs, and flags
  display. FEI profile pages returned HTTP 403 to automated checks but opened in the
  web viewer; catalog links for both channels returned HTTP 200.
- Evidence: [FEI schedule], [official entries], TV.nu, and the Swedish federation
  schedule.

### Austin Championship — Round 4, 15 Nov

- Reused group `43fd72e5-8fa4-49ee-97c5-9f2be6e81324`.
- Created published activity `62c770ea-0183-4973-a25e-62d24ff1cdba`,
  `Runda 4`, Eurosport 2, 20:00–23:00; broadcast
  `6e8a1ab2-44fd-1137-b72b-088544b354db`.
- Linked Jesper Svensson and Pontus Nyholm, both confirmed in the official field.
  No individual Round 4 tee times were published; no detail activities were added.
- Added one card on 15 Nov with the round, channel, time window, golfers, ages, clubs,
  profiles, and flags.
- The Eurosport 2 catalog fallback points to Max Sports. Automated curl and web checks
  returned HTTP 403; no event-specific Max URL was available.
- Evidence: PGA TOUR field and international delivery schedule, event schedule,
  and TV.nu.

## Dashboard correction — Sweden–Bosnia, 14 Nov

- The Admin Dashboard flagged Draft activity
  `d28e1a09-971b-4398-9f23-e723584ab5d1`.
- Kept published activity `0bb67ea0-bf16-4838-b490-189295c63ce8` on the Viaplay feed
  `073896ba-f30f-dd3e-9e5c-3a49e7761e12`, with Viaplay as its channel.
- Linked the separate Viaplay Sport feed
  `4c591b16-4ec8-fc3f-9202-3c9a36a00e50` to activity
  `d28e1a09-971b-4398-9f23-e723584ab5d1`, 20:40–22:40, and published it.
- Both activities retain the same group and 25-player Swedish roster. The match guide
  confirms a 20:45 kickoff. Removed a direct Viaplay link after it returned HTTP 404;
  active channel catalog links return HTTP 200.
- The dashboard now says `No upcoming activity issues.` Public card count stayed at
  eight. Its row shows both channels but only 19:30–00:00, omitting the separate
  20:40–22:40 Viaplay Sport window. The title also appends the match title after the
  group title.
- Evidence: [SvFF match guide] and TV.nu.

## Rejected and hidden broadcasts

All IDs below are hidden, unprocessed, and unlinked. Domestic, foreign-fixture,
highlights, and unidentified items were rejected from their broadcast metadata.
Complete-field decisions have `ParticipationEvidence` saved against each broadcast.

### ATP Finals: complete singles and doubles fields have no Swedish entrants (8)

- `748ae8ca-887a-f139-9a63-f5c0a8b7add3`
- `d4861f80-2504-f133-9f2c-3e38cda734a8`
- `1f361ab5-eaee-cf33-bf2b-117f0832d7c4`
- `7c5b62f1-575f-9635-93b7-b6f3143a4963`
- `795d16f8-689e-5238-901a-06c556df502c`
- `39a2a169-74e2-0f39-b504-21563ec39da0`
- `8f54e209-ccd4-6736-b848-ac74a9d2cfc5`
- `b3a1fc70-1a19-f432-8085-8e5a528cf938`

Evidence: official [ATP singles Race to Turin] and [ATP doubles Race to Turin].

### Generic winter-sports studio listing (1)

- `eed189ab-5de9-2d32-ace0-6c0e3a7a97e7`

### Domestic Idre Fjäll episodes (2)

- `b64e41c1-34e4-ce37-90db-7648c76e6e41`
- `db63c4da-2227-953c-a526-de565c61c79e`

### Generic NHL highlights (1)

- `0aebcda5-c0a9-8531-8e25-0c28fa9435c1`

### Domestic Damallsvenskan broadcasts (2)

- `e8d515b3-ef79-a036-a6f7-b7f60b571969`
- `4efcdf7a-6ff8-a030-85ff-33e8321a14d9`

### Domestic Swedish U20 hockey fixture (1)

- `24335b37-6b57-b73d-99aa-da735a493054`

### Unidentified weightlifting listing (1)

- `2f44dc00-8bfa-b33a-bd7a-04154ff4242e`

### Champion of Champions: complete draw has no Swedes (2)

- `31b07bad-6790-e132-90ef-b2b40904f7bd`
- `d7dc434e-b253-d23e-a8bc-5ae2ba3888a7`

Evidence: the [official WST draw and match centre].

### Domestic Bergsåker horse racing (1)

- `87c3ddfb-ec41-ab31-afd4-b150e02b935e`

### Generic UEFA Nations League highlights (2)

- `e8854cc5-af3a-003e-a4e9-f6dc1c7f15df`
- `4374c034-a57d-883d-8e14-7b2a310e9422`

### Foreign national-team fixtures, Swiss hockey, and NBA (13)

- `04e7b77d-162e-7735-9e61-0ecc0d5bda48`
- `a492a7bf-ac3e-7c3f-86c8-c76d77f047f9`
- `27a67139-5eb0-503d-8292-433daf01c91d`
- `ed3d4510-dfaa-5932-ab87-d84ee4b6a3f1`
- `5f18e93f-ca68-8c3a-986e-4afcf313cf38`
- `7140e82d-1e57-f430-8d95-90260119bfac`
- `7e7f3a27-2a80-1e32-9a19-17494a25dece`
- `91e42c9f-e17c-5f30-9a3a-4c345e20ddc6`
- `0cba9668-3d22-d836-8f13-0a9b5fc8619f`
- `a16fceef-529c-693d-81dc-b3fcff4651ed`
- `f74ac34e-fa4b-813a-9d3f-30c76157d002`
- `2aa75d1a-8648-8930-b813-7eb1503ccc61`
- `5a31e5c5-9d24-7332-a2eb-7f705454989d`

### Domestic Swedish handball and volleyball fixtures (3)

- `5c224378-d423-3b3c-b952-5fcf8f5c6d8f`
- `24473c13-af3b-3b34-85c5-e1f03d9c7d5d`
- `7c1b7924-f283-313f-a5e9-91a9a151b7de`

### Skate America: complete official entries have no Swedes (1)

- `0b9639a1-d2f8-f632-bf85-34423694cc90`

Evidence: [official ISU entries].

## Unresolved and visible broadcasts

Every ID remains visible and unprocessed, has its listed organization, and has at least
two `ActivityEvidence` or `ParticipationEvidence` source records.

### T100 Dubai — T100 Triathlon World Tour (1)

- `4a09555c-a465-723f-a79a-ca449d234dc2` — two wildcard places remain unassigned.
  The [PTO start list] has no names for those places; do not infer participation.

### Superprestige Niel — Cycling (2)

The event pages do not supply complete start lists. [Niel official event page]

- `d1ecf1fe-4af5-a138-a1df-f28e4a74aba1`
- `df601e3d-4489-0836-a25b-cdade000806c`

### Women’s hockey World Championship quarterfinal — Ishockeylandslaget (D) (1)

The opponent is unknown; the broadcast is not mapped to a confirmed Swedish fixture.
[IIHF schedule]

- `4cedbdd0-43b6-1c38-b407-7849f1769ff5`

### Levi women’s slalom — FIS Alpine Ski World Cup (Women) (4)

No event-specific official start list was available. [FIS calendar] and [Swedish alpine
roster policy]

- `a88c683a-fc54-4737-a146-fa556f086fc0`
- `1aaeb22c-70c9-2b38-b0fd-699f7e2165fd`
- `c92ba2e2-db89-1d31-8256-4eba74a9dfad`
- `190cfacb-e705-a331-96c2-773b0b0cfa10`

### Superprestige Merksplas — Cycling (2)

The event page does not supply a complete start list. [Merksplas official event page]

- `48b30142-56e0-0d38-95a6-d130f104ee66`
- `daf54d93-a7c1-d73b-bf27-942f585dfdfd`

### Saitama Criterium — Cycling (1)

The official announcement says further entrants will be announced; its current list is
partial. [Saitama official announcement]

- `28920d2f-4b0d-2030-86ab-7344d0b85fba`

### Levi men’s slalom — FIS Alpine Ski World Cup (Men) (4)

No event-specific official start list was available. [FIS calendar] and [Swedish alpine
roster policy]

- `95df27f8-ad0e-3234-80d0-3e57dc9b2d1f`
- `a19537cd-6e57-0a39-be5c-4daf23235b72`
- `fa41947e-4b58-b538-8709-8e447f63cb48`
- `45a05d75-b60a-b130-b174-01aa55384045`

### Bollekescross Hamme — Cycling (2)

Entries are not yet published. [Hamme official rider page]

- `e95f0a0e-e407-093b-a0d6-8ec1d8ea3611`
- `b0c6af74-573d-9c3b-a114-a1ad0d7cb343`

### Women’s hockey World Championship semifinals — Ishockeylandslaget (D) (4)

The semifinal teams are not known. [IIHF schedule]

- `2aedd7f7-c769-c93d-9d90-f6d37c57da2c`
- `79c12ace-35d2-9f30-9912-08e1eae41f1d`
- `9986469d-9dee-d33b-b3ec-e04c17bc90e4`
- `f3840800-f4ca-1a38-b6d5-ec7f0849cc3c`

### Sweden–Belgium women’s basketball — Basketlandslaget (dam) (3)

The federation selected 14 players, but the 12-player match roster was not published.
[Swedish federation squad release]

- `f3569876-e1a0-da37-9d70-b55cfb595f43`
- `6e42c75f-d0b7-1934-9cc6-b0ba283e0977`
- `50bd5925-7c2f-0535-a2b5-8cb01461f23b`

## Public card counts

Counts were read from hosted `https://sesport.se` pages before and after processing.
Grouped events count as one card.

| Public date | Before | After | Change |
| --- | ---: | ---: | ---: |
| 2026-11-11 | 3 | 3 | 0 |
| 2026-11-12 | 3 | 4 | +1 DP World Tour Championship |
| 2026-11-13 | 5 | 6 | +1 DP World Tour Championship |
| 2026-11-14 | 7 | 8 | +1 DP World Tour Championship |
| 2026-11-15 | 0 | 6 | +6 confirmed events |

The six 15 Nov cards are DP World Tour Championship, The ANNIKA, speed skating,
Grand Slam of Darts, Stuttgart show jumping, and Austin Championship. Dashboard and
participant changes on 14 Nov added no card. Restoring the unresolved T100 broadcast
added no activity card.

## Public presentation and verification notes

- TV.nu evidence is saved to activities but hidden from source lists by the configured
  source-host block.
- The 11 Nov hockey card and 14 Nov Sweden–Bosnia card display the combined window for
  separate feed segments. Their grouped headings also append the match title. These
  need a presentation fix; no code or configuration was changed.
- The Beijing speed-skating card omits its end time; the Darts card badge is approximate,
  though its segment rows show exact windows.
- The Eurosport 2 Max Sports catalog fallback returned HTTP 403. No event URL was
  available. This remains a provider-link limitation.
- All processed activities have a Swedish Person participant and a titled group.
  Displayed ages, clubs, profiles, flags, channel windows, and links were checked on
  `https://sesport.se`.
- No code, asset, schema, or configuration changes were made. No tests or builds were
  run for this data-only job.

## Source links

[SVT Text 399]: https://www.svt.se/text-tv/399
[IIHF schedule]: https://www.iihf.com/en/events/2026/ww/schedule
[SVT Play page]: https://www.svtplay.se/video/eQrrLWw/ishockey-vm-damer/japan-sverige
[FEI schedule]: https://www.fei.org/events/2026_CI_0005/Stuttgart
[official entries]: https://www.hippobase.com/EventInfo/Entries/CompetitorHorse.aspx?EventID=750
[Austin event schedule]: https://www.austinchampionship.com/about/schedule-of-events/
[SvFF match guide]:
  https://www.svenskfotboll.se/nyheter/landslag/2026/09/herr-matchguide-bosnien-borta
[PTO start list]: https://stats.protriathletes.org/race/dubai-t100/2026/participants
[PTO calendar]:
  https://stats.protriathletes.org/pro-race-calendar?brand=T100+Triathlon
[ATP singles Race to Turin]:
  https://www.atptour.com/en/rankings/singles-race-to-turin
[ATP doubles Race to Turin]:
  https://www.atptour.com/en/rankings/doubles-race-to-turin
[official WST draw and match centre]:
  https://www.wst.tv/matches/1ec300d9-da1c-4b16-95e7-d296c399255b
[official ISU entries]:
  https://isu-skating.com/en/figure-skating/events/eventdetail/isu-gp-skate-america-2026/
[Niel official event page]:
  https://www.superprestigecyclocross.be/nl/kalender/2026-2027/niel
[Merksplas official event page]:
  https://www.superprestigecyclocross.be/nl/kalender/2026-2027/merksplas
[Saitama official announcement]: https://saitama-criterium.jp/news_20261015/
[Hamme official rider page]: https://bollekescrosshammezogge.be/renners/
[FIS calendar]:
  https://www.fis-ski.com/DB/alpine-skiing/calendar-results.html?noselection=true
[Swedish alpine roster policy]:
  https://goteborg.skidor.com/idrotter/alpint/landslag/alpina-landslaget
[Swedish federation squad release]:
  https://via.tt.se/pressmeddelande/4600379
