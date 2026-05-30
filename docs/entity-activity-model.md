# Entity-Activity Model

SE Sport is entity-first.

The system should not start by collecting every possible sports event and then
try to decide whether each event is relevant to a country.

Instead, the system should:

1. Identify sport-related entities that are relevant to a selected country.
2. Collect activities related to those entities.
3. Store entities, activities, relationships, and evidence in normalized data.

## Entity

An entity is something worth tracking because it has a meaningful connection to
the selected country and an international sport-related role.

Examples with Sweden as the selected country:

- Armand Duplantis
- Ebba Andersson
- William Karlsson
- Tre Kronor
- Sweden Women's National Football Team
- The Solberg family

Entities should be relatively stable. The entity watchlist may change when new
athletes, teams, or groups become relevant, but it should not change hour by
hour.

## Entity Types

Initial entity types:

- `Person`
- `NationalTeam`
- `Club`
- `ForeignTeamWithCountryRelevance`
- `FamilyOrGroup`
- `Organization`
- `Other`

The list should stay broad enough to support different sports without making
sport-specific assumptions in the core model.

## Activity

An activity is something related to one or more tracked entities.

Examples:

- match
- race
- tournament
- stage
- championship
- qualification event
- roster announcement
- transfer
- ranking update
- injury update
- other international sport-related occurrence

A sports event is one type of activity. It should not be treated as the root of
the whole domain model.

## Entity Activity Link

Activities are relevant because they are connected to tracked entities.

The link between an entity and an activity should explain the role the entity
has in that activity.

Examples:

- Armand Duplantis competes in a Diamond League event.
- William Karlsson plays for Vegas Golden Knights in an NHL game.
- Tre Kronor participates in an IIHF World Championship game.
- A Swedish football club plays in a European club qualifier.
- The Solberg family is connected to a motorsport championship activity.

## Country Relevance

Country relevance should normally be explained through the tracked entity.

The system should be able to answer:

```text
Why is this activity relevant to this country?
```

A good answer names the relevant entity and explains the connection.

Example:

```text
This activity is relevant to Sweden because William Karlsson, a Swedish ice
hockey player, is connected to one of the participating teams.
```

## Relationship to the Older Event-First Model

Earlier parts of the codebase were designed around an event-first model:

```text
collect event -> determine country connection
```

The new direction is:

```text
track entity -> collect related activity
```

Older event, participant, roster, import, and source concepts may still be
useful, but they should support the entity-first model rather than define the
product model.

Do not delete the older model until the replacement is implemented and the data
migration path is understood.

## Implementation Direction

The next implementation step should introduce minimal domain types for:

- `TrackedEntity`
- `TrackedEntityType`
- `EntityCountryRelevance`
- `Activity`
- `ActivityType`
- `EntityActivityLink`

The first version can keep these types simple. The goal is to give future code a
correct center of gravity, not to model every sport-specific detail immediately.