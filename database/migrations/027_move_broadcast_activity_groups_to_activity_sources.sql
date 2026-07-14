alter table broadcasts
   add column if not exists activity_group_source_kind_id text null;

alter table broadcasts
   add column if not exists activity_group_source_activity_id uuid null
      references activities(id)
      on delete set null;

update broadcasts b
set activity_group_source_kind_id =
      'ActivityGroupForActivity',
    activity_group_source_activity_id = (
       select a.id
       from activities a
       where a.activity_group_id = b.activity_group_id
       order by a.activity_date, a.local_start_time nulls last, a.id
       limit 1
    )
where b.activity_group_id is not null;

alter table broadcasts
   add constraint broadcasts_activity_group_source_kind_check
      check (
         activity_group_source_kind_id is null
         or activity_group_source_kind_id =
            'ActivityGroupForActivity'
      );

create index if not exists broadcasts_activity_group_source_activity_id_idx
   on broadcasts(activity_group_source_activity_id);

alter table broadcasts
   drop column if exists activity_group_id;
