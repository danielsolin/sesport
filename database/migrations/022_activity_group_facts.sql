alter table public.facts
   add column activity_group_id uuid;

alter table public.facts
   drop constraint facts_exactly_one_subject_check;

alter table public.facts
   add constraint facts_activity_group_id_fkey
   foreign key (activity_group_id)
   references public.activity_groups(id)
   on delete cascade;

alter table public.facts
   add constraint facts_exactly_one_subject_check
   check (num_nonnulls(activity_id, activity_group_id, entity_id) = 1);

create index facts_activity_group_id_created_at_idx
   on public.facts (activity_group_id, created_at, id)
   where activity_group_id is not null;
