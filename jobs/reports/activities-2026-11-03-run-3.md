# Activity Processing Report — 2026-11-03 (run 3)

Started: 2026-11-03 12:10:04 UTC.
Completed: 2026-11-03 12:31 UTC.

## Final reconciliation

The starting snapshot contained 27 visible, unprocessed broadcasts. Exact-ID readback found 12
Rejected, 1 Processed, and 14 Unresolved; no IDs are missing or invalid.
Rejected feeds are hidden only. The Processed feed is linked to a Published activity, marked
processed, and hidden. All 14 Unresolved feeds remain visible and unprocessed, have an
organization marker, and have typed evidence.

| Outcome | Count | Final state |
| --- | ---: | --- |
| Rejected | 12 | Hidden, unprocessed, unlinked |
| Processed | 1 | Linked, processed, hidden |
| Unresolved | 14 | Visible, unprocessed, unlinked, organization set |

## Activity created

Created `74329547-a61e-49b7-9717-81e63b6d44b4` in existing group
`2d15029c-31a8-4fdf-abca-49d973913afb`, Ungdoms-OS Dakar. The group title, date span, and
public-date mode remain unchanged.
Felus Ding – Ioannis Sarougiannidis — 3 Nov, 16:15–17:15 Europe/Stockholm; Match; Published; SVT
Play; Olympic Games organization.
The official Dakar schedule lists the Group F match at 15:15 Dakar time, which is 16:15 in
Stockholm. The source does not give an end time, so 17:15 is a generous 60-minute estimate
within the 16:10–18:00 SVT feed window.
Linked broadcast `1370fac2-8124-8c33-9fab-3a75739e759f`; its organization is Olympic Games.
Readback confirms the broadcast is linked, processed, and hidden.
Created Person `65370cd9-49ba-4fab-a001-8ea0a951d59a` with a dedicated SOK profile, verified
birthdate 2009-02-23, male gender, Swedish sporting identity, and tier_3 priority. Added
formative Club `b8ec94f4-b56f-4233-83b7-e61e728f9dec` and current Team
`866da499-d14d-48ba-a759-9ffddeff2669`. The official 2026/27 roster lists Felus.
Created active participant link `bcc2e334-b3ed-4996-a416-11898119f3fd`, with Olympic Games
context, plus person-to-club link `8630830f-8e82-4f71-a889-272d253d9d7f` and person-to-team link
`b9dfa395-2c1c-4ec0-ac67-f25ef8f231dd`. No group or existing activity was changed.
The sports reference table has no badminton ID. The new activity, Person, Club, and Team use the
existing `multi-sport` ID to satisfy the current catalog and match the YOG group.

## Rejected broadcasts

The reason applies to every ID in each group. Rejected rows have no activity link.

### Draw and studio programming (2)

- `1b198961-3c7a-fc3d-801a-77ce2afd6aa4` — UEFA Nations League draw programme.
- `8cec7df9-4621-fd32-bf07-2ee638053156` — squad announcement and studio analysis.

### Domestic Allsvenskan fixtures (6)

- `aa15990b-d9bc-7130-9754-b7e53762693f`
- `5c167390-f43b-c03f-8be2-06a7633c5fe2`
- `bd7cf3e1-ad7f-763d-b1dc-80f81b33d738`
- `05d4be6a-c24a-2239-aa87-c48816cdad87`
- `92c1eb1f-2697-9c34-951e-17fd132268e7`
- `fc79e4d1-7389-b63d-87e9-d71d6be92552`

### Foreign-team NHL fixtures (2)

- `18ea9d9f-d9ea-1c30-beb2-63da08499544` — Vancouver–Philadelphia.
- `df9dd127-8680-6830-8434-412fb4fd723f` — San Jose–NY Islanders.

### Champion of Champions snooker (2)

The complete 16-player draw confirms no Swedish entrants. Added a new ParticipationEvidence
source to each broadcast, using SnookerHQ’s 3 Nov draw.
- `aff4d195-8075-4533-9586-e92f046bd96a`
- `0ec926f3-2aab-7f3e-8d6b-bd322c02d6be`

## Unresolved broadcasts

All IDs below remain visible and unprocessed with their known organization and at least one
typed ActivityEvidence or ParticipationEvidence source. No complete current list settled Swedish
participation.

### Aramco China Championship — Ladies European Tour (4)

The official event page and headliner article do not provide the complete current field.
- `5d287d00-7eb9-333b-b181-7eee04fe29b6`
- `dd6bcc69-cc2e-5530-ac2c-1abd0cd13f68`
- `a21dad6c-4781-d63f-9b9a-dd539edd8560`
- `cd553543-4177-c539-9762-120f1123f589`

### Superprestige Middelkerke — Cycling (4)

Flanders Classics and the official event calendar confirm the event but publish no complete
entry list.
- `43de7f25-89ea-de35-876d-2b92d99bbdf1`
- `126fde57-e4c4-2735-88bd-b38a7df0c20e`
- `99757053-53ba-d236-97b6-117bcbe6ef5d`
- `f532a367-bab2-ee34-a267-88bb003e39fb`

### Singapore Criterium — Cycling (1)

The organizer lists selected riders, not the full professional field.
- `fd21bf81-225f-a03e-b89f-5c077f8f8368`

### Brazil Grand Prix FP1 — Formula One World Championship (4)

The official 2026 driver list and named rookie announcements do not establish the full Brazil
session lineup.
- `af0d3a4a-f7a0-2b3e-a272-6073f67fee67`
- `d11fa2ec-c16f-293d-b360-791e2eae3e7a`
- `dbe441a5-996f-4530-b047-d8fc7ee89f1b`
- `eeb3e7c1-1f08-5638-ae04-e64c4274620d`

### Cup of China gala — International Skating Union (1)

Official event and Grand Prix entries do not establish the gala lineup before event results.
- `ae578af0-55ca-2235-958f-8e62b5d8017c`

## Public and dashboard verification

All pages were retrieved from `https://sesport.se` and returned HTTP 200.

| Date | Before | After | Explanation |
| --- | ---: | ---: | --- |
| 3 Nov | 2 | 2 | Added one row to the existing grouped YOG card |
| 4 Nov | 1 | 1 | Unchanged WTT Champions grouped card |

The new row shows 16:15–17:15, the match title, direct SVT Play link, Felus Ding, age 17, LUGI
badminton, Swedish flag, and working SOK profile. The SVT program, SOK profile, Badminton
Federation roster, LUGI site, and Swedish flag asset returned HTTP 200.
The grouped YOG card retains the existing equestrian activity’s sport icon and summary while
displaying the badminton row. The row is accurate; mixed-sport card presentation needs code work
outside this job’s application-data scope.
Authenticated Admin Dashboard returned HTTP 200. “Activities needing attention” reports no
upcoming activity issues, and there are no open todos.

## Evidence saved

ActivityEvidence: official Dakar competition schedule, CNOSF badminton results, and SVT guide.
ParticipationEvidence: CNOSF results and SOK’s Dakar 2026 squad announcement. StreamLink: the
direct SVT Play program page.
PersonFacts: SOK profile and Badminton Federation 2026/27 roster. Bio: official LUGI badminton
club site and Badmintonligan roster. ParticipationEvidence for both snooker feeds records the
complete draw. Existing typed research sources remain on the 14 unresolved feeds.

  No code, assets, schema, configuration, or deployment changes were made. No automated tests
  were run.
