create table if not exists tracked_entities
(
   id uuid primary key,
   canonical_name text not null,
   entity_type text not null,
   sport_id text not null,
   sport_name text not null,
   country_id text not null,
   country_code text not null,
   country_name text not null,
   country_relevance_kind text not null,
   country_relevance_reason text not null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),

   constraint tracked_entities_type_check
      check (entity_type in (
         'Person',
         'NationalTeam',
         'Club',
         'RecurringEvent',
         'Pair',
         'Organization',
         'Other'
      )),

   constraint tracked_entities_country_relevance_kind_check
      check (country_relevance_kind in (
         'NationalityOrSportingIdentity',
         'NationalTeamRepresentation',
         'BasedInCountry',
         'RecurringEventOriginOrInterest',
         'Manual'
      ))
);

create table if not exists entity_relationships
(
   id uuid primary key,
   subject_entity_id uuid not null references tracked_entities(id),
   relationship_type text not null,
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
   source_id text not null,
   source_name text not null,
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
   activity_type text not null,
   sport_id text not null,
   sport_name text not null,
   context text null,
   time_kind text not null,
   starts_at timestamptz null,
   starts_on date null,
   ends_on date null,
   time_description text null,
   country_relevance_explanation text not null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),

   constraint activities_type_check
      check (activity_type in (
         'Match',
         'Race',
         'Tournament',
         'Stage',
         'Championship',
         'Qualification',
         'RosterAnnouncement',
         'Transfer',
         'Ranking',
         'CoachingRole',
         'OtherSportingActivity'
      )),

   constraint activities_time_kind_check
      check (time_kind in ('ExactStart', 'DateRange', 'ToBeDetermined')),

   constraint activities_time_shape_check
      check (
         (time_kind = 'ExactStart' and starts_at is not null) or
         (time_kind = 'DateRange' and starts_on is not null and ends_on is not null and ends_on >= starts_on) or
         (time_kind = 'ToBeDetermined')
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
   producer_type text not null,
   source_id text not null,
   source_name text not null,
   external_id text null,
   fingerprint text not null,
   title text not null,
   description text null,
   raw_content text null,
   activity_type text not null,
   sport_external_id text not null,
   sport_name text not null,
   context text null,
   time_kind text not null,
   starts_at timestamptz null,
   starts_on date null,
   ends_on date null,
   time_description text null,
   confidence numeric(4,3) null,
   status text not null,
   group_id text null references activity_proposal_groups(id),
   activity_id uuid null references activities(id),
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),

   constraint activity_proposals_producer_type_check
      check (producer_type in ('WebImport', 'AiSearch', 'Manual')),

   constraint activity_proposals_status_check
      check (status in (
         'Pending',
         'Approved',
         'Rejected',
         'NeedsChanges',
         'Duplicate'
      )),

   constraint activity_proposals_confidence_check
      check (confidence is null or (confidence >= 0 and confidence <= 1)),

   constraint activity_proposals_type_check
      check (activity_type in (
         'Match',
         'Race',
         'Tournament',
         'Stage',
         'Championship',
         'Qualification',
         'RosterAnnouncement',
         'Transfer',
         'Ranking',
         'CoachingRole',
         'OtherSportingActivity'
      )),

   constraint activity_proposals_time_kind_check
      check (time_kind in ('ExactStart', 'DateRange', 'ToBeDetermined')),

   constraint activity_proposals_time_shape_check
      check (
         (time_kind = 'ExactStart' and starts_at is not null) or
         (time_kind = 'DateRange' and starts_on is not null and ends_on is not null and ends_on >= starts_on) or
         (time_kind = 'ToBeDetermined')
      ),

   constraint activity_proposals_activity_reference_check
      check (
         (status = 'Approved' and activity_id is not null) or
         (status <> 'Approved')
      )
);

create table if not exists activity_proposal_entity_links
(
   id uuid primary key,
   proposal_id text not null references activity_proposals(id),
   entity_id uuid not null references tracked_entities(id),
   proposed_role text not null,
   explanation text not null,
   context_name text null,
   confidence numeric(4,3) null,

   constraint activity_proposal_entity_links_role_check
      check (proposed_role in (
         'CompetesIn',
         'PlaysForContext',
         'SelectedForRoster',
         'TransferSubject',
         'CoachingRole',
         'RecurringEventEdition',
         'RelatedOrganization',
         'Other'
      )),

   constraint activity_proposal_entity_links_confidence_check
      check (confidence is null or (confidence >= 0 and confidence <= 1))
);

create table if not exists activity_proposal_evidence
(
   id uuid primary key,
   proposal_id text not null references activity_proposals(id),
   source_id text not null,
   source_name text not null,
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
   entity_id uuid not null references tracked_entities(id),
   role text not null,
   explanation text not null,
   context_name text null,

   constraint activity_entity_links_role_check
      check (role in (
         'CompetesIn',
         'PlaysForContext',
         'SelectedForRoster',
         'TransferSubject',
         'CoachingRole',
         'RecurringEventEdition',
         'RelatedOrganization',
         'Other'
      ))
);

create table if not exists activity_evidence
(
   id uuid primary key,
   activity_id uuid not null references activities(id),
   proposal_id text null references activity_proposals(id),
   source_id text not null,
   source_name text not null,
   uri text null,
   title text null,
   observed_at timestamptz not null,
   summary text not null,
   created_at timestamptz not null default now()
);
