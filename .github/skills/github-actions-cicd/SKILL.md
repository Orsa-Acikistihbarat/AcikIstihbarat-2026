---
name: github-actions-cicd
description: 'Use when setting up, debugging, or troubleshooting GitHub Actions CI/CD workflows in this repo — reusable workflows referenced via uses:, cross-repo/org reusable workflow calls, Docker Compose based VPS deployment (SSH deploy), "workflow was not found" errors, SSH key/auth failures in deploy steps, Docker network conflicts during compose up, or any pipeline touching ci.yml/deploy.yml.'
---

# GitHub Actions CI/CD (this repo)

Two workflows drive this project's automation:
- `.github/workflows/ci.yml` — calls a **reusable workflow** hosted in the org-wide `Orsa-Acikistihbarat/.github` repo (`reusable-ci.yml`) to run build/lint checks.
- `.github/workflows/deploy.yml` — SSHes into the Hostinger VPS, pulls `main`, and rebuilds the Docker Compose stack (`db`, `api`, `frontend`, `admin`, `caddy`).

## Reusable workflows (`ci.yml`) — "workflow was not found"

If `uses: <org>/<repo>/.github/workflows/<file>.yml@<ref>` fails with `workflow was not found`, despite the ref/branch, org "Actions access" setting, and calling repo's Actions permissions all looking correct, **do not trust the GitHub web UI's file browser/path display** — it can show a misleading path, especially for repos literally named `.github` (the UI seems to fold/hide the `.github/` prefix inconsistently).

Get ground truth via the GitHub API instead:
```powershell
gh api repos/<owner>/<repo>/contents/.github/workflows/<file>.yml --jq '{path: .path, name: .name}'
# If 404, try without the .github prefix:
gh api repos/<owner>/<repo>/contents/workflows/<file>.yml --jq '{path: .path, name: .name}'
```
The reusable workflow file **must physically live under `.github/workflows/`** in its own repo to be callable at all — a file merely named `workflows/foo.yml` at the repo root (missing the `.github/` prefix) will never resolve, no matter what path the caller's `uses:` references.

**Fix** (move the file to the correct path, preserving history):
```powershell
git clone https://github.com/<owner>/<repo>.git <tmp-dir>
cd <tmp-dir>
mkdir .github
git mv workflows .github/workflows   # or git mv workflows/<file>.yml .github/workflows/<file>.yml
git commit -m "fix: move reusable workflow into .github/workflows"
git push origin main
```
Then verify with the same `gh api ... --jq '.path'` call before retriggering the caller workflow (empty commit: `git commit --allow-empty -m "retrigger" && git push`).

Other things to verify (in order, cheapest first) if this happens again:
1. Ground-truth file path via `gh api` (above) — usually the actual root cause.
2. Org setting: **Settings → Actions → General → Access** = "Accessible from repositories in the organization."
3. Calling repo's **Settings → Actions → General → Workflow permissions** allows org reusable workflows.
4. `ref` (branch/tag/SHA) actually exists on the target repo.

## Deploy workflow (`deploy.yml`) — SSH + Docker Compose to VPS

Known failure modes and fixes, roughly in the order they tend to surface:

1. **`ssh-keyscan` exits 1 with no output** → network-level block, not an SSH config issue. Check for a **cloud provider firewall** (e.g. Hostinger's dashboard-level firewall) separate from the OS firewall (`ufw`) — `ufw status` can show inactive while the cloud firewall still blocks inbound port 22 externally. Confirm with `Test-NetConnection <host> -Port 22` from a local machine after any firewall change (allow ~1 minute to propagate).

2. **`Permission denied (publickey,password)`, exit 255** → check `~/.ssh/authorized_keys` on the VPS for corruption. Appending a key via `cat >> authorized_keys` or a web console heredoc paste can merge two keys onto one unparseable line if a trailing newline is missing. Fix with a single-line command (avoid multi-line heredocs through web/VNC consoles, which can mangle pastes):
   ```bash
   printf '%s\n%s\n' "ssh-ed25519 AAAA... key1" "ssh-ed25519 AAAA... key2" > ~/.ssh/authorized_keys
   chmod 600 ~/.ssh/authorized_keys
   ```

3. **`network ... has active endpoints`** when running `docker compose up -d <subset-of-services>` → never pass a subset of service names to `up`/`build` if other services share the same Compose network (e.g. a reverse proxy like `caddy` not included in the subset). Always run the full stack:
   ```bash
   docker compose build <services-to-rebuild>   # build subset is fine
   docker compose up -d                          # up: no service args, whole stack
   ```

4. **`container ... is not connected to the network ...`** → leftover corrupted Docker network/container bookkeeping from a prior partial/failed `up`. One-time manual fix on the VPS:
   ```bash
   docker compose down --remove-orphans
   ```
   For durable protection against recurrence, add this as a step in `deploy.yml` before `build`/`up` (safe — does not touch named volumes, so DB data persists):
   ```yaml
   docker compose down --remove-orphans
   docker compose build <services>
   docker compose up -d
   ```

## General debugging approach for this pipeline

- Always retrigger with an empty commit to re-run a workflow without unrelated changes: `git commit --allow-empty -m "retrigger: <reason>" && git push origin main`.
- When the GitHub web UI and API/CLI disagree, trust `gh api` — it returns the real tracked git path/content, not a rendered/cached UI view.
- Diagnose one layer at a time (network → auth → application-level Docker state) — each layer's error can look unrelated to the previous one but is actually the next blocker in the same chain.
