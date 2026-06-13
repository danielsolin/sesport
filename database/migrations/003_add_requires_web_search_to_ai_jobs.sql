begin;

alter table ai_jobs
   add column requires_web_search boolean not null default true;

commit;
