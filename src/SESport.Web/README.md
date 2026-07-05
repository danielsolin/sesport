# SESport.Web

`SESport.Web` is the Razor Pages web application for the public sesport
site and the manual administration interface.

## Prerequisites

- .NET 10 SDK

## Start the local server

Run these commands from the repository root:

```powershell
dotnet run --project src\SESport.Web\SESport.Web.csproj --launch-profile http
```

The `http` launch profile serves the app at:

```text
http://localhost:5109
```

The `https` launch profile is also available if local HTTPS is configured:

```powershell
dotnet run --project src\SESport.Web\SESport.Web.csproj --launch-profile https
```

It serves:

```text
https://localhost:7156
http://localhost:5109
```

The web app reads the database connection from environment variables. Copy
`.env.example` to `.env`, then load it into your shell before starting the
app. The app does not auto-read `.env`.

The relevant keys are the `SESPORT_POSTGRES_*` variables. A simple Bash
example is:

```bash
set -a
. ./.env
set +a
dotnet run --project src/SESport.Web/SESport.Web.csproj --launch-profile http
```

## Administration

The admin area starts at:

```text
http://localhost:5109/Admin
```

Admin pages always require authentication. Set `Admin:Password` in every
environment, including local development, and then use the login page:

```text
http://localhost:5109/Admin/Login
```

For a local password without editing tracked files, use an environment
variable:

```powershell
$env:Admin__Password="<local-password>"
dotnet run --project src\SESport.Web\SESport.Web.csproj --launch-profile http
```

If `xng.sesport.se` is protected with Basic Auth, set:

```powershell
$env:SearXNG__BasicAuthUsername="<searxng-user>"
$env:SearXNG__BasicAuthPassword="<searxng-password>"
```

## SearXNG Config

The file in `deploy/searxng/settings.yml` is an override, not a
full replacement. Keep `use_default_settings: true` there so the
container merges our local tweaks with the image defaults.

If you test the backend directly on `127.0.0.1:8088`, SearXNG will log
warnings about missing `X-Forwarded-For` or `X-Real-IP` headers. That
is expected; SearXNG is served through Caddy at `xng.sesport.se`.

## Sport Date Rule

- Persisted activity dates and times always represent the real calendar
  moment when an activity happens.
- `SportDay` is only used for presentation and grouping, such as public
  page buckets and admin date views.
- Do not use `SportDay` to rewrite database dates during import or editing.
