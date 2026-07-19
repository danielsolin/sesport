alter table ai_job_prompts
   add column min_tool_rounds integer null;

alter table ai_job_runs
   add column prompt_min_tool_rounds integer null;
