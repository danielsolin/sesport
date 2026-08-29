# systemd Units

Copy only the units that belong on the target machine to
`/etc/systemd/system/` with `sudo. Then reload systemd and enable the
services or timer you want active.

The SESport web services load `/home/daniel/sesport/.env` through
`EnvironmentFile`. Keep the single active PostgreSQL connection there.

The `dotnet-run.service` unit runs the local web app from
`/home/daniel/sesport/src/SESport.Web` with the Release build configuration
and the Development runtime environment. It builds on start with:

```text
dotnet run --configuration Release
```

It does not enable Browser Refresh or Hot Reload.

The separate `sesport-dev.service` unit runs
`/home/daniel/sesport/src/SESport.Web` with `dotnet watch`. Changes to source
files and static assets are therefore available at `dev.sesport.se` without a
publish step. Caddy proxies that hostname to port 5001.

The `llama-server.service` unit invokes the locally configured LLM startup
command. Keep the service's startup command stable and update its model
configuration when switching the active model.

The `sesport-unison.service` unit is a user service for the local two-way
sync client. The remote host only needs a matching Unison binary and SSH
access; do not run a second Unison service on the remote host.

The `sesport-mcp.service` unit runs the published `SESport.MCP` server, which
exposes the project's web research tools (`web_search`, `web_get_page`) over
Streamable HTTP on loopback (`http://127.0.0.1:5110/`). It runs as a long-lived
process so the Playwright/web stack stays warm; Codex CLI connects to the URL
instead of spawning a per-thread child process. Republish
`src/SESport.MCP` and restart the unit after code changes.

## Units

- `llama-server.service`
- `searxng.service` for local AI-run machines only
- `dotnet-run.service`
- `sesport.service`
- `sesport-dev.service`
- `sesport-mcp.service`
- `sesport-db-backup.service`
- `sesport-db-backup.timer`
- `sesport-db-cleanup.service`
- `sesport-db-cleanup.timer`
- `sesport-db-vacuum-full.service`
- `sesport-db-vacuum-full.timer`
- `sesport-web-stats.service`
- `sesport-web-stats.timer`
- `sesport-unison.service` as a user service on the sync client

## Install Example

```bash
sudo cp deploy/systemd/*.service /etc/systemd/system/
sudo cp deploy/systemd/*.timer /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now sesport.service
```

The MCP unit runs a published build, so publish it before first start. It
listens on loopback only and needs no public hostname:

```bash
dotnet publish src/SESport.MCP/SESport.MCP.csproj -c Release \
  -o src/SESport.MCP/publish
sudo systemctl enable --now sesport-mcp.service
```

Install GoAccess before enabling the web statistics timer:

```bash
sudo apt-get install goaccess
sudo systemctl enable --now sesport-web-stats.timer
```

The timer generates a report for the previous calendar day every night and
writes the HTML reports to `data/web-stats`. Requests under `/Admin` are
excluded from the reports.

The database cleanup timer runs once per hour, with a randomized delay of up
to 30 minutes to avoid starting maintenance exactly on the hour.

The full AI database vacuum timer runs nightly at 03:30, with a randomized
delay of up to 10 minutes. It rewrites only `ai_job_runs` and uses bounded
lock and statement timeouts. Enable it with:

```bash
sudo systemctl enable --now sesport-db-vacuum-full.timer
```

## Unison Two-Way Sync

Install the same Unison version on the local sync client and the remote host.
The profile in `deploy/unison/sesport.prf` is intended for the local client
and synchronizes only `bin/`, `data/`, and `jobs/`.

Install the profile and user service on the local client:

```bash
mkdir -p /home/daniel/.unison
cp deploy/unison/sesport.prf /home/daniel/.unison/sesport.prf
mkdir -p /home/daniel/.config/systemd/user
cp deploy/systemd/sesport-unison.service \
   /home/daniel/.config/systemd/user/sesport-unison.service
unison sesport
systemctl --user daemon-reload
systemctl --user enable --now sesport-unison.service
sudo loginctl enable-linger daniel
```

The first `unison sesport` run must be manual if the replicas have not been
synchronized before. Review and resolve the initial changes interactively.
The service uses `repeat = 60` and leaves conflicting changes for manual
resolution.

Inspect the service with:

```bash
systemctl --user status sesport-unison.service
journalctl --user -u sesport-unison.service -f
```

Do not enable `searxng.service` on a web or database host unless that
machine also runs AI jobs locally.

## Backup Switch

If you are moving `sesport-db-backup` from the user timer to the system
timer, disable the user timer before enabling the system one.
