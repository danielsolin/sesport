create table if not exists activity_publication_statuses
(
   id text primary key,
   label text not null,
   sort_order integer not null
);

insert into activity_publication_statuses (id, label, sort_order)
values
   ('Draft', 'Draft', 10),
   ('Published', 'Published', 20)
on conflict (id) do update
set
   label = excluded.label,
   sort_order = excluded.sort_order;

alter table activities
   add column if not exists publication_status_id text not null default 'Draft'
      references activity_publication_statuses(id);

alter table activities
   add column if not exists slug text null;

alter table activities
   add column if not exists published_at timestamptz null;

create unique index if not exists activities_slug_unique
   on activities(slug)
   where slug is not null;

create index if not exists activities_publication_listing_idx
   on activities(publication_status_id, activity_date, local_start_time);
