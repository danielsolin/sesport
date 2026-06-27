alter table ai_jobs
   add column if not exists active_prompt_id uuid null;

update ai_jobs j
set active_prompt_id = (
   select p.id
   from ai_job_prompts p
   where p.job_id = j.id
      and p.enabled = true
   order by p.version desc
   limit 1
);

do $$
begin
   if not exists (
      select 1
      from pg_constraint c
      join pg_class t on t.oid = c.conrelid
      where c.conname = 'ai_jobs_active_prompt_id_fkey'
         and t.relname = 'ai_jobs'
   ) then
      alter table ai_jobs
         add constraint ai_jobs_active_prompt_id_fkey
         foreign key (active_prompt_id)
         references ai_job_prompts(id)
         on delete set null;
   end if;
end;
$$;
