begin;

alter table broadcasts
   add column entity_id uuid null references entities(id)
      on delete set null;

create index broadcasts_entity_id_idx
   on broadcasts(entity_id);

commit;
