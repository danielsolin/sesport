# AI Platform

This project will use a generic AI platform instead of feature-specific
adapters such as the old activity teaser implementation.

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

In the current implementation the repositories live in
`src/SESport.Web/Data` to match the existing repository pattern in the web
project. They can be moved to `SESport.Data` later if we want to consolidate
data access there.

### `src/SESport.Web/Pages/Admin/AI`

- admin UI for providers
- admin UI for jobs
- admin UI for prompt versions
- run history and run details

## First Job

The teaser generator should be migrated first:

- job id: `generate-activity-teaser`
- provider: `openrouter-free`
- prompt version: `1`
- output mode: `json_object`
