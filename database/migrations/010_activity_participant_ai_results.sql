create table public.activity_participant_ai_result_sets
(
   activity_id uuid not null
      references public.activities(id) on delete cascade,
   job_id text not null
      references public.ai_jobs(id) on delete cascade,
   run_id uuid not null
      references public.ai_job_runs(id) on delete cascade,
   checked_sources jsonb not null default '[]'::jsonb,
   created_at timestamp with time zone not null default now(),
   updated_at timestamp with time zone not null default now(),
   primary key (activity_id, job_id)
);

create index activity_participant_ai_result_sets_run_id_idx
   on public.activity_participant_ai_result_sets (run_id);

create table public.activity_participant_ai_result_values
(
   activity_id uuid not null,
   job_id text not null,
   entity_id uuid not null
      references public.entities(id) on delete cascade,
   field_key text not null,
   value_text text,
   value_json jsonb not null,
   sources jsonb not null default '[]'::jsonb,
   created_at timestamp with time zone not null default now(),
   updated_at timestamp with time zone not null default now(),
   constraint activity_participant_ai_result_values_field_key_not_blank_check
      check (btrim(field_key) <> ''),
   primary key (activity_id, job_id, entity_id, field_key),
   foreign key (activity_id, job_id)
      references public.activity_participant_ai_result_sets(
         activity_id,
         job_id
      )
      on delete cascade
);

create index activity_participant_ai_result_values_entity_id_idx
   on public.activity_participant_ai_result_values (entity_id);
