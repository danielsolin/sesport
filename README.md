# SE Sport

SE Sport helps users discover country-relevant international sport.

The first configured country is Sweden, but the model must work for any
country.

## Core Concept

The project starts with entities, not events.

1. Identify sport-related entities that are relevant to a country.
2. Collect activities related to those entities.
3. Store the result in a normalized database.

Examples of Swedish entities:

- Ebba Andersson
- William Karlsson
- Tre Kronor
- Sweden Women's National Football Team
- The Solberg family

An activity can be a match, race, tournament, stage, championship,
qualification event, roster announcement, or another international
sport-related occurrence.

The project is not intended to be a general domestic sports calendar.

See `docs/product-goal.md` for product scope.
See `docs/source-strategy.md` for collection strategy.
See `docs/use-cases.md` for example queries.