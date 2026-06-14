alter table ai_job_runs
   add column if not exists tool_round_count integer not null default 0;

alter table ai_job_runs
   add column if not exists conversation_character_count integer not null
   default 0;
