# SESport.Web

`SESport.Web` is the Razor Pages web application for the public sesport
site and the manual administration interface.

## Prerequisites

- .NET 10 SDK
- Tesseract OCR with English language data on machines that run AI jobs

On Ubuntu, install the OCR dependency with:

```bash
sudo apt-get install tesseract-ocr tesseract-ocr-eng
```

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

The repository-root `.env` file is the source of truth for the single
PostgreSQL database. The web app reads the database connection from process
environment variables. Copy `.env.example` to `.env`, then load it into your
shell before starting the app. The app does not auto-read `.env`; systemd
loads it through `EnvironmentFile` in deployed services.

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

## Membership

Public membership uses passwordless email links. Development logs the login
link when SMTP is not configured. Production requires the following values in
the host-local `.env` file:

```text
MemberAuth__PublicBaseUrl=https://sesport.se
Smtp__Host=<smtp_host>
Smtp__Port=587
Smtp__UseSsl=true
Smtp__Username=<smtp_username>
Smtp__Password=<smtp_password>
Smtp__FromAddress=<sender_email>
Smtp__FromName=sesport
```

The login token is single-use and expires after fifteen minutes by default.
The database also contains the member-to-entity watch table for the later
notification feature; the public watch UI is not enabled yet.

SearXNG is used only by AI runs. Run it locally on the machine that runs
AI jobs and point the application at that local instance:

```bash
docker compose up -d searxng
```

```powershell
$env:SearXNG__BaseUrl="http://127.0.0.1:8088/"
```

## SearXNG Config

The file in `deploy/searxng/settings.yml` is an override, not a
full replacement. Keep `use_default_settings: true` there so the
container merges our local tweaks with the image defaults.

The application defaults to `http://127.0.0.1:8088/` for SearXNG. The
service is not part of the public `*.sesport.se` deployment surface.

## Sport Date Rule

- Persisted activity dates and times always represent the real calendar
  moment when an activity happens.
- `SportDay` is only used for presentation and grouping, such as public
  page buckets and admin date views.
- Do not use `SportDay` to rewrite database dates during import or editing.
