# Activity Processing Report — 2026-11-03

Started: 2026-11-03 00:40:16 UTC.
Completed: 2026-11-03 01:24 UTC.

## Final reconciliation

The 93 starting broadcast IDs match the final database audit exactly. All final states and links
passed readback checks:

| Outcome | Count | Verified state |
| --- | ---: | --- |
| Rejected | 40 | Hidden, unprocessed, no activity link |
| Processed | 11 | Linked, processed, hidden |
| Unresolved | 42 | Visible, unprocessed, no activity link, organization set |

No snapshot IDs were missing or added. No state, link-count, or organization-marker invariant
failed. Unresolved rows have an identifiable organization in the database.

## Activity updates

### NOCCO Hockey Games — Czechia vs Sweden

- `cf7dbeab-c984-193c-8552-7f25ba8c3dbf` →
  `f026a196-d9d7-5212-b1c5-ae4d5b405c64`, 5 Nov, 18:50–20:00, SVT2.
- `8d8c489b-390a-ed3e-b659-d95f19372b37` →
  `3ccad4f9-8d21-5fa9-9e1f-1623420e70e5`, 5 Nov, 20:00–20:30, SVT24.
- Both use the Swedish men’s hockey organization and are processed and hidden.
- Each activity has 24 active players with birthdates, profiles, club links, and team links.
- The public card combines the feed windows into 17:50–20:30 and lists all channels;
  it does not display the two narrower TV segments separately.

### Dam-VM 2026

- `1f34e12a-32a4-3c3f-88c6-e7012ea5038e` →
  `80cbf85a-6a93-4f3d-838b-1a55eee1149b`, 6 Nov, 14:50–17:20.
- `2c10fd41-82bf-a13a-90b5-8588e31725a7` →
  `fab39b97-53b8-4248-91b9-f18af4a4bcdc`, 8 Nov, 14:50–17:30.
- Both use the Swedish women’s team organization and have direct SVT Play links.
- Each uses the confirmed 24-player Swedish roster. TV.nu supports the scheduled times;
  the existing 8 Nov 14:50 SVT1 coverage remains intact.

### Youth Olympic Games — Dakar

- Retained `98739524-de88-487a-a1c5-ba8007b76ccf` for 5 Nov, 09:55–12:00,
  and `e5930a14-a777-4d17-a137-2d7d6a62006b` for 6 Nov, 09:55–13:00.
- Linked new feeds `633a2be0-0b50-423d-be24-ed98a3acaf17` and
  `6a307967-7cfe-7636-bd96-a1854a97a60d`; both are processed and hidden.
- Merged duplicate activities `a39ba8ba-32b2-42e3-bbc9-e5aedb7b12bb` and
  `a12ec640-ce8b-4dc3-8345-dbc2c06dec87`. Moved two historical broadcast links and
  their activity evidence to the retained activities; deduplicated Valentina’s links.
- SVT confirms both schedules. The public cards show Valentina Jarl’s profile, age, and
  club. No sourced individual start time is required for equestrian jumping.

### WTT Champions Frankfurt

- Created group `4fc0a5ec-2386-41a6-881e-f469d2cf2995` and three published activities:
- `5e2ec03d-489e-48f8-af60-219cb286b486` — Elias Ranefur–Dang Qiu, 3 Nov, 18:30–19:30.
- `81010bd0-c9a3-4de2-9866-ad69cb24ba75` — Anton Källberg–Lim Jonghoon,
  4 Nov, 19:00–20:00.
- `eda8f5b2-7a0c-4626-9737-4c74e6c97f43` — Truls Möregårdh–Oh Junsung,
  5 Nov, 15:20–16:20.
- Linked feeds `c89a7293-4314-a132-8fb2-370b9d9513e8`,
  `5e5b87ef-4762-2533-9a4c-26ab7bd82334`, and
  `4cd3cef7-0f44-ca3d-af91-7b96318044ad`; each is processed and hidden.
- Added direct SVT Play event links. Match times and players are supported by SVT, SBTF,
  and TV.nu; profile, birthdate, club, and star facts were checked.

### Cup of China

- Created group `426381ef-58f0-425a-bf6a-7aa57b884f3b` and activity
  `239f82be-02d7-4c5e-b770-90a34084d356`: men’s free program, 8 Nov, 05:55–08:25.
- Linked feed `04c89583-dc5f-ea37-adea-4c7c0ded6b51`; it is processed and hidden.
- Added the direct Viaplay link and verified Andreas Nordebäck’s 2004-03-12 birthdate,
  ISU profile, formative club, and current club/team. ISU lists Stockholms Konståknings-
  klubb (SKK); SOK still lists Skating & Dance Academy. Both dated affiliations remain
  represented as formative and current facts, respectively.
- The public card shows Andreas’s age, formative club, Swedish flag, and stream link.
- The separate Cup of China gala feed remains unresolved; its organization is ISU.

### Abu Dhabi Aldar Championship

- Created activity `02cc2e9e-f220-4598-8f18-fa9656d27fb3` in existing group
  `3ab9fb18-485d-4199-84a7-1ab4aab7444d`: round 4, 8 Nov, 07:30–14:00.
- Linked feed `c6be1518-0cf0-5636-acc4-1a53fea24059`; it is processed and hidden.
- Added Joakim Lagergren and Mikael Lindberg, and saved the direct Viaplay event link.
  TV.nu supports the 07:30 segment start; the broadcast end is 14:00.
- The page retains the separate existing 08:30–14:30 Viaplay Sport/V Sport Golf window.
  Both schedule rows show the Swedish players, ages, clubs, and links.

## Rejected broadcasts

All listed feeds were hidden, left unprocessed, and have no activity link. The reason applies to
every ID in its group.

### Metadata excludes eligible Swedish participation (1)

Reason: The ATP Court 1 listing names only five non-Swedish matches.

- `8079b5ec-69bc-273a-bd32-8b74f02ffd82` — ATP TOUR: Paris Masters 1000

### Generic highlights (1)

Reason: The snooker listing is generic highlights without a live eligible segment.

- `291cae35-28ab-3b3f-b1c6-4676f86248a8` — Snooker: International Championship

### TOTO Japan Classic (4)

Reason: Complete official LPGA and JLPGA fields have no Swedish entrants.

- `09f61289-00b8-3d32-9c57-a156ea0fdbd1` — TOTO Japan Classic
- `31e2ade8-6abe-c535-a023-976b49cea45d` — TOTO Japan Classic
- `8a3547ea-de0e-4b37-afe5-994c73526c8d` — TOTO Japan Classic
- `f1e88788-32d9-403c-ac64-f2f37f038a00` — TOTO Japan Classic

### Qatar MotoGP (15)

Reason: Complete official Moto3, Moto2, and MotoGP entries have no Swedes.

- `0f02f35c-cb44-fb3f-aa3a-d5fe3677d3b4` — MotoGP, Qatars Grand Prix
- `0f49a5b9-7f17-cd3e-a980-0717ac0e9881` — MotoGP, Qatars Grand Prix
- `1194eb5e-c8d1-093f-9057-1a473a673a09` — Moto3, Qatars Grand Prix
- `1e906eb5-494b-5c38-b872-780427bf9e9a` — MotoGP, Qatars Grand Prix
- `1eef3169-b7d7-c93f-aba5-923bf6986fa5` — Moto2, Qatars Grand Prix
- `61fd5d13-6b63-b23b-aa71-772b5f67c2d5` — MotoGP, Qatars Grand Prix
- `64425664-0a4e-1930-9dbf-902d766b604d` — Moto3, Qatars Grand Prix
- `8564f437-0b7c-0e3e-9916-96a55bd1bbb5` — Moto2, Qatars Grand Prix
- `90a4adcd-9e1d-733b-9882-8d55eef2b4ee` — MotoGP, Qatars Grand Prix
- `b986da08-46fe-323d-96d6-d97f6cc5fe0f` — Moto2, Qatars Grand Prix
- `c3dc8d7d-a195-213f-ab23-fdc729ad0438` — Moto3, Qatars Grand Prix
- `d24d43e3-30bc-993b-8b66-d27a689f5da8` — Moto2, Qatars Grand Prix
- `ef6c5c08-aeb2-c834-a7cd-2df30f89f50e` — Moto3, Qatars Grand Prix
- `f40ac2dd-2810-f63f-a105-8afaf42c0e73` — Moto2, Qatars Grand Prix
- `f996ec1b-6a15-0738-9670-fda2cb94ed94` — Moto3, Qatars Grand Prix

### Formula One Brazil GP (18)

Reason: The complete 2026 driver list has no Swedish driver in these feeds.

- `0ba6090d-9d50-4a35-98dc-910bb6bbfc25` — Formel 1, Onboard
- `1301c974-7954-7a33-957b-7e4aa082d52d` — Formel 1, Track Positioning
- `4c430a41-0ebe-b136-8920-18afbe5e4d49` — Formel 1, Track Positioning
- `753a147d-df5d-cc35-b9b5-76215a8d57d8` — Formel 1, F1 Multiview
- `75d21d36-d552-7731-81b4-c83b47d84ded` — Formel 1, Timing
- `761de2b0-b4e2-4d3c-93c7-95461769b13d` — Formel 1, Timing
- `8a607425-89ae-7d36-8f40-793202d845c4` — Formel 1, Pitlane
- `8f13409f-c8e9-0e30-ac10-f3a7b593744c` — Formel 1, Onboard
- `907440c1-43fc-4232-96f6-0031f8f016fd` — Formel 1, Timing
- `9df9051a-f273-0d32-a5f3-1ade59e7fb98` — Formel 1, Track Positioning
- `a9c1cc50-4a8e-fb3a-84ba-23c949cdef3c` — Formel 1, Pitlane
- `c4983713-cdce-b438-bdde-10e372bfa41f` — Formel 1, Pitlane
- `da26a8e4-cbef-7739-9351-ed03d162b84a` — Formel 1, Pitlane
- `e80802b3-db25-7039-81c6-39ffb9dfd56b` — Formel 1, Track Positioning
- `ee2fb13f-7883-1f34-a225-f6791c3ccfb6` — Formel 1, Timing
- `f181f14a-a1d7-b837-99e1-bced972a6058` — Formel 1, Brasiliens Grand Prix
- `f2e401a7-8619-a639-847a-96c2a35259e1` — Formel 1, Onboard
- `fab7a9d3-7320-d43e-a900-bb99e7396606` — Formel 1, Onboard

### UFC Fight Night (1)

Reason: The complete official fight card has no Swedish participant.

- `7f3025b4-4420-1c39-be30-6ad559c7b7e8` — Cortes-Acosta vs Walker

## Unresolved broadcasts

All listed feeds remain visible and unprocessed, with no activity link. Reasons and
organizations are recorded below; markers are set on all 42 rows.

### ATP Tour (28)

Reason: Organization: ATP Tour. Swedish entrants are confirmed, but available schedules and
feeds cannot be mapped to exact Swedish matches. Paris Masters match mappings and the Stockholm
Open feed lineup remain unverified.

- `17afb9f1-2fe7-d93a-9746-88e67574b540` — Paris Masters (1000): huvudsändning
- `1a659cdc-7f42-a43c-849f-1bafaf79e9c5` — ATP TOUR: Paris Masters 1000
- `1d749c16-ebad-fb31-b326-983ff4cd6fd8` — Paris Masters (1000): Court 1
- `2206cf10-ba3e-ff30-ad67-fb13c5063d00` — ATP TOUR: Paris Masters 1000
- `22d960f2-8618-3135-bfea-bdf682b73abd` — Paris Masters (1000): kvartsfinaler
- `2b81c0a7-7426-883c-9811-514f4c4af36d` — Paris Masters (1000): huvudsändning
- `426f52c3-3f50-5f30-96b4-32a168ecc6b1` — Paris Masters (1000): Court Central
- `46e35311-4d1e-1f38-a945-c55e6d615665` — Paris Masters (1000): Court 1
- `48b37d60-9723-8b39-a04d-060317a9c61a` — Paris Masters (1000): huvudsändning
- `49af8fd5-9753-e03e-a3d6-9101a40a8ae3` — Paris Masters (1000): huvudsändning
- `53e3e171-dca8-8d35-a075-7ec3d0a473bc` — Paris Masters (1000): Court 1
- `5d49e34e-eb21-6030-bbd5-66cb37f0cb19` — Paris Masters (1000): Court Central
- `6f010bdd-4251-2d3d-b04b-db2abfb12a3b` — ATP TOUR: Paris Masters 1000
- `77864914-d88c-6a38-b494-c3542db4462c` — Paris Masters (1000): Court Central
- `90018c10-1100-fc3f-898c-2fd523c2b701` — ATP TOUR: Paris Masters 1000
- `96d21ab0-33a7-3235-918f-d0eace29e737` — Paris Masters (1000): semifinaler
- `a1119652-d972-6e32-bdcf-7d40e1b506d6` — Paris Masters (1000): huvudsändning
- `a2bf7396-9674-e038-a3ae-68f5913e4903` — Stockholm Open (250)
- `a3cdd690-f775-1331-99cf-40a7a81258bf` — ATP TOUR: Paris Masters 1000
- `b994d62b-dff6-443d-9a17-bb47a0f66c81` — Paris Masters (1000): huvudsändning
- `ba1f53ee-3598-0c39-802a-b79335e5573d` — ATP TOUR: Paris Masters 1000
- `bce5492e-10cd-3439-978e-1732e353bdf0` — ATP TOUR: Paris Masters 1000
- `cb46e96e-e191-1a34-863c-b1cdee2da473` — ATP TOUR: Stockholm Open 250
- `cff0b13d-5bea-5a34-826b-2f11811267c6` — ATP TOUR: Paris Masters 1000
- `d25d861b-8ab6-2638-93ce-c0117a3efae6` — Paris Masters (1000): final
- `dd0cec50-f2e1-603c-9992-071e67505add` — Paris Masters (1000): kvartsfinaler
- `e2231b3a-50fb-3e3c-8a97-05ea37b26fea` — ATP TOUR: Paris Masters 1000
- `f0a0e918-f503-6237-a4b3-ef3a0fa8e817` — ATP TOUR: Paris Masters 1000

### Cycling (5)

Reason: Organization: Cycling. No complete official start list confirms Swedish participation
for these events.

- `126fde57-e4c4-2735-88bd-b38a7df0c20e` — Superprestige - Middelkerke, Herrar
- `43de7f25-89ea-de35-876d-2b92d99bbdf1` — Superprestige - Middelkerke, Damer
- `99757053-53ba-d236-97b6-117bcbe6ef5d` — Superprestige - Middelkerke, Damer
- `f532a367-bab2-ee34-a267-88bb003e39fb` — MIDDELKERKE - Superprestige
- `fd21bf81-225f-a03e-b89f-5c077f8f8368` — Cykel Singapore, Herrar

### Aramco China Championship (4)

Reason: Organization: Ladies European Tour. The official event page does not publish a complete
current field. The missing marker on `cd553543-4177-c539-9762-120f1123f589` was set.

- `5d287d00-7eb9-333b-b181-7eee04fe29b6` — Aramco China Championship
- `a21dad6c-4781-d63f-9b9a-dd539edd8560` — Aramco China Championship
- `cd553543-4177-c539-9762-120f1123f589` — Aramco China Championship
- `dd6bcc69-cc2e-5530-ac2c-1abd0cd13f68` — Aramco China Championship

### Cup of China gala (1)

Reason: Organization: International Skating Union. No complete confirmed Swedish participant
list is available for the gala.

- `ae578af0-55ca-2235-958f-8e62b5d8017c` — Cup of China 2026

### Brazil GP FP1 alternate feeds (4)

Reason: Organization: Formula One World Championship. FP1 alternate feeds may include a Swedish
reserve driver; the full Sao Paulo FP1 participant list was not confirmed.

- `af0d3a4a-f7a0-2b3e-a272-6073f67fee67` — Formel 1, Onboard
- `d11fa2ec-c16f-293d-b360-791e2eae3e7a` — Formel 1, Track Positioning
- `dbe441a5-996f-4530-b047-d8fc7ee89f1b` — Formel 1, Timing
- `eeb3e7c1-1f08-5638-ae04-e64c4274620d` — Formel 1, Pitlane

## Public pages and dashboard

Verified pages on `https://sesport.se`:

| Date | Cards before | Cards after | Result |
| --- | ---: | ---: | --- |
| 3 Nov | 1 | 2 | WTT match added |
| 4 Nov | 0 | 1 | WTT match added |
| 5 Nov | 5 | 6 | WTT match added; hockey and YOG updated |
| 6 Nov | 4 | 4 | Dam-VM and YOG updated |
| 8 Nov | 7 | 8 | Cup card added; Abu segment added |

WTT titles, times, players, and streams render on 3–5 Nov. The corrected YOG times and Valentina
Jarl profile render on 5–6 Nov. The 8 Nov Cup and Abu cards show their expected times, Swedish
participants, flags, and streams.

The authenticated Admin Dashboard reported “No upcoming activity issues” and “No open todos.” No
dashboard corrections were needed.

## Evidence and scope

New typed source records saved during the run:

- `ActivityEvidence`: 11 activities, 4 activity groups.
- `ParticipationEvidence`: 5 activities, 2 activity groups, 86 broadcasts.
- `PersonFacts`: 2 entities. `StreamLink`: 8 activities.
- Two existing Aramco source tuples were reused.

Research sources include SVT, Swedish Ice Hockey Association, SBTF, TV.nu, ISU, SOK, DP World
Tour, FIA, Formula1.com, MotoGP, LPGA/JLPGA, UFC, ATP event pages, Singapore Criterium,
Superprestige, and Aramco Team Series. Complete no-Swede findings were saved as
`ParticipationEvidence`; event and schedule facts as `ActivityEvidence`; Andreas’s profile and
club facts as `PersonFacts`.

Key source pages, all saved in typed evidence records:

- [Swehockey roster](https://swehockey.se/toppnyheter/landslagstrupper-oktobernovember/)
- [SVT WTT schedule](https://www.svt.se/sport/bordtennis/sa-sander-svt-bordtennis-2026)
- [SVT Youth Olympic guide](https://www.svt.se/sport/beachvolleyboll/guide-ungdoms-os-2026)
- [TV.nu Cup of China](https://www.tv.nu/s/p_1634794_20261108)
- [TV.nu Abu Dhabi](https://www.tv.nu/s/p_1634798_20261108)
- [ISU: Andreas Nordebäck](https://isu-skating.com/en/figure-skating/skaters/andreas-nordeback/)
- [ISU Cup schedule PDF](https://results.isu.org/schedules/gpchn2026_ColouredTimeSchedule.pdf)
- [TOTO Japan Classic field](https://toto-japan-classic.com/en/players.html)
- [MotoGP rider roster](https://www.motogp.com/en/riders/)
- [Aramco China Championship](https://www.aramcoteamseries.com/en-us/aramco-china-championship/)
- [Superprestige calendar](https://www.superprestigecyclocross.be/nl/kalender/2026-2027/middelkerke)
- [Singapore Criterium programme](https://singaporecriterium.com/location-copy/)

SBTF, DP World Tour, FIA, Formula1.com, UFC, and ATP pages are also saved as sources.

No code, assets, schema, configuration, or deployment changes were made.
