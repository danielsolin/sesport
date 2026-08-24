begin;

create unique index if not exists sources_stream_link_correlation_url_uidx
   on public.sources (correlation_type, correlation_id, url)
   where kind = 'StreamLink';

commit;
