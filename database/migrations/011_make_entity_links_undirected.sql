delete from entity_to_entity_links duplicate
using entity_to_entity_links kept
where duplicate.id > kept.id
  and duplicate.source_entity_id = kept.target_entity_id
  and duplicate.target_entity_id = kept.source_entity_id;

create unique index if not exists entity_to_entity_links_entity_pair_unique
on entity_to_entity_links (
   least(source_entity_id, target_entity_id),
   greatest(source_entity_id, target_entity_id)
);
