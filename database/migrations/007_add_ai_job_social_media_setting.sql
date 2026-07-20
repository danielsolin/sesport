alter table ai_jobs
   add column include_social_media boolean not null default false;

alter table ai_job_runs
   add column job_include_social_media boolean null;
