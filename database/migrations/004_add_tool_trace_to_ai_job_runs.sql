alter table ai_job_runs
   add column if not exists tool_trace jsonb null;
