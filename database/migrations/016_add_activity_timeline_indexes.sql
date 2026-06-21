create index if not exists activities_starts_at_idx
   on activities(starts_at);

create index if not exists activity_evidence_activity_created_idx
   on activity_evidence(activity_id, created_at desc);

create index if not exists activity_proposals_activity_id_idx
   on activity_proposals(activity_id);
