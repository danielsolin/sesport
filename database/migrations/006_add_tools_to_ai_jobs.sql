alter table ai_jobs
   add column tools_json jsonb null;

alter table ai_jobs
   add column tools_description text null;
