alter table ai_job_prompts
add column if not exists max_tool_rounds integer null;
