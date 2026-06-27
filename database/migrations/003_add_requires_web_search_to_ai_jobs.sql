begin;

alter table ai_jobs
   add column if not exists requires_web_search boolean not null
      default true;

commit;
