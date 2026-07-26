create table public.ai_automation_rules
(
   id uuid primary key,
   event_id text not null,
   job_id text not null
      references public.ai_jobs(id) on delete restrict,
   enabled boolean not null default true,
   created_at timestamp with time zone not null default now(),
   updated_at timestamp with time zone not null default now(),
   unique (event_id, job_id)
);

create index ai_automation_rules_event_idx
   on public.ai_automation_rules (event_id)
   where enabled;
