create table if not exists ai_activity_search_runs
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
   lmstudio_plugin_id text null,
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

create table if not exists ai_activity_search_run_items
(
   id uuid primary key,
   run_id text not null references ai_activity_search_runs(id)
      on delete cascade,
   entity_id uuid null references tracked_entities(id),
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

create index if not exists ai_activity_search_run_items_run_id_idx
on ai_activity_search_run_items(run_id);

create index if not exists ai_activity_search_run_items_entity_id_idx
on ai_activity_search_run_items(entity_id);
