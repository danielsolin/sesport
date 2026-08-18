# SESport.AI

`SESport.AI` is the runtime layer for the project's AI-assisted sport
research and enrichment. It turns a configured AI job into a provider request,
optionally gives the model controlled web tools, validates the result, and
returns an `AiJobResult` to the application layer.

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
    -> LlamaServerClient, OpenRouterClient, or GoogleTranslateClient
    -> Protocols, Llama, WebSearch, and WebPages as needed
SESport.Web
    -> post-processes the completed AiJobResult
```

For a queued run, `AiJobRunner` stores a pending run and a web worker later
claims it through `IAiJobProcessor`. For a direct run, the runner claims and
executes the run itself. The run repository is an abstraction from
`SESport.Core.AI`; the concrete PostgreSQL repository is in
`SESport.Data`.

The active research provider is `LlamaServerClient`. Its tool loop can search
with SearXNG, fetch and normalize web pages, inspect relevant images with OCR,
and submit a structured report. `OpenRouterClient` remains available for
archived configurations, while `GoogleTranslateClient` adapts translation
jobs to the same provider contract.

## Architectural boundaries

- `SESport.AI` depends on `SESport.Core`, never on `SESport.Data`.
- AI runtime code does not issue SQL or create PostgreSQL connections.
- `SESport.Core.AI` owns job, prompt, provider, run, and result contracts.
- `SESport.Core.Configuration` owns option types and configuration defaults.
- `SESport.Web` owns dependency-injection composition and host workers.
- `SESport.Data` implements the repository contracts and owns PostgreSQL.
- Provider wire formats stay in `Protocols`, not in provider client classes.
- Provider-specific Llama behavior stays in `Llama`, not in generic clients.

This keeps the AI library usable with another host or persistence adapter.
The host supplies repositories, configuration, HTTP clients, logging, and the
worker lifecycle through dependency injection.

## Project structure

```text
src/SESport.AI/
|-- Clients/       External AI provider adapters and provider contract
|-- Jobs/          Job execution, prompt rendering, and execution gate
|-- Llama/         llama-server request and tool-loop implementation details
|-- Protocols/     Shared provider request/response wire-format helpers
|-- WebPages/      Safe page fetching, extraction, normalization, and OCR
|-- WebSearch/     SearXNG search, caching, rotation, and rate limiting
|-- Schemas/       JSON schemas supplied to structured-output jobs
```

The directory layout mirrors the namespaces. `Schemas` contains resources,
not C# types, and therefore has no corresponding namespace.

## Namespace overview

### `SESport.AI.Clients`

This namespace contains adapters that implement the common AI provider
contract. Each adapter translates the project's `AiProviderDefinition`, job,
prompt, and rendered input into provider-specific requests and maps the
response back to `AiJobResult`.

Examples:

- `IAiProviderClient` is the provider boundary used by `AiJobRunner`.
- `LlamaServerClient` runs the active llama-server integration.
- `OpenRouterClient` adapts the OpenRouter-compatible HTTP API.
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

### `SESport.AI.Llama`

This namespace contains implementation details specific to the llama-server
tool loop. It is intentionally internal-facing: callers should use
`LlamaServerClient` and the provider contract rather than these helpers.

Examples:

- `LlamaRequestFactory` creates initial, tool-round, and final requests.
- `LlamaConversationTrimmer` keeps conversations within the configured size
  budget while preserving the latest useful context.
- `LlamaResponseReader` parses model responses and tool calls.
- `LlamaToolTrace` records tool and reasoning diagnostics for a run.

### `SESport.AI.Protocols`

This namespace contains wire-format helpers shared by more than one provider
adapter. Keeping them separate prevents a dependency cycle between the
generic client adapters and the Llama implementation.

Examples:

- `ResponsesRequestBuilder` builds an OpenAI Responses-style request.
- `ResponsesRequestFormat` applies JSON-object and JSON-schema output modes.
- `ResponsesOutputValidator` extracts and validates structured output.
- `AiRequestJsonSerializer` serializes captured provider request payloads.

### `SESport.AI.WebPages`

This namespace retrieves and normalizes page content for AI research. It
validates URLs, retries transient failures, chooses HTML, browser, cURL, or
PDF retrieval paths, extracts relevant links and images, and can append OCR
text from images. The public page contract lives here with its implementation
and page result models.

Examples:

- `IWebPageContentClient` is the injectable page-content contract.
- `WebPageContentClient` coordinates fetching, retries, normalization, and
  image-text enrichment.
- `WebPageContent` is the normalized page result returned to callers.
- `WebPageImageOcr` extracts text from downloaded images using Tesseract.

### `SESport.AI.WebSearch`

This namespace provides web search to AI jobs. The SearXNG client rotates
configured engines, applies recent-search fallbacks, retries transient and
rate-limit failures, and applies a rate limiter. The caching decorator avoids
repeating equivalent searches, while a relevance guard rejects clearly
irrelevant result sets.

Examples:

- `IWebSearchClient` is the injectable search contract.
- `SearxngWebSearchClient` calls the local SearXNG service.
- `CachedWebSearchClient` decorates a search client with an in-memory cache.
- `SearchRateLimiter` coordinates global and per-engine cooldowns.

## Configuration and external services

Option types and defaults are in `SESport.Core.Configuration`, including
Llama-server, SearXNG, page-fetch, cache, and rate-limit settings. The web
host binds configuration and registers the implementations in
`AiServiceCollectionExtensions`.

AI jobs may require the following runtime dependencies:

- a configured AI provider, usually a local llama-server instance;
- local SearXNG at `http://127.0.0.1:8088/` for web research;
- Playwright Chromium for browser-backed fetching and translation;
- Tesseract with English language data for image OCR.

### Codex CLI provider

The `codex-cli` provider runs `codex exec` as a full Codex agent. It uses the
configured working directory, the rendered job prompt, the prompt output
schema, and Codex's JSONL event stream. Jobs that require web search enable
Codex live search automatically.

The prompt editor exposes a `Reasoning effort` setting for `codex-cli`
jobs. It is passed to `codex exec` as `model_reasoning_effort` through
`--config`; the default is `medium`, and other providers ignore the setting.

The host can configure the executable and working directory with the
`CodexCli__ExecutablePath` and `CodexCli__WorkingDirectory` configuration
keys. `CodexCli__TimeoutSeconds` controls the process timeout and defaults to
20 minutes. The provider intentionally runs with full Codex access for its
agent-backed use case.

Codex jobs that require web search also receive the internal
sesport-web-tools MCP server. It exposes web_get_page and web_find_in_page,
reusing the page fetcher's PDF, Playwright, fallback, and URL policy behavior.
The server is started over stdio for each Codex run.
CodexCli__WebToolsEnabled defaults to true, while
CodexCli__WebToolsProjectPath and CodexCli__WebToolsTimeoutSeconds can
override its project path and per-tool timeout. Build the solution before
running Codex jobs because the server is started with --no-build.

Browser-backed page fetching uses Chromium as its baseline and opportunistically
tries Playwright's alternate Chromium mode, installed Chrome, Firefox, or
WebKit when those browser runtimes are available on the host.

These dependencies are host concerns. The AI project receives their clients
or options through constructors and does not own deployment or process
startup.

## Maintaining the structure

When adding code, place it according to the dependency it owns:

- add a new provider adapter to `Clients`;
- add provider wire-format code shared by adapters to `Protocols`;
- add Llama-only request or tool-loop code to `Llama`;
- add search or page-fetch behavior to `WebSearch` or `WebPages`;
- add execution workflow code to `Jobs`;
- add shared job models or configuration to `SESport.Core`.

Do not make `SESport.AI` reference `SESport.Data`. If a new host needs a
different persistence mechanism, implement the existing Core repository
contracts in that host's infrastructure layer.
