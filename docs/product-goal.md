# Product Goal

The core idea behind SE Sport is simple: a user should be able to open
`www.sesport.se` and immediately see what Swedish-relevant sport is happening
today.

The first version must answer this question before anything else:

```text
What blue-and-yellow sport is happening today?
```

In this context, blue-and-yellow sport means sports events with a clear Sweden
connection. The connection can come from Sweden participating as a national
team, Swedish athletes participating individually, or Swedish people being
present on relevant team rosters.

## Version 1 Minimum

The first public version of `www.sesport.se` should, at minimum, provide a
today view for Swedish-relevant sports events.

This view should help the user answer:

- what is happening today
- when it starts
- which sport and competition it belongs to
- why the event is relevant to Sweden

Everything else is secondary until this works.

## Launch Data Rule

Every event shown in the public version must come through the standard SE Sport
ingestion process and be stored in the application database.

For the first public version, source collection should be manual by default. The
operator should personally collect source files from a defined source list,
place them in a defined local structure, and run the normal import process.

The import process may be started through a CLI command or a UI button. The
important rule is that the public site must read normalized SE Sport data from
PostgreSQL after import. It should not contain one-off hardcoded launch events,
hand-written page content, or source-specific shortcuts that bypass the
ingestion model.

A once-per-day manual collection cycle is acceptable for the first version,
because the initial product only promises to show what is happening today.

This manual path is not a temporary embarrassment. It is the baseline recovery
path. Automation can be added later, but the product should still have a working
manual source collection and import process if automated collection fails.

This rule matters more than full automation for the first version.

## Required Coverage

The exact sports, leagues, competitions, and tours required for version 1 are
not decided yet.

Version 1 coverage should be based on the season when the first public version
is launched. The goal is not to cover every important Swedish sport immediately.
The goal is to cover the sports and competitions a normal Swedish sports fan
would expect to see at that time of year.

If the first public version launches during summer, summer-relevant sources
should be prioritized. This likely means many football sources. Ice hockey and
cross-country skiing would not be launch requirements in that case, even though
they are important Swedish sports overall.

This launch coverage list must be defined before the first public version:

- Football: to be decided
- Athletics: to be decided
- Motorsport: to be decided
- Tennis: to be decided
- Golf: to be decided
- Other summer individual sports: to be decided
- Other summer team sports: to be decided
- Ice hockey: not required for a summer launch
- Winter sports: not required for a summer launch

The list should be based on whether a normal Swedish sports fan would expect the
event to appear when asking what Swedish-relevant sport is happening today.

## Product Priority

When making tradeoffs, prefer work that improves the today view over broader but
less focused features.

Examples of lower-priority work until the today view is useful:

- advanced user-defined watchlists
- historical statistics
- broad multi-country support
- complex bracket prediction
- generic sports news
- exhaustive source coverage without Sweden relevance

The product should stay focused on country-relevant sports discovery, not become
a generic sports calendar.
