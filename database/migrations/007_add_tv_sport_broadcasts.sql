create table if not exists tv_sport_import_runs
(
   id uuid primary key,
   source_key text not null,
   source_uri text null,
   started_at timestamptz not null,
   finished_at timestamptz null,
   status text not null,
   broadcast_count integer not null default 0,
   created_at timestamptz not null default now()
);

create table if not exists tv_sport_broadcasts
(
   id uuid primary key,
   import_run_id uuid null references tv_sport_import_runs(id),
   source_key text not null,
   external_id text not null,
   fingerprint text not null,
   channel_id text not null,
   channel_name text null,
   title text not null,
   description text null,
   categories text[] not null,
   starts_at timestamptz not null,
   ends_at timestamptz not null,
   time_zone_id text not null default 'Europe/Stockholm',
   raw_programme_xml text null,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),

   constraint tv_sport_broadcasts_time_check
      check (ends_at > starts_at),

   constraint tv_sport_broadcasts_fingerprint_unique
      unique (fingerprint)
);

create index if not exists tv_sport_broadcasts_starts_at_idx
on tv_sport_broadcasts (starts_at);

create index if not exists tv_sport_broadcasts_channel_id_idx
on tv_sport_broadcasts (channel_id);
