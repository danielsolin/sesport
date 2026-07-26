create table public.activity_broadcast_links
(
   activity_id uuid not null
      references public.activities(id) on delete cascade,
   broadcast_id uuid not null
      references public.broadcasts(id) on delete restrict,
   created_at timestamp with time zone not null default now(),
   primary key (activity_id, broadcast_id)
);

create index activity_broadcast_links_broadcast_idx
   on public.activity_broadcast_links (broadcast_id);
