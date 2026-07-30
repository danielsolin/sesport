create table public.activity_participant_ai_result_sources
(
   id uuid not null primary key,
   activity_id uuid not null,
   job_id text not null,
   entity_id uuid,
   field_key text,
   source_id uuid not null
      references public.sources(id) on delete cascade,
   sort_order integer not null,
   created_at timestamp with time zone not null default now(),
   constraint activity_participant_ai_result_sources_scope_check
      check ((entity_id is null) = (field_key is null)),
   constraint activity_participant_ai_result_sources_unique unique nulls not
      distinct (activity_id, job_id, entity_id, field_key, source_id),
   foreign key (activity_id, job_id)
      references public.activity_participant_ai_result_sets(
         activity_id,
         job_id
      )
      on delete cascade,
   foreign key (activity_id, job_id, entity_id, field_key)
      references public.activity_participant_ai_result_values(
         activity_id,
         job_id,
         entity_id,
         field_key
      )
      on delete cascade
);

create index activity_participant_ai_result_sources_source_id_idx
   on public.activity_participant_ai_result_sources (source_id);

drop table public.activity_participant_ai_result_set_sources;

drop table public.activity_participant_ai_result_value_sources;
