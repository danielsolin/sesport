alter table if exists ai_job_runs
add column if not exists raw_request jsonb null;

update ai_job_runs r
set raw_request =
   jsonb_build_object(
      'model',
      p.model,
      'input',
      r.rendered_prompt
   )
   ||
   case
      when pr.max_output_tokens is null then '{}'::jsonb
      else jsonb_build_object(
         'max_output_tokens',
         pr.max_output_tokens
      )
   end
   ||
   case
      when pr.temperature is null then '{}'::jsonb
      else jsonb_build_object('temperature', pr.temperature)
   end
   ||
   case
      when j.output_mode = 'json_object' then
         '{"response_format":{"type":"json_object"}}'::jsonb
      when j.output_mode = 'json_schema' and pr.output_schema is not null then
         jsonb_build_object(
            'response_format',
            jsonb_build_object(
               'type',
               'json_schema',
               'json_schema',
               jsonb_build_object(
                  'name',
                  'prompt_' || replace(pr.id::text, '-', ''),
                  'strict',
                  true,
                  'schema',
                  pr.output_schema
               )
            )
         )
      else '{}'::jsonb
   end
   ||
   coalesce(p.request_options, '{}'::jsonb)
   ||
   coalesce(pr.request_options, '{}'::jsonb)
from ai_jobs j,
     ai_providers p,
     ai_job_prompts pr
where r.job_id = j.id
  and p.id = j.provider_id
  and pr.id = r.prompt_id
  and r.raw_request is null;
