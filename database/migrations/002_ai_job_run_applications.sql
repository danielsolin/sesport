create table public.ai_job_run_applications
(
   run_id uuid not null
      references public.ai_job_runs(id) on delete cascade,
   target_type text not null,
   target_id text not null,
   applied_at timestamp with time zone not null default now(),
   primary key (run_id, target_type, target_id)
);

create index ai_job_run_applications_target_idx
   on public.ai_job_run_applications (target_type, target_id);
