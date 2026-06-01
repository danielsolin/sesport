create table if not exists proposal_reject_reasons
(
   id text primary key,
   label text not null,
   sort_order integer not null
);

insert into proposal_reject_reasons (id, label, sort_order)
values
   ('Hallucination', 'Hallucination', 10),
   ('Duplicate', 'Duplicate', 20),
   ('OutOfScope', 'Out of scope', 30)
on conflict (id) do update
set
   label = excluded.label,
   sort_order = excluded.sort_order;

alter table activity_proposals
   add column if not exists reject_reason_id text null references proposal_reject_reasons(id),
   add column if not exists reject_comment text null;

do $$
begin
   if not exists (
      select 1
      from pg_constraint
      where conname = 'activity_proposals_reject_reason_status_check'
   ) then
      alter table activity_proposals
         add constraint activity_proposals_reject_reason_status_check
         check (
            (status_id = 'Rejected') or
            (reject_reason_id is null and reject_comment is null)
         );
   end if;
end $$;
