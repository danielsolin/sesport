alter table ai_jobs
   add column if not exists tools_json jsonb null;

alter table ai_jobs
   add column if not exists tools_description text null;
