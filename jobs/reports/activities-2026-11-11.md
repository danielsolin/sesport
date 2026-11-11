# Activity Processing Report — 2026-11-11

## Run and audit

- Started: 2026-11-11 07:07:37 Europe/Stockholm.
- Starting snapshot: 68 visible, unprocessed broadcasts; no activity links.
- Snapshot SHA-256: `a1b709001378145c9d869e8efe1e8e77f817a87be422bcb95d84974697ccb1a3`.
- Final audit: 8 processed, 31 rejected, 29 unresolved; 0 missing IDs or anomalies.
- All processed and unresolved broadcasts have an organization; each outcome matches its
  required visibility, processing, and activity-link state.

## Activities created or changed

All eight activities are published for public date 2026-11-17. Times below are local
Europe/Stockholm times. Existing groups were reused except that the CHL group end date was
extended through 17 November.

### Champions Hockey League

Group `4f0373e1-59da-4c5b-aa03-d30fe49812a1` was extended through 2026-11-17.

- `bd84fdda-cfe4-45bf-819d-fa373290f4e0` — Frölunda HC–Växjö Lakers, Viaplay,
  18:55–22:30; broadcast `0136cba2-7695-9536-a0e9-26d2953e3f22`.
- `12e37b7b-212e-40da-8175-7fecb96b2be0` — Frölunda HC–Växjö Lakers,
  V Sport Vinter, 18:55–22:00; broadcast `6bdbea81-7dcc-6e36-aa16-994f1d596877`.
- `25265bd1-4e88-4245-8dbe-2de0ea147009` — Skellefteå AIK–Rögle BK, Viaplay,
  18:55–22:30; broadcast `3056b933-27f6-ef3f-815b-2561e74e0f9b`.
- `354ff728-178d-4a7c-badc-00bb8d531a67` — Skellefteå AIK–Rögle BK, V Sport 1,
  18:55–21:30; broadcast `e16d7f89-8bbe-4d3d-ba01-8afae0c4517b`.
- Linked 46 Frölunda/Växjö players to each match segment and 51 Skellefteå/Rögle
  players to each segment. Participant profile, birthdate, and team URL checks passed.

### Nations League

Reused group `01071d39-9288-454f-9c4b-54e3d167aecf`.

- `698520f0-1355-4919-8195-94efc705cfb4` — Polen–Sverige, Viaplay,
  20:00–00:00; broadcast `18d5c8a4-ea2d-5736-b626-e69fc0cf9906`.
- `91a58cfc-c7ab-421c-9265-36de1d1772be` — Polen–Sverige, Viaplay Sport,
  20:40–22:40; broadcast `c5da7176-6242-6930-869e-2dc38b09b221`.
- Linked the 24-player official squad to both segments. Age, profile, formative club,
  and current team-link checks passed.

### Grand Slam of Darts

Reused group `799893d7-ab1a-4815-bbba-5ac75f3f9cb6`.

- `e2c2508d-f757-446b-a58d-d3619e4f0e08` — Viaplay, 20:00–00:30;
  broadcast `a4bb91ea-4dd3-c632-91a9-2b0bf0150f47`.
- `f08b5738-8948-4a5f-bece-a2191a0e2102` — V Sport Extra, 20:00–00:00;
  broadcast `dd5df743-2743-3e3d-ae23-4165adce1e16`.
- Linked Jeffrey de Graaf as active in both activities. Verified birthdate, profile, and
  formative club. No speculative team link was added for this individual sport.

All eight broadcasts were linked to their segment, assigned the activity organization,
marked processed, and hidden. Added 47 evidence/source records and 244 participant links.

## Rejected broadcasts

These 31 broadcasts were hidden; only `hidden_at` changed. Each group shares the stated
reason.

Generic studio or highlights programming:

- `5de6a09a-cbbc-343d-a507-7f462aa5d761` — Premier League Show, generic studio programme.
- `8f60b7df-2581-7837-ba3d-e4ea191a7200` — NHL On The Fly, highlights/studio programme.
- `4be1a2de-75dc-c83f-8a32-2d5d34c4ffd1` — Bollklubben, generic studio programme.

Domestic Swedish competitions:

- `41d49a15-8172-c03d-a92b-1a6ba7f60040` — Damallsvenskan.
- `7044c913-0854-6e3d-b100-690e11cc52d0` — Gripen–Sandviken bandy.
- `39a426c8-1ea3-9b36-95b7-b4ae3e3c8b4c` — Swedish Basketball League.
- `3fc67d6a-e89d-143c-99be-25d1a32fd52e` — Swedish Basketball League.
- `77cf5492-c218-593e-9bdd-20f09e7042ae` — Swedish Basketball League.
- `c3d5fee4-ceeb-5836-bc3a-fea9d970ebfa` — Swedish Basketball League.
- `f56b9c20-df9b-f83c-977a-12734d12300a` — Swedish Basketball League.
- `c050b162-189d-2c39-b8a1-7c9a4faeca15` — Handbollsligan.

ATP Finals broadcasts; the complete singles and doubles fields had no Swedish entrants:

- `12254afd-4d67-fa36-bbda-e9447f6b1b61`
- `217bf705-46eb-cd34-b0d0-2e41c510dd53`
- `7b54d53a-e7eb-1236-b948-4793f055c8e4`
- `f3ab0163-a5db-df3f-87e6-245caee719a8`
- `514468be-95da-303e-9da3-769506fea488`
- `bc4fee34-4789-733f-b5ad-13e8d0bb0d8a`
- `45f97654-9b54-b53c-9e17-d167e7ac81db`
- `e516b2c7-3e96-5636-aad3-a37c99bc8073`

Foreign-club Champions Hockey League fixtures; no Swedish club was involved:

- `84c0220a-8ee2-e632-b03d-f3828813c17f` — Bili Tygri–Kölner Haie.
- `19319df7-9221-9d33-bebc-8972739cf5ca` — Plzen–Davos.
- `6f5a8d62-77a1-a23a-bc7d-ce082866063b` — Tappara–Adler Mannheim.

Foreign national-team fixtures; no Swedish national team was involved:

- `0135163c-88d6-e333-918b-54744e1fdd02` — Ireland–Kosovo.
- `061c3deb-7fab-af33-a688-3ad815bb3212` — Ukraine–Georgia.
- `2d7663f8-189f-de34-a2a6-cd007286a96f` — Hungary–Northern Ireland.
- `89841e6a-04d2-5f3b-a3f7-6be4f8271f8a` — Wales–Portugal.
- `aabf934a-54be-8136-883a-ac25535b2a28` — Denmark–Norway.
- `bb5f9a2c-8854-8e3c-8ecc-ce486bf2524e` — Israel–Austria.
- `f732c698-8b03-9030-ac36-9f904b21630a` — Bosnia and Herzegovina–Romania.
- `69386d64-069a-5b36-9eea-4bf143c694fc` — Denmark–Norway.

- `7454ca33-ffcf-873f-ac76-fcb1cd6026b3` — Tampa Bay–Washington; foreign NHL fixture,
  with no Swedish club involved.

## Unresolved broadcasts

All 29 remain visible, unprocessed, and unlinked. Each has a known organization.

Generic TBA listing, organization `Fotbollslandslaget (herr)`:

- `f49a3711-30ac-3b39-9ac0-1341afb5bdf8` — TV listing gives no match identity; no safe
  association to a specific fixture was found.

T100 Dubai wildcard places, organization `T100 Triathlon World Tour`:

- `4a09555c-a465-723f-a79a-ca449d234dc2`
- `fb15c1c6-6162-0a37-8621-82077484f1d8`
- Official 2026 participant lists still show unnamed wildcard positions.

Cyclocross, organization `Cycling`:

- `48b30142-56e0-0d38-95a6-d130f104ee66`
- `daf54d93-a7c1-d73b-bf27-942f585dfdfd`
- `0c97916b-3505-093d-beec-26b1f21afeef`
- `e95f0a0e-e407-093b-a0d6-8ec1d8ea3611`
- `b0c6af74-573d-9c3b-a114-a1ad0d7cb343`
- `b1e783ae-93bc-453b-9b8e-50998c9b286a`
- Merksplas and Hamme event pages do not publish complete 2026/27 start lists.

Women’s IIHF World Championship later rounds, organization `Ishockeylandslaget (D)`:

- Quarterfinals: `4cedbdd0-43b6-1c38-b407-7849f1769ff5`,
  `8262dbbc-52ca-5036-9245-bdb5e52ffb1d`.
- Semifinal listings:
  `2aedd7f7-c769-c93d-9d90-f6d37c57da2c`,
  `79c12ace-35d2-9f30-9912-08e1eae41f1d`,
  `e87ee685-f224-b53f-86f9-810730ddb329`,
  `14f0f9a6-ee62-d93c-9a97-fb5006909ebd`,
  `bc194878-9a2a-563f-9857-de5d8d317e2c`,
  `9986469d-9dee-d33b-b3ec-e04c17bc90e4`,
  `f3840800-f4ca-1a38-b6d5-ec7f0849cc3c`,
  `a41d4795-75ad-c234-a53e-75e4456b595a`,
  `d663b80c-5c25-9e3c-a018-938198553550`.
- Final listings: `da878fa6-b3dc-1a39-bc18-b22817834498`,
  `5895be4b-453f-b13c-a1ce-2d4b76574b4d`,
  `dfb866d9-19f7-d83b-bab7-95bbc47021df`.
- The group-stage standings did not yet determine later-round opponents.

Women’s Sweden–Belgium basketball qualifier, organization `Basketlandslaget (dam)`:

- `6e42c75f-d0b7-1934-9cc6-b0ba283e0977`
- `f3569876-e1a0-da37-9d70-b55cfb595f43`
- `50bd5925-7c2f-0535-a2b5-8cb01461f23b`
- The preliminary squad is not the confirmed matchday roster of 12.

Grand Slam of Darts, organization `PDC / WDF`:

- `623dfb44-ec21-403b-9374-7d5dc00fc80f` — 15 Nov Viaplay listing starts at 20:00, but
  PDC and TV.nu show 20:30; the session and time conflict remains unresolved.
- `6b67db15-d728-7b38-97b4-d0abeca27668`
- `de148822-348c-7530-8184-3744ba8efb51`
- Anton Östlund’s 16 Nov match depends on his Saturday result, which was not yet known.

## Evidence reviewed

Official and schedule evidence was saved to the matching database evidence/source records.

- ATP singles field:
  https://www.nittoatpfinals.com/en/news/nitto-atp-finals-2026-field-is-set
- ATP doubles field:
  https://www.nittoatpfinals.com/en/news/luz-matos-nitto-atp-finals-2026-qualification
- T100 participants: https://stats.protriathletes.org/race/dubai-t100/2026/participants
- IIHF schedule: https://www.iihf.com/en/events/2026/ww/schedule
- Merksplas calendar:
  https://www.superprestigecyclocross.be/nl/kalender/2026-2027/merksplas
- Hamme roster: https://bollekescrosshammezogge.be/renners/
- Basketball: Basket.se and the FIBA November roster tracker; tracker URL is retained in
  the existing broadcast evidence records.
- PDC draw:
  https://tv.pdc.tv/news/2026/november/09/littler-drawn-greaves-zonneveld-at-mr-vegas-grand-slam/
- CHL schedule: https://www.chl.hockey/en/sweden
- SvFF squad:
  https://www.svenskfotboll.se/nyheter/landslag/2026/11/herrlandslaget-novembertrupp/
- SvFF withdrawal:
  https://www.svenskfotboll.se/nyheter/landslag/2026/11/herrlandslaget-aterbud/
- TV.nu schedules and event pages plus direct Viaplay event pages were saved per activity.

## Public site and dashboard verification

- Public page: `https://sesport.se/?date=2026-11-17`.
- Card count changed from 0 before the run to 4 after the run.
- The four cards are Frölunda–Växjö, Skellefteå–Rögle, Polen–Sverige, and Grand Slam of Darts.
  Eight activity segments are grouped into those four event cards.
- The rendered schedule merges distinct segment windows into an approximate card-wide range.
  It omits shorter segment end times and some segment titles. Some channels, including Viaplay
  Sport and V Sport Extra, use a generic catalog link or no link on the rendered page. Direct
  event links are saved in the activity sources. This is an unresolved presentation defect.
- Dashboard `https://sesport.se/Admin/Dashboard`: no upcoming activity issues and no open todos.

## Scope and validation

- No code, assets, schema, configuration, deployment, or unrelated application data changed.
- No tests or builds were run for this data-only job.
- The final SQL audit found all 68 starting IDs, exactly one expected outcome per ID,
  no anomalies, and no missing organizations for processed or unresolved broadcasts.
