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
SESport.Data.Repositories
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
|-- Models/          Query, command, and result models for repositories
|-- Repositories/    PostgreSQL repositories and SQL
|-- PostgresDataSourceFactory.cs
|-- PostgreSqlJson.cs
|-- EntityLinkEntityNotFoundException.cs
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

### `SESport.Data.Repositories`

This namespace contains the concrete PostgreSQL repositories. Each repository
owns SQL and mapping for a coherent persistence area and accepts an
`NpgsqlDataSource` through dependency injection. Methods are asynchronous and
accept cancellation tokens for request and worker cancellation.

Examples:

- `ActivityRepository` handles activity, group, participant, and related
  activity-list queries and writes.
- `AdminRepository` handles reference data, entities, entity links, and
  entity merge operations.
- `AiRepository` handles AI definitions, runs, claims, diagnostics, and the
  Core AI repository contracts.
- `BroadcastImportRepository` performs transactional broadcast imports and
  supports explicit connection ownership for standalone tools.

Other repositories keep narrower areas separate:

- `AdminBroadcastRepository` handles broadcast administration.
- `AiAdminRepository` handles editable AI provider, job, prompt, and
  automation configuration.
- `AiAutomationRepository` reads enabled automation job identifiers.
- `ActivityParticipantAiResultRepository` stores AI participant results.
- `DashboardRepository`, `FactRepository`, and `SourceReferenceRepository`
  serve their corresponding read and write areas.

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

- Put SQL and Npgsql operations in `Repositories`.
- Put repository query, command, and result DTOs in `Models`.
- Put reusable domain concepts, identifiers, and repository contracts in
  `SESport.Core`.
- Put provider execution in `SESport.AI`, never in this project.
- Put configuration binding and service registration in the executable host.
- Keep a repository focused on one coherent persistence area.

The current namespace graph is intentionally acyclic:

```text
SESport.Data.Repositories
    -> SESport.Data.Models
    -> SESport.Core
SESport.Data
    -> SESport.Core.Configuration
```

No namespace restructuring is required at present. A future split should be
driven by a new persistence boundary or an actual dependency cycle, rather
than by creating namespaces that merely mirror individual SQL tables.
