# Activities Run Report — November 10, 2026, Run 2

- Start timestamp: 2026-11-10 20:49:51 UTC.
- Starting snapshot: 32 visible, unprocessed broadcasts; none had activity links.
- Final outcomes: 4 processed, 28 unresolved, 0 rejected, 0 audit anomalies.
- All 4 processed broadcasts are linked to one activity, processed, and hidden.
- All 28 unresolved broadcasts remain visible and unprocessed, with organizations set where known.

## Processed activities

### Austin Championship

- Existing group: `43fd72e5-8fa4-49ee-97c5-9f2be6e81324`, November 12–15.
- Activity `43fd72e5-8fa4-49ee-97c5-9f2be6e81325`, `Runda 3`, November 14,
  20:00–23:00 Europe/Stockholm.
  - Linked broadcast `ba113e80-2ea2-1337-b5a0-d56552d25615`; processed and hidden.
- Activity `62c770ea-0183-4973-a25e-62d24ff1cdba`, `Runda 4`, November 15,
  20:00–23:00 Europe/Stockholm.
  - Linked broadcast `5c0743d4-4003-a930-bd10-07f196a46c48`; processed and hidden.
- Both activities remain published in the existing group. Jesper Svensson and Pontus
  Nyholm are active participants. TV.nu confirmed Eurosport 2 timing; its stream link
  resolves to Max.
- Added Jesper's current club, Upsala Golfklubb, entity
  `a84b16e5-5e90-f123-f8cc-f91599380892`, and saved SGF PersonFacts evidence.
- The public schedule remained at 9 cards on November 14 and 7 on November 15.
  These existing activities added no cards.

### Rotax Grand Finals

- Created group `af9142cc-a5a5-4a26-95b1-d6ce450ba615`,
  `Rotax Grand Finals`, on November 14, with `sport_day` public date mode.
- Organization is Motorsport, `652c67c3-c1c5-4557-9121-d22af3f332dd`.
  Both activities are published, use `Europe/Stockholm`, and show Viaplay.
- `Finaler del 1`, public time 10:30–13:45:
  - Activity `c2a9fde8-fa95-4129-ab1c-4fe0c56af3bc`.
  - Stored time 09:30–12:45 UTC.
  - Linked broadcast `f6fedab7-e099-2a36-8aa3-dfebb13807ab`; processed and hidden.
- `Finaler del 2`, public time 14:00–17:50:
  - Activity `c6d18482-f08e-4cb9-a9a6-759a5e4a4b71`.
  - Stored time 13:00–16:50 UTC.
  - Linked broadcast `406894cf-a93f-d13f-86c4-ca3444cfb178`; processed and hidden.
- The organizer's November 10 update confirms finals on November 14. Final official class
  lists confirm Sebastian Hedin (Senior), Hannes Morin and Vigor Hjorvard (DD2), and
  Jonatan Morin (DD2 Masters). Each is an active Person participant in both activities.
- The revised programme gives no class-level start times. No detail activities were added.
  No star changes were supported; all four participants remain `tier_3`.
- Public person data: Hannes is 24 with Umeå AK; Jonatan is 34 with Umeå AK; Sebastian is
  21 with Järfälla Motorklubb; Vigor is 21 with KAK. Vigor's stored birthdate was corrected
  to 2005-08-11 from saved PersonFacts evidence; his displayed age remains 21.
- The card shows the group title only, with both segment rows, channels, times, four
  participants, ages, clubs, and person-specific profile links. Both Viaplay segment URLs
  returned HTTP 200.
- Hannes and Jonatan profile URLs returned HTTP 200. Sebastian's profile loaded in the
  web viewer. Direct requests to the Rotax profile host returned HTTP 429 for Sebastian
  and Vigor, so those two direct checks were rate-limited.
- Sources saved with the activities and people include the official schedule update,
  official Senior, DD2, and DDM entry lists, both Viaplay event pages, TV.nu listings,
  Rotax profiles, and participant person-fact sources.
- The public card count rose from 9 to 10 on November 14. November 15 remained at 7.
  Rotax added one grouped card on November 14.

## Unresolved broadcasts

The 28 records below remain visible and unprocessed. Their organization markers are set
where known. They are handled as unresolved and should be revisited only if new evidence
appears.

### Women's Ice Hockey World Championship — 14

Organization: Ishockeylandslaget (D). Later-round participation depends on results.

- `4cedbdd0-43b6-1c38-b407-7849f1769ff5`
- `8262dbbc-52ca-5036-9245-bdb5e52ffb1d`
- `e87ee685-f224-b53f-86f9-810730ddb329`
- `2aedd7f7-c769-c93d-9d90-f6d37c57da2c`
- `79c12ace-35d2-9f30-9912-08e1eae41f1d`
- `bc194878-9a2a-563f-9857-de5d8d317e2c`
- `14f0f9a6-ee62-d93c-9a97-fb5006909ebd`
- `9986469d-9dee-d33b-b3ec-e04c17bc90e4`
- `f3840800-f4ca-1a38-b6d5-ec7f0849cc3c`
- `d663b80c-5c25-9e3c-a018-938198553550`
- `a41d4795-75ad-c234-a53e-75e4456b595a`
- `da878fa6-b3dc-1a39-bc18-b22817834498`
- `dfb866d9-19f7-d83b-bab7-95bbc47021df`
- `5895be4b-453f-b13c-a1ce-2d4b76574b4d`

### Grand Slam of Darts — 3

Organization: PDC / WDF.

- `623dfb44-ec21-403b-9374-7d5dc00fc80f`: Viaplay starts at 20:00; PDC and TV.nu list
  20:30, so exact session mapping remains unresolved.
- `de148822-348c-7530-8184-3744ba8efb51` and
  `6b67db15-d728-7b38-97b4-d0abeca27668`: Monday match coverage depends on Anton
  Östlund's Saturday result.

### T100 Triathlon World Tour — 2

Organization: T100 Triathlon World Tour.

Four wildcard entries remain unnamed, so the Swedish participant mapping is uncertain.

- `4a09555c-a465-723f-a79a-ca449d234dc2`
- `fb15c1c6-6162-0a37-8621-82077484f1d8`

### Cycling — 6

Organization: Cycling.

Complete current start lists were unavailable to confirm Swedish participation.

- `48b30142-56e0-0d38-95a6-d130f104ee66`
- `daf54d93-a7c1-d73b-bf27-942f585dfdfd`
- `0c97916b-3505-093d-beec-26b1f21afeef`
- `e95f0a0e-e407-093b-a0d6-8ec1d8ea3611`
- `b1e783ae-93bc-453b-9b8e-50998c9b286a`
- `b0c6af74-573d-9c3b-a114-a1ad0d7cb343`

### Women's Basketball Euro qualifier — 3

Organization: Basketlandslaget (dam).

The match-day roster was unpublished, so participation remains uncertain.

- `f3569876-e1a0-da37-9d70-b55cfb595f43`
- `6e42c75f-d0b7-1934-9cc6-b0ba283e0977`
- `50bd5925-7c2f-0535-a2b5-8cb01461f23b`

## Final checks

- The 32 starting IDs were audited individually: 32 found, 4 processed, 28 unresolved,
  0 rejected, and 0 anomalies.
- Public verification used only `https://sesport.se/?date=2026-11-14` and
  `https://sesport.se/?date=2026-11-15`.
- `https://sesport.se/Admin/Dashboard` reported `No upcoming activity issues.`
- A first guarded Rotax insert rolled back when the generated participant-type column was
  detected. The corrected transaction committed; readback confirmed the activities,
  participants, sources, broadcast links, and statuses.
- No code, assets, schema, configuration, or deployment were changed. No tests or builds
  were run.
- Follow-up limit: direct checks for the Sebastian and Vigor Rotax profile pages were
  rate-limited with HTTP 429; the public page displays person-specific profile links.
