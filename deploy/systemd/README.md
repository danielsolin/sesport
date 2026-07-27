# systemd Units

These files are the source of truth for SESport systemd units.

Copy only the units that belong on the target machine to
`/etc/systemd/system/` with `sudo`, then reload systemd and enable the
services or timer you want active.

The SESport web services load `/home/daniel/sesport/.env` through
`EnvironmentFile`. Keep the single active PostgreSQL connection there.

## Units

- `llama-server.service`
- `searxng.service` for local AI-run machines only
- `sesport.service`
- `sesport-dev.service`
- `sesport-db-backup.service`
- `sesport-db-backup.timer`
- `sesport-db-cleanup.service`
- `sesport-db-cleanup.timer`
- `sesport-web-stats.service`
- `sesport-web-stats.timer`

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

The timer generates a report for the previous calendar day every night,
commits the HTML reports in `data/web-stats`, and pushes them to the current
branch.

The database cleanup timer runs once per hour, with a randomized delay of up
to 30 minutes to avoid starting maintenance exactly on the hour.

Do not enable `searxng.service` on a web or database host unless that
machine also runs AI jobs locally.

## Backup Switch

If you are moving `sesport-db-backup` from the user timer to the system
timer, disable the user timer before enabling the system one.
