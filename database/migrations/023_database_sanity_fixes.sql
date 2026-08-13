create unique index activity_entity_links_activity_entity_uidx
   on public.activity_entity_links (activity_id, entity_id);

create index broadcasts_import_run_id_idx
   on public.broadcasts (import_run_id);

create index broadcasts_ends_at_idx
   on public.broadcasts (ends_at);

create index activities_activity_date_idx
   on public.activities (activity_date);
