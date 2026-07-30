drop table public.activity_participant_ai_results;

create table public.activity_participant_ai_results
(
   id uuid not null primary key,
   activity_id uuid not null
      references public.activities(id) on delete cascade,
   job_id text not null
      references public.ai_jobs(id) on delete cascade,
   run_id uuid not null
      references public.ai_job_runs(id) on delete cascade,
   entity_id uuid not null
      references public.entities(id) on delete cascade,
   field_key text not null,
   value_text text,
   value_json jsonb not null,
   source_id uuid not null
      references public.sources(id) on delete cascade,
   sort_order integer not null,
   created_at timestamp with time zone not null default now(),
   updated_at timestamp with time zone not null default now(),
   constraint activity_participant_ai_results_field_key_not_blank_check
      check (btrim(field_key) <> '')
);

create index activity_participant_ai_results_run_id_idx
   on public.activity_participant_ai_results (run_id);

create index activity_participant_ai_results_activity_job_idx
   on public.activity_participant_ai_results (activity_id, job_id);

create index activity_participant_ai_results_entity_id_idx
   on public.activity_participant_ai_results (entity_id);

create index activity_participant_ai_results_source_id_idx
   on public.activity_participant_ai_results (source_id);

create index activity_participant_ai_results_sort_order_idx
   on public.activity_participant_ai_results (activity_id, job_id, sort_order);
