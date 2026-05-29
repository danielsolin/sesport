# Source Strategy

SE Sport should use source adapters per data source or provider, not per
league as a default rule.

The core ingestion model must stay independent of provider details. Source
adapters can use official APIs, source-specific feeds, downloaded files,
community aggregators, or carefully reviewed scraping, but they should all
produce the same shared ingestion concepts:

- `ImportRequest`
- `ImportRun`
- `ImportedEvent`
- `ImportIssue`
- `ExternalMapping`

## Adapter Preference

Prefer official or source-specific adapters for high-value competitions where
data quality matters and the source is reasonably stable.

Use multi-sport aggregators for breadth, discovery, fallback, or prototyping.
Aggregators should not become a hard dependency for the core product model.

Use scraping only after checking that the source allows it, that the data
cannot reasonably be fetched through a cleaner interface, and that the adapter
can cache results and avoid unnecessary traffic.

Do not bypass anti-bot or security systems. If a source blocks plain server-side
requests, prefer official documents, cached source files, or a different
provider surface over browser emulation or Cloudflare workarounds.

## First Provider Types

- Official or source-specific providers, such as IIHF schedule data.
- League or federation providers, such as a future NHL schedule and roster
  source.
- Multi-sport aggregators, such as community-backed sports data APIs.
- Manual or semi-manual imports, such as CSV or JSON files for small sources.

## Design Rules

- `SESport.Core` must not reference source-specific projects.
- Source adapters should live outside `SESport.Core`.
- Source adapters should map provider-specific payloads to imported records.
- Source adapters should report incomplete or suspicious data as issues.
- Source adapters should preserve source IDs for later mapping and audits.
- Provider lock-in should be isolated to adapter projects.

## Assisted Import Operations

SE Sport should treat imports as assisted automation. Importers should automate
the normal case, but they must report uncertainty instead of silently hiding it.

SE Sport should not pretend to know. It should know when it knows. When it does
not know, it should explain what is uncertain.

If more of the import process can be automated later than expected, that is a
bonus. The operating model should still assume that humans or AI agents may
need to review and repair source mappings, cached documents, and parser logic.

Each import run should produce issues that explain what happened in operational
terms. Useful issue kinds include:

- `MissingSourceMapping`: A required provider mapping is not configured.
- `NoEventsFound`: The source was readable, but no events were found.
- `ParsingFailed`: A known value could not be parsed.
- `SourceUnavailable`: The source could not be reached.
- `UnexpectedSourceShape`: The source shape no longer matches expectations.
- `UnknownCountryCode`: A source country code is not mapped.

Reviewers can then inspect failed or suspicious runs, update provider mappings,
refresh cached files, or adjust parser logic.

## Useful Refresh Timing

Collectors should avoid polling a source just because time passed. They should
refresh when new country-relevant information can reasonably exist.

For scheduled sports events, this often means using a sport-specific expected
event duration. If an imported event has unresolved participants, the earliest
useful refresh is usually after the event starts, the sport's expected duration
has passed, and a small publication buffer has elapsed.

Example for ice hockey:

```text
semi-final starts at 15:20
expected ice hockey duration is 2h 30m
publication buffer is 15m
next useful refresh is around 18:05
```

This is operational scheduling metadata, not provider truth. A collector may
still retry earlier after source failures, and a manual refresh should remain
possible. The goal is to avoid unnecessary source traffic when the source cannot
reasonably contain new participant information yet.

## Current Direction

The first source-specific adapter project is `SESport.Sources.Iihf`. It maps
IIHF-like schedule data into the shared ingestion model.

IIHF stats endpoints are provider mappings, not domain identifiers. For example,
`2026/wm` currently maps to `https://stats.iihf.com/Hydra/969/index.html`.
This mapping should live in adapter configuration or data, not in core domain
code.

IIHF does not expose a public `robots.txt` at the standard location. Direct
server-side requests to schedule pages may be blocked by Cloudflare. The IIHF
adapter should therefore prefer official schedule documents, cached HTML, or
other stable public document surfaces before attempting direct live HTML
fetching.

Before adding broader network access, inspect each IIHF data surface for public
APIs, embedded JSON, static documents, usage terms, and caching requirements.

## References

- TheSportsDB: community-backed sports database and API.
- SportDB.dev: multi-sport sports data API.
- IIHF schedule pages and stats documents: potential official data surface.
