# Deploy

This folder contains the non-code deployment assets for SESport.

## Contents

- `deploy/systemd/`
  - systemd units and timer for the web app, SearXNG, backups, and LLM
- `deploy/caddy/Caddyfile`
  - reverse proxy config for `sesport.se`, `dev.sesport.se`, and
    `xng.sesport.se`
- `deploy/searxng/settings.yml`
  - override config mounted into the SearXNG container

## Notes

- Copy the systemd units into `/etc/systemd/system/`
- Copy the Caddyfile into the Caddy config location used on the VPS
- The SearXNG override is mounted directly from the repo by
  `compose.yaml`
