# Deploy

This folder contains the non-code deployment assets for SESport.

## Contents

- `deploy/systemd/`
  - systemd units and timer for the web app, SearXNG, backups, and LLM
- `deploy/caddy/Caddyfile`
  - reverse proxy config for `sesport.se` and `dev.sesport.se`
- `deploy/mail.md`
  - direct Postfix delivery and OpenDKIM setup for the web host
- `deploy/searxng/settings.yml`
  - override config mounted into the local SearXNG container used by AI runs

## Role Split

- Web/database host:
  - runs the public web services
  - may operate the single PostgreSQL database referenced by `.env`
  - does not run SearXNG for this project
- Local AI-run machine:
  - runs local SearXNG with `docker compose up -d searxng`
  - may run LLM services and AI workers
  - does not start a second PostgreSQL database for this project

## Docker

Docker is required on local AI-run machines for SearXNG, and on the
database host only if that host operates the PostgreSQL container.

On Ubuntu:

```bash
sudo apt-get update
sudo apt-get install -y docker.io docker-compose-v2
sudo systemctl enable --now docker
sudo usermod -aG docker "$USER"
```

Open a new shell after `usermod` before running Docker without `sudo`.

Start local SearXNG on an AI-run machine:

```bash
docker compose up -d searxng
```

Start PostgreSQL only on the machine that intentionally operates the
database referenced by `.env`:

```bash
docker compose up -d postgres
```

## Notes

- Copy the systemd units into `/etc/systemd/system/`
- Install GoAccess on the web host for the `/Admin/Config/Stats` report
- Copy the Caddyfile into the Caddy config location used on the VPS
- Keep a host-local `.env` in the repository root on each machine that runs
  a systemd service. The file is intentionally ignored by git, but
  `sesport.service`, `sesport-dev.service`, and related service units load it
  through `EnvironmentFile=/home/daniel/sesport/.env`.
- The SearXNG override is mounted directly from the repo by `compose.yaml`
- SearXNG is intended to run locally on the machine that runs AI jobs.
  It is not exposed through the public `*.sesport.se` sites.
- PostgreSQL connection settings come from `.env`. There is one active
  project database; do not start another one for local AI-run machines.
