create table sports
(
   id text primary key,
   name text not null,
   icon_id text null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now()
);

insert into sports (id, name, icon_id)
values
   ('football', 'Football', 'mdi:soccer'),
   ('ice-hockey', 'Ice hockey', 'mdi:hockey-puck')
on conflict (id) do nothing;

create table activity_types
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
on conflict (id) do nothing;

create table entity_types
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
on conflict (id) do nothing;

create table country_relevance_kinds
(
   id text primary key,
   label text not null,
   sort_order integer not null
);

insert into country_relevance_kinds (id, label, sort_order)
values
   ('NationalityOrSportingIdentity',
      'Nationality or sporting identity',
      10),
   ('NationalTeamRepresentation', 'National team representation', 20),
   ('BasedInCountry', 'Based in country', 30),
   (
      'RecurringEventOriginOrInterest',
      'Recurring event origin or interest',
      40
   ),
   ('Manual', 'Manual', 1000)
on conflict (id) do nothing;

create table entity_watch_priorities
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
on conflict (id) do nothing;

create table entity_stability_kinds
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
on conflict (id) do nothing;

create table activity_entity_link_roles
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
on conflict (id) do nothing;

create table producer_types
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
on conflict (id) do nothing;

create table proposal_statuses
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
on conflict (id) do nothing;

create table activity_publication_statuses
(
   id text primary key,
   label text not null,
   sort_order integer not null
);

insert into activity_publication_statuses (id, label, sort_order)
values
   ('Draft', 'Draft', 10),
   ('Published', 'Published', 20)
on conflict (id) do nothing;

create table proposal_reject_reasons
(
   id text primary key,
   label text not null,
   sort_order integer not null
);

insert into proposal_reject_reasons (id, label, sort_order)
values
   ('Hallucination', 'Hallucination', 10),
   ('Duplicate', 'Duplicate', 20),
   ('OutOfScope', 'Out of scope', 30)
on conflict (id) do nothing;

create table sources
(
   id text primary key,
   name text not null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now()
);

create table countries
(
   id text primary key,
   code text not null,
   name text not null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),
   constraint countries_code_unique unique (code)
);

insert into countries (id, code, name)
values ('se', 'SE', 'Sweden')
on conflict (id) do nothing;

create table activity_groups
(
   id uuid primary key,
   title text not null,
   sport_id text not null references sports(id),
   start_date date not null,
   end_date date not null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),
   constraint activity_groups_date_check
      check (end_date >= start_date)
);

create index activity_groups_sport_title_date_idx
   on activity_groups(sport_id, title, start_date, end_date);

create table entities
(
   id uuid primary key,
   canonical_name text not null,
   entity_type_id text not null references entity_types(id),
   sport_id text not null references sports(id),
   country_id text not null references countries(id),
   country_relevance_kind_id text not null
      references country_relevance_kinds(id),
   country_relevance_reason text not null,
   watch_priority_id text not null references entity_watch_priorities(id),
   expected_stability_id text not null references entity_stability_kinds(id),
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),
   person_gender_id text null,
   alias_name text null,
   constraint entities_person_gender_id_valid
      check (
         person_gender_id is null or
         person_gender_id in ('female', 'male', 'non_binary')
      ),
   constraint entities_person_gender_only_for_persons
      check (
         entity_type_id = 'Person' or person_gender_id is null
      )
);

create table activities
(
   id uuid primary key,
   title text not null,
   description text null,
   teaser text null,
   activity_type_id text not null references activity_types(id),
   sport_id text not null references sports(id),
   activity_date date not null,
   local_start_time time null,
   starts_at timestamptz null,
   time_zone_id text not null default 'Europe/Stockholm',
   publication_status_id text not null default 'Draft'
      references activity_publication_statuses(id),
   tv_channel_name text null,
   slug text null,
   published_at timestamptz null,
   facts text null,
   activity_group_id uuid null references activity_groups(id)
      on delete set null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),
   constraint activities_time_shape_check
      check (
         (local_start_time is not null and starts_at is not null) or
         (local_start_time is null and starts_at is null)
      )
);

create unique index activities_slug_unique
   on activities(slug)
   where slug is not null;

create index activities_publication_listing_idx
   on activities(publication_status_id, activity_date, local_start_time);

create index activities_activity_group_id_idx
   on activities(activity_group_id);

create table activity_proposals
(
   id text primary key,
   producer_type_id text not null references producer_types(id),
   producer text null,
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
   reject_reason_id text null references proposal_reject_reasons(id),
   reject_comment text null,
   activity_id uuid null references activities(id),
   prompt text null,
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
      ),
   constraint activity_proposals_reject_reason_status_check
      check (
         (status_id = 'Rejected') or
         (reject_reason_id is null and reject_comment is null)
      )
);

create table activity_proposal_entity_links
(
   id uuid primary key,
   proposal_id text not null references activity_proposals(id),
   entity_id uuid not null references entities(id),
   proposed_role_id text not null references activity_entity_link_roles(id),
   explanation text not null,
   context_name text null,
   confidence numeric(4,3) null,
   constraint activity_proposal_entity_links_confidence_check
      check (confidence is null or (confidence >= 0 and confidence <= 1))
);

create table activity_proposal_evidence
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

create table activity_entity_links
(
   id uuid primary key,
   activity_id uuid not null references activities(id),
   entity_id uuid not null references entities(id),
   organization_entity_id uuid null references entities(id)
      on delete set null
);

create index activity_entity_links_activity_id_idx
   on activity_entity_links(activity_id);

create index activity_entity_links_entity_id_idx
   on activity_entity_links(entity_id);

create index activity_entity_links_organization_entity_id_idx
   on activity_entity_links(organization_entity_id);

create table activity_evidence
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

create table entity_to_entity_links
(
   id uuid primary key,
   source_entity_id uuid not null references entities(id)
      on delete cascade,
   target_entity_id uuid not null references entities(id)
      on delete cascade,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),
   constraint entity_to_entity_links_distinct_entities_check
      check (source_entity_id <> target_entity_id),
   constraint entity_to_entity_links_unique
      unique (source_entity_id, target_entity_id)
);

create index entity_to_entity_links_source_entity_id_idx
   on entity_to_entity_links(source_entity_id);

create index entity_to_entity_links_target_entity_id_idx
   on entity_to_entity_links(target_entity_id);

create unique index entity_to_entity_links_entity_pair_unique
   on entity_to_entity_links (
      least(source_entity_id, target_entity_id),
      greatest(source_entity_id, target_entity_id)
   );

create table broadcast_import_runs
(
   id uuid primary key,
   source_key text not null,
   source_uri text null,
   started_at timestamptz not null,
   finished_at timestamptz null,
   status text not null,
   broadcast_count integer not null default 0,
   created_at timestamptz not null default now()
);

create table broadcasts
(
   id uuid primary key,
   import_run_id uuid null references broadcast_import_runs(id),
   source_key text not null,
   external_id text not null,
   fingerprint text not null,
   channel_id text not null,
   channel_name text null,
   title text not null,
   description text null,
   categories text[] not null,
   is_replay boolean not null default false,
   original_air_date date null,
   starts_at timestamptz not null,
   ends_at timestamptz not null,
   time_zone_id text not null default 'Europe/Stockholm',
   raw_programme_xml text null,
   hidden_at timestamptz null,
   entity_id uuid null references entities(id) on delete set null,
   activity_group_source_kind_id text null,
   activity_group_source_activity_id uuid null references activities(id)
      on delete set null,
   activity_group_draft_title text null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),
   constraint broadcasts_activity_group_source_kind_check
      check (
         activity_group_source_kind_id is null or
         activity_group_source_kind_id = 'ActivityGroupForActivity'
      ),
   constraint broadcasts_time_check
      check (ends_at > starts_at),
   constraint broadcasts_fingerprint_unique
      unique (fingerprint)
);

create index broadcasts_starts_at_idx
   on broadcasts (starts_at);

create index broadcasts_channel_id_idx
   on broadcasts (channel_id);

create index broadcasts_visible_starts_at_idx
   on broadcasts (starts_at)
   where hidden_at is null;

create index broadcasts_entity_id_idx
   on broadcasts(entity_id);

create index broadcasts_activity_group_source_activity_id_idx
   on broadcasts(activity_group_source_activity_id);

create index broadcasts_categories_gin_idx
   on broadcasts using gin (categories);

create table broadcast_ignore
(
   id uuid primary key,
   kind text not null,
   value text not null,
   source_key text null,
   reason text null,
   is_active boolean not null default true,
   created_at timestamptz not null default now(),
   constraint broadcast_ignore_kind_value_source_unique
      unique nulls not distinct (kind, value, source_key)
);

create index broadcast_ignore_active_kind_idx
   on broadcast_ignore (kind, source_key)
   where is_active = true;

insert into broadcast_ignore (
   id,
   kind,
   value,
   source_key,
   reason
)
values
   (
      '5ed80f1d-0d57-4f6b-a261-3c9b4b64df64',
      'channel_name',
      'Horse & Country TV',
      'iptv-epg-se',
      'Horse racing channel is outside the target sports scope.'
   ),
   (
      'f05f8c33-ef23-4e47-b9bf-22b6da761f6d',
      'channel_name',
      'ATG Live',
      'iptv-epg-se',
      'Horse racing channel is outside the target sports scope.'
   ),
   (
      '541ab42f-2799-40f2-a904-8c8de49bd45f',
      'channel_name',
      'Fight Sports',
      'iptv-epg-se',
      'Channel is outside the target sports scope.'
   ),
   (
      '8952ba8d-7b65-43c8-9d12-93a1c1de906c',
      'channel_name',
      'GINX eSports TV',
      'iptv-epg-se',
      'Channel is outside the target sports scope.'
   ),
   (
      '4914e8ef-89e0-4aa8-8914-f277c675b15c',
      'channel_name',
      'Extreme Sports Channel',
      'iptv-epg-se',
      'Channel is outside the target sports scope.'
   )
on conflict (id) do nothing;

create table ai_providers
(
   id text primary key,
   label text not null,
   kind text not null,
   base_address text null,
   model text null,
   api_key_source text null,
   request_options jsonb not null default '{}'::jsonb,
   enabled boolean not null default true,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now()
);

create table ai_jobs
(
   id text primary key,
   label text not null,
   description text null,
   provider_id text not null references ai_providers(id),
   output_mode text not null,
   enabled boolean not null default true,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),
   requires_web_search boolean not null default true,
   active_prompt_id uuid null,
   tools_json jsonb null,
   conditional_tools_json jsonb null
);

create table ai_job_prompts
(
   id uuid primary key,
   job_id text not null references ai_jobs(id) on delete cascade,
   version integer not null,
   system_prompt text not null,
   user_prompt_template text not null,
   output_schema jsonb null,
   request_options jsonb not null default '{}'::jsonb,
   temperature numeric(4,2) null,
   max_output_tokens integer null,
   enabled boolean not null default true,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),
   max_tool_rounds integer null,
   unique(job_id, version)
);

alter table ai_jobs
   add constraint ai_jobs_active_prompt_id_fkey
      foreign key (active_prompt_id)
      references ai_job_prompts(id)
      on delete set null;

create table ai_job_runs
(
   id uuid primary key,
   job_id text not null references ai_jobs(id),
   prompt_id uuid not null references ai_job_prompts(id),
   provider_id text not null references ai_providers(id),
   provider_model text null,
   status_id text not null,
   correlation_id text null,
   input_payload jsonb not null,
   rendered_prompt text not null,
   raw_request jsonb null,
   raw_response jsonb null,
   output_text text null,
   error_message text null,
   started_at timestamptz not null,
   completed_at timestamptz null,
   duration_seconds numeric(12,3) null,
   input_tokens integer null,
   output_tokens integer null,
   reasoning_tokens integer null,
   created_at timestamptz not null default now(),
   tool_trace jsonb null,
   tool_round_count integer not null default 0,
   conversation_character_count integer not null default 0,
   prompt_version integer null,
   prompt_system_prompt text null,
   prompt_user_prompt_template text null,
   execution_environment text null
);

create index ai_job_prompts_job_id_enabled_version_idx
   on ai_job_prompts(job_id, enabled, version desc);

create index ai_job_runs_job_id_started_at_idx
   on ai_job_runs(job_id, started_at desc);

create index ai_job_runs_provider_id_started_at_idx
   on ai_job_runs(provider_id, started_at desc);

create index ai_job_runs_status_id_started_at_idx
   on ai_job_runs(status_id, started_at desc);

create index ai_job_runs_started_at_desc_idx
   on ai_job_runs(started_at desc);

create index ai_job_runs_job_corr_started_idx
   on ai_job_runs(job_id, correlation_id, started_at desc);

create index ai_job_runs_exec_claim_idx
   on ai_job_runs(
      execution_environment,
      status_id desc,
      started_at asc,
      created_at asc,
      id asc
   )
   where status_id in ('pending', 'running');

create index ai_job_runs_exec_env_idx
   on ai_job_runs(execution_environment)
   where execution_environment is not null
      and btrim(execution_environment) <> '';

create table ai_activity_search_runs
(
   id text primary key,
   started_at timestamptz not null,
   completed_at timestamptz null,
   status_id text not null,
   client_mode text not null,
   base_address text not null,
   requested_model text not null,
   api_key_source text not null,
   allow_web_search boolean not null,
   web_search_tool_type text not null,
   plugin_id text null,
   search_date date not null,
   window_start date not null,
   window_end date not null,
   max_proposals integer not null,
   write_to_database boolean not null,
   run_directory text null,
   output_path text null,
   total_entity_count integer not null,
   completed_item_count integer not null default 0,
   failed_item_count integer not null default 0,
   proposal_count integer not null default 0,
   persisted_proposal_count integer not null default 0,
   error_message text null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now()
);

create table ai_activity_search_run_items
(
   id uuid primary key,
   run_id text not null references ai_activity_search_runs(id)
      on delete cascade,
   entity_id uuid null references entities(id),
   entity_key text not null,
   entity_name text not null,
   status_id text not null,
   proposal_count integer null,
   persisted_proposal_count integer null,
   result_path text null,
   failure_path text null,
   error_type text null,
   error_message text null,
   started_at timestamptz not null,
   completed_at timestamptz not null,
   duration_seconds numeric(12,3) not null,
   created_at timestamptz not null default now()
);

create index ai_activity_search_run_items_run_id_idx
   on ai_activity_search_run_items(run_id);

create index ai_activity_search_run_items_entity_id_idx
   on ai_activity_search_run_items(entity_id);
