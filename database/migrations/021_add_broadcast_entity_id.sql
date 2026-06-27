begin;

alter table broadcasts
   add column if not exists entity_id uuid null references entities(id)
      on delete set null;

create index if not exists broadcasts_entity_id_idx
   on broadcasts(entity_id);

commit;
