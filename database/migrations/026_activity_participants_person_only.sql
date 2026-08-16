alter table public.entities
   add constraint entities_id_entity_type_uidx
   unique (id, entity_type_id);

alter table public.activity_entity_links
   add column participant_entity_type_id text
   generated always as ('Person'::text) stored;

alter table public.activity_entity_links
   add constraint activity_entity_links_person_entity_fkey
   foreign key (entity_id, participant_entity_type_id)
   references public.entities (id, entity_type_id)
   not valid;
