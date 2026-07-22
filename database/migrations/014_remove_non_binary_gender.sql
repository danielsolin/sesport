alter table entities
drop constraint entities_person_gender_id_valid;

alter table entities
add constraint entities_person_gender_id_valid
check (
   person_gender_id is null
   or person_gender_id in ('female', 'male')
);
