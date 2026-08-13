alter table public.ai_job_runs
   add column diagnostic_payload_purged_at timestamp with time zone;

create index ai_job_runs_diagnostic_retention_idx
   on public.ai_job_runs (created_at)
   where status_id in ('completed', 'failed', 'archived')
      and diagnostic_payload_purged_at is null;
