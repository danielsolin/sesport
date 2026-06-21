create index if not exists entities_canonical_name_idx
   on entities(canonical_name);

create index if not exists sources_name_idx
   on sources(name);

create index if not exists activity_proposal_evidence_proposal_obs_idx
   on activity_proposal_evidence(proposal_id, observed_at desc);
