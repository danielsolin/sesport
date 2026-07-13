alter table ai_jobs
   add column if not exists conditional_tools_json jsonb null;
