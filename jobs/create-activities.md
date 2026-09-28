# Job Instruction: Create Activities from Broadcasts

## Objective

Review broadcasts that are visible when the run starts and create activities
only for events that match the participation scope below. Complete Step 1
before creating any activities.

## Broadcast Selection

The operator does not supply a target date. At the start of each run, include
every broadcast whose `hidden_at` is unset, regardless of its date or
`processed_at` value. Treat this set as the run's input. Broadcasts imported
after selection belong to the next run.

## Participation Scope

An event is in scope only when Swedish representation is established in one
of these ways:

- A Swedish athlete competes individually in an international competition.
- A Swedish national team competes in an international competition.
- A Swedish club competes in an international competition against a
  non-Swedish opponent.

Swedish nationality alone does not make an athlete relevant when they compete
for a foreign club or team. Matches between foreign teams are out of scope,
even if Swedish athletes may be on their rosters. Domestic competitions are
also out of scope.

## Step 1: Hide Broadcasts Outside Scope

Do this before creating or updating activities.

1. Read the run's selected broadcasts whose `hidden_at` is still unset. Inspect
   their title, description, categories, channel, linked entities, and
   `processed_at` state.
2. Hide a broadcast only when the available information clearly establishes
   that it is outside scope. Examples include a fixture between foreign
   national teams, a fixture between foreign clubs, or a studio programme about
   domestic competitions.
3. Generic highlights, recaps, and studio programmes are not specific events
   to convert. Hide them when the listing clearly identifies them as such.
4. Do not treat a missing participant name or missing Swedish team name as
   proof that an individual competition has no Swedish participant. Leave
   uncertain broadcasts visible and unprocessed for later review.
5. No web research is needed to hide clearly out-of-scope broadcasts. Do not
   use external research in this step to resolve uncertain participation.
6. Before writing, select the exact broadcast IDs to hide. Update only those
   rows; do not change their text or mark them processed. Confirm the affected
   IDs and report each reason.

Examples:

- Hide a Lithuania–Andorra match because neither national team is Swedish.
- Hide an NHL fixture between two North American clubs, regardless of whether
  Swedish athletes may play for those clubs.
- Keep a Swedish national team match against another country in the queue.
- Keep a Swedish club's international match against a non-Swedish club in the
  queue.
- Leave a general tennis tournament listing for later review if the available
  information does not establish whether a Swedish player is competing.

## Step 2: Research and Decide Swedish Participation

Start this step only after Step 1 is complete. Work only on selected broadcasts
that remain visible. For a broadcast with `processed_at` set, first inspect its
existing activity and links. Reuse a matching activity and verify or finalize
its data, adding only missing links. If no matching activity exists or records
conflict, leave the broadcast unchanged and report it rather than creating a
duplicate. Step 1 has already hidden broadcasts that were clearly outside the
participation scope.

1. Group broadcasts that cover the same specific event or match, including
   listings on multiple channels, so each event is researched once.
   Associate generic or event-wide listings with an event only when their
   source metadata, channel, and timing point to one event and no competing
   event fits. Otherwise leave them visible and unprocessed.
2. Manually research whether Swedish participation is established. Do not run
   the `decide-swedish-participation` AI job. Prefer current official entry
   lists, start lists, rosters, and event or team sources. A partial article,
   search result, or missing name is not proof that no Swedish participant is
   involved.
3. Apply the participation scope above to the evidence:
   - If Swedish representation is confirmed, record the event and exact
     broadcast IDs for Step 3. Do not create the activity yet.
   - If a complete authoritative source confirms there are no Swedish
     participants, reject the event and hide its broadcasts. Do not create an
     activity or mark the broadcasts processed.
   - If participation remains uncertain, leave the broadcast visible and
     unprocessed. Set its organization to the correct event organization as
     an investigation marker. Do not create an activity.
4. Before hiding rejected broadcasts, select and verify their exact IDs. Hide
   only those rows. Keep unresolved broadcasts visible and unprocessed.
5. Verify the classification, evidence, hidden IDs, and unresolved broadcasts.
   Report the confirmed events and their IDs for Step 3.

## Step 3: Create Activities and Finalize Broadcasts

Work only on the confirmed in-scope events and broadcast IDs recorded in
Step 2.

1. Review comparable activities to follow existing conventions. Check for an
   existing activity for each confirmed event before creating one. Reuse a
   matching activity to avoid duplicates. Create one activity per supported
   event-day or match, not one per channel listing.
2. Set the appropriate title, sport, activity type, date, broadcast times,
   time zone, channel, and organization. Save authoritative source URLs as
   activity evidence. Create activities in a guarded manual `psql` transaction,
   following existing activity data and publication conventions. For a
   multi-day event, create or reuse an activity group spanning the official
   event dates and attach the daily activity to it.
3. Link every Swedish participant confirmed in Step 2 to the activity. Create
   an active person-participant link only when one does not already exist, and
   set the event organization in that link's organization context. Verify the
   participant list.
4. Link every broadcast that covers the event to its activity. Verify that the
   activity and all broadcast links were saved successfully before continuing.
   Create only missing broadcast links.
5. Set each linked broadcast's organization to the activity's organization
   and mark it processed. Then hide those broadcasts. Update only the verified
   source broadcast IDs.
6. Verify activity details, linked broadcast IDs, matching organization, and
   hidden status. Report created activities and the IDs of linked broadcasts.

## Step 4: Verify Public Presentation

Start after Step 3 has finalized the activities and their broadcasts.

1. Open the public activity listing for each activity date using
   `/?date=YYYY-MM-DD`. Inspect the rendered participant lists for every
   activity created or updated in Step 3.
2. For every displayed person participant, verify that a birthdate is stored
   and that the public page displays an age. Verify that the person's formative
   club, or otherwise the earliest documented club, appears in the public club
   column. Include inactive participants when they are displayed.
3. If a birthdate or club is missing, research authoritative athlete,
   federation, or club sources. Use the actual formative club when verified;
   otherwise use the earliest documented club in the athlete's development.
   Never use the current club for this field; current club affiliation belongs
   in the person's Team link. Reuse the correct existing Club entity when
   available. Do not describe a club as formative without supporting evidence.
   If evidence is conflicting or insufficient, leave the fact unchanged and
   report it for review rather than using the current club as a fallback.
4. Save source URLs for verified person facts as `PersonFacts` evidence. Make
   data changes only in a guarded manual `psql` transaction. Create a Club
   entity only when no matching entity exists, then link it to the person.
5. For team-sport matches, verify the team-based flag beside each applicable
   participant. Follow the existing rules for foreign teams and national-team
   participants. Check linked Team countries before changing the query; when
   several linked teams share one known country, that country can be used.
   Do not infer a rendering bug from a participant's row position alone.
6. Confirm each displayed flag has a supported country ID and a matching SVG
   asset. If either is missing, make the focused code or asset change needed,
   then verify the rendered page and that the asset loads successfully.
7. Verify that no displayed participant is missing an age or club, and that
   applicable team flags render. Check the hosted page separately from local
   rendering. Report any difference; do not describe a local-only change as
   fixed on the hosted site.
