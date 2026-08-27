# SESport.Data

`SESport.Data` is the PostgreSQL persistence layer for sesport. It uses
Npgsql to read and write the single application database, maps SQL result sets
to application-facing data models, and implements the repository contracts
owned by `SESport.Core`.

The project is infrastructure, not a domain or web layer. It owns SQL,
Npgsql data-source usage, transactions, PostgreSQL-specific JSON handling,
and the mapping between relational rows and repository models. It does not
contain AI provider execution, Razor Pages, or application configuration
binding.

## Role in the system

The normal runtime path is:

```text
SESport.Web or an import tool
    -> NpgsqlDataSource
SESport.Data feature namespaces
    -> parameterized SQL and row mapping
PostgreSQL
    -> SESport.Data.Models
SESport.Web or the calling tool
```

The web host creates one `NpgsqlDataSource` from the configured connection
string and registers repositories through dependency injection. The
repositories receive that data source in their constructors. Standalone
import tooling may use `BroadcastImportRepository.Connect` with an explicit
connection string and then dispose the repository when the import completes.

For AI jobs, `AiRepository` implements the repository contracts from
`SESport.Core.AI`. `SESport.AI` consumes those contracts without referencing
this project. This preserves the dependency direction between AI execution
and PostgreSQL infrastructure.

## Architectural boundaries

- `SESport.Data` references `SESport.Core` and does not reference
  `SESport.AI` or `SESport.Web`.
- `SESport.Data` owns PostgreSQL access, SQL, row mapping, and transactions.
- `SESport.Core` owns shared domain types, AI contracts, and configuration
  defaults consumed by this project.
- `SESport.Web` owns configuration binding, dependency-injection composition,
  request handling, and UI behavior.
- `SESport.AI` owns provider clients and AI execution, not persistence.
- Data migrations define schema only; they must not seed application data.
- Data-backed tests use the database resolved from `.env` and must follow the
  repository rule for safely isolated, unpublished test data.

The data project exposes concrete repositories intentionally. The AI
repository also implements Core interfaces where the AI runtime needs a
storage abstraction. The web application currently composes the concrete
repositories directly because they are its application-facing persistence
services.

## Project structure

```text
src/SESport.Data/
|-- Activities/      Activity and participant repositories and SQL
|-- Admin/           Administration, dashboard, and todo repositories
|-- AI/              AI repositories and SQL
|-- Broadcasts/      Broadcast repositories and SQL
|-- Entities/        Entity repositories and SQL
|-- Facts/           Fact repository and SQL
|-- Members/         Member and watch repositories and SQL
|-- PostgresDataSourceFactory.cs
|-- PostgreSqlJson.cs
|-- Sources/         Source reference repository and SQL
|-- Statistics/      Public statistics repository and SQL
|-- EntityLinkEntityNotFoundException.cs
|-- Models/          Query, command, and result models for repositories
```

The directory layout mirrors the namespaces. The root namespace contains the
small infrastructure entry points and shared persistence helpers. There are
no entity classes representing an ORM state model; repositories use explicit
SQL projections and application-facing records instead.

## Namespace overview

### `SESport.Data`

This is the root infrastructure namespace. It contains the public data-source
factory used by the web host, an internal PostgreSQL JSON normalization helper,
and a repository-facing exception that the web layer can translate into an
HTTP result.

Examples:

- `PostgresDataSourceFactory` creates the configured `NpgsqlDataSource`.
- `PostgreSqlJson` normalizes JSON before it is persisted in PostgreSQL.
- `EntityLinkEntityNotFoundException` represents a failed entity-link
  operation when a referenced entity disappeared.

### `SESport.Data.Models`

This namespace contains the data contracts returned by queries and accepted by
repository write operations. They are deliberately lightweight records or
mutable command models, not database entities and not Razor PageModels. The
models may be consumed by `SESport.Web` or import tools, but they contain no
web or UI behavior.

Examples:

- `ActivityListItem` and `ActivityEditModel` represent activity projections
  and activity write data.
- `EntityListItem` and `EntityEditModel` represent entity search and edit
  data.
- `BroadcastListItem` and `BroadcastSaveResult` represent broadcast admin and
  import results.
- `AdminDashboardSnapshot` represents the dashboard query projection.

The files group models by feature: activities, administration, broadcasts,
AI participant results, and the admin dashboard. If a type becomes a shared
domain concept rather than a persistence projection, it belongs in
`SESport.Core` instead.

### Repository namespaces

There is no shared repository namespace. Concrete PostgreSQL repositories are
grouped in namespaces directly below `SESport.Data`. Each
repository owns SQL and mapping for a coherent persistence area and accepts an
`NpgsqlDataSource` through dependency injection. The class names retain the
`Repository` suffix, so the namespace names do not repeat it.

- `SESport.Data.Activities` contains activity, group, participant, and public
  activity query repositories.
- `SESport.Data.Admin` contains administration, dashboard, and todo
  repositories.
- `SESport.Data.AI` contains AI definition, run, application, automation, and
  administration repositories.
- `SESport.Data.Broadcasts` contains broadcast administration, channel-link,
  import, and stream-source persistence.
- `SESport.Data.Entities` contains entity query, mutation, merge, and facade
  repositories.
- `SESport.Data.Facts`, `SESport.Data.Sources`, and
  `SESport.Data.Statistics` contain their corresponding persistence areas.
- `SESport.Data.Members` contains member, watch, and push-notification
  repositories.

Examples of the resulting type names:

- `SESport.Data.Activities.ActivityRepository` handles activity, group,
  participant, and related activity-list queries and writes.
- `SESport.Data.Admin.AdminRepository` handles reference data, entities,
  entity links, and entity merge operations.
- `SESport.Data.AI.AiRepository` handles AI definitions, runs, claims,
  diagnostics, and the Core AI repository contracts.
- `SESport.Data.Broadcasts.BroadcastImportRepository` performs transactional
  broadcast imports and supports explicit connection ownership for standalone
  tools.

The directory layout mirrors the namespaces. A repository may call another
repository namespace when a shared persistence operation is required, but
each feature keeps its own SQL and row mapping together.

## Persistence areas

The repository split follows the application's main data responsibilities:

- Activities, activity groups, participants, and activity links.
- Entities, reference data, entity links, and entity merging.
- Broadcast import and broadcast administration.
- Facts and source references attached to domain records.
- AI providers, jobs, prompts, runs, automations, and applied results.
- Dashboard summaries and operational health projections.

Repositories can join related tables when the returned projection requires
it. They should still select only the columns consumed by the projection and
keep feature-specific SQL in the repository that owns that feature.

## Data access conventions

### Connection ownership

The web host binds configuration and calls
`PostgresDataSourceFactory.CreateDefault`. The factory resolves the
connection string through `SESport.Core.Configuration` when no explicit
value is supplied. The resulting data source is registered as a singleton and
shared by repository instances.

Import tools may pass an explicit connection string. That path is kept in
`BroadcastImportRepository` because the tool is a standalone process and must
own and dispose its data source without requiring web-host composition.

### SQL and mapping

- Use parameterized `NpgsqlCommand` values for external and user-provided
  input.
- Keep SQL beside the repository method that owns the query.
- Map result columns explicitly to a model instead of exposing database rows.
- Use transactions for multi-step imports and consistency-sensitive writes.
- Pass cancellation tokens to connection, command, reader, and transaction
  operations.
- Use shared Core identifiers and constants instead of repeating domain
  literals where practical.
- Keep PostgreSQL-specific JSON repair in `PostgreSqlJson`.

### Schema and application data

Database migrations belong to the database schema workflow. They may create
or alter tables, indexes, and constraints, but must not seed application
records. Application data changes are deliberate operational actions through
the appropriate repository or an explicitly reviewed `psql` command.

## Testing

`tests/SESport.Data.Tests` covers repository behavior, SQL construction, and
database-backed operations. The project does not have a separate test
database by default; its shared bootstrap resolves the connection from
`.env`. Tests that write data must use a safely distant date, keep records
unpublished unless publication is the behavior under test, and clean up even
when setup fails.

The usual commands are:

```bash
dotnet build src/SESport.Data/SESport.Data.csproj
dotnet test tests/SESport.Data.Tests/SESport.Data.Tests.csproj
```

Run database-backed tests only against the intentionally configured database.
The data project itself does not start PostgreSQL or run migrations.

## Maintaining the structure

When adding data access code:

- Put SQL and Npgsql operations in the feature namespace that owns them.
- Put repository query, command, and result DTOs in `Models`.
- Put reusable domain concepts, identifiers, and repository contracts in
  `SESport.Core`.
- Put provider execution in `SESport.AI`, never in this project.
- Put configuration binding and service registration in the executable host.
- Keep a repository focused on one coherent persistence area.

The namespace graph is intentionally acyclic:

```text
SESport.Data.<feature>
    -> SESport.Data.Models
    -> SESport.Core
SESport.Data
    -> SESport.Core.Configuration
```

Feature namespaces should represent coherent persistence areas, not individual
SQL tables. New repositories should join the closest existing feature
namespace unless they introduce a genuinely separate persistence area.
