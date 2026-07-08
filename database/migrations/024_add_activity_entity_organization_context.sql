alter table activity_entity_links
   add column organization_entity_id uuid null references entities(id)
      on delete set null;

create index if not exists
   activity_entity_links_organization_entity_id_idx
   on activity_entity_links(organization_entity_id);
