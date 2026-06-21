create index if not exists entities_type_name_idx
   on entities(entity_type_id, canonical_name);

create index if not exists activity_entity_links_activity_id_idx
   on activity_entity_links(activity_id);

create index if not exists activity_proposal_entity_links_proposal_id_idx
   on activity_proposal_entity_links(proposal_id);

create index if not exists activity_proposal_evidence_proposal_id_idx
   on activity_proposal_evidence(proposal_id);

create index if not exists activity_proposals_status_date_time_title_idx
   on activity_proposals(
      status_id,
      activity_date,
      local_start_time,
      title
   );
