create table if not exists ai_providers
(
   id text primary key,
   label text not null,
   kind text not null,
   base_address text null,
   model text null,
   api_key_source text null,
   request_options jsonb not null default '{}'::jsonb,
   enabled boolean not null default true,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now()
);

create table if not exists ai_jobs
(
   id text primary key,
   label text not null,
   description text null,
   provider_id text not null references ai_providers(id),
   output_mode text not null,
   enabled boolean not null default true,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now()
);

create table if not exists ai_job_prompts
(
   id uuid primary key,
   job_id text not null references ai_jobs(id) on delete cascade,
   version integer not null,
   system_prompt text not null,
   user_prompt_template text not null,
   output_schema jsonb null,
   temperature numeric(4,2) null,
   max_output_tokens integer null,
   enabled boolean not null default true,
   created_at timestamptz not null default now(),
   updated_at timestamptz not null default now(),
   unique(job_id, version)
);

create table if not exists ai_job_runs
(
   id uuid primary key,
   job_id text not null references ai_jobs(id),
   prompt_id uuid not null references ai_job_prompts(id),
   provider_id text not null references ai_providers(id),
   status_id text not null,
   correlation_id text null,
   input_payload jsonb not null,
   rendered_prompt text not null,
   raw_response jsonb null,
   output_text text null,
   error_message text null,
   started_at timestamptz not null,
   completed_at timestamptz null,
   duration_seconds numeric(12,3) null,
   input_tokens integer null,
   output_tokens integer null,
   reasoning_tokens integer null,
   created_at timestamptz not null default now()
);

create index if not exists ai_job_prompts_job_id_enabled_version_idx
on ai_job_prompts(job_id, enabled, version desc);

create index if not exists ai_job_runs_job_id_started_at_idx
on ai_job_runs(job_id, started_at desc);

create index if not exists ai_job_runs_provider_id_started_at_idx
on ai_job_runs(provider_id, started_at desc);

create index if not exists ai_job_runs_status_id_started_at_idx
on ai_job_runs(status_id, started_at desc);

insert into ai_providers (
   id,
   label,
   kind,
   base_address,
   model,
   api_key_source,
   request_options
)
values (
   'openrouter-free',
   'OpenRouter Free',
   'openrouter',
   'https://openrouter.ai/api/v1/',
   'openrouter/free',
   'environment:OPENROUTER_API_KEY',
   '{}'::jsonb
)
on conflict (id) do nothing;

insert into ai_jobs (
   id,
   label,
   description,
   provider_id,
   output_mode
)
values (
   'generate-activity-teaser',
   'Generate activity teaser',
   'Create a short Swedish teaser for an activity.',
   'openrouter-free',
   'json_object'
)
on conflict (id) do nothing;

insert into ai_job_prompts (
   id,
   job_id,
   version,
   system_prompt,
   user_prompt_template,
   output_schema,
   temperature,
   max_output_tokens
)
values (
   '00000000-0000-0000-0000-000000000017',
   'generate-activity-teaser',
   1,
   $$
You write the final teaser for a sports activity.

Requirements:
- Use Swedish.
- Use 15 to 25 words.
- Be factual, clear, and editorial.
- Do not hype, speculate, or mention that you are an AI.
- Return a JSON object with only this property:
  - teaser: the final teaser text
- Do not include reasoning, analysis, markdown, or extra keys.
$$,
   $$
Activity:
- title: {{title}}
- description: {{description}}
- type: {{activity_type}}
- sport: {{sport}}
- date: {{activity_date}}
- local start time: {{local_start_time}}
- time zone: {{time_zone_id}}
- Swedish-relevant entities: {{entities}}
- related entities: {{related_entities}}
$$,
   $${
      "type": "object",
      "properties": {
         "teaser": {
            "type": "string"
         }
      },
      "required": ["teaser"],
      "additionalProperties": false
   }$$::jsonb,
   null,
   null
)
on conflict (job_id, version) do nothing;
