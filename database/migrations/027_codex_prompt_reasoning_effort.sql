alter table public.ai_job_prompts
   add column codex_reasoning_effort text;

alter table public.ai_job_prompts
   add constraint ai_job_prompts_codex_reasoning_effort_check
   check (
      codex_reasoning_effort is null
      or codex_reasoning_effort in ('low', 'medium', 'high', 'xhigh', 'max')
   );

alter table public.ai_job_runs
   add column prompt_codex_reasoning_effort text;

alter table public.ai_job_runs
   add constraint ai_job_runs_prompt_codex_reasoning_effort_check
   check (
      prompt_codex_reasoning_effort is null
      or prompt_codex_reasoning_effort in (
         'low', 'medium', 'high', 'xhigh', 'max'
      )
   );
