# Job Instruction: Update Broadcast Titles and Descriptions

## Objective
Update the `title` and `description` fields in the `broadcasts` table to
align with the "sesport" editorial standards. The goal is to provide a
mobile-first, high-impact viewing experience for users interested in Swedish
athletes in international sports.

---

## Core Principles

### 1. Mobile-First (Portrait Mode)
Users view these updates primarily on mobile devices in portrait
orientation. Titles must be extremely compact to avoid truncation.
- **Use Abbreviations:** e.g., "Grand Prix" $\rightarrow$ "GP".
- **Minimize Length:** Remove any non-essential words.

### 2. The "sesport" Lens
The focus is on "Who is playing?" and "What is happening?".
- **Prioritize Matchups:** For sports involving direct competition (Tennis,
  Football, etc.), the names of the participants are the most important
  element.
- **Identify Segments:** For racing or multi-part events (F1, Motorsport),
  the specific segment (Qualifying, Race, Sprint) is crucial.
- **Eliminate Redundancy:** Remove general sport names (e.g., "Formel 1",
  "Tennis", "Ishockey") because the context is provided by the
  organization/category.

### 3. Information Hierarchy
- **Title:** The "Hook" (compact, high impact).
- **Description:** The "Context" (details, tournament info, and location).

---

## Transformation Rules

### 1. `broadcast.title`
The title must follow one of two patterns:

#### Pattern A: Matchup (e.g., Tennis, Football, etc.)
Use when the broadcast is a confrontation between two entities.
- **Format:** `[Participant A] - [Participant B]`
- **Example:** `Andersson - Smith` (instead of "US Open, Singel, 4:e
  omgången, Andersson vs Smith")

#### Pattern B: Segment (e.g., F1, Golf, etc.)
Use when the broadcast is a specific part or stage of a larger event.
- **Format:** `[Event] [Short Name/Identifier]: [Moment]`
- **Example:** `Italien GP: Kval` (instead of "Formel 1, Italiens Grand Prix,
  Kval")
- **Example:** `US Open: Dag 1` (instead of "US Open, Dag 1")
- **Example:** `British Open: Dag 4`

### 2. `broadcast.description`
The description provides additional context and location.
- **Format:** `[Context (only if not in title)]. Från [Location][, [Country] 
(only if not obvious from title)].`
- **Strict Redundancy Rule:** Do not repeat information present in the title. 
If the title already identifies the event, the segment, or the country (e.g., 
"Italien GP" implies Italy), omit that information from the description.
- **The Location Segment:** The part `Från [Location]` must **always** be 
present.
- **The Country Rule:** Include `[Country]` **only if**:
    1. The country is **not** implied by the title.
    2. **AND** the `[Location]` is not so globally famous that the country
       is obvious.
- **The Granularity Rule:** Avoid redundancy. If the `title` already contains
  the primary venue (e.g., a track or stadium name), use a more granular
  location (e.g., the city or town) in the `description` instead.

---

## Examples

**Example 1 (Tennis):**
- **Original Title:** `US Open, Singel | 4:e omgången`
- **Original Desc:** `Upplev spänningen... Flushing Meadows, USA.`
- **New Title:** `Andersson - Smith`
- **New Desc:** `US Open, 4:e omgången. Från Flushing Meadows, USA.`

**Example 2 (F1):**
- **Original Title:** `Formel 1, Italiens Grand Prix`
- **Original Desc:** `Kommentering: Janne Blomqvist... Autodromo Nazionale 
Monza.`
- **New Title:** `Italien GP: Race`
- **New Desc:** `Från Autodromo Nazionale Monza.`

**Example 3 (Football):**
- **Original Title:** `Liverpool - Atlético Madrid`
- **Original Desc:** `UEFA Champions League | Plats: Anfield | Omgång 1`
- **New Title:** `Liverpool - Atlético Madrid`
- **New Desc:** `UEFA Champions League, Omgång 1. Från Anfield.`

**Example 4 (Football):**
- **Original Title:** `Frosinone Calcio - Venezia FC`
- **Original Desc:** `Fotboll från Stadio Benito Stirpe... Serie A, Omgång 3.`
- **New Title:** `Frosinone - Venezia`
- **New Desc:** `Serie A, Omgång 3. Från Stadio Benito Stirpe.`

**Example 5 (Granularity):**
- **Original Title:** `GT4 Germany, Sachsenring, Race 1`
- **Original Desc:** `GT4 racing in Germany.`
- **New Title:** `Sachsenring: Race 1`
- **New Desc:** `GT4 Germany. Från Hohenstein-Ernstthal.`

---

## Execution Workflow (The Loop)

The agent must perform the following steps in a continuous loop until no more 
unprocessed broadcasts are found for the specified date:

1. **Fetch Targets:** Call `db_get_broadcast` for the target date.
2. **Check Termination:** If `found` is `false`, the job is complete. Exit.
3. **Analyze:** Read the current `title` and `description`. Compare them 
against the **Transformation Rules** above.
4. **Apply:** Call `db_update_broadcast` with the new `title` and `description`.
5. **Repeat:** Immediately return to Step 1.

### Input Requirements

- **Target Date:** The operator **must** provide a specific target date 
(YYYY-MM-DD) for the job. 
- **Missing Date:** If the operator issues the command without a date, the 
agent **must not** proceed or assume "today". The agent must stop and 
explicitly ask the operator for the missing date.

### Error Handling

- **Missing Date:** Stop and ask for the date.
- **Update Failure:** If an update fails, report the error and proceed to the 
next broadcast to ensure the loop continues.
