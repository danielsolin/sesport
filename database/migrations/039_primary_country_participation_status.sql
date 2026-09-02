begin;

alter table public.entities
   add column primary_country_participation_status_id text;

alter table public.entities
   add column primary_country_participation_reason text;

alter table public.entities
   add constraint entities_primary_country_participation_status_check
   check (
      primary_country_participation_status_id is null
      or primary_country_participation_status_id = 'RepresentsOtherCountry'
   );

alter table public.entities
   add constraint entities_primary_country_participation_person_check
   check (
      entity_type_id = 'Person'
      or primary_country_participation_status_id is null
   );

alter table public.entities
   add constraint entities_primary_country_participation_reason_check
   check (
      primary_country_participation_status_id is not null
      or primary_country_participation_reason is null
   );

commit;
