alter table ai_job_runs
   add column if not exists execution_environment text null;
