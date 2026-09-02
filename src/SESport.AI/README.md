# SESport.AI

`SESport.AI` is the runtime layer for the project's AI-assisted sport
research and enrichment. It turns a configured AI job into a provider request,
validates the result, and returns an `AiJobResult` to the application layer.
Web search and page fetching are hosted by `SESport.MCP`.

The project is deliberately a runtime library rather than a complete AI
application. Job definitions, prompts, providers, and run records are shared
models in `SESport.Core.AI`. Their PostgreSQL implementation belongs to
`SESport.Data`. `SESport.Web` composes the services and applies completed
results to the site's domain objects.

## Role in the system

The normal execution path is:

```text
SESport.Web
    -> IAiJobRunner / AiJobRequest
SESport.AI.Jobs
    -> repository contracts in SESport.Core.AI
    -> IAiProviderClient
SESport.AI.Clients
    -> CodexCliClient, OpenCodeCliClient, or GoogleTranslateClient
    -> external harnesses or provider-specific services
SESport.Web
    -> post-processes the completed AiJobResult
```

For a queued run, `AiJobRunner` stores a pending run and a web worker later
claims it through `IAiJobProcessor`. For a direct run, the runner claims and
executes the run itself. The run repository is an abstraction from
`SESport.Core.AI`; the concrete PostgreSQL repository is in
`SESport.Data`.

The Web host registers the external-harness adapters and the translation
adapter. `LlamaServerClient` and `OpenRouterClient` are obsolete compatibility
clients and are not registered by the host. The legacy Llama client and its
tool-loop support remain in this project under `Clients/Legacy`; they are
kept only for compatibility with older provider configurations.

## Architectural boundaries

- `SESport.AI` depends on `SESport.Core`, never on `SESport.Data`.
- AI runtime code does not issue SQL or create PostgreSQL connections.
- `SESport.Core.AI` owns job, prompt, provider, run, and result contracts.
- `SESport.Core.Configuration` owns option types and configuration defaults.
- `SESport.Web` owns dependency-injection composition and host workers.
- `SESport.MCP` owns the web-search and page-fetch implementations exposed to
  external harnesses.
- `SESport.Data` implements the repository contracts and owns PostgreSQL.
- Provider wire formats stay in `Protocols`, not in provider client classes.
- Legacy Llama behavior is isolated under `Clients/Legacy`.

This keeps the AI library usable with another host or persistence adapter.
The host supplies repositories, configuration, HTTP clients, logging, and the
worker lifecycle through dependency injection.

## Project structure

```text
src/SESport.AI/
|-- Clients/       External AI provider adapters and provider contract
|-- Clients/Legacy/ Llama and OpenRouter compatibility adapters
|-- Jobs/          Job execution, prompt rendering, and execution gate
|-- Protocols/     Shared provider request/response wire-format helpers
|-- Clients/Legacy/Schemas/ Archived Llama tool JSON schemas
```

The web implementation is in `src/SESport.MCP`. The legacy Llama tool loop is
kept in `src/SESport.AI/Clients/Legacy`; its namespace remains unchanged so
existing callers can be migrated separately from this physical move.

## Namespace overview

### `SESport.AI.Clients`

This namespace contains adapters that implement the common AI provider
contract. Each adapter translates the project's `AiProviderDefinition`, job,
prompt, and rendered input into provider-specific requests and maps the
response back to `AiJobResult`.

Examples:

- `IAiProviderClient` is the provider boundary used by `AiJobRunner`.
- `CodexCliClient` and `OpenCodeCliClient` launch the external AI harnesses.
- `OpenRouterClient` is retained under `Clients/Legacy` for compatibility.
- `GoogleTranslateClient` handles translation jobs through a browser-backed
  translation request.

### `SESport.AI.Jobs`

This namespace owns the application-facing AI execution workflow. It loads
job configuration through Core repository contracts, renders prompt templates,
selects a provider, persists progress, and coordinates queued or direct runs.

Examples:

- `AiJobRunner` builds execution contexts, claims runs, and maps results.
- `TemplatePromptRenderer` resolves JSON input tokens in system and user
  prompts, including the configured primary-country values.
- `AiJobExecutionGate` limits in-process execution to one run per provider.
- `IAiJobRunner`, `IAiJobProcessor`, and `IAiPromptRenderer` are the public
  seams used by the host and by tests.

### `SESport.AI.Protocols`

This namespace contains wire-format helpers shared by more than one provider
adapter. Keeping them separate prevents a dependency cycle between the
generic client adapters and the Llama implementation.

Examples:

- `ResponsesRequestBuilder` builds an OpenAI Responses-style request.
- `ResponsesRequestFormat` applies JSON-object and JSON-schema output modes.
- `ResponsesOutputValidator` extracts and validates structured output.
- `AiRequestJsonSerializer` serializes captured provider request payloads.

### Legacy compatibility code

The old `LlamaServerClient` and its Llama tool-loop helpers live under
`src/SESport.AI/Clients/Legacy`. They are obsolete and are not registered by
the Web host. The MCP README documents the active web tools and their runtime
configuration.

## Configuration and external services

Option types and defaults are in `SESport.Core.Configuration`. The Web host
binds AI configuration and registers the active adapters in
`AiServiceCollectionExtensions`; the MCP host binds SearXNG and page-fetch
configuration for its own web tools.

AI jobs may require the following runtime dependencies:

- a configured external AI harness;
- Playwright Chromium for browser-backed translation.

### Codex CLI provider

The `codex-cli` provider runs `codex exec` as a full Codex agent. It uses the
configured working directory, the rendered job prompt, the prompt output
schema, and Codex's JSONL event stream. Jobs that require web search enable
Codex live search automatically.

The prompt editor exposes a `Reasoning effort` setting for `codex-cli`
jobs. It is passed to `codex exec` as `model_reasoning_effort` through
`--config`; the default is `medium`, and other providers ignore the setting.

The provider edit form exposes `Codex profile` and `Codex system instruction`
fields for `codex-cli` and `codex-cli-local` providers. They are stored in
the provider's `request_options` JSON as `codex_profile` and
`codex_system_instruction`; the dedicated fields own those reserved keys and
the raw request-options editor omits them. The system instruction replaces the
default agent intro lines in the job prompt; a blank value keeps the default.
The profile
is only applied to `codex-cli-local` providers and selects the model and
model provider from `~/.codex/config.toml` or
`~/.codex/local.config.toml` via `--profile`; `codex-cli` providers ignore
it and keep using the configured model. `codex-cli-local` providers run the
same full-access Codex agent as `codex-cli`, so a local provider typically
sets a profile such as `local` and leaves the model field empty.

The host can configure the executable and working directory with the
`CodexCli__ExecutablePath` and `CodexCli__WorkingDirectory` configuration
keys. `CodexCli__TimeoutSeconds` controls the process timeout and defaults to
20 minutes. The provider intentionally runs with full Codex access for its
agent-backed use case.

`GoogleTranslateClient` uses a browser-backed translation request. The MCP
README documents the separate Playwright and Tesseract requirements for web
page fetching and image OCR.

These dependencies are host concerns. The AI project receives its clients or
options through constructors and does not own deployment or process startup.

### OpenCode CLI provider

The `opencode-cli` provider runs `opencode run` as the local OpenCode agent.
It uses OpenCode's normal user configuration, default model, configured MCP
servers, project context, and agent behavior. SESport passes the rendered
prompt and configured output schema as the single user message and does not
add provider-specific agent instructions or issue MCP requests itself.

The command uses JSON events for progress and maps OpenCode text, reasoning,
and tool events into the common AI run trace. It intentionally does not pass
the SESport model, tool definitions, or web-search flag to OpenCode. The
OpenCode installation and its configuration are the source of truth for those
settings.

The host can configure the executable and working directory with the
`OpenCodeCli__ExecutablePath` and `OpenCodeCli__WorkingDirectory` configuration
keys. `OpenCodeCli__TimeoutSeconds` controls the process timeout and defaults
to 20 minutes. The process inherits the host user's OpenCode environment.
When no working directory is configured, the client uses the Git repository
root when it can find one above the host process directory. When the default
executable name is used, an installed `~/.opencode/bin/opencode` is preferred
when present.

## Maintaining the structure

When adding code, place it according to the dependency it owns:

- add a new provider adapter to `Clients`;
- add provider wire-format code shared by adapters to `Protocols`;
- add new web-search or page-fetch behavior to `SESport.MCP`;
- keep old Llama request or tool-loop code under `Clients/Legacy`;
- add execution workflow code to `Jobs`;
- add shared job models or configuration to `SESport.Core`.

Do not make `SESport.AI` reference `SESport.Data`. If a new host needs a
different persistence mechanism, implement the existing Core repository
contracts in that host's infrastructure layer.
