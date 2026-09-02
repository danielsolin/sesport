# Database structure

This document describes the PostgreSQL schema used by SESport. The schema is
split into reference data, sports content, broadcast ingestion, member
features, and AI execution data.

## Design principles

- UUIDs identify mutable domain records; short text IDs identify configured
  reference values and AI jobs.
- Timestamps are stored as `timestamp with time zone`. Activities and
  broadcasts also retain local date/time fields for editorial scheduling.
- Many-to-many relationships use explicit link tables so that links can carry
  metadata, provenance, or independent lifecycle rules.
- Imported and AI-generated data keeps source and execution provenance instead
  of replacing the original record.
- Foreign keys use `cascade` for owned child data, `set null` for optional
  associations, and `restrict` where deleting the referenced record would
  remove important history.

## Reference and configuration tables

### `sports`

Defines the sports used by activities, groups, and entities. It also stores
display metadata and sport-specific editorial rules such as whether a start
time is required and whether the sport is a team sport.

### `countries`

Provides the normalized country catalogue used by entities. The country code
is unique so that country identity is not duplicated in entity rows.

### `country_relevance_kinds`

Defines why a country is relevant to an entity. The entity stores both the
selected kind and a required explanatory reason.

### `entity_types`

Defines the kinds of entities that can be represented, such as people,
organizations, teams, or competitions.

### `entity_stability_kinds`

Defines the expected stability of an entity identity. This supports decisions
about whether an entity is likely to persist or change over time.

### `entity_watch_priorities`

Defines the priority used when deciding which entities deserve monitoring or
further enrichment.

### `activity_types`

Defines the available activity classifications and their display order. An
activity references one type instead of storing a free-form label.

### `activity_publication_statuses`

Defines the publication lifecycle for activities, for example draft or
published. Activities reference this table to keep public visibility explicit.

### `activity_entity_link_roles`

Stores ordered labels for roles that can be used when presenting an
activity-to-entity relationship. It is reference data and is not itself the
activity relationship table.

## Entity and source model

### `entities`

Stores the canonical identities followed by SESport. An entity is associated
with a type, sport, country, country relevance, stability, and watch priority.
Person-specific fields such as gender, birthdate, height, weight, and
formative club are nullable and constrained to person entities. Person
entities may also have a primary-country participation status and optional
explanation when they should not be counted for the primary country.

### `entity_images`

Stores image bytes and metadata associated with an entity. The table keeps
source, creator, license, attribution, file, and review metadata alongside
the optional image binary. It also stores an optional source-provided
thumbnail binary and its dimensions, MIME type, checksum, and media URL.
It supports candidate and approved images and permits at most one primary
image per entity. PostgreSQL stores the image bytes directly so image
metadata and content remain part of the same backup and transaction.

### `entity_to_entity_links`

Stores relationships between two entities, such as an athlete and a team.
The pair is unique in both directions, preventing the same relationship from
being inserted twice with source and target reversed.

### `sources`

Stores reusable external evidence: URL, title, excerpt, correlation identity,
source kind, and observation time. Facts and AI participant results reference
these rows instead of embedding source URLs repeatedly.

## Activity model

### `activity_groups`

Represents a multi-day competition, series, or other activity container. It
provides a title, sport, and date span for the activities belonging to it.

### `activities`

Stores the scheduled sport content shown and published by SESport. It carries
the title, description, sport and type, publication state, optional local
schedule, UTC-backed timestamps, public slug, teaser, and broadcast channel.
An activity may belong to an activity group and may have one organization
context.

### `activity_entity_links`

Connects activities to their participating entities. The optional
`organization_entity_id` is retained as participant-link metadata for
compatibility, while the activity's organization context is stored on
`activities.organization_entity_id`. `represented_entity_id` snapshots the
team or other entity the participant represented at the time of linking.
`is_active` allows an inactive participant to remain visible without being
treated as currently participating.
The database enforces at most one row per `(activity_id, entity_id)` pair;
the represented entity is metadata on that relation.

### `activity_broadcast_links`

Associates activities with the broadcasts that cover them. This is an explicit
many-to-many link because one activity can have several broadcasts and one
broadcast can support several activities.

### `facts`

Stores short normalized facts about exactly one subject: an activity, an
activity group, or an entity. The check constraint prevents a fact from
belonging to more than one kind of subject, or to none of them. Activity-group
facts are shared by every activity in that group.

### `fact_source_links`

Associates facts with the external sources that support them. The composite
primary key prevents the same source from being attached to one fact twice.

## Broadcast ingestion

### `broadcast_import_runs`

Records each broadcast-source collection run, including source, URI, timing,
status, and the number of imported broadcasts. It provides operational
history for the rows created or updated by ingestion.

### `broadcasts`

Stores normalized TV and streaming programme records from external sources.
The source key, external ID, fingerprint, channel, categories, schedule,
replay state, visibility, and optional entity/activity references support both
deduplication and editorial matching. The fingerprint is globally unique so
that equivalent records from different sources can converge on one row.

### `broadcast_channel_links`

Stores canonical broadcast channel names and their URLs. Each row also keeps
alternative names, active state, and update timestamps so channel links can be
maintained independently from imported broadcast rows.

### `broadcast_ignore`

Stores active rules for excluding broadcasts during ingestion. Rules can be
scoped by kind, value, and source, and retain a reason for manual auditing.

## Editorial todos

### `todos`

Stores manually entered editorial tasks. Each task is classified as applying
to broadcasts, activities, or entities. `correlation_id` is nullable so that
future versions can associate a task with one specific record without
changing the table shape.

## Member and notification model

### `members`

Stores passwordless member accounts, normalized email identities, verification
and login timestamps, and the member's push-notification lead-time setting. A
default lead time of zero keeps push notifications disabled until configured.

### `member_login_tokens`

Stores hashed, one-time login tokens used by the passwordless member flow.
Tokens retain their request, expiry, and consumption timestamps and are owned
by their member.

### `member_entity_watches`

Associates members with the entities they follow. The composite primary key
allows one watch per member and entity, and deleting either owner removes the
watch.

### `member_push_subscriptions`

Stores Web Push subscriptions for members, including the endpoint and the
cryptographic subscription keys required to deliver a notification. A single
endpoint belongs to at most one member.

### `member_activity_push_notifications`

Tracks the notification lifecycle for a member and activity: when it is
scheduled, claimed, sent, and last updated. Its composite key prevents
duplicate notification records for the same member and activity.

## AI configuration and execution

### `ai_providers`

Defines the configured AI backends, including provider kind, address, model,
request options, and enablement. Jobs reference a provider rather than
duplicating connection configuration.

### `ai_jobs`

Defines an AI operation and its runtime policy: output mode, provider, tools,
token limits, queue priority, web-search behavior, and active prompt.

### `ai_job_prompts`

Stores versioned system prompts, user templates, output schemas, and request
options for jobs. A run references the exact prompt version used.

### `ai_job_runs`

Stores the audit record for one AI execution. It contains the input and
rendered prompts, request and response payloads, status, timing, token counts,
tool trace, errors, and snapshots of the relevant job/provider/prompt settings.
The snapshots preserve historical explainability when configuration changes.
The raw request, raw response, and tool trace may be purged after the
diagnostic retention period; `diagnostic_payload_purged_at` records that this
has happened without removing the run's result or operational metadata.

### `ai_job_run_applications`

Records where an AI run was applied. Its composite key makes application
tracking idempotent for a run, target type, and target ID. Cleanup removes
application rows whose typed target no longer exists, while unknown target
types are retained for forward compatibility.

### `ai_automation_rules`

Maps application events to AI jobs. The unique event/job pair prevents
duplicate rules, while the enabled flag allows automation to be paused without
deleting its configuration.

### `activity_participant_ai_results`

Stores normalized AI-enriched participant fields for an activity and job.
Each row identifies the entity and field, keeps both a display value and JSON
value, preserves the supporting source when available, and records ordering
and, when still available, the run that produced it.

## Migration bookkeeping

### `schema_migrations`

Records which numbered migration files have been applied, together with their
checksums and application times. It protects the database from silently
running a changed migration file.
