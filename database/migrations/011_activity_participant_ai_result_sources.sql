create table public.activity_participant_ai_result_set_sources
(
   activity_id uuid not null,
   job_id text not null,
   source_id uuid not null
      references public.sources(id) on delete cascade,
   sort_order integer not null,
   created_at timestamp with time zone not null default now(),
   primary key (activity_id, job_id, source_id),
   foreign key (activity_id, job_id)
      references public.activity_participant_ai_result_sets(
         activity_id,
         job_id
      )
      on delete cascade
);

create index activity_participant_ai_result_set_sources_source_id_idx
   on public.activity_participant_ai_result_set_sources (source_id);

create table public.activity_participant_ai_result_value_sources
(
   activity_id uuid not null,
   job_id text not null,
   entity_id uuid not null,
   field_key text not null,
   source_id uuid not null
      references public.sources(id) on delete cascade,
   sort_order integer not null,
   created_at timestamp with time zone not null default now(),
   primary key (activity_id, job_id, entity_id, field_key, source_id),
   foreign key (activity_id, job_id, entity_id, field_key)
      references public.activity_participant_ai_result_values(
         activity_id,
         job_id,
         entity_id,
         field_key
      )
      on delete cascade
);

create index activity_participant_ai_result_value_sources_source_id_idx
   on public.activity_participant_ai_result_value_sources (source_id);

alter table public.activity_participant_ai_result_sets
   drop column checked_sources;

alter table public.activity_participant_ai_result_values
   drop column sources;
