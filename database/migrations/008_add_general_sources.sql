alter table sources
   rename to ingestion_sources;

alter table activity_proposals
   rename column source_id to ingestion_source_id;

alter table activity_proposal_evidence
   rename column source_id to ingestion_source_id;

alter table activity_evidence
   rename column source_id to ingestion_source_id;

create table sources
(
   id uuid primary key,
   correlation_type text not null,
   correlation_id text not null,
   kind text not null,
   url text not null,
   title text null,
   excerpt text null,
   observed_at timestamptz not null default now(),
   created_at timestamptz not null default now()
);

create index sources_correlation_idx
   on sources(correlation_type, correlation_id, kind);

create index sources_url_idx
   on sources(url);

create index sources_observed_at_idx
   on sources(observed_at desc);
