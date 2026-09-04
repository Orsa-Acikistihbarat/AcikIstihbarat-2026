# TroubleShooting — VPS SSL and Deployment

**Project(s):** AcikIstihbarat-2026

---

## 2026-08-08 22:50 UTC — VPS Hostinger Ingress Firewall Block

### Problem
Site timed out. `Test-NetConnection` timed out on ports 80, 443, and 22.

### Root Cause
Hostinger's external dashboard firewall was in a "Default Deny" state when activated, blocking all incoming HTTP, HTTPS, and SSH traffic.

### Fix Applied
Logged in via Web Terminal, disabled UFW to ensure no local firewall conflicts, and correctly configured Hostinger ingress rules to explicitly allow 80 and 443.

### Verification
`curl -I http://187.127.85.102` returned connection refused instead of timeout, indicating traffic reached the VPS but Caddy wasn't listening correctly.

### Prevention / Notes
When using a VPS provider dashboard firewall, ensure `default-allow` or explicit rules for necessary ports are added before enabling it, as it overrides the OS-level firewall.

---

## 2026-08-08 22:50 UTC — Caddy DNS Resolution Crash in Docker

### Problem
Caddy logged `dial tcp: lookup acme-staging-v02.api.letsencrypt.org on 127.0.0.53:53: connection refused`. Caddy couldn't get Let's Encrypt certificates.

### Root Cause
Docker containers cannot always use the Ubuntu host's default `systemd-resolved` loopback (`127.0.0.53`), causing DNS queries inside Alpine/Caddy to fail entirely.

### Fix Applied
Injected public DNS servers directly into the Caddy service block in `docker-compose.yml`:
```yaml
dns:
  - 8.8.8.8
  - 1.1.1.1
```

### Verification
Caddy successfully connected to Let's Encrypt API servers.

### Prevention / Notes
Always hardcode reliable public DNS in reverse proxy Docker services if host resolution is unpredictable.

---

## 2026-08-08 22:50 UTC — Next.js 502 Bad Gateway / 400 Bad Request

### Problem
Site loaded with `502 Bad Gateway`. Next.js logs showed `400 Bad Request` from the backend API.

### Root Cause
1. `NEXT_PUBLIC_API_URL` was passed to the `frontend` container environment, but not added to the `Dockerfile` as an `ARG`, causing Next.js to bake `http://localhost:5128` into the client bundle, causing Mixed Content.
2. After fixing the URL to `https://api.acikistihbarat.com`, the request hit Caddy, which proxied it internally as plain HTTP. The backend ASP.NET Core API had `app.UseHttpsRedirection()` active, which blocked the incoming HTTP request with a `400 Bad Request`.
3. When Caddy was restarted, the Next.js container tried to fetch data while Caddy was offline, crashing the Next.js process (resulting in the `502 Bad Gateway`).

### Fix Applied
- Added `ARG NEXT_PUBLIC_API_URL` to `acik-istihbarat-public/Dockerfile`.
- Removed `app.UseHttpsRedirection()` from `AcikIstihbarat.API/Program.cs`.
- Restarted the frontend container to recover from the crash.

### Verification
Frontend successfully fetched data from the API and rendered correctly without 502s or 400s.

### Prevention / Notes
Never use `app.UseHttpsRedirection()` in an ASP.NET Core container that sits behind an SSL-terminating reverse proxy unless you explicitly configure Forwarded Headers middleware.

---

## 2026-08-08 22:50 UTC — Caddy Corrupted Image & Staging Certificate

### Problem
Browser flagged the site as "Not Safe" despite no mixed content. Running `exec` in the Caddy container threw `exec: "rm": executable file not found in $PATH` or `exec: "caddy": executable file not found`.

### Root Cause
1. The `caddy:2-alpine` image layers were severely corrupted on the VPS disk during a pull or extraction.
2. Due to the earlier DNS failures, Let's Encrypt had issued a "Staging" (untrusted) certificate to Caddy. 

### Fix Applied
- Deleted the corrupted image (`docker rmi caddy:2-alpine`).
- Wiped the cached staging certificates directly from the host storage: `sudo rm -rf /var/lib/docker/volumes/*caddy_data/_data/caddy/certificates`.
- Recreated the container (`docker compose up -d caddy`), forcing a fresh image pull and a fresh Production Let's Encrypt certificate.
- Overcame browser caching of the bad certificate by testing in Incognito Mode.

### Verification
Site displayed a valid, trusted SSL certificate (green padlock) in Incognito Mode.

### Prevention / Notes
When Caddy gets stuck in Staging mode, deleting its `certificates` directory is the fastest way to reset its ACME rate-limits and force a Production cert pull.
