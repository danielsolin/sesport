create table if not exists sports
(
   id text primary key,
   name text not null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now()
);

insert into sports (id, name)
values
   ('football', 'Football'),
   ('ice-hockey', 'Ice hockey')
on conflict (id) do update
set name = excluded.name;

create table if not exists activity_types
(
   id text primary key,
   label text not null,
   sort_order integer not null,
   is_active boolean not null default true
);

insert into activity_types (id, label, sort_order)
values
   ('Match', 'Match', 10),
   ('Race', 'Race', 20),
   ('Tournament', 'Tournament', 30),
   ('Stage', 'Stage', 40),
   ('Championship', 'Championship', 50),
   ('Qualification', 'Qualification', 60),
   ('RosterAnnouncement', 'Roster announcement', 70),
   ('Transfer', 'Transfer', 80),
   ('Ranking', 'Ranking', 90),
   ('CoachingRole', 'Coaching role', 100),
   ('OtherSportingActivity', 'Other sporting activity', 1000)
on conflict (id) do update
set
   label = excluded.label,
   sort_order = excluded.sort_order;

create table if not exists entity_types
(
   id text primary key,
   label text not null,
   sort_order integer not null
);

insert into entity_types (id, label, sort_order)
values
   ('Person', 'Person', 10),
   ('NationalTeam', 'National team', 20),
   ('Club', 'Club', 30),
   ('RecurringEvent', 'Recurring event', 40),
   ('Pair', 'Pair/group', 50),
   ('Organization', 'Organization', 60),
   ('Other', 'Other', 1000)
on conflict (id) do update
set
   label = excluded.label,
   sort_order = excluded.sort_order;

create table if not exists country_relevance_kinds
(
   id text primary key,
   label text not null,
   sort_order integer not null
);

insert into country_relevance_kinds (id, label, sort_order)
values
   ('NationalityOrSportingIdentity', 'Nationality or sporting identity', 10),
   ('NationalTeamRepresentation', 'National team representation', 20),
   ('BasedInCountry', 'Based in country', 30),
   (
      'RecurringEventOriginOrInterest',
      'Recurring event origin or interest',
      40
   ),
   ('Manual', 'Manual', 1000)
on conflict (id) do update
set
   label = excluded.label,
   sort_order = excluded.sort_order;

create table if not exists entity_watch_priorities
(
   id text primary key,
   label text not null,
   sort_order integer not null
);

insert into entity_watch_priorities (id, label, sort_order)
values
   ('tier_1', 'Tier 1', 10),
   ('tier_2', 'Tier 2', 20),
   ('tier_3', 'Tier 3', 30),
   ('review', 'Review', 100)
on conflict (id) do update
set
   label = excluded.label,
   sort_order = excluded.sort_order;

create table if not exists entity_stability_kinds
(
   id text primary key,
   label text not null,
   sort_order integer not null
);

insert into entity_stability_kinds (id, label, sort_order)
values
   ('long_term', 'Long term', 10),
   ('medium_term', 'Medium term', 20),
   ('short_term', 'Short term', 30)
on conflict (id) do update
set
   label = excluded.label,
   sort_order = excluded.sort_order;

create table if not exists entity_relationship_types
(
   id text primary key,
   label text not null,
   sort_order integer not null
);

insert into entity_relationship_types (id, label, sort_order)
values
   ('PlaysFor', 'Plays for', 10),
   ('CompetesOn', 'Competes on', 20),
   ('Coaches', 'Coaches', 30),
   ('OrganizedBy', 'Organized by', 40),
   ('Other', 'Other', 1000)
on conflict (id) do update
set
   label = excluded.label,
   sort_order = excluded.sort_order;

create table if not exists activity_entity_link_roles
(
   id text primary key,
   label text not null,
   sort_order integer not null
);

insert into activity_entity_link_roles (id, label, sort_order)
values
   ('CompetesIn', 'Competes in', 10),
   ('PlaysForContext', 'Plays for context', 20),
   ('SelectedForRoster', 'Selected for roster', 30),
   ('TransferSubject', 'Transfer subject', 40),
   ('CoachingRole', 'Coaching role', 50),
   ('RecurringEventEdition', 'Recurring event edition', 60),
   ('RelatedOrganization', 'Related organization', 70),
   ('Other', 'Other', 1000)
on conflict (id) do update
set
   label = excluded.label,
   sort_order = excluded.sort_order;

create table if not exists producer_types
(
   id text primary key,
   label text not null,
   sort_order integer not null
);

insert into producer_types (id, label, sort_order)
values
   ('WebImport', 'Web import', 10),
   ('AiSearch', 'AI search', 20),
   ('Manual', 'Manual', 30)
on conflict (id) do update
set
   label = excluded.label,
   sort_order = excluded.sort_order;

create table if not exists proposal_statuses
(
   id text primary key,
   label text not null,
   sort_order integer not null
);

insert into proposal_statuses (id, label, sort_order)
values
   ('Pending', 'Pending', 10),
   ('Approved', 'Approved', 20),
   ('Rejected', 'Rejected', 30),
   ('NeedsChanges', 'Needs changes', 40),
   ('Duplicate', 'Duplicate', 50)
on conflict (id) do update
set
   label = excluded.label,
   sort_order = excluded.sort_order;

create table if not exists sources
(
   id text primary key,
   name text not null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now()
);

create table if not exists tracked_entities
(
   id uuid primary key,
   canonical_name text not null,
   entity_type_id text not null references entity_types(id),
   sport_id text not null references sports(id),
   country_id text not null,
   country_code text not null,
   country_name text not null,
   country_relevance_kind_id text not null references country_relevance_kinds(id),
   country_relevance_reason text not null,
   watch_priority_id text not null references entity_watch_priorities(id),
   expected_stability_id text not null references entity_stability_kinds(id),
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now()
);

create table if not exists entity_relationships
(
   id uuid primary key,
   subject_entity_id uuid not null references tracked_entities(id),
   relationship_type_id text not null references entity_relationship_types(id),
   target_name text not null,
   target_kind text not null,
   valid_from date null,
   valid_to date null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),

   constraint entity_relationships_valid_range_check
      check (valid_to is null or valid_from is null or valid_to >= valid_from)
);

create table if not exists entity_evidence
(
   id uuid primary key,
   entity_id uuid null references tracked_entities(id),
   relationship_id uuid null references entity_relationships(id),
   source_id text not null references sources(id),
   uri text null,
   title text null,
   observed_at timestamptz not null,
   summary text not null,
   created_at timestamptz not null default now(),

   constraint entity_evidence_owner_check
      check (
         (entity_id is not null and relationship_id is null) or
         (entity_id is null and relationship_id is not null)
      )
);

create table if not exists activities
(
   id uuid primary key,
   title text not null,
   description text null,
   activity_type_id text not null references activity_types(id),
   sport_id text not null references sports(id),
   activity_date date not null,
   local_start_time time null,
   starts_at timestamptz null,
   time_zone_id text not null default 'Europe/Stockholm',
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),

   constraint activities_time_shape_check
      check (
         (local_start_time is not null and starts_at is not null) or
         (local_start_time is null and starts_at is null)
      )
);

create table if not exists activity_proposal_groups
(
   id text primary key,
   fingerprint text not null,
   activity_id uuid null references activities(id),
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now()
);

create table if not exists activity_proposals
(
   id text primary key,
   producer_type_id text not null references producer_types(id),
   source_id text not null references sources(id),
   external_id text null,
   fingerprint text not null,
   title text not null,
   description text null,
   raw_content text null,
   activity_type_id text not null references activity_types(id),
   sport_id text not null references sports(id),
   context text null,
   activity_date date not null,
   local_start_time time null,
   starts_at timestamptz null,
   time_zone_id text not null default 'Europe/Stockholm',
   confidence numeric(4,3) null,
   status_id text not null references proposal_statuses(id),
   group_id text null references activity_proposal_groups(id),
   activity_id uuid null references activities(id),
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),

   constraint activity_proposals_confidence_check
      check (confidence is null or (confidence >= 0 and confidence <= 1)),

   constraint activity_proposals_time_shape_check
      check (
         (local_start_time is not null and starts_at is not null) or
         (local_start_time is null and starts_at is null)
      ),

   constraint activity_proposals_activity_reference_check
      check (
         (status_id = 'Approved' and activity_id is not null) or
         (status_id <> 'Approved')
      )
);

create table if not exists activity_proposal_entity_links
(
   id uuid primary key,
   proposal_id text not null references activity_proposals(id),
   entity_id uuid not null references tracked_entities(id),
   proposed_role_id text not null references activity_entity_link_roles(id),
   explanation text not null,
   context_name text null,
   confidence numeric(4,3) null,

   constraint activity_proposal_entity_links_confidence_check
      check (confidence is null or (confidence >= 0 and confidence <= 1))
);

create table if not exists activity_proposal_evidence
(
   id uuid primary key,
   proposal_id text not null references activity_proposals(id),
   source_id text not null references sources(id),
   uri text null,
   title text null,
   observed_at timestamptz not null,
   summary text not null,
   raw_excerpt text null,
   created_at timestamptz not null default now()
);

create table if not exists activity_entity_links
(
   id uuid primary key,
   activity_id uuid not null references activities(id),
   entity_id uuid not null references tracked_entities(id)
);

create table if not exists activity_evidence
(
   id uuid primary key,
   activity_id uuid not null references activities(id),
   proposal_id text null references activity_proposals(id),
   source_id text not null references sources(id),
   uri text null,
   title text null,
   observed_at timestamptz not null,
   comment text null,
   created_at timestamptz not null default now()
);
