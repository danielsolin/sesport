create table public.facts
(
   id uuid primary key,
   activity_id uuid
      references public.activities(id) on delete cascade,
   entity_id uuid
      references public.entities(id) on delete cascade,
   fact_text text not null,
   created_at timestamp with time zone not null default now(),
   updated_at timestamp with time zone not null default now(),
   constraint facts_exactly_one_subject_check check (
      (activity_id is null) <> (entity_id is null)
   ),
   constraint facts_text_not_blank_check check (
      btrim(fact_text) <> ''
   )
);

create index facts_activity_id_created_at_idx
   on public.facts (activity_id, created_at, id)
   where activity_id is not null;

create index facts_entity_id_created_at_idx
   on public.facts (entity_id, created_at, id)
   where entity_id is not null;
