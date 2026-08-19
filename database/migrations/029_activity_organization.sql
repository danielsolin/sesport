alter table public.activities
   add column organization_entity_id uuid;

alter table public.activities
   add constraint activities_organization_entity_id_fkey
   foreign key (organization_entity_id)
   references public.entities(id)
   on delete set null;

create index activities_organization_entity_id_idx
   on public.activities (organization_entity_id);
