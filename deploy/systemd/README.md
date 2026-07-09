# systemd Units

These files are the source of truth for SESport systemd units.

Copy only the units that belong on the target machine to
`/etc/systemd/system/` with `sudo`, then reload systemd and enable the
services or timer you want active.

## Units

- `llama-server.service`
- `searxng.service` for local AI-run machines only
- `sesport.service`
- `sesport-dev.service`
- `sesport-db-backup.service`
- `sesport-db-backup.timer`
- `sesport-db-cleanup.service`
- `sesport-db-cleanup.timer`

## Install Example

```bash
sudo cp deploy/systemd/*.service /etc/systemd/system/
sudo cp deploy/systemd/*.timer /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now sesport.service
```

Do not enable `searxng.service` on the VPS/database host unless that machine
also runs AI jobs locally.

## Backup Switch

If you are moving `sesport-db-backup` from the user timer to the system
timer, disable the user timer before enabling the system one.
