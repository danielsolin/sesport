alter table tv_sport_broadcasts
add column if not exists is_replay boolean not null default false;

alter table tv_sport_broadcasts
add column if not exists original_air_date date null;
