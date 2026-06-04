alter table sports
add column if not exists icon_id text null;

update sports
set icon_id = 'mdi:soccer'
where id = 'football'
   and icon_id is null;

update sports
set icon_id = 'mdi:hockey-puck'
where id = 'ice-hockey'
   and icon_id is null;
