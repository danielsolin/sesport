# SESport.AIActivitySearch

Runs an OpenAI Responses-compatible AI activity search for entities in
`data/entity-watchlist.json`.

The default target is OpenRouter at `https://openrouter.ai/api/v1` with the
`openai/gpt-oss-20b` model.
When `--lmstudio-plugin` is used, the base URL switches to
`http://127.0.0.1:1234/api/v1`.

## Run With OpenRouter

Set an OpenRouter key and run the tool directly. This is the default path.
Each run writes a timestamped structured directory under
`data\ai-activity-search-runs`.

```powershell
$env:OPENROUTER_API_KEY="<api-key>"
dotnet run --project tools\SESport.AIActivitySearch
```

Search one entity in the watchlist:

```powershell
dotnet run --project tools\SESport.AIActivitySearch `
   -- --entity tre-kronor
```

## Run With LM Studio

Start LM Studio's local API server, load `gpt-oss-20b`, and make sure the
model has access to the `web-search` tool if you want live search.

For the LM Studio plugin path, use `/api/v1/chat` integrations:

```powershell
dotnet run --project tools\SESport.AIActivitySearch `
   -- --entity tre-kronor --lmstudio-plugin altra/web-search
```

The OpenAI-compatible path can still be pointed elsewhere with `--base-url`:

```powershell
dotnet run --project tools\SESport.AIActivitySearch `
   -- --base-url http://127.0.0.1:1234/v1 --model gpt-oss-20b
```

Write the JSON result to a file:

```powershell
dotnet run --project tools\SESport.AIActivitySearch `
   -- --entity tre-kronor --output ai-activity-search.json
```

Override the structured run directory:

```powershell
dotnet run --project tools\SESport.AIActivitySearch `
   -- --take 5 --run-dir data\ai-activity-search-runs\test-run
```

The run directory contains:

- `manifest.json` with run settings, status, duration per entity, and
  aggregate results.
- `entities/*.json` with one result file per completed entity.
- `failures/*.json` with one error file per failed entity, including duration.

The tool writes structured results to disk by default and does not print the
full JSON document to stdout unless `--output` is also set.

At startup the tool logs the selected client, base URL, model, and a masked API
key source. For the default OpenRouter target it only falls back to
`OPENROUTER_API_KEY`; it will not accidentally send an `OPENAI_API_KEY` to
OpenRouter.

## Overnight Mode

`--overnight` is intended for long unattended LM Studio runs. It searches all
watchlist entities unless `--entity` or `--take` is set, continues after
individual entity failures, waits five seconds between entities, and stops
after five consecutive failures.

```powershell
dotnet run --project tools\SESport.AIActivitySearch `
   -- --overnight --lmstudio-plugin altra/web-search --timeout 3600
```

For richer debugging output, add `--include-raw`. Raw responses can be large,
so the default structured files keep them out unless requested.

## OpenAI-Compatible Settings

The tool is configured around the OpenAI Responses API shape. To point it at
OpenAI later:

```powershell
$env:OPENAI_API_KEY="<api-key>"
dotnet run --project tools\SESport.AIActivitySearch `
   -- --base-url https://api.openai.com/v1 --model gpt-5
```

Useful options:

- `--entity <id>` searches one watchlist entity.
- `--take <count>` searches the first N entities when `--entity` is not set.
- `--max <count>` controls the maximum proposal count per entity.
- `--date <yyyy-mm-dd>` sets the search date.
- `--look-back <days>` includes days before the search date. Default: `0`.
- `--look-ahead <days>` includes days after the search date. Default: `30`.
- `--timeout <seconds>` sets HTTP timeout. Default: `100`, or `300` when
  `--lmstudio-plugin` is used.
- `--model <name>` overrides the model for either client mode.
- `--api-key <key>` overrides environment-based API key selection.
- `--lmstudio-plugin <id>` uses LM Studio `/api/v1/chat` integrations.
- `--lmstudio-url <url>` sets the LM Studio `/api/v1` base URL.
- `--lmstudio-tools <list>` sets allowed plugin tools. Default: `search`.
- `--web-tool <type>` sets the Responses tool type. Default: `web_search`.
- `--no-web-search` omits the `web_search` tool from the request.
- `--run-dir <path>` overrides the structured run directory.
- `--overnight` enables safe long-running batch defaults.
- `--all` searches all entities when `--entity` is not set.
- `--continue-on-error` records failures and keeps going.
- `--delay <seconds>` waits between entities. Default: `0`, or `5` with
  `--overnight`.
- `--stop-after-failures <count>` stops after repeated consecutive failures.
  Default: unlimited, or `5` with `--overnight`.
- `--write-to-db` persists generated proposals into `activity_proposals`,
  `activity_proposal_entity_links`, and `activity_proposal_evidence`.
- `--connection-string <value>` sets the database connection string for
  `--write-to-db`.
- `--include-raw` includes raw model content and the full raw response.
