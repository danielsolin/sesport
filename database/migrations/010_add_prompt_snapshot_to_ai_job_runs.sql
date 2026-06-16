alter table ai_job_runs
   add column if not exists prompt_version integer null;

alter table ai_job_runs
   add column if not exists prompt_system_prompt text null;

alter table ai_job_runs
   add column if not exists prompt_user_prompt_template text null;
