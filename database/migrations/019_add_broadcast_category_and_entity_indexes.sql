create index if not exists broadcasts_categories_gin_idx
   on broadcasts using gin (categories);

create index if not exists activity_entity_links_entity_id_idx
   on activity_entity_links(entity_id);
