alter table entities
   add column if not exists person_gender_id text null;

do $$
begin
   if not exists (
      select 1
      from pg_constraint c
      join pg_class t on t.oid = c.conrelid
      where c.conname = 'entities_person_gender_id_valid'
         and t.relname = 'entities'
   ) then
      alter table entities
         add constraint entities_person_gender_id_valid
         check (
            person_gender_id is null or
            person_gender_id in ('female', 'male', 'non_binary')
         );
   end if;
end;
$$;

do $$
begin
   if not exists (
      select 1
      from pg_constraint c
      join pg_class t on t.oid = c.conrelid
      where c.conname =
         'entities_person_gender_only_for_persons'
         and t.relname = 'entities'
   ) then
      alter table entities
         add constraint entities_person_gender_only_for_persons
         check (
            entity_type_id = 'Person' or person_gender_id is null
         );
   end if;
end;
$$;
