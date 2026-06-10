alter table if exists ai_job_runs
add column if not exists provider_model text null;
