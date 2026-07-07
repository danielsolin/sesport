# Deploy

This folder contains the non-code deployment assets for SESport.

## Contents

- `deploy/systemd/`
  - systemd units and timer for the web app, SearXNG, backups, and LLM
- `deploy/caddy/Caddyfile`
  - reverse proxy config for `sesport.se` and `dev.sesport.se`
- `deploy/searxng/settings.yml`
  - override config mounted into the local SearXNG container used by AI runs

## Role Split

- VPS/database host:
  - runs the public web services
  - runs PostgreSQL with `docker compose up -d postgres`
  - does not run SearXNG for this project
- Local AI-run machine:
  - runs local SearXNG with `docker compose up -d searxng`
  - may run LLM services and AI workers
  - does not run PostgreSQL for this project

## Docker

Docker is required on local AI-run machines for SearXNG, and on the
VPS/database host only if it operates the PostgreSQL container.

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

Start PostgreSQL only on the VPS/database host:

```bash
docker compose up -d postgres
```

## Notes

- Copy the systemd units into `/etc/systemd/system/`
- Copy the Caddyfile into the Caddy config location used on the VPS
- The SearXNG override is mounted directly from the repo by `compose.yaml`
- SearXNG is intended to run locally on the machine that runs AI jobs.
  It is not exposed through the public `*.sesport.se` sites.
- PostgreSQL is intended to run only on the VPS/database host.
