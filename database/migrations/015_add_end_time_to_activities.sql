alter table activities
   add column local_end_time time null,
   add column ends_at timestamptz null;

alter table activities
   add constraint activities_end_time_shape_check
      check (
         (
            local_end_time is null and
            ends_at is null
         ) or
         (
            local_end_time is not null and
            ends_at is not null and
            starts_at is not null and
            ends_at > starts_at
         )
      );
