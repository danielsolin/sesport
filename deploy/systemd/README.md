# systemd Units

Copy only the units that belong on the target machine to
`/etc/systemd/system/` with `sudo. Then reload systemd and enable the
services or timer you want active.

The SESport web services load `/home/daniel/sesport/.env` through
`EnvironmentFile`. Keep the single active PostgreSQL connection there.

The `sesport-dev.service` unit runs
`/home/daniel/sesport/src/SESport.Web` with `dotnet watch`. Changes to source
files and static assets are therefore available at `dev.sesport.se` without a
publish step. Caddy proxies that hostname to port 5001.

The `llama-server.service` unit invokes the locally configured LLM startup
command. Keep the service's startup command stable and update its model
configuration when switching the active model.

The `sesport-unison.service` unit is a user service for the local two-way
sync client. The remote host only needs a matching Unison binary and SSH
access; do not run a second Unison service on the remote host.

## Units

- `llama-server.service`
- `searxng.service` for local AI-run machines only
- `sesport.service`
- `sesport-dev.service`
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
