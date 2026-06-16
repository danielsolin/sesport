# AI Platform

This project use a generic AI platform that is not overly abstracted.

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
- `status_id`: `pending`, `running`, `completed`, `failed`
- `correlation_id`: optional caller-supplied trace id
- `input_payload`: raw JSON input
- `rendered_prompt`: final prompt text
- `raw_response`: raw provider response JSON
- `output_text`: parsed output text
- `error_message`: error detail if the run failed
- `started_at`, `completed_at`, `duration_seconds`: timing
- `input_tokens`, `output_tokens`, `reasoning_tokens`: usage metadata

## Project Structure

### `src/SESport.Core/AI`

- `Abstractions/`
- `Models/`
- `Rendering/`
- `Providers/`
- `Validation/`

### `src/SESport.Data/AI`

- repositories for provider config
- repositories for jobs and prompt versions
- repositories for execution history

The AI repositories now live in `src/SESport.Data/AI`, and the web project
consumes them through a project reference to `SESport.Data`.

### `src/SESport.Web/Pages/Admin/AI`

- admin UI for providers
- admin UI for jobs
- admin UI for prompt versions
- run history and run details

## Runtime Ownership

AI background workers are enabled only in `sesport-dev.service`.

- `sesport-dev.service` sets `Ai:EnableBackgroundWorkers=true`
- `sesport.service` keeps `Ai:EnableBackgroundWorkers=false`

This lets production display AI run history without polling or claiming new
runs from the shared database.

## First Job

The teaser generator should be migrated first:

- job id: `generate-activity-teaser`
- provider: `openrouter-free`
- prompt version: `1`
- output mode: `json_object`
