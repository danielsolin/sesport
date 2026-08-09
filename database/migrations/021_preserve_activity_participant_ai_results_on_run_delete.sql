alter table public.activity_participant_ai_results
   alter column run_id drop not null;

alter table public.activity_participant_ai_results
   drop constraint activity_participant_ai_results_run_id_fkey;

alter table public.activity_participant_ai_results
   add constraint activity_participant_ai_results_run_id_fkey
   foreign key (run_id)
   references public.ai_job_runs(id)
   on delete set null;
