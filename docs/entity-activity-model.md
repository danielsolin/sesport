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
- Stockholm Marathon
- Vasaloppet

Entities should be relatively stable. The entity watchlist may change when new
athletes, teams, events, or groups become relevant, but it should not change
hour by hour.

## Entity Types

Initial entity types might include:

- `Person`
- `NationalTeam`
- `Club`
- `RecurringEvent`
- `Pair`
- `Organization`
- `Other`

Foreign clubs or teams are relationship targets, not country-relevant entities.
For example, Viktor Gyokeres is a stable Sweden-relevant entity. Arsenal FC is a
club he may have a current `PlaysFor` relationship with. Arsenal FC should not
be modeled as a stable Sweden-relevant entity because of that relationship.

A recurring event can be an entity when the event itself is a stable thing worth
tracking for a country, not merely one activity instance. Examples include
Stockholm Marathon and Vasaloppet.

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
- other international sport-related occurrence

A sports event is one type of activity. It should not be treated as the root of
the whole domain model.

A recurring event entity may produce one or more activity instances over time.
For example, Vasaloppet is a tracked entity, while a specific edition or race day
is an activity related to that entity.

## Entity Relationship

Tracked entities can have relationships to other entities or external sporting
contexts.

Examples:

- a person plays for a foreign club;
- a person coaches a foreign team;
- a person competes on an international tour;
- a Swedish club participates in a European competition;
- a recurring event belongs to an international series.

These relationships can change over time and must not be confused with stable
country relevance.

## Entity Activity Link

Activities are relevant because they are connected to tracked entities.

The link between an entity and an activity should explain the role the entity
has in that activity.

Examples:

- Armand Duplantis competes in a Diamond League event.
- William Karlsson plays for Vegas Golden Knights in an NHL game.
- Tre Kronor participates in an IIHF World Championship game.
- A Swedish football club plays in a European club qualifier.
- Oliver Solberg is competes in a WRC event.
- Vasaloppet has a race with international elite participation.

## Country Relevance

Country relevance should normally be explained through the tracked entity.

The system should be able to answer:

```text
Why is this activity relevant to this country?
```

A good answer names the relevant entity and explains the connection.

Examples:

```text
This activity is relevant to Sweden because William Karlsson, a Swedish ice
hockey player, is connected to one of the participating teams.
```

```text
This activity is relevant to Sweden because Vasaloppet is a Swedish recurring
sports event with major Swedish sporting and public interest.
```

A tracked entity can be relevant to a country for different reasons. Initial
country relevance reasons include:

- the entity is a person with that country's nationality or sporting identity;
- the entity is a national team representing that country;
- the entity is a club or organization based in that country;
- the entity is a recurring event based in that country with major sporting or
  public interest for that country.

A foreign club or team must not be used as the stable country-relevant entity
only because it currently has a person from the selected country. Track the
person as the country-relevant entity. Store the foreign club or team as a
relationship target with validity dates and evidence.

For recurring event entities, country relevance may come from both origin and
interest. Vasaloppet belongs to Sweden because it is Swedish and creates major
sporting interest for Swedish users. 
