# SESport.AIActivitySearch

Runs an OpenAI Responses-compatible AI activity search for entities in
`data/entity-watchlist.json`.

The default target is LM Studio running locally at
`http://127.0.0.1:1234/v1` with the `gpt-oss-20b` model.
When `--lmstudio-plugin` is used, the default model becomes
`openai/gpt-oss-20b`, which matches LM Studio's native model identifier.

## Run Locally

Start LM Studio's local API server, load `gpt-oss-20b`, and make sure the
model has access to the `web-search` tool if you want live search.

For the LM Studio plugin path, use `/api/v1/chat` integrations:

```powershell
dotnet run --project tools\SESport.AIActivitySearch `
   -- --entity tre-kronor --lmstudio-plugin altra/web-search
```

The default path uses the OpenAI Responses API shape. This is useful for
OpenAI compatibility, but LM Studio may ignore OpenAI built-in tool types:

```powershell
dotnet run --project tools\SESport.AIActivitySearch `
   -- --entity tre-kronor
```

Search the first entity in the watchlist:

```powershell
dotnet run --project tools\SESport.AIActivitySearch
```

Write the JSON result to a file:

```powershell
dotnet run --project tools\SESport.AIActivitySearch `
   -- --entity tre-kronor --output ai-activity-search.json
```

Write a structured run directory:

```powershell
dotnet run --project tools\SESport.AIActivitySearch `
   -- --take 5 --run-dir data\ai-activity-search-runs\test-run
```

The run directory contains:

- `manifest.json` with run settings, status, duration per entity, and
  aggregate results.
- `entities/*.json` with one result file per completed entity.
- `failures/*.json` with one error file per failed entity, including duration.

When `--run-dir` is set, the tool writes results to disk and does not print
the full JSON document to stdout unless `--output` is also set.

## Overnight Mode

`--overnight` is intended for long unattended LM Studio runs. It writes a
timestamped run directory under `data\ai-activity-search-runs`, searches all
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
- `--lmstudio-plugin <id>` uses LM Studio `/api/v1/chat` integrations.
- `--lmstudio-url <url>` sets the LM Studio `/api/v1` base URL.
- `--lmstudio-tools <list>` sets allowed plugin tools. Default: `search`.
- `--web-tool <type>` sets the Responses tool type. Default: `web_search`.
- `--no-web-search` omits the `web_search` tool from the request.
- `--run-dir <path>` writes a structured run directory.
- `--overnight` enables safe long-running batch defaults.
- `--all` searches all entities when `--entity` is not set.
- `--continue-on-error` records failures and keeps going.
- `--delay <seconds>` waits between entities. Default: `0`, or `5` with
  `--overnight`.
- `--stop-after-failures <count>` stops after repeated consecutive failures.
  Default: unlimited, or `5` with `--overnight`.
- `--include-raw` includes raw model content and the full raw response.

The first version prints proposal drafts only. It does not write to the
database; that keeps model calibration separate from persistence.
