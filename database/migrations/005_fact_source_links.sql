create table public.fact_source_links
(
   fact_id uuid not null
      references public.facts(id) on delete cascade,
   source_id uuid not null
      references public.sources(id) on delete cascade,
   created_at timestamp with time zone not null default now(),
   primary key (fact_id, source_id)
);

create index fact_source_links_source_id_idx
   on public.fact_source_links (source_id);
