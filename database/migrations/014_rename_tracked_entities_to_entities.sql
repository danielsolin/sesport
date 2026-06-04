do $$
begin
   if to_regclass('public.entities') is null
      and to_regclass('public.tracked_entities') is not null then
      alter table tracked_entities rename to entities;
   end if;
end $$;

do $$
begin
   if exists (
      select 1
      from pg_constraint
      where conname = 'tracked_entities_pkey'
        and conrelid = to_regclass('public.entities')
   ) then
      alter table entities
      rename constraint tracked_entities_pkey to entities_pkey;
   end if;

   if exists (
      select 1
      from pg_constraint
      where conname = 'tracked_entities_country_id_fk'
        and conrelid = to_regclass('public.entities')
   ) then
      alter table entities
      rename constraint tracked_entities_country_id_fk
      to entities_country_id_fk;
   end if;
end $$;
