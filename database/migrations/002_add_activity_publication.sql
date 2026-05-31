alter table activities
   add column if not exists publication_status text not null default 'Draft';

alter table activities
   add column if not exists slug text null;

alter table activities
   add column if not exists published_at timestamptz null;

do $$
begin
   if not exists (
      select 1
      from pg_constraint
      where conname = 'activities_publication_status_check'
   ) then
      alter table activities
         add constraint activities_publication_status_check
         check (publication_status in ('Draft', 'Published'));
   end if;
end $$;

create unique index if not exists activities_slug_unique
   on activities(slug)
   where slug is not null;

create index if not exists activities_publication_listing_idx
   on activities(publication_status, starts_at, starts_on);
