create table if not exists competitions
(
   id text primary key,
   name text not null,
   sport_id text not null,
   sport_name text not null,
   status text not null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),

   constraint competitions_status_check
      check (status in ('Scheduled', 'Ongoing', 'Completed', 'Unknown'))
);

insert into competitions
(
   id,
   name,
   sport_id,
   sport_name,
   status
)
values
(
   'competition:iihf-world-championship-2026',
   '2026 IIHF Ice Hockey World Championship',
   'sport:ice-hockey',
   'Ice hockey',
   'Ongoing'
)
on conflict (id) do update
set
   name = excluded.name,
   sport_id = excluded.sport_id,
   sport_name = excluded.sport_name,
   status = excluded.status,
   updated_at = now();
