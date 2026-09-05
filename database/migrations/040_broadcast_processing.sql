begin;

alter table public.broadcasts
   add column processed_at timestamp with time zone;

create index broadcasts_unprocessed_visible_starts_at_idx
   on public.broadcasts (starts_at)
   where hidden_at is null and processed_at is null;

commit;
