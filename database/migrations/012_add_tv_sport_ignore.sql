create table if not exists tv_sport_ignore (
   id uuid primary key,
   kind text not null,
   value text not null,
   source_key text null,
   reason text null,
   is_active boolean not null default true,
   created_at timestamptz not null default now(),
   constraint tv_sport_ignore_kind_value_source_unique
      unique nulls not distinct (kind, value, source_key)
);

create index if not exists tv_sport_ignore_active_kind_idx
on tv_sport_ignore (kind, source_key)
where is_active = true;

insert into tv_sport_ignore (
   id,
   kind,
   value,
   source_key,
   reason
)
values (
   '5ed80f1d-0d57-4f6b-a261-3c9b4b64df64',
   'channel_name',
   'SE - Horse & Country TV',
   'iptv-epg-se',
   'Horse racing channel is outside the target sports scope.'
),
(
   'f05f8c33-ef23-4e47-b9bf-22b6da761f6d',
   'channel_name',
   'ATG Live',
   'iptv-epg-se',
   'Horse racing channel is outside the target sports scope.'
),
(
   '541ab42f-2799-40f2-a904-8c8de49bd45f',
   'channel_name',
   'Fight Sports',
   'iptv-epg-se',
   'Channel is outside the target sports scope.'
),
(
   '8952ba8d-7b65-43c8-9d12-93a1c1de906c',
   'channel_name',
   'GINX eSports TV',
   'iptv-epg-se',
   'Channel is outside the target sports scope.'
),
(
   '4914e8ef-89e0-4aa8-8914-f277c675b15c',
   'channel_name',
   'Extreme Sports Channel',
   'iptv-epg-se',
   'Channel is outside the target sports scope.'
)
on conflict (kind, value, source_key) do update
set
   reason = excluded.reason,
   is_active = true;
