alter table public.activity_groups
   add column public_date_mode text
      default 'sport_day' not null;

alter table public.activity_groups
   add constraint activity_groups_public_date_mode_check
   check (
      public_date_mode in ('sport_day', 'local_calendar_date')
   );
