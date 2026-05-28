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

## Current Direction

The first source-specific adapter project is `SESport.Sources.Iihf`. It should
start by mapping IIHF-like schedule data into the shared ingestion model before
performing network access.

Before adding network access, inspect the real IIHF data surface for public
APIs, embedded JSON, static documents, robots rules, usage terms, and caching
requirements.

## References

- TheSportsDB: community-backed sports database and API.
- SportDB.dev: multi-sport sports data API.
- IIHF schedule pages and stats documents: potential official data surface.
