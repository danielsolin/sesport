alter table public.activity_entity_links
   add column represented_entity_id uuid;

alter table public.activity_entity_links
   add constraint activity_entity_links_represented_entity_id_fkey
   foreign key (represented_entity_id)
   references public.entities(id)
   on delete restrict;

create index activity_entity_links_represented_entity_id_idx
   on public.activity_entity_links (represented_entity_id);
