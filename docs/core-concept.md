# Core Concept: Country-Relevant International Sport

SE Sport is about international sport from the perspective of a selected
country.

It does not aim to cover all sports played within that country. It aims to
identify international, foreign, or cross-border sports events that are
relevant to the selected country because the country is represented directly or
indirectly.

The first configured country is Sweden, but the system must be designed so
that Sweden can be replaced by any other country through configuration.

## Definition

A sports event is in scope when it is both:

1. An international, foreign, or cross-border sports event.
2. Relevant to the selected country through meaningful participation by
   athletes, national teams, clubs, teams, coaches, or other important sporting
   entities connected to that country.

The central question is:

```text
Is this an international sports event where someone or something connected to
the selected country is meaningfully participating?
```

If the answer is yes, the event is in scope.

## What Counts as International Sport

In this project, international sport does not only mean national teams playing
against other national teams.

International sport includes any sporting context where participation crosses
national borders, takes place outside the selected country's domestic sports
system, or involves competition between entities from different countries.

This includes:

- national teams competing internationally;
- individual athletes competing in international competitions;
- clubs from the selected country competing against clubs from other countries;
- athletes from the selected country competing for foreign clubs or teams;
- foreign clubs, teams, or leagues where participants from the selected country
  have meaningful roles;
- international tournaments, world tours, continental competitions, global
  leagues, foreign leagues, qualification events, and similar cross-border
  sporting contexts.

## Examples with Sweden as the Selected Country

The following are in scope:

- Sweden playing Finland in ice hockey.
- The Swedish national football team playing a World Cup qualifier.
- A Swedish athlete competing in the Olympics, World Championships, Diamond
  League, ATP, WTA, PGA, LPGA, UFC, Formula 1, or similar international
  competitions.
- A Swedish football club playing in a European club competition.
- A Swedish ice hockey club playing in an international club competition.
- An NHL game where Swedish players have meaningful roles.
- A Premier League match where a Swedish player starts, scores, assists, or is
  otherwise a relevant participant.
- A foreign team that is especially relevant to Sweden because several Swedish
  players, coaches, or other key figures are involved.

## What Is Out of Scope

Domestic sport is not in scope by default.

The project is not intended to duplicate existing domestic sports calendars,
league tables, fixture lists, result feeds, or news services. Those are already
available from many other sources.

A domestic league match is only relevant if it has a clear connection to
international sport, such as qualification for international competition,
national team relevance, transfer relevance, or another explicit cross-border
context.

## Country-Agnostic Design Requirement

The selected country must be treated as configuration, not as a hardcoded
assumption.

The same model should work for Sweden, Norway, Finland, Denmark, Germany,
Canada, Japan, or any other country.

Country-specific data such as athletes, clubs, national teams, domestic
competitions, international competitions, nationality rules, and relevance
scoring must be configurable.

The general model is:

```text
International sports events with meaningful participation by entities connected
to the selected country.
```
