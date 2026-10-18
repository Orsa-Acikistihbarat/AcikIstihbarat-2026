# Project TechCard — AcikIstihbarat-2026

Canonical registry of routes, hosts and deploy facts. Update when new facts are verified.

## Production VPS (Hostinger)
- Host: `187.127.85.102` (DNS: `acikistihbarat.com`, `www.acikistihbarat.com`, `api.acikistihbarat.com`)
- SSH: `ssh -i ~/.ssh/id_ed25519 root@187.127.85.102` (port 22)
- Deploy path: `/docker/acikistihbarat-2026`
- GitHub: variables `VPS_PORT`, `DEPLOY_PATH`; secrets `VPS_HOST`, `VPS_USER`, `VPS_SSH_PRIVATE_KEY` (environment `production`)

## Services (docker compose, network `acik-network`)
| Container | Host port | Notes |
|---|---|---|
| acik_caddy | 80, 443 | Reverse proxy, config in `Caddyfile` (routes by Host) |
| acik_frontend | 14000 -> 3000 | Next.js public site |
| acik_api | 15128 -> 8080 | .NET API |
| acik_admin | 15173 -> 80 | Vite admin (nginx) |
| acik_db | 11433 -> 1433 | SQL Server, volume `sqldata` |
| acik_acikmedya_webhook | internal 4001 | Newsletter webhook |

All services use `restart: unless-stopped` (added 2026-10-18).

## Deploy
- Push to `main` triggers `.github/workflows/deploy.yml` (git reset, compose build, EF migrations, `compose up -d`).
- Manual: `cd /docker/acikistihbarat-2026 && docker compose up -d`.
- Verify by image SHA, not compose output: compare `docker inspect <c> --format '{{.Image}}'` with `docker images`.
- When verifying via Caddy, send `-H 'Host: <routed-hostname>'`.

## Incident log
- 2026-10-18: site returned 502. Frontend died ~10-13 with Node heap OOM; other containers exited (255) after a host restart and had no restart policy. Fixed with `docker compose up -d`, then added restart policies, `NODE_OPTIONS=--max-old-space-size=1024` and `mem_limit: 1536m` for the frontend.
