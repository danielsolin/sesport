# SE Sport

SE Sport is a country-based sports calendar and watchlist. It helps a user
find upcoming sports events where a selected country is relevant, across
sports, competitions, teams, and individual athletes.

See [use cases](docs/use-cases.md) for examples of future user-defined views.
See [source strategy](docs/source-strategy.md) for data ingestion direction.
See [PostgreSQL](database/postgres/README.md) for the first persistence slice.

Local PostgreSQL development uses Docker Compose.

The first configured country is Sweden, but Sweden is not a hardcoded product
rule. The same model should be able to support other countries later.

## Project Rules

No line in any text file should exceed 80 characters unless a longer line is
technically required for the file to work.

Indentation should use three spaces per level. Tabs should never be used for
indentation.

All project artifacts, documentation, code, UI text, schemas, and comments
should be written in English.

Prefer small, focused commits that preserve detailed project history.

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
- Country connection: Sweden participates as a national team

## Core Concepts

- `Id`: A stable internal identifier for a domain object.
- `Country`: The country a user follows, such as Sweden.
- `Sport`: The sport an event belongs to, such as ice hockey.
- `Competition`: The tournament, league, cup, tour, or series that contains
  events.
- `CompetitionStatus`: The lifecycle state of a competition.
- `Event`: A scheduled sports occurrence, such as a match, race, bout, meet,
  round, or game.
- `Participant`: A team, national team, club, athlete, player, driver,
  fighter, or esports player taking part in an event.
- `Person`: A person connected to a participant, such as a player on a roster.
- `RosterMembership`: A person's role on a team or other participant roster.
- `CountryConnection`: One reason a country is connected to an event.
- `ImportRun`: One attempt to collect events from a source.
- `ImportIssue`: A warning or error found while importing source data.
- `IEventSourceImporter`: A source adapter that can produce import runs.
- `EventQuery`: A user-defined event filter over schedules and country
  connection evidence.
- `Source`: An external data source, such as a league, federation, or API.
- `ImportedEvent`: A raw event shape from a source before full resolution.
- `ExternalMapping`: A link from a source-specific ID to an internal entity.

## Source Adapters

Source-specific adapters live outside `SESport.Core`. The first adapter project
is `SESport.Sources.Iihf`, which maps IIHF-like schedule data into the shared
ingestion model. The first document client can read IIHF schedule HTML behind a
testable schedule client boundary. Saved schedule HTML can also be imported
from disk.

## First Country Connection Rule

An event is relevant to a country when the event has one or more country
connections for that country.

The first supported country connection types are:

- A participant represents the country.
- A person connected to an event participant has that country as a nationality.

For the first domain slice, Sweden vs Switzerland is relevant to Sweden
because the Sweden men's national ice hockey team creates a country connection
to Sweden by representing Sweden in the event.

For club-team events, country connections can come from roster evidence. If
Las Vegas Golden Knights play a Stanley Cup Final and have Swedish players on
the roster, the event can be relevant to Sweden even though Las Vegas does not
represent Sweden.

This rule should later extend to individual athletes, club teams, esports
players, motorsport drivers, and other country-specific participation patterns
without making Sweden a special case in the product logic.
