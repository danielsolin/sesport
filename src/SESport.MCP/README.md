# SESport.MCP

An MCP (Model Context Protocol) server that exposes the project's existing
web research tools to external MCP clients such as Codex CLI.

The server registers three tools and forwards calls to the existing clients
and page-search support in `SESport.AI`:

| Tool           | Forwards to                          | Returns              |
|--------------  |--------------------------------------|----------------------|
| `web_search`   | `IWebSearchClient.SearchAsync`       | `WebSearchResponse`  |
| `web_get_page` | `IWebPageContentClient.FetchAsync` | `WebPageToolResponse` |
| `web_find_in_page` | `IWebPageContentClient.FetchAsync` | text |

The server does not summarize the fetched content. The MCP response projects
the fetcher result to the public response contract and deliberately omits the
internal `MainTextFull`, `RelevantLinks`, and `RelevantImages` fields;
`MainText` retains the shared
`WebPageFetchDefaults.MaxResponseCharacters` cutoff and ends with `[CUTOFF]`
when truncation occurs. Use `web_find_in_page` to find text beyond the cutoff.
The response includes `RenderWarning` when placeholder content suggests that
the rendered page may be incomplete. `include_social_media` is hard-coded to
`false` in the current version.

### Structured content serialization

The tools opt into MCP structured content, so the SDK generates an output
schema from the return type and validates the serialized response against it.
`WebPageContent` and `WebSearchResponse` have nullable properties that are
regularly `null`; the default JSON serialization omits those, and the
validator then fails with `must have required property 'publishedAt'`.

To keep the internal `SESport.AI` records untouched, the server registers the
tools with explicit `JsonSerializerOptions` (`DefaultIgnoreCondition = Never`,
plus the `DefaultJsonTypeInfoResolver` that System.Text.Json 9+ requires
before options are marked read-only). This emits `null` values explicitly so
the response always satisfies the schema.

## Configuration

The server reads the same environment configuration as `SESport.Web`, in
particular the `SearXNG__*` variables (see the repository-root `.env`).
Load the environment before starting the server:

```sh
. ./.env
```

`web_get_page` uses the same fetcher pipeline as the web app, including the
Playwright browser fetcher, so the Playwright browsers must be installed on
the machine running the server (`playwright install` / `dotnet` Playwright
browsers).

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
codex mcp add sesport-web --url http://127.0.0.1:5110/
```

or add it to `~/.codex/config.toml`:

```toml
[mcp_servers.sesport-web]
url = "http://127.0.0.1:5110/"
```

Verify the registration:

```sh
codex mcp list
codex mcp get sesport-web
```
