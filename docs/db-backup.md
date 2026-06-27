# Database Backup Timer

This repo ships a `systemd` service and timer for running
`bin/db-backup.sh` once an hour on the server.
The script keeps only the last 24 hours of `.dump` backups.

## Files

- `deploy/systemd/sesport-db-backup.service`
- `deploy/systemd/sesport-db-backup.timer`

## Install

Copy the units into `/etc/systemd/system/`, then reload `systemd` and
enable the timer:

```bash
sudo cp deploy/systemd/sesport-db-backup.service \
   /etc/systemd/system/
sudo cp deploy/systemd/sesport-db-backup.timer \
   /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now sesport-db-backup.timer
```

## Verify

```bash
systemctl status sesport-db-backup.timer
systemctl list-timers sesport-db-backup.timer
journalctl -u sesport-db-backup.service -n 100 --no-pager
```

## Notes

- The service expects the repo to be at `/home/daniel/sesport`.
- `bin/db-backup.sh` also commits and pushes each backup file, so Git
  credentials and push access must already work non-interactively.
- `flock` prevents overlapping backup runs if one job is still active
  when the next hourly trigger fires.
- Older `.dump` files are removed on each run, so the backup set stays
  within a rolling 24-hour window.
- If the user timer is already enabled, disable it before switching to
  the system timer.
