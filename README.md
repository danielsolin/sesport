# SE Sport

SE Sport helps users discover country-relevant international sport.

It is for users whose main sports interest is not one traditional club or team.
The user's "team" is instead a country, and that country is represented by athletes,
national teams, clubs, coaches, or people inside foreign teams.

The first configured country is Sweden, but the model must work for any country.

Example 1: "Team Sweden" can be represented by Armand Duplantis in a Diamond League
event.  
Example 2: "Team Sweden" can be represented by New York Rangers if at least one Swedish
player is meaningfully involved.  
Example 3: "Team Sweden" can be represented by IF Elfsborg if the club is competing
on an international level, like the UEFA Europa League.  

## Current Launch Goal

SE Sport must be live at `www.sesport.se` on June 14, 2026, ahead of Sweden's
first FIFA World Cup match on June 15, 2026.

The launch target is intentionally manual-first. The priority is to ship a
reliable administration interface for manually creating, editing, reviewing,
and publishing country-relevant activities, and a public site that presents
that curated activity data clearly to users.

Automation remains part of the long-term product direction, but it is secondary
for this launch. Any source imports, AI-assisted discovery, proposal generation,
or deduplication completed before launch should support the manual workflow
rather than block the public release.

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
