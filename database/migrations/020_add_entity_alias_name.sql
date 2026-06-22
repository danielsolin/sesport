alter table entities
   add column if not exists alias_name text null;

alter table entities
   add constraint entities_alias_name_only_for_persons
   check (
      entity_type_id = 'Person' or alias_name is null
   );
