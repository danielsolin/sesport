drop table public.activity_participant_ai_result_sources;

drop table public.activity_participant_ai_result_values;

alter table public.activity_participant_ai_result_sets
   rename to activity_participant_ai_results;

drop index if exists public.activity_participant_ai_result_sets_run_id_idx;

alter table public.activity_participant_ai_results
   add column id uuid,
   add column parent_id uuid,
   add column row_kind text,
   add column entity_id uuid,
   add column field_key text,
   add column value_text text,
   add column value_json jsonb,
   add column source_id uuid,
   add column sort_order integer;

alter table public.activity_participant_ai_results
   alter column run_id drop not null;

alter table public.activity_participant_ai_results
   alter column id set not null;

alter table public.activity_participant_ai_results
   alter column row_kind set not null;

alter table public.activity_participant_ai_results
   drop constraint activity_participant_ai_result_sets_pkey;

alter table public.activity_participant_ai_results
   add constraint activity_participant_ai_results_pkey primary key (id);

alter table public.activity_participant_ai_results
   add constraint activity_participant_ai_results_row_kind_check
      check (row_kind in ('set', 'value', 'source'));

alter table public.activity_participant_ai_results
   add constraint activity_participant_ai_results_set_row_check
      check (
         row_kind <> 'set' or (
            run_id is not null and
            parent_id is null and
            entity_id is null and
            field_key is null and
            value_text is null and
            value_json is null and
            source_id is null and
            sort_order is null
         )
      );

alter table public.activity_participant_ai_results
   add constraint activity_participant_ai_results_value_row_check
      check (
         row_kind <> 'value' or (
            parent_id is not null and
            entity_id is not null and
            field_key is not null and
            btrim(field_key) <> '' and
            value_json is not null and
            source_id is null and
            sort_order is null
         )
      );

alter table public.activity_participant_ai_results
   add constraint activity_participant_ai_results_source_row_check
      check (
         row_kind <> 'source' or (
            parent_id is not null and
            source_id is not null and
            entity_id is null and
            field_key is null and
            value_text is null and
            value_json is null and
            sort_order is not null
         )
      );

alter table public.activity_participant_ai_results
   add constraint activity_participant_ai_results_parent_id_fkey
      foreign key (parent_id)
      references public.activity_participant_ai_results(id)
      on delete cascade;

alter table public.activity_participant_ai_results
   add constraint activity_participant_ai_results_source_id_fkey
      foreign key (source_id)
      references public.sources(id)
      on delete cascade;

alter table public.activity_participant_ai_results
   add constraint activity_participant_ai_results_entity_id_fkey
      foreign key (entity_id)
      references public.entities(id)
      on delete cascade;

create index activity_participant_ai_results_run_id_idx
   on public.activity_participant_ai_results (run_id);

create index activity_participant_ai_results_parent_id_idx
   on public.activity_participant_ai_results (parent_id);

create index activity_participant_ai_results_source_id_idx
   on public.activity_participant_ai_results (source_id);

create index activity_participant_ai_results_entity_id_idx
   on public.activity_participant_ai_results (entity_id);

create index activity_participant_ai_results_job_kind_idx
   on public.activity_participant_ai_results (
      activity_id,
      job_id,
      row_kind
   );

create unique index activity_participant_ai_results_set_unique_idx
   on public.activity_participant_ai_results (activity_id, job_id)
   where row_kind = 'set';

create unique index activity_participant_ai_results_source_unique_idx
   on public.activity_participant_ai_results (parent_id, source_id)
   where row_kind = 'source';
