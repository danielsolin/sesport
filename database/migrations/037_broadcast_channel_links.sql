begin;

create table public.broadcast_channel_links
(
   canonical_name text primary key,
   url text not null,
   aliases text[] default '{}'::text[] not null,
   is_active boolean default true not null,
   created_at timestamp with time zone default now() not null,
   updated_at timestamp with time zone default now() not null,
   constraint broadcast_channel_links_canonical_name_check
      check (btrim(canonical_name) <> ''),
   constraint broadcast_channel_links_url_check
      check (btrim(url) <> '')
);

create index broadcast_channel_links_active_name_idx
   on public.broadcast_channel_links (is_active, canonical_name);

commit;
