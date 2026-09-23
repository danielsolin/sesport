begin;

alter table public.entities
   add column url text;

commit;
