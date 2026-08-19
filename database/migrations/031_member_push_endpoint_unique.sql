alter table public.member_push_subscriptions
   drop constraint member_push_subscriptions_member_endpoint_unique;

alter table public.member_push_subscriptions
   add constraint member_push_subscriptions_endpoint_unique
   unique (endpoint);
