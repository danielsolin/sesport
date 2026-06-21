create index if not exists activity_types_sort_label_idx
   on activity_types(sort_order, label);

create index if not exists sports_name_idx
   on sports(name);

create index if not exists countries_name_idx
   on countries(name);

create index if not exists entity_types_label_idx
   on entity_types(label);

create index if not exists entity_watch_priorities_sort_label_idx
   on entity_watch_priorities(sort_order, label);

create index if not exists entity_stability_kinds_sort_label_idx
   on entity_stability_kinds(sort_order, label);

create index if not exists activity_entity_link_roles_sort_label_idx
   on activity_entity_link_roles(sort_order, label);

create index if not exists producer_types_sort_label_idx
   on producer_types(sort_order, label);

create index if not exists proposal_statuses_sort_label_idx
   on proposal_statuses(sort_order, label);

create index if not exists proposal_reject_reasons_sort_label_idx
   on proposal_reject_reasons(sort_order, label);

create index if not exists activity_publication_statuses_sort_label_idx
   on activity_publication_statuses(sort_order, label);
