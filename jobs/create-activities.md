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
3. Hide listings that do not communicate what will be shown. Judge the title
   together with its categories, description, channel, and linked event context.
   A bare title such as `EM` with no sport or event category is junk. A day or
   round label such as `Dag 2` with only a broad category such as `Golf` is also
   junk when no other metadata identifies the competition. Generic placeholder
   descriptions do not identify an event. Keep a short title when other fields
   make the event clear.
4. Generic highlights, recaps, and studio programmes are not specific events
   to convert. Hide them when the listing clearly identifies them as such.
5. Do not treat a missing participant name or missing Swedish team name as
   proof that an individual competition has no Swedish participant. Leave
   uncertain broadcasts visible and unprocessed for later review.
6. No web research is needed to hide clearly out-of-scope broadcasts. Do not
   use external research in this step to resolve uncertain participation.
7. Before writing, select the exact broadcast IDs to hide. Update only those
   rows; do not change their text or mark them processed. Confirm the affected
   IDs and report each reason.

Examples:

- Hide a Lithuania–Andorra match because neither national team is Swedish.
- Hide an NHL fixture between two North American clubs, regardless of whether
  Swedish athletes may play for those clubs.
- Hide a bare `EM` listing with no sport or event category.
- Hide a `Dag 2` golf listing when no other metadata identifies the tournament.
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
   the `decide-swedish-participation` AI job. Start with a broad search for
   Swedish participants in the sport or series for the current season. For
   established series such as Formula 1, MotoGP/Moto2/Moto3, and professional
   snooker, a reliable sport-level finding applies to all visible broadcasts
   for that series and season. If it establishes there are no Swedish
   participants, reject and hide those broadcasts without checking
   event-specific entry lists, draws, or participant lists. Record the broad
   search and its supporting source. An unanswered query or a missing name in
   one incomplete source is not proof by itself.
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
   following existing activity data and publication conventions. All daily
   activities from the same competition must share one activity group. Reuse
   an existing group or create one spanning the competition dates, then attach
   every related daily activity to that group.
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
3. Ensure every person participant linked to an activity created or updated in
   Step 3 has a dedicated person profile or athlete information page in
   `entities.url`, including inactive participants. Prefer a profile from the
   person's federation, club, team, or another relevant organization. If none
   is available, use a dedicated Wikipedia page for a sufficiently known
   person, or the person's own website or sport-related social media page.
   The page's primary purpose must be to provide information about the person
   or their athletic career. Do not use news stories, signing or transfer
   announcements, interviews, match reports, or other articles that merely
   mention or discuss the person. Verify that the page is about the correct
   person and is in Swedish or English. Verify that the public participant
   name links to this URL. Do not use a generic homepage or search results
   page. If no suitable URL can be found, report the participant rather than
   inventing a URL.
4. If a birthdate or club is missing, research authoritative athlete,
   federation, or club sources. Use the actual formative club when verified;
   otherwise use the earliest documented club in the athlete's development.
   Never use the current club for this field; current club affiliation belongs
   in the person's Team link. Reuse the correct existing Club entity when
   available. Do not describe a club as formative without supporting evidence.
   If evidence is conflicting or insufficient, leave the fact unchanged and
   report it for review rather than using the current club as a fallback.
5. Save source URLs for verified person facts as `PersonFacts` evidence. Make
   data changes only in a guarded manual `psql` transaction. Create a Club
   entity only when no matching entity exists, then link it to the person.
6. For team-sport matches, verify the team-based flag beside each applicable
   participant. Follow the existing rules for foreign teams and national-team
   participants. Check linked Team countries before changing the query; when
   several linked teams share one known country, that country can be used.
   Do not infer a rendering bug from a participant's row position alone.
7. Confirm each displayed flag has a supported country ID and a matching SVG
   asset. If either is missing, make the focused code or asset change needed,
   then verify the rendered page and that the asset loads successfully.
8. Verify every linked person has an `entities.url`, no displayed participant
   is missing an age, club, or URL, and applicable team flags render. Check the
   hosted page separately from local rendering. Report any difference; do not
   describe a local-only change as fixed on the hosted site.

## Final Visible Broadcast Check

Before reporting completion, inspect every broadcast whose `hidden_at` is unset.
This final check also covers broadcasts imported after the run's initial
selection; assign their organization marker without otherwise processing them.

1. Ensure every visible broadcast is assigned to its relevant event or
   competition organization. This organization marker shows that the broadcast
   was considered and remains under investigation; it does not confirm Swedish
   participation or mark the broadcast as processed.
2. If available metadata cannot identify what the broadcast covers or its
   relevant organization, hide it under Step 1's junk-listing rule. Do not leave
   a visible broadcast unassigned.
3. Keep unresolved broadcasts unprocessed. Report their IDs and assigned
   organizations, and confirm that no visible broadcast is unassigned.
