# SESport.Core

`SESport.Core` is the dependency-free shared kernel of sesport. It contains
the domain vocabulary, identifiers, parsing rules, formatting helpers,
configuration types, source-evidence contracts, and AI job contracts used by
the other projects.

The project has no project or package references. It is deliberately below
the runtime and infrastructure layers:

```text
SESport.Core
    <- SESport.AI
    <- SESport.Data
    <- SESport.Web
    <- tools and tests
```

Core defines concepts and contracts. It does not open database connections,
call AI providers, bind host configuration, render Razor Pages, or own
dependency-injection composition.

## Role in the system

Core gives the rest of the solution a shared language for:

- sport activities, broadcasts, entities, people, countries, and facts;
- stable identifiers and controlled domain values;
- source references and evidence attached to imported or AI-produced data;
- AI jobs, prompts, providers, runs, results, and persistence contracts;
- time-zone-aware sport dates and display formatting;
- configuration options, defaults, environment resolution, and keys.

The layers use those concepts as follows:

```text
SESport.Web
    -> domain types, AI contracts, configuration options
SESport.AI
    -> AI contracts, source models, defaults, and formatting helpers
SESport.Data
    -> domain types, AI repository contracts, and configuration defaults
tools
    -> broadcast/domain parsers, identifiers, and configuration helpers
```

The executable projects still own their configuration sources and composition.
For example, `SESport.Web` binds `appsettings.json` and environment values,
while `SESport.Core.Configuration` supplies the option types and defaults.

## Architectural boundaries

- Core must remain free of PostgreSQL, Npgsql, Playwright, and provider SDKs.
- Core must not depend on `SESport.AI`, `SESport.Data`, or `SESport.Web`.
- Domain rules belong in Core when they are shared by more than one host.
- Provider-specific behavior belongs in `SESport.AI`, not in Core AI models.
- SQL and persistence mapping belong in `SESport.Data`.
- Host-specific configuration binding belongs in the executable project.
- Country-specific behavior uses `PrimaryCountry`, not hard-coded country
  names or codes in consuming code.
- Application data must not be seeded by database migrations.

Core types are mostly immutable records, enums, constants, and pure helpers.
The few mutable `*EditModel` types are shared input contracts for admin data
operations; they contain no UI or persistence behavior.

## Project structure

```text
src/SESport.Core/
|-- AI/             AI contracts, models, parsing, and AI summaries
|-- Broadcast/      Broadcast models, parsing, and participation rules
|-- Configuration/  Defaults, options, keys, and environment helpers
|-- Domain/         Shared domain records, enums, and domain constants
|-- Formatting/     General date, time, percentage, and text helpers
|-- Identifiers/    Strongly typed IDs and deterministic GUID helpers
|-- Sources/        Source references, evidence, and correlation constants
```

The `AI/Models` and `AI/Interfaces` folders are organizational folders. Their
types intentionally use the flat `SESport.Core.AI` namespace so callers can
consume the AI contract surface through one namespace. They are not separate
C# namespaces.

## Namespace overview

### `SESport.Core.AI`

This namespace defines the provider-independent AI contract surface. It holds
job, prompt, provider, run, result, automation, and participant-result models,
repository interfaces used by the AI runtime, and pure AI output helpers.
Provider clients and PostgreSQL implementations live in other projects.

Examples:

- `AiJobDefinition`, `AiPromptDefinition`, and `AiProviderDefinition` model
  configuration loaded from persistence.
- `AiJobRequest`, `AiJobResult`, and `AiJobRun` describe execution input,
  output, and persisted state.
- `IAiJobDefinitionRepository` and `IAiJobRunRepository` are storage
  contracts implemented by `SESport.Data`.
- `ActivityParticipantAiOutputParser` parses structured participant output,
  while `AiRunSummaryFormatter` creates compact run summaries.

`AiRunSummaryFormatter` belongs to this namespace because its behavior is AI
specific. Keeping it in generic `Formatting` would make the dependency graph
cyclic: AI uses general formatting, while the formatter needs AI job IDs.

### `SESport.Core.Broadcast`

This namespace contains the broadcast domain model and reusable parsing and
normalization rules. It converts external broadcast data into Core records,
resolves activity types, identifies relevant participants, and prepares
activity-related broadcast data without accessing a database.

Examples:

- `Broadcast` and `BroadcastImportRun` are the core import records.
- `BroadcastXmlParser` parses XMLTV-style broadcast input.
- `BroadcastParticipationCheckParser` maps an AI participation response to a
  typed result.
- `BroadcastEntityFilter` and `BroadcastParticipantNameFormatter` apply
  reusable matching and normalization rules.

The namespace may use `PrimaryCountry` and general formatting helpers, but it
does not know which persistence or web host will consume its results.

### `SESport.Core.Configuration`

This namespace owns all code-defined application configuration: option types,
defaults, configuration section keys, environment-variable resolution, API
key resolution, and connection-string construction. It does not bind a host's
configuration object or register services.

Examples:

- `AiDefaults`, `AiWorkerDefaults`, and `LlamaServerDefaults` centralize AI
  runtime limits and timeouts.
- `SearxngWebSearchClientOptions` and `WebSearchRateLimitOptions` describe
  search configuration.
- `WebPageFetchDefaults` and `WebStatsOptions` describe web-related runtime
  behavior.
- `PostgresConnectionStrings`, `ApplicationConfigurationKeys`, and
  `ApiKeySourceResolver` resolve shared configuration concerns.

`GlobalUsings.cs` supplies shared domain and identifier namespaces to the
configuration files. The executable host remains responsible for binding
these options from JSON or environment providers.

### `SESport.Core.Domain`

This namespace is the central product vocabulary. It contains domain records,
controlled identifiers, enums, date concepts, and constants that must be
consistent across web, AI, data, and import tooling.

Examples:

- `Country`, `Person`, `Sport`, and `FactRecord` represent shared domain
  records.
- `ActivityType`, `TrackedEntityType`, and the status/ID classes define
  controlled values.
- `PrimaryCountry` is the single source for the configured primary country.
- `SportDay` and `SportDayWindow` define presentation-day behavior around the
  configured local-time cutoff.
- `SportFilter` provides reusable sport-filter normalization.

`SportDay` is a presentation and grouping concept. It must not be used to
rewrite the actual persisted calendar moment of an activity.

### `SESport.Core.Formatting`

This namespace contains general, reusable formatting and time helpers. These
helpers are intentionally independent of application features so they can be
used by domain parsing, data mapping, web presentation, and tools.

Examples:

- `DateDisplay` defines invariant date and time display formats.
- `TimeZoneHelper` resolves time zones and converts values to and from UTC.
- `TimeTextFormatter` extracts displayable time text from source strings.
- `PercentageDisplayFormatter` formats decimal ratios for display.
- `UnicodeTextSanitizer` removes invalid null and surrogate characters.

AI-specific summaries are kept in `SESport.Core.AI`; this namespace should
not reference feature-specific identifiers or models.

### `SESport.Core.Identifiers`

This namespace provides strongly typed identifiers for important domain
objects and the deterministic GUID helper used when stable IDs are derived
from external values.

Examples:

- `ActivityId`, `CountryId`, `EntityId`, `PersonId`, and `SportId` prevent
  accidental interchange of semantically different string IDs.
- `DeterministicGuid` creates repeatable GUIDs from stable input values.

Identifiers are intentionally small value objects. Persistence adapters can
map them to database columns, while domain and import code can retain the
semantic type.

### `SESport.Core.Sources`

This namespace describes where information came from and how it relates to a
domain record. It is shared by broadcast imports, facts, AI research, and
data repositories.

Examples:

- `SourceReference` represents a persisted source link and its metadata.
- `SourceEvidenceDraft` represents source evidence produced before storage.
- `SourceKinds` and `SourceCorrelationTypes` define stable source categories
  and correlation targets.

Source types are contracts and value objects only. Fetching pages and calling
search providers belong to `SESport.AI`; storing references belongs to
`SESport.Data`.

## Dependency layout

The important feature-level dependency direction is:

```text
AI          -> Formatting, Sources, Domain
Broadcast   -> Formatting, Domain
Sources     -> Domain
Domain      -> Formatting
Configuration, Formatting, and Identifiers stay below feature code.
```

The diagram represents allowed conceptual dependencies, not every file-level
`using` statement. The key rule is that shared helpers do not point back to
feature namespaces. In particular, generic formatting no longer depends on
AI-specific identifiers.

## Maintaining the structure

When adding Core code:

- Put shared business concepts and invariants in `Domain`.
- Put AI contracts and provider-independent AI models in `AI`.
- Put schema-independent source evidence in `Sources`.
- Put strong IDs in `Identifiers`.
- Put pure cross-feature display and time helpers in `Formatting`.
- Put defaults and option types in `Configuration`.
- Put broadcast parsing and normalization rules in `Broadcast`.
- Keep host, database, and provider implementation details out of Core.

Prefer shared constants, enums, identifiers, and helpers over repeating
magic strings. When a rule is only useful to one adapter or host, keep it in
that owning project instead of widening Core's responsibility.

## Testing

Core tests cover domain behavior, parsing, formatting, configuration, and AI
contract helpers. Core itself has no database or network runtime dependency,
so its tests should remain deterministic and infrastructure-free.

The usual command is:

```bash
dotnet test tests/SESport.Core.Tests/SESport.Core.Tests.csproj
```
