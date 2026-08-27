# sesport

sesport is a country-independent sports information platform. It turns
heterogeneous sport, broadcast, editorial, and research data into a
normalized, source-aware model that can be queried and presented through a
web application.

The current and only live implementation of sesport is available at
[sesport.se](https://sesport.se).

The system is designed around a configurable primary-country context, but the
domain model and runtime are intended to support any country. Country
relevance is data and configuration, not a separate application variant.

## Core concept: country representation

sesport is about identifying when a country is represented in international
sport. The relevant fact is not a person's nationality or general association
with a country, but whether a person or team represents the configured primary
country in a specific competition.

This creates two separate concepts:

- **Activity relevance**: an activity is in scope when it belongs to an
  international competition and the primary country is represented by a
  person, team, or national team.
- **Person participation**: the system may still store and process individual
  participants as `Person` entities. A person who is part of a relevant team
  is relevant to the activity's team context, but is not automatically a
  person representing the primary country individually.

The international competition is the important context. A match between two
teams from the same country is still relevant when it takes place in an
international tournament. Domestic leagues and other domestic competitions
remain outside this scope, even when the primary country is represented.

Representation must be established from the competition or activity context
and reliable evidence. It must not be inferred solely from nationality,
citizenship, birthplace, name, or other general person facts. This distinction
allows the platform to present country representation at activity level while
preserving person-based participant data, enrichment, watching, and historical
records.

## Technical purpose

The platform has four closely related responsibilities:

- maintain canonical entities such as people, teams, organizations, and
  competitions;
- maintain scheduled activities, activity groups, participants, and related
  broadcasts;
- enrich records with facts and structured research results while preserving
  source and execution provenance;
- expose public, member, and administrative workflows over the same durable
  data model.

The database is the system of record. AI is an enrichment and automation layer,
not a replacement for domain validation or persistence. AI runs are versioned,
audited, and applied through explicit post-processing steps.

## Architecture

The solution is split into projects with explicit dependency boundaries:

```text
                         +----------------------+
                         | SESport.Web          |
                         | Razor Pages and host |
                         +----------+-----------+
                                    |
                 +------------------+------------------+
                 |                                     |
        +--------v---------+                  +--------v---------+
        | SESport.Data     |                  | SESport.AI       |
        | SQL and Npgsql   |                  | AI and research  |
        +--------+---------+                  +--------+---------+
                 |                                     |
                 v                                     v
          PostgreSQL database                 AI providers,
                                               search, pages, OCR

        SESport.Data and SESport.AI both depend on SESport.Core.
        SESport.Core has no infrastructure or provider dependency.
```

### `SESport.Core`

The shared kernel contains domain vocabulary, stable identifiers, parsing and
normalization rules, source-evidence contracts, AI contracts, configuration
option types, defaults, and pure formatting helpers.

Core does not open database connections, call providers, bind host
configuration, or render web pages.

### `SESport.Data`

The persistence layer uses Npgsql and explicit SQL repositories. It owns the
PostgreSQL data source, transactions, row mapping, PostgreSQL-specific JSON
handling, and implementations of the repository contracts used by the AI
runtime.

It does not contain Razor Pages or AI provider execution.

### `SESport.AI`

The AI runtime provides provider adapters, prompt rendering, structured-output
validation, queued and direct job execution, web search, page retrieval and
normalization, browser fallbacks, PDF handling, and image OCR.

AI code receives persistence contracts and configuration through interfaces and
options. It does not issue SQL or create PostgreSQL connections.

### `SESport.MCP`

The MCP server exposes selected web research capabilities to external MCP
clients such as Codex CLI. It is intentionally a thin host: it owns the MCP
transport, tool contracts, and response serialization, while forwarding the
actual search and page-fetch work to `SESport.AI`.

It does not introduce a separate domain or persistence layer. The server
normally runs as a stateless Streamable HTTP service on loopback. Its detailed
configuration, tools, and deployment instructions are in the
[`SESport.MCP` guide](src/SESport.MCP/README.md).

### `SESport.Web`

The executable host is an ASP.NET Core Razor Pages application. It composes
configuration and dependency injection, serves public and authenticated pages,
provides administration pages, and coordinates application services with the
Data and AI projects.

The host also runs background workers for pending AI runs, stale-run timeout
handling, delayed application of completed AI results, and member push
notifications.

## Main data model

PostgreSQL stores both domain data and operational history. The main areas are:

- reference data for sports, countries, activity types, entity types, and
  publication states;
- canonical entities and bidirectional entity relationships;
- activity groups, activities, participants, represented entities, and
  organization context;
- broadcasts, visibility, deduplication fingerprints, and ignore rules;
- reusable sources, facts, and links that preserve external evidence;
- AI providers, jobs, versioned prompts, runs, automation rules, applied
  results, and diagnostic data;
- members, watched entities, login tokens, and push notification state.

Key data rules include the following:

- activities and broadcasts retain both UTC-backed timestamps and local
  scheduling fields where editorial display requires them;
- publication state is explicit, so stored activities are not automatically
  public;
- many-to-many relationships use link tables because the links carry metadata
  and lifecycle state;
- AI applications are recorded so completed runs can be retried safely and
  applied idempotently;
- migrations change schema only. Application data is managed separately and
  is never seeded by migrations.

The complete schema overview is in
[`database/STRUCTURE.md`](database/STRUCTURE.md).

## Runtime flows

### Content flow

```text
External or editorial input
        -> Core parsing and normalization
        -> repository commands
        -> PostgreSQL
        -> public, member, and admin projections
```

Repositories map SQL projections to application models instead of exposing
database rows directly. The web layer consumes those repositories and keeps
request handling and presentation separate from SQL.

### AI flow

```text
Web or automation event
        -> job and prompt selection
        -> persisted AI run
        -> worker claim and provider execution
        -> optional search, page fetch, and OCR
        -> structured result validation
        -> persisted result and source links
        -> explicit domain post-processing
```

AI job definitions, providers, prompts, and runs are persisted. A run keeps
the input, rendered prompt, selected configuration, status, timing, usage
metadata, and diagnostic trace needed to understand what happened after a
configuration changes.

The active provider can use a local model server. Other provider adapters are
available through the same contract. Web research uses a local SearXNG
instance by default, and page retrieval can use HTTP, browser, PDF, or OCR
paths depending on the source.

## Configuration and dependencies

Code-defined options and defaults live in `SESport.Core.Configuration`.
`SESport.Web` owns configuration binding and service composition. The active
PostgreSQL connection is resolved from these environment variables:

```text
SESPORT_POSTGRES_HOST
SESPORT_POSTGRES_PORT
SESPORT_POSTGRES_DB
SESPORT_POSTGRES_USER
SESPORT_POSTGRES_PASSWORD
```

The repository-root `.env` file is the source of truth for the one active
application database. The web process does not load `.env` automatically;
deployment or the invoking shell must provide the variables.

AI jobs that use web research require a SearXNG service. The default base URL
is `http://127.0.0.1:8088/`. Browser-backed fetching requires the applicable
Playwright browser runtime, and image OCR requires Tesseract with the needed
language data.

Optional host features use configuration for administration, passwordless
member email, Web Push, SMTP, web statistics, and provider-specific settings.
Secrets belong in host-local environment configuration and never in tracked
files.

## Local development

The solution targets the .NET 10 SDK.

1. Create the local environment file and set the PostgreSQL values:

   ```bash
   cp .env.example .env
   # edit .env
   set -a
   . ./.env
   set +a
   ```

2. Start PostgreSQL with Docker Compose only on a machine that is intended to
   operate the database referenced by `.env`:

   ```bash
   docker compose up -d postgres
   ```

3. Apply schema migrations using the migration procedure configured for
   the environment.

4. Start SearXNG if the application will run AI jobs:

   ```bash
   docker compose up -d searxng
   ```

5. Build and run the web application:

   ```bash
   dotnet build
   dotnet run --project src/SESport.Web
   ```

The local HTTP endpoint is `http://localhost:5109`. The administrative area is
under `/Admin` and requires the configured administrator credentials.

## Testing

Run the complete test suite with:

```bash
dotnet test
```

Project-level test commands are useful when a solution restore is affected by
machine-specific SDK workload resolvers:

```bash
dotnet test tests/SESport.Core.Tests
dotnet test tests/SESport.Data.Tests
dotnet test tests/SESport.AI.Tests
dotnet test tests/SESport.Web.Tests
```

Core tests are deterministic and infrastructure-free. Data-backed tests use
the same PostgreSQL database resolved from `.env`; there is no separate test
database by default. Such tests must use safely distant dates, keep records
unpublished unless publication is under test, and clean up data even when
setup fails.

## Repository layout

```text
src/SESport.Core/   Shared domain, contracts, configuration, and parsers
src/SESport.Data/   PostgreSQL access and repositories
src/SESport.AI/     AI providers, jobs, search, pages, and OCR
src/SESport.MCP/    MCP host for external web research tools
src/SESport.Web/    Razor Pages host, UI, services, and workers
database/           Schema migrations and database documentation
tests/              Unit, integration, AI, web, and parser tests
```

## Further reading

- [`SESport.Core` guide](src/SESport.Core/README.md)
- [`SESport.Data` guide](src/SESport.Data/README.md)
- [`SESport.AI` guide](src/SESport.AI/README.md)
- [`SESport.MCP` guide](src/SESport.MCP/README.md)
- [`SESport.Web` guide](src/SESport.Web/README.md)
- [`Database guide`](database/README.md)
