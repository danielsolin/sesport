# SE Sport

SE Sport is a country-based sports calendar and watchlist. It helps a user
find upcoming sports events where a selected country is relevant, across
sports, competitions, teams, and individual athletes.

The first configured country is Sweden, but Sweden is not a hardcoded product
rule. The same model should be able to support other countries later.

## Project Rules

No line in any text file should exceed 80 characters unless a longer line is
technically required for the file to work.

Indentation should use three spaces per level. Tabs should never be used for
indentation.

All project artifacts, documentation, code, UI text, schemas, and comments
should be written in English.

Multi-line C# declarations should place the closing parenthesis and semicolon
on their own line, aligned with the declaration that opened the parameter list.

## First Domain Slice

The first concrete event is Sweden vs Switzerland in the 2026 IIHF Ice Hockey
World Championship.

- Sport: Ice hockey
- Competition: 2026 IIHF Ice Hockey World Championship
- Event: Sweden vs Switzerland
- Stage: Quarter-final
- Start time: May 28, 2026, 20:20 Europe/Stockholm
- Relevance: Sweden participates as a national team

## Core Concepts

- `Country`: The country a user follows, such as Sweden.
- `Sport`: The sport an event belongs to, such as ice hockey.
- `Competition`: The tournament, league, cup, tour, or series that contains
  events.
- `Event`: A scheduled sports occurrence, such as a match, race, bout, meet,
  round, or game.
- `Participant`: A team, national team, club, athlete, player, driver,
  fighter, or esports player taking part in an event.
- `Relevance`: The reason an event should appear for a followed country.

## First Relevance Rule

An event is relevant to a country when at least one participant in the event
represents that country.

For the first domain slice, Sweden vs Switzerland is relevant to Sweden
because the Sweden men's national ice hockey team represents Sweden in the
event.

This rule should later extend to individual athletes, club teams, esports
players, motorsport drivers, and other country-specific participation patterns
without making Sweden a special case in the product logic.
