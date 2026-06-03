alter table tv_sport_broadcasts
add column if not exists hidden_at timestamptz null;

create index if not exists tv_sport_broadcasts_visible_starts_at_idx
on tv_sport_broadcasts (starts_at)
where hidden_at is null;
