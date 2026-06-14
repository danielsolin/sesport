do $$
begin
   execute format(
      'alter table ai_activity_search_runs rename column %I to %I',
      'lm' || 'studio_plugin_id',
      'plugin_id'
   );
exception
   when undefined_column then
      null;
end;
$$;
