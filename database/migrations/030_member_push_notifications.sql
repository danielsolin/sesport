alter table public.members
   add column push_notification_lead_time_minutes integer;

alter table public.members
   add constraint members_push_notification_lead_time_check
   check (
      push_notification_lead_time_minutes is null
      or push_notification_lead_time_minutes > 0
   );

create table public.member_push_subscriptions (
   id uuid not null primary key,
   member_id uuid not null,
   endpoint text not null,
   p256dh text not null,
   auth text not null,
   expiration_at timestamp with time zone,
   created_at timestamp with time zone default now() not null,
   updated_at timestamp with time zone default now() not null,
   constraint member_push_subscriptions_member_id_fkey
      foreign key (member_id)
      references public.members(id)
      on delete cascade,
   constraint member_push_subscriptions_endpoint_check
      check (length(btrim(endpoint)) > 0),
   constraint member_push_subscriptions_p256dh_check
      check (length(btrim(p256dh)) > 0),
   constraint member_push_subscriptions_auth_check
      check (length(btrim(auth)) > 0),
   constraint member_push_subscriptions_member_endpoint_unique
      unique (member_id, endpoint)
);

create index member_push_subscriptions_member_id_idx
   on public.member_push_subscriptions (member_id);

create table public.member_activity_push_notifications (
   member_id uuid not null,
   activity_id uuid not null,
   scheduled_at timestamp with time zone not null,
   claimed_at timestamp with time zone,
   sent_at timestamp with time zone,
   created_at timestamp with time zone default now() not null,
   updated_at timestamp with time zone default now() not null,
   constraint member_activity_push_notifications_pkey
      primary key (member_id, activity_id),
   constraint member_activity_push_notifications_member_id_fkey
      foreign key (member_id)
      references public.members(id)
      on delete cascade,
   constraint member_activity_push_notifications_activity_id_fkey
      foreign key (activity_id)
      references public.activities(id)
      on delete cascade
);

create index member_activity_push_notifications_due_idx
   on public.member_activity_push_notifications (
      sent_at,
      scheduled_at,
      claimed_at
   );
