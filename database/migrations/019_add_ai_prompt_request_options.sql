alter table if exists ai_job_prompts
   add column if not exists request_options jsonb not null
      default '{}'::jsonb;

update ai_job_prompts
set request_options = jsonb_build_object(
   'tools',
   jsonb_build_array(
      jsonb_build_object(
         'type',
         'openrouter:web_search'
      )
   )
)
where id = '201c3db1-2e98-4df2-ade9-ef8a5901e201';

