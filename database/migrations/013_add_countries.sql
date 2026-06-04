create table if not exists countries
(
   id text primary key,
   code text not null,
   name text not null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),

   constraint countries_code_unique unique (code)
);

insert into countries (id, code, name)
values ('se', 'SE', 'Sweden')
on conflict (id) do update
set
   code = excluded.code,
   name = excluded.name,
   updated_at = now();

do $$
begin
   if exists (
      select 1
      from information_schema.columns
      where table_name = 'tracked_entities'
        and column_name = 'country_code'
   ) and exists (
      select 1
      from information_schema.columns
      where table_name = 'tracked_entities'
        and column_name = 'country_name'
   ) then
      insert into countries (id, code, name)
      select distinct on (lower(trim(country_id)))
         lower(trim(country_id)),
         upper(trim(country_code)),
         trim(country_name)
      from tracked_entities
      where coalesce(trim(country_id), '') <> ''
        and coalesce(trim(country_code), '') <> ''
        and not exists (
           select 1
           from countries c
           where c.code = upper(trim(tracked_entities.country_code))
        )
      order by lower(trim(country_id)), trim(country_name)
      on conflict (id) do nothing;

      update tracked_entities e
      set country_id = c.id
      from countries c
      where c.code = upper(trim(e.country_code));

      update tracked_entities
      set country_id = lower(trim(country_id))
      where exists (
         select 1
         from countries c
         where c.id = lower(trim(tracked_entities.country_id))
      );
   end if;
end $$;

do $$
begin
   if not exists (
      select 1
      from pg_constraint
      where conname = 'tracked_entities_country_id_fk'
   ) then
      alter table tracked_entities
      add constraint tracked_entities_country_id_fk
      foreign key (country_id) references countries(id);
   end if;
end $$;

alter table tracked_entities
drop column if exists country_code,
drop column if exists country_name;
