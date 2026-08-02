# AI Platform

This project uses a generic AI platform that is not overly abstracted.

## Database Model

### `ai_providers`

Stores provider-level configuration.

- `id`: stable text key, for example `openrouter-free`
- `label`: human-readable name
- `kind`: provider adapter name, for example `openrouter`
- `base_address`: API base URL
- `model`: default model name
- `api_key_source`: where the key comes from
- `request_options`: JSON with provider-specific request defaults
- `enabled`: feature flag
- `created_at`, `updated_at`: audit timestamps

### `ai_jobs`

Defines a reusable job.

- `id`: stable text key, for example `generate-activity-teaser`
- `label`: admin UI label
- `description`: short explanation
- `provider_id`: default provider
- `output_mode`: `text`, `json_object`, or `json_schema`
- `requires_web_search`: whether web search is required, default `true`
- `tools_json`: base tool definitions for the request
- `conditional_tools_json`: conditional tool rules evaluated against the
  job and prompt before each request
- `tool_call_max_tokens`: optional tool-round cap, defaulting to 1024
- `enabled`: feature flag

### `ai_job_prompts`

Versioned prompt definitions.

- `id`: immutable UUID
- `job_id`: owning job
- `version`: prompt version number
- `system_prompt`: system-level instructions
- `user_prompt_template`: rendered from job input
- `output_schema`: optional JSON schema
- `temperature`: optional model temperature
- `max_output_tokens`: optional output cap
- `enabled`: feature flag

### `ai_job_runs`

Stores each execution.

- `id`: immutable UUID
- `job_id`, `prompt_id`, `provider_id`: execution references
- `status_id`: `pending`, `running`, `completed`, `failed`, or `archived`
- `correlation_id`: optional caller-supplied trace id
- `input_payload`: raw JSON input
- `execution_environment`: worker identity for pending/running ownership
- `rendered_prompt`: final prompt text
- `raw_response`: raw provider response JSON
- `output_text`: parsed output text
- `error_message`: error detail if the run failed
- `started_at`, `completed_at`, `duration_seconds`: timing
- `input_tokens`, `output_tokens`, `reasoning_tokens`: usage metadata
- `tool_trace`, `tool_round_count`, `conversation_character_count`: web/tool
  execution diagnostics

## Project Structure

### `src/SESport.Core/Configuration`

- all code-defined application configuration
- defaults, option types, environment-variable resolution, and keys
- AI provider, web-search, page-fetch, and worker configuration

Subsystem-specific configuration remains centralized here. `SESport.AI`
consumes it but owns the configured clients and runtime behavior.
Executable projects own configuration binding and dependency-injection
composition.

### `src/SESport.Core/AI`

- AI job, provider, prompt, rendered prompt, and run models
- `AiJobRunStatusIds` and `AiJobIds`
- repository contracts used by the AI runtime and Data implementation
- execution-environment helper
- `ActivitySearchEntity`, shared by AI clients and Data repositories

### `src/SESport.AI`

- `Clients/`
- `Jobs/`
- `Llama/`
- `Protocols/`
- `WebPages/`
- `WebSearch/`

`SESport.AI` owns provider clients, prompt rendering, web-search/page-fetch
clients, activity-search orchestration, and job execution. It depends on
`SESport.Core` and does not contain PostgreSQL access.

The namespace layout is also the dependency layout. Provider contracts live
with their owning area, and shared provider wire-format helpers live in
`Protocols`. This avoids cycles between generic clients, Llama helpers,
search, and page fetching. See [the namespace guide][ai-readme] for the
project-level description.

[ai-readme]: ../src/SESport.AI/README.md

### `src/SESport.Data/AI`

- `AiRepository`: run/job/prompt/provider reads and writes
- `AiAdminRepository`: admin CRUD for AI configuration
- activity-search proposal and run repositories
- SQL for AI-related database access

`SESport.Data` owns the Npgsql implementation. It depends on `SESport.Core`
and does not depend on `SESport.AI`.

### `src/SESport.Web/Pages/Admin/Config/Ai`

- admin UI for providers
- admin UI for jobs
- admin UI for prompt versions

### `src/SESport.Web/Pages/Admin/Runs`

- run history and run details

## Runtime Ownership

AI background workers run in both the development and production web
services.

The web application registers the AI pending-run and timeout workers at
startup. Both services use the single PostgreSQL database defined by `.env`.
The current worker-registration behavior is intentionally shared between
development and production services.
