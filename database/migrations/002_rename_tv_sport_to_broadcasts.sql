begin;

alter table tv_sport_import_runs
   rename to broadcast_import_runs;

alter table tv_sport_broadcasts
   rename to broadcasts;

alter table tv_sport_ignore
   rename to broadcast_ignore;

alter table broadcasts
   rename constraint tv_sport_broadcasts_pkey
   to broadcasts_pkey;

alter table broadcasts
   rename constraint tv_sport_broadcasts_import_run_id_fkey
   to broadcasts_import_run_id_fkey;

alter table broadcasts
   rename constraint tv_sport_broadcasts_time_check
   to broadcasts_time_check;

alter table broadcasts
   rename constraint tv_sport_broadcasts_fingerprint_unique
   to broadcasts_fingerprint_unique;

alter table broadcast_import_runs
   rename constraint tv_sport_import_runs_pkey
   to broadcast_import_runs_pkey;

alter table broadcast_ignore
   rename constraint tv_sport_ignore_pkey
   to broadcast_ignore_pkey;

alter table broadcast_ignore
   rename constraint tv_sport_ignore_kind_value_source_unique
   to broadcast_ignore_kind_value_source_unique;

alter index tv_sport_broadcasts_starts_at_idx
   rename to broadcasts_starts_at_idx;

alter index tv_sport_broadcasts_channel_id_idx
   rename to broadcasts_channel_id_idx;

alter index tv_sport_broadcasts_visible_starts_at_idx
   rename to broadcasts_visible_starts_at_idx;

alter index tv_sport_ignore_active_kind_idx
   rename to broadcast_ignore_active_kind_idx;

commit;
