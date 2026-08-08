create table public.todos (
   id uuid not null primary key,
   target_type_id text not null,
   text text not null,
   correlation_id text,
   created_at timestamp with time zone default now() not null,
   done_at timestamp with time zone,
   constraint todos_target_type_id_check check (
      target_type_id in ('Broadcasts', 'Activities', 'Entities')
   ),
   constraint todos_text_not_blank_check check (btrim(text) <> '')
);

create index todos_open_created_at_idx
   on public.todos (created_at, id)
   where done_at is null;
