--
-- PostgreSQL database dump
--

\restrict JCzg5EYhMw6o4etPKoAsw7mv3TvMpuo7EpIIpzIm05gbn7k63NWHOWAyWjEoTlF

-- Dumped from database version 17.10 (Debian 17.10-1.pgdg13+1)
-- Dumped by pg_dump version 18.4 (Ubuntu 18.4-0ubuntu0.26.04.1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: activities; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.activities (
    id uuid NOT NULL,
    title text NOT NULL,
    description text,
    activity_type_id text NOT NULL,
    sport_id text NOT NULL,
    activity_date date NOT NULL,
    local_start_time time without time zone,
    starts_at timestamp with time zone,
    time_zone_id text DEFAULT 'Europe/Stockholm'::text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    publication_status_id text DEFAULT 'Draft'::text NOT NULL,
    slug text,
    published_at timestamp with time zone,
    teaser text,
    tv_channel_name text,
    facts text,
    activity_group_id uuid,
    local_end_time time without time zone,
    ends_at timestamp with time zone,
    CONSTRAINT activities_end_time_shape_check CHECK ((((local_end_time IS NULL) AND (ends_at IS NULL)) OR ((local_end_time IS NOT NULL) AND (ends_at IS NOT NULL) AND (starts_at IS NOT NULL) AND (ends_at > starts_at)))),
    CONSTRAINT activities_time_shape_check CHECK ((((local_start_time IS NOT NULL) AND (starts_at IS NOT NULL)) OR ((local_start_time IS NULL) AND (starts_at IS NULL))))
);


--
-- Name: activity_entity_link_roles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.activity_entity_link_roles (
    id text NOT NULL,
    label text NOT NULL,
    sort_order integer NOT NULL
);


--
-- Name: activity_entity_links; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.activity_entity_links (
    id uuid NOT NULL,
    activity_id uuid NOT NULL,
    entity_id uuid NOT NULL,
    organization_entity_id uuid
);


--
-- Name: activity_groups; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.activity_groups (
    id uuid NOT NULL,
    title text NOT NULL,
    sport_id text NOT NULL,
    start_date date NOT NULL,
    end_date date NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT activity_groups_date_check CHECK ((end_date >= start_date))
);


--
-- Name: activity_publication_statuses; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.activity_publication_statuses (
    id text NOT NULL,
    label text NOT NULL,
    sort_order integer NOT NULL
);


--
-- Name: activity_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.activity_types (
    id text NOT NULL,
    label text NOT NULL,
    sort_order integer NOT NULL,
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: ai_job_prompts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.ai_job_prompts (
    id uuid NOT NULL,
    job_id text NOT NULL,
    version integer NOT NULL,
    system_prompt text NOT NULL,
    user_prompt_template text NOT NULL,
    output_schema jsonb,
    temperature numeric(4,2),
    max_output_tokens integer,
    enabled boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    request_options jsonb DEFAULT '{}'::jsonb NOT NULL,
    max_tool_rounds integer,
    min_tool_rounds integer
);


--
-- Name: ai_job_runs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.ai_job_runs (
    id uuid NOT NULL,
    job_id text NOT NULL,
    prompt_id uuid NOT NULL,
    provider_id text NOT NULL,
    status_id text NOT NULL,
    correlation_id text,
    input_payload jsonb NOT NULL,
    rendered_prompt text NOT NULL,
    raw_response jsonb,
    output_text text,
    error_message text,
    started_at timestamp with time zone NOT NULL,
    completed_at timestamp with time zone,
    duration_seconds numeric(12,3),
    input_tokens integer,
    output_tokens integer,
    reasoning_tokens integer,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    raw_request jsonb,
    provider_model text,
    tool_trace jsonb,
    tool_round_count integer DEFAULT 0 NOT NULL,
    conversation_character_count integer DEFAULT 0 NOT NULL,
    prompt_version integer,
    prompt_system_prompt text,
    prompt_user_prompt_template text,
    execution_environment text,
    job_label text,
    provider_label text,
    rendered_system_prompt text,
    job_output_mode text,
    job_requires_web_search boolean,
    job_tools_json jsonb,
    job_conditional_tools_json jsonb,
    job_tool_call_max_tokens integer,
    provider_kind text,
    provider_base_address text,
    provider_api_key_source text,
    provider_request_options_json jsonb,
    prompt_output_schema_json jsonb,
    prompt_request_options_json jsonb,
    prompt_temperature numeric(4,2),
    prompt_max_output_tokens integer,
    prompt_max_tool_rounds integer,
    max_output_tokens integer,
    prompt_min_tool_rounds integer,
    job_include_social_media boolean
);


--
-- Name: ai_jobs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.ai_jobs (
    id text NOT NULL,
    label text NOT NULL,
    description text,
    provider_id text NOT NULL,
    output_mode text NOT NULL,
    enabled boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    requires_web_search boolean DEFAULT true NOT NULL,
    active_prompt_id uuid,
    tools_json jsonb,
    conditional_tools_json jsonb,
    tool_call_max_tokens integer,
    model text,
    queue_priority integer DEFAULT 0 NOT NULL,
    include_social_media boolean DEFAULT false NOT NULL
);


--
-- Name: ai_providers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.ai_providers (
    id text NOT NULL,
    label text NOT NULL,
    kind text NOT NULL,
    base_address text,
    model text,
    api_key_source text,
    request_options jsonb DEFAULT '{}'::jsonb NOT NULL,
    enabled boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: broadcast_ignore; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.broadcast_ignore (
    id uuid NOT NULL,
    kind text NOT NULL,
    value text NOT NULL,
    source_key text,
    reason text,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: broadcast_import_runs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.broadcast_import_runs (
    id uuid NOT NULL,
    source_key text NOT NULL,
    source_uri text,
    started_at timestamp with time zone NOT NULL,
    finished_at timestamp with time zone,
    status text NOT NULL,
    broadcast_count integer DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: broadcasts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.broadcasts (
    id uuid NOT NULL,
    import_run_id uuid,
    source_key text NOT NULL,
    external_id text NOT NULL,
    fingerprint text NOT NULL,
    channel_id text NOT NULL,
    channel_name text,
    title text NOT NULL,
    description text,
    categories text[] NOT NULL,
    starts_at timestamp with time zone NOT NULL,
    ends_at timestamp with time zone NOT NULL,
    time_zone_id text DEFAULT 'Europe/Stockholm'::text NOT NULL,
    raw_programme_xml text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    is_replay boolean DEFAULT false NOT NULL,
    original_air_date date,
    hidden_at timestamp with time zone,
    entity_id uuid,
    activity_group_source_kind_id text,
    activity_group_source_activity_id uuid,
    activity_group_draft_title text,
    CONSTRAINT broadcasts_activity_group_source_kind_check CHECK (((activity_group_source_kind_id IS NULL) OR (activity_group_source_kind_id = 'ActivityGroupForActivity'::text))),
    CONSTRAINT broadcasts_time_check CHECK ((ends_at > starts_at))
);


--
-- Name: countries; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.countries (
    id text NOT NULL,
    code text NOT NULL,
    name text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: country_relevance_kinds; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.country_relevance_kinds (
    id text NOT NULL,
    label text NOT NULL,
    sort_order integer NOT NULL
);


--
-- Name: entities; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.entities (
    id uuid NOT NULL,
    canonical_name text NOT NULL,
    entity_type_id text NOT NULL,
    sport_id text NOT NULL,
    country_id text NOT NULL,
    country_relevance_kind_id text NOT NULL,
    country_relevance_reason text NOT NULL,
    watch_priority_id text NOT NULL,
    expected_stability_id text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    person_gender_id text,
    alias_name text,
    bio text,
    bio_eng text,
    birthdate date,
    height integer,
    weight integer,
    formative_club text,
    CONSTRAINT entities_person_gender_id_valid CHECK (((person_gender_id IS NULL) OR (person_gender_id = ANY (ARRAY['female'::text, 'male'::text])))),
    CONSTRAINT entities_person_gender_only_for_persons CHECK (((entity_type_id = 'Person'::text) OR (person_gender_id IS NULL)))
);


--
-- Name: entity_stability_kinds; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.entity_stability_kinds (
    id text NOT NULL,
    label text NOT NULL,
    sort_order integer NOT NULL
);


--
-- Name: entity_to_entity_links; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.entity_to_entity_links (
    id uuid NOT NULL,
    source_entity_id uuid NOT NULL,
    target_entity_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT entity_to_entity_links_distinct_entities_check CHECK ((source_entity_id <> target_entity_id))
);


--
-- Name: entity_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.entity_types (
    id text NOT NULL,
    label text NOT NULL,
    sort_order integer NOT NULL
);


--
-- Name: entity_watch_priorities; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.entity_watch_priorities (
    id text NOT NULL,
    label text NOT NULL,
    sort_order integer NOT NULL
);


--
-- Name: sources; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sources (
    id uuid NOT NULL,
    correlation_type text NOT NULL,
    correlation_id text NOT NULL,
    kind text NOT NULL,
    url text NOT NULL,
    title text,
    excerpt text,
    observed_at timestamp with time zone DEFAULT now() NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: sports; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sports (
    id text NOT NULL,
    name text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    icon_id text,
    display_name text
);


--
-- Name: activities activities_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activities
    ADD CONSTRAINT activities_pkey PRIMARY KEY (id);


--
-- Name: activity_entity_link_roles activity_entity_link_roles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_entity_link_roles
    ADD CONSTRAINT activity_entity_link_roles_pkey PRIMARY KEY (id);


--
-- Name: activity_entity_links activity_entity_links_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_entity_links
    ADD CONSTRAINT activity_entity_links_pkey PRIMARY KEY (id);


--
-- Name: activity_groups activity_groups_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_groups
    ADD CONSTRAINT activity_groups_pkey PRIMARY KEY (id);


--
-- Name: activity_publication_statuses activity_publication_statuses_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_publication_statuses
    ADD CONSTRAINT activity_publication_statuses_pkey PRIMARY KEY (id);


--
-- Name: activity_types activity_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_types
    ADD CONSTRAINT activity_types_pkey PRIMARY KEY (id);


--
-- Name: ai_job_prompts ai_job_prompts_job_id_version_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ai_job_prompts
    ADD CONSTRAINT ai_job_prompts_job_id_version_key UNIQUE (job_id, version);


--
-- Name: ai_job_prompts ai_job_prompts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ai_job_prompts
    ADD CONSTRAINT ai_job_prompts_pkey PRIMARY KEY (id);


--
-- Name: ai_job_runs ai_job_runs_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ai_job_runs
    ADD CONSTRAINT ai_job_runs_pkey PRIMARY KEY (id);


--
-- Name: ai_jobs ai_jobs_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ai_jobs
    ADD CONSTRAINT ai_jobs_pkey PRIMARY KEY (id);


--
-- Name: ai_providers ai_providers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ai_providers
    ADD CONSTRAINT ai_providers_pkey PRIMARY KEY (id);


--
-- Name: broadcast_ignore broadcast_ignore_kind_value_source_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.broadcast_ignore
    ADD CONSTRAINT broadcast_ignore_kind_value_source_unique UNIQUE NULLS NOT DISTINCT (kind, value, source_key);


--
-- Name: broadcast_ignore broadcast_ignore_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.broadcast_ignore
    ADD CONSTRAINT broadcast_ignore_pkey PRIMARY KEY (id);


--
-- Name: broadcast_import_runs broadcast_import_runs_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.broadcast_import_runs
    ADD CONSTRAINT broadcast_import_runs_pkey PRIMARY KEY (id);


--
-- Name: broadcasts broadcasts_fingerprint_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.broadcasts
    ADD CONSTRAINT broadcasts_fingerprint_unique UNIQUE (fingerprint);


--
-- Name: broadcasts broadcasts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.broadcasts
    ADD CONSTRAINT broadcasts_pkey PRIMARY KEY (id);


--
-- Name: countries countries_code_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.countries
    ADD CONSTRAINT countries_code_unique UNIQUE (code);


--
-- Name: countries countries_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.countries
    ADD CONSTRAINT countries_pkey PRIMARY KEY (id);


--
-- Name: country_relevance_kinds country_relevance_kinds_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.country_relevance_kinds
    ADD CONSTRAINT country_relevance_kinds_pkey PRIMARY KEY (id);


--
-- Name: entities entities_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entities
    ADD CONSTRAINT entities_pkey PRIMARY KEY (id);


--
-- Name: entity_stability_kinds entity_stability_kinds_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entity_stability_kinds
    ADD CONSTRAINT entity_stability_kinds_pkey PRIMARY KEY (id);


--
-- Name: entity_to_entity_links entity_to_entity_links_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entity_to_entity_links
    ADD CONSTRAINT entity_to_entity_links_pkey PRIMARY KEY (id);


--
-- Name: entity_to_entity_links entity_to_entity_links_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entity_to_entity_links
    ADD CONSTRAINT entity_to_entity_links_unique UNIQUE (source_entity_id, target_entity_id);


--
-- Name: entity_types entity_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entity_types
    ADD CONSTRAINT entity_types_pkey PRIMARY KEY (id);


--
-- Name: entity_watch_priorities entity_watch_priorities_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entity_watch_priorities
    ADD CONSTRAINT entity_watch_priorities_pkey PRIMARY KEY (id);


--
-- Name: sources sources_pkey1; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sources
    ADD CONSTRAINT sources_pkey1 PRIMARY KEY (id);


--
-- Name: sports sports_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sports
    ADD CONSTRAINT sports_pkey PRIMARY KEY (id);


--
-- Name: activities_activity_group_id_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX activities_activity_group_id_idx ON public.activities USING btree (activity_group_id);


--
-- Name: activities_publication_listing_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX activities_publication_listing_idx ON public.activities USING btree (publication_status_id, activity_date, local_start_time);


--
-- Name: activities_slug_unique; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX activities_slug_unique ON public.activities USING btree (slug) WHERE (slug IS NOT NULL);


--
-- Name: activities_starts_at_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX activities_starts_at_idx ON public.activities USING btree (starts_at);


--
-- Name: activity_entity_link_roles_sort_label_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX activity_entity_link_roles_sort_label_idx ON public.activity_entity_link_roles USING btree (sort_order, label);


--
-- Name: activity_entity_links_activity_id_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX activity_entity_links_activity_id_idx ON public.activity_entity_links USING btree (activity_id);


--
-- Name: activity_entity_links_entity_id_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX activity_entity_links_entity_id_idx ON public.activity_entity_links USING btree (entity_id);


--
-- Name: activity_entity_links_organization_entity_id_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX activity_entity_links_organization_entity_id_idx ON public.activity_entity_links USING btree (organization_entity_id);


--
-- Name: activity_groups_sport_title_date_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX activity_groups_sport_title_date_idx ON public.activity_groups USING btree (sport_id, title, start_date, end_date);


--
-- Name: activity_publication_statuses_sort_label_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX activity_publication_statuses_sort_label_idx ON public.activity_publication_statuses USING btree (sort_order, label);


--
-- Name: activity_types_sort_label_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX activity_types_sort_label_idx ON public.activity_types USING btree (sort_order, label);


--
-- Name: ai_job_prompts_job_id_enabled_version_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ai_job_prompts_job_id_enabled_version_idx ON public.ai_job_prompts USING btree (job_id, enabled, version DESC);


--
-- Name: ai_job_runs_exec_claim_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ai_job_runs_exec_claim_idx ON public.ai_job_runs USING btree (execution_environment, status_id DESC, started_at, created_at, id) WHERE (status_id = ANY (ARRAY['pending'::text, 'running'::text]));


--
-- Name: ai_job_runs_exec_env_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ai_job_runs_exec_env_idx ON public.ai_job_runs USING btree (execution_environment) WHERE ((execution_environment IS NOT NULL) AND (btrim(execution_environment) <> ''::text));


--
-- Name: ai_job_runs_job_corr_started_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ai_job_runs_job_corr_started_idx ON public.ai_job_runs USING btree (job_id, correlation_id, started_at DESC);


--
-- Name: ai_job_runs_job_id_started_at_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ai_job_runs_job_id_started_at_idx ON public.ai_job_runs USING btree (job_id, started_at DESC);


--
-- Name: ai_job_runs_provider_id_started_at_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ai_job_runs_provider_id_started_at_idx ON public.ai_job_runs USING btree (provider_id, started_at DESC);


--
-- Name: ai_job_runs_started_at_desc_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ai_job_runs_started_at_desc_idx ON public.ai_job_runs USING btree (started_at DESC);


--
-- Name: ai_job_runs_status_id_started_at_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ai_job_runs_status_id_started_at_idx ON public.ai_job_runs USING btree (status_id, started_at DESC);


--
-- Name: broadcast_ignore_active_kind_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX broadcast_ignore_active_kind_idx ON public.broadcast_ignore USING btree (kind, source_key) WHERE (is_active = true);


--
-- Name: broadcasts_activity_group_source_activity_id_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX broadcasts_activity_group_source_activity_id_idx ON public.broadcasts USING btree (activity_group_source_activity_id);


--
-- Name: broadcasts_categories_gin_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX broadcasts_categories_gin_idx ON public.broadcasts USING gin (categories);


--
-- Name: broadcasts_channel_id_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX broadcasts_channel_id_idx ON public.broadcasts USING btree (channel_id);


--
-- Name: broadcasts_entity_id_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX broadcasts_entity_id_idx ON public.broadcasts USING btree (entity_id);


--
-- Name: broadcasts_starts_at_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX broadcasts_starts_at_idx ON public.broadcasts USING btree (starts_at);


--
-- Name: broadcasts_visible_starts_at_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX broadcasts_visible_starts_at_idx ON public.broadcasts USING btree (starts_at) WHERE (hidden_at IS NULL);


--
-- Name: countries_name_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX countries_name_idx ON public.countries USING btree (name);


--
-- Name: entities_canonical_name_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX entities_canonical_name_idx ON public.entities USING btree (canonical_name);


--
-- Name: entities_type_name_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX entities_type_name_idx ON public.entities USING btree (entity_type_id, canonical_name);


--
-- Name: entity_stability_kinds_sort_label_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX entity_stability_kinds_sort_label_idx ON public.entity_stability_kinds USING btree (sort_order, label);


--
-- Name: entity_to_entity_links_entity_pair_unique; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX entity_to_entity_links_entity_pair_unique ON public.entity_to_entity_links USING btree (LEAST(source_entity_id, target_entity_id), GREATEST(source_entity_id, target_entity_id));


--
-- Name: entity_to_entity_links_source_entity_id_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX entity_to_entity_links_source_entity_id_idx ON public.entity_to_entity_links USING btree (source_entity_id);


--
-- Name: entity_to_entity_links_target_entity_id_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX entity_to_entity_links_target_entity_id_idx ON public.entity_to_entity_links USING btree (target_entity_id);


--
-- Name: entity_types_label_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX entity_types_label_idx ON public.entity_types USING btree (label);


--
-- Name: entity_watch_priorities_sort_label_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX entity_watch_priorities_sort_label_idx ON public.entity_watch_priorities USING btree (sort_order, label);


--
-- Name: sources_correlation_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX sources_correlation_idx ON public.sources USING btree (correlation_type, correlation_id, kind);


--
-- Name: sources_observed_at_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX sources_observed_at_idx ON public.sources USING btree (observed_at DESC);


--
-- Name: sources_url_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX sources_url_idx ON public.sources USING btree (url);


--
-- Name: sports_name_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX sports_name_idx ON public.sports USING btree (name);


--
-- Name: activities activities_activity_group_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activities
    ADD CONSTRAINT activities_activity_group_id_fkey FOREIGN KEY (activity_group_id) REFERENCES public.activity_groups(id) ON DELETE SET NULL;


--
-- Name: activities activities_activity_type_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activities
    ADD CONSTRAINT activities_activity_type_id_fkey FOREIGN KEY (activity_type_id) REFERENCES public.activity_types(id);


--
-- Name: activities activities_publication_status_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activities
    ADD CONSTRAINT activities_publication_status_id_fkey FOREIGN KEY (publication_status_id) REFERENCES public.activity_publication_statuses(id);


--
-- Name: activities activities_sport_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activities
    ADD CONSTRAINT activities_sport_id_fkey FOREIGN KEY (sport_id) REFERENCES public.sports(id);


--
-- Name: activity_entity_links activity_entity_links_activity_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_entity_links
    ADD CONSTRAINT activity_entity_links_activity_id_fkey FOREIGN KEY (activity_id) REFERENCES public.activities(id);


--
-- Name: activity_entity_links activity_entity_links_entity_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_entity_links
    ADD CONSTRAINT activity_entity_links_entity_id_fkey FOREIGN KEY (entity_id) REFERENCES public.entities(id);


--
-- Name: activity_entity_links activity_entity_links_organization_entity_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_entity_links
    ADD CONSTRAINT activity_entity_links_organization_entity_id_fkey FOREIGN KEY (organization_entity_id) REFERENCES public.entities(id) ON DELETE SET NULL;


--
-- Name: activity_groups activity_groups_sport_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_groups
    ADD CONSTRAINT activity_groups_sport_id_fkey FOREIGN KEY (sport_id) REFERENCES public.sports(id);


--
-- Name: ai_job_prompts ai_job_prompts_job_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ai_job_prompts
    ADD CONSTRAINT ai_job_prompts_job_id_fkey FOREIGN KEY (job_id) REFERENCES public.ai_jobs(id) ON DELETE CASCADE;


--
-- Name: ai_job_runs ai_job_runs_job_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ai_job_runs
    ADD CONSTRAINT ai_job_runs_job_id_fkey FOREIGN KEY (job_id) REFERENCES public.ai_jobs(id);


--
-- Name: ai_job_runs ai_job_runs_prompt_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ai_job_runs
    ADD CONSTRAINT ai_job_runs_prompt_id_fkey FOREIGN KEY (prompt_id) REFERENCES public.ai_job_prompts(id);


--
-- Name: ai_job_runs ai_job_runs_provider_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ai_job_runs
    ADD CONSTRAINT ai_job_runs_provider_id_fkey FOREIGN KEY (provider_id) REFERENCES public.ai_providers(id);


--
-- Name: ai_jobs ai_jobs_active_prompt_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ai_jobs
    ADD CONSTRAINT ai_jobs_active_prompt_id_fkey FOREIGN KEY (active_prompt_id) REFERENCES public.ai_job_prompts(id) ON DELETE SET NULL;


--
-- Name: ai_jobs ai_jobs_provider_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.ai_jobs
    ADD CONSTRAINT ai_jobs_provider_id_fkey FOREIGN KEY (provider_id) REFERENCES public.ai_providers(id);


--
-- Name: broadcasts broadcasts_activity_group_source_activity_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.broadcasts
    ADD CONSTRAINT broadcasts_activity_group_source_activity_id_fkey FOREIGN KEY (activity_group_source_activity_id) REFERENCES public.activities(id) ON DELETE SET NULL;


--
-- Name: broadcasts broadcasts_entity_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.broadcasts
    ADD CONSTRAINT broadcasts_entity_id_fkey FOREIGN KEY (entity_id) REFERENCES public.entities(id) ON DELETE SET NULL;


--
-- Name: broadcasts broadcasts_import_run_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.broadcasts
    ADD CONSTRAINT broadcasts_import_run_id_fkey FOREIGN KEY (import_run_id) REFERENCES public.broadcast_import_runs(id);


--
-- Name: entities entities_country_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entities
    ADD CONSTRAINT entities_country_id_fk FOREIGN KEY (country_id) REFERENCES public.countries(id);


--
-- Name: entity_to_entity_links entity_to_entity_links_source_entity_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entity_to_entity_links
    ADD CONSTRAINT entity_to_entity_links_source_entity_id_fkey FOREIGN KEY (source_entity_id) REFERENCES public.entities(id) ON DELETE CASCADE;


--
-- Name: entity_to_entity_links entity_to_entity_links_target_entity_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entity_to_entity_links
    ADD CONSTRAINT entity_to_entity_links_target_entity_id_fkey FOREIGN KEY (target_entity_id) REFERENCES public.entities(id) ON DELETE CASCADE;


--
-- Name: entities tracked_entities_country_relevance_kind_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entities
    ADD CONSTRAINT tracked_entities_country_relevance_kind_id_fkey FOREIGN KEY (country_relevance_kind_id) REFERENCES public.country_relevance_kinds(id);


--
-- Name: entities tracked_entities_entity_type_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entities
    ADD CONSTRAINT tracked_entities_entity_type_id_fkey FOREIGN KEY (entity_type_id) REFERENCES public.entity_types(id);


--
-- Name: entities tracked_entities_expected_stability_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entities
    ADD CONSTRAINT tracked_entities_expected_stability_id_fkey FOREIGN KEY (expected_stability_id) REFERENCES public.entity_stability_kinds(id);


--
-- Name: entities tracked_entities_sport_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entities
    ADD CONSTRAINT tracked_entities_sport_id_fkey FOREIGN KEY (sport_id) REFERENCES public.sports(id);


--
-- Name: entities tracked_entities_watch_priority_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entities
    ADD CONSTRAINT tracked_entities_watch_priority_id_fkey FOREIGN KEY (watch_priority_id) REFERENCES public.entity_watch_priorities(id);


--
-- PostgreSQL database dump complete
--

\unrestrict JCzg5EYhMw6o4etPKoAsw7mv3TvMpuo7EpIIpzIm05gbn7k63NWHOWAyWjEoTlF
