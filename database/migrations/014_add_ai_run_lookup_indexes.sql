create index if not exists ai_job_runs_job_corr_started_idx
   on ai_job_runs(job_id, correlation_id, started_at desc);

create index if not exists ai_job_runs_exec_claim_idx
   on ai_job_runs(
      execution_environment,
      status_id desc,
      started_at asc,
      created_at asc,
      id asc
   )
   where status_id in ('pending', 'running');

create index if not exists ai_job_runs_exec_env_idx
   on ai_job_runs(execution_environment)
   where execution_environment is not null
      and btrim(execution_environment) <> '';
