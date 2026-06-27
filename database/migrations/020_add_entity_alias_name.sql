alter table entities
   add column if not exists alias_name text null;

do $$
begin
   if not exists (
      select 1
      from pg_constraint c
      join pg_class t on t.oid = c.conrelid
      where c.conname = 'entities_alias_name_only_for_persons'
         and t.relname = 'entities'
   ) then
      alter table entities
         add constraint entities_alias_name_only_for_persons
         check (
            entity_type_id = 'Person' or alias_name is null
         );
   end if;
end;
$$;
