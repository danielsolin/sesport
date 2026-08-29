begin;

alter table public.activity_participant_ai_results
   alter column source_id drop not null;

alter table public.activity_participant_ai_results
   drop constraint activity_participant_ai_results_source_id_fkey;

alter table public.activity_participant_ai_results
   add constraint activity_participant_ai_results_source_id_fkey
   foreign key (source_id)
   references public.sources(id)
   on delete set null;

commit;
