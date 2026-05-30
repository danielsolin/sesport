# SE Sport

SE Sport helps users discover country-relevant international sport.

It is for users whose main sports interest is not one traditional club or team.
The user's "team" is instead a country, and that country is represented by athletes,
national teams, clubs, coaches, or people inside foreign teams.

Example 1: "Team Sweden" can be part of a New York Rangers game if Swedish players
are meaningfully involved.  
Example 2: "Team Sweden" can be represented by IF Elfsborg if the club is competing
on an international level, like the UEFA Europa League.  
Example 3: "Team Sweden" can be represented by Armand Duplantis in a Diamond League
event.

The first configured country is Sweden, but the model must work for any
country.

## Core Concept

The project starts with entities, not events.

1. Identify sport-related entities that are relevant to a country.
2. Collect activities related to those entities.
3. Store the result in a normalized database.

Examples of Swedish entities:

- Ebba Andersson (athlete)
- William Karlsson (athlete)
- Tre Kronor (national team)
- Sweden Women's National Football Team (national team)
- Åhman/Hellvig (beach volleyball pair)

An activity can be a match, race, tournament, stage, championship, qualification event,
roster announcement, or another international sport-related occurrence.

See `docs/product-goal.md` for product scope.  
See `docs/source-strategy.md` for collection strategy.  
See `docs/use-cases.md` for example queries.  
