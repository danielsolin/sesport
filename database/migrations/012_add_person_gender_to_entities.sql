alter table entities
   add column if not exists person_gender_id text null;

alter table entities
   add constraint entities_person_gender_id_valid
   check (
      person_gender_id is null or
      person_gender_id in ('female', 'male', 'non_binary')
   );

alter table entities
   add constraint entities_person_gender_only_for_persons
   check (
      entity_type_id = 'Person' or person_gender_id is null
   );
