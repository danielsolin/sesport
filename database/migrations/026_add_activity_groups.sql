create table activity_groups
(
   id uuid primary key,
   title text not null,
   sport_id text not null references sports(id),
   start_date date not null,
   end_date date not null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),
   constraint activity_groups_date_check
      check (end_date >= start_date)
);

create index activity_groups_sport_title_date_idx
   on activity_groups(sport_id, title, start_date, end_date);

alter table activities
   add column if not exists activity_group_id uuid null
      references activity_groups(id)
      on delete set null;

create index if not exists activities_activity_group_id_idx
   on activities(activity_group_id);

alter table broadcasts
   add column if not exists activity_group_id uuid null
      references activity_groups(id)
      on delete set null;

create index if not exists broadcasts_activity_group_id_idx
   on broadcasts(activity_group_id);
