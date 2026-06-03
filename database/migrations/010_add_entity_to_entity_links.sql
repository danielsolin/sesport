create table if not exists entity_to_entity_links
(
   id uuid primary key,
   source_entity_id uuid not null references tracked_entities(id)
      on delete cascade,
   target_entity_id uuid not null references tracked_entities(id)
      on delete cascade,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),

   constraint entity_to_entity_links_distinct_entities_check
      check (source_entity_id <> target_entity_id),
   constraint entity_to_entity_links_unique
      unique (source_entity_id, target_entity_id)
);

create index if not exists entity_to_entity_links_source_entity_id_idx
on entity_to_entity_links(source_entity_id);

create index if not exists entity_to_entity_links_target_entity_id_idx
on entity_to_entity_links(target_entity_id);
