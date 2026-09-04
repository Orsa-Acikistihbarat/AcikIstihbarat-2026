# Project Progress Report — AcikIstihbarat-2026

---

## 2026-08-08 22:50 UTC — VPS Deployment, SSL Troubleshooting, and Data Cleanup

### Changes Made

| Area | Change | Type |
|------|--------|------|
| Deployment | Removed `app.UseHttpsRedirection()` from `Program.cs` | Bug Fix |
| Deployment | Added `NEXT_PUBLIC_API_URL` ARG to Frontend `Dockerfile` | Bug Fix |
| Deployment | Added `dns` configuration to Caddy in `docker-compose.yml` | Config |
| Data | Triggered `cleanup-html` and `cleanup-yazi-html` API endpoints | Data Fix |

### Issues Resolved

- VPS Firewall timeout: Disabled UFW, configured Hostinger Ingress rules.
- Caddy DNS failure: Added `8.8.8.8` to Caddy service in docker-compose.
- Next.js 502/400 errors: Fixed by removing ASP.NET Core HTTPS Redirection.
- Next.js Mixed Content: Fixed by passing Next.js API URL to Docker build stage.
- Caddy "Not Safe" / Corrupted Image: Re-pulled Caddy alpine image and wiped cached Staging certificates from host volume.

### TODO Items Created

- Add visual loading indicators to search bars during navigation — 🟡 Medium
- Implement Footer — 🟡 Medium

### Notes

The full application stack is now stable, online, and fully secured with Let's Encrypt Production SSL certificates. The Next.js frontend has a cached API state, so if any database migrations are run in the future, the Next.js container must be restarted.
