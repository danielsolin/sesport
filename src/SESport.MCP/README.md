# SESport.MCP

An MCP (Model Context Protocol) server that exposes the project's web research
and activity lookup tools to external MCP clients such as Codex CLI.

The server registers five tools and owns the search, page-fetch, and activity
lookup implementations behind them:

| Tool | Implementation | Returns |
| --- | --- | --- |
| `web_search` | `IWebSearchClient` | `WebSearchResponse` |
| `web_get_page` | `IWebPageContentClient` | `WebPageToolResponse` |
| `web_find_in_page` | `IWebPageContentClient` | text |
| `db_search_activity` | `ActivityReadRepository` | activity summaries |
| `db_get_activity` | `ActivityReadRepository` | activity details |

`db_search_activity` searches published activities using at least one of
`text`, `date`, or `sport`. Text and sport matching are case-insensitive. The
sport value is matched against the database ID, name, and display name; no
sport list is hard-coded in the server. Search results use the same activity
grouping identity as the public timeline, so one activity is not returned
multiple times because of grouped rows or broadcast variants. Use the
returned UUID with `db_get_activity`.

`db_get_activity` returns one activity with its core schedule, group and
organization context, and person participants. Participant rows include
their birth date, formative club, and any stored start time.
The response intentionally omits facts, source metadata, broadcasts,
publication state, teaser, TV channel, and other operational relations.

The server does not summarize the fetched content. The MCP response projects
the fetcher result to the public response contract and deliberately omits the
internal `MainTextFull`, `RelevantLinks`, and `RelevantImages` fields;
`MainText` retains the shared
`WebPageFetchDefaults.MaxResponseCharacters` cutoff and ends with `[CUTOFF]`
when truncation occurs. Use `web_find_in_page` to find text beyond the cutoff.
The response includes `RenderWarning` when placeholder content suggests that
the rendered page may be incomplete. `include_social_media` is hard-coded to
`false` in the current version.

Clean page results are cached for a short period and equivalent concurrent
requests share one fetch. Failed, empty, partial, and warning-bearing results
are not stored as clean cache entries. The page pipeline follows redirects
manually, checks each target with the basic URL policy, and limits downloaded
responses to `WebPageFetchDefaults.MaximumResponseBytes`.

### Structured content serialization

The tools opt into MCP structured content, so the SDK generates an output
schema from the return type and validates the serialized response against it.
`WebPageContent` and `WebSearchResponse` have nullable properties that are
regularly `null`; the default JSON serialization omits those, and the
validator then fails with `must have required property 'publishedAt'`.

To keep the shared records untouched, the server registers the
tools with explicit `JsonSerializerOptions` (`DefaultIgnoreCondition = Never`,
plus the `DefaultJsonTypeInfoResolver` that System.Text.Json 9+ requires
before options are marked read-only). This emits `null` values explicitly so
the response always satisfies the schema.

## Configuration

The server reads its environment configuration, in particular the `SearXNG__*`
variables (see the repository-root `.env`).
Load the environment before starting the server:

```sh
. ./.env
```

`web_get_page` uses the fetcher pipeline hosted by this server, including the
Playwright browser fetcher, so the Playwright browsers must be installed on
the machine running the server (`playwright install` / `dotnet` Playwright
browsers). Tesseract with English language data is required for image OCR.

The URL policy intentionally covers the current internal deployment's basic
needs: only HTTP(S), ordinary public hostnames, and literal public IP
addresses are accepted. It is not a custom DNS or general SSRF subsystem.

The MCP project owns the web and database lookup tools. Legacy AI provider
clients, including Llama and OpenRouter, remain in
`SESport.AI/Clients/Legacy` and are not registered by this server.

## Running

The server speaks MCP over **Streamable HTTP** (stateless) and is intended to
run as a long-lived process, so the Playwright/web stack stays warm across
sessions. It is deployed as the `sesport-mcp.service` systemd unit:

```sh
sudo systemctl enable --now sesport-mcp.service
systemctl status sesport-mcp.service
```

The unit runs the project directly with `dotnet run --configuration Release`,
builds changed source files when it starts, and listens on loopback:

```
SESPORT_MCP_URL=http://127.0.0.1:5110   # overridable
```

To run it manually (for development) instead of via systemd:

```sh
dotnet run --project src/SESport.MCP
```

After a code change, restart the unit to build and run the updated source:

```sh
sudo systemctl restart sesport-mcp.service
```

## Registering with Codex CLI

Point Codex at the running Streamable HTTP endpoint (not a child command):

```sh
codex mcp add sesport --url http://127.0.0.1:5110/
```

or add it to `~/.codex/config.toml`:

```toml
[mcp_servers.sesport]
url = "http://127.0.0.1:5110/"
```

Verify the registration:

```sh
codex mcp list
codex mcp get sesport
```
