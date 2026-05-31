# SESport.AIActivitySearch

Runs an OpenAI Responses-compatible AI activity search for entities in
`data/entity-watchlist.json`.

The default target is LM Studio running locally at
`http://127.0.0.1:1234/v1` with the `gpt-oss-20b` model.

## Run Locally

Start LM Studio's local API server, load `gpt-oss-20b`, and make sure the
model has access to the `web-search` tool if you want live search.

From the repository root:

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
- `--no-web-search` omits the `web_search` tool from the request.

The first version prints proposal drafts only. It does not write to the
database; that keeps model calibration separate from persistence.
