create table public.members (
   id uuid not null primary key,
   email text not null,
   email_normalized text not null,
   email_verified_at timestamp with time zone,
   created_at timestamp with time zone default now() not null,
   updated_at timestamp with time zone default now() not null,
   last_login_at timestamp with time zone,
   constraint members_email_not_blank_check
      check (length(btrim(email)) > 0),
   constraint members_email_normalized_not_blank_check
      check (length(btrim(email_normalized)) > 0)
);

create unique index members_email_normalized_uidx
   on public.members (email_normalized);

create table public.member_login_tokens (
   id uuid not null primary key,
   member_id uuid not null,
   token_hash text not null,
   requested_at timestamp with time zone not null,
   expires_at timestamp with time zone not null,
   consumed_at timestamp with time zone,
   constraint member_login_tokens_member_id_fkey
      foreign key (member_id)
      references public.members(id)
      on delete cascade,
   constraint member_login_tokens_hash_unique
      unique (token_hash),
   constraint member_login_tokens_expiry_check
      check (expires_at > requested_at)
);

create index member_login_tokens_member_requested_idx
   on public.member_login_tokens (member_id, requested_at desc);

create index member_login_tokens_expires_at_idx
   on public.member_login_tokens (expires_at);

create table public.member_entity_watches (
   member_id uuid not null,
   entity_id uuid not null,
   created_at timestamp with time zone default now() not null,
   constraint member_entity_watches_pkey
      primary key (member_id, entity_id),
   constraint member_entity_watches_member_id_fkey
      foreign key (member_id)
      references public.members(id)
      on delete cascade,
   constraint member_entity_watches_entity_id_fkey
      foreign key (entity_id)
      references public.entities(id)
      on delete cascade
);

create index member_entity_watches_entity_id_idx
   on public.member_entity_watches (entity_id);
