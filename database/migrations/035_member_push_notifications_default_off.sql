begin;

alter table public.members
   drop constraint members_push_notification_lead_time_check;

alter table public.members
   alter column push_notification_lead_time_minutes set default 0;

alter table public.members
   add constraint members_push_notification_lead_time_check
   check (
      push_notification_lead_time_minutes is null
      or push_notification_lead_time_minutes >= 0
   );

commit;
