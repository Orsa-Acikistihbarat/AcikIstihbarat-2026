# acikmedya-webhook

Tiny internal HTTP service that receives self-contained HTML over a POST request
and writes it atomically to the `/acikmedya` publish folder, which is bind-mounted
live into the `frontend` container. This makes publishing instant - no git commit,
no CI build, no redeploy required.

## Why this exists

`acik-istihbarat-public/public/acikmedya/index.html` is served raw (byte-for-byte,
no Next.js layout/React wrapping) at https://www.acikistihbarat.com/acikmedya via a
rewrite in `next.config.ts`. That file is intentionally **not** git-tracked (see
root `.gitignore`) so that routine `git pull`-based deploys never overwrite or
conflict with content published through this webhook.

**Important:** `acik-istihbarat-public/public/acikmedya/` must contain ONLY
`index.html`. That whole folder is bind-mounted into both the `acikmedya-webhook`
and `frontend` containers, and Next.js serves everything under `public/` verbatim -
any other file placed there (docs, notes, etc.) would become publicly reachable at
a predictable URL. Keep documentation here in this folder instead.

## Publishing new content

```
curl -X POST https://www.acikistihbarat.com/hooks/acikmedya \
  -H "Authorization: Bearer $ACIKMEDYA_WEBHOOK_TOKEN" \
  -H "Content-Type: text/html" \
  --data-binary @index.html
```

Rules:
- Body must be a single, fully self-contained HTML document: inline or absolute-URL
  CSS/JS/images only. No relative paths assuming Next.js routing/asset pipeline.
- Body must start with `<!doctype html` or `<html` (case-insensitive) or the
  request is rejected with `400`.
- Max body size is 256 KB (`413` if exceeded).
- The bearer token is supplied out-of-band (VPS-only `.env`, see `.env.example` at
  repo root) - it is never stored in this repository.
- Rate limited to 10 requests/minute per client IP (`429` if exceeded).
- Publish is instant: the response confirms the write, and the change is visible at
  `/acikmedya` immediately - no CI/CD run is triggered by this endpoint.
- The route is served with `Cache-Control: no-cache, must-revalidate` (set in
  `next.config.ts`) so updates become visible immediately, without stale
  browser/CDN caching.

## Response codes
- `200` - written successfully.
- `401` - missing/invalid bearer token.
- `400` - wrong `Content-Type` or payload failed the HTML sanity check.
- `413` - payload exceeds 256 KB.
- `429` - rate limit exceeded.
- `500` - write failure on the server side.

## Per-folder newsletters (`/hooks/acikmedya/:folder`)

A second, independent endpoint publishes dated newsletter issues into named folders
instead of the single `/acikmedya` page above. Content lands under
`acik-istihbarat-public/acikmedya-data/<folder>/` (bind-mounted read-write into this
container, read-only into `frontend`) and is served by the Next.js app at
`/acikmedya/<folder>` (App Router route, NOT the raw-file rewrite used by
`/acikmedya`) — see `acik-istihbarat-public/AGENTS.md` for the frontend side.

```
curl -X POST https://www.acikistihbarat.com/hooks/acikmedya/AcikGazete \
  -H "Authorization: Bearer $ACIKMEDYA_WEBHOOK_TOKEN" \
  -H "Content-Type: text/html" \
  --data-binary @issue.html
```

Rules:
- `:folder` must match `^[A-Za-z0-9_-]+$` and be one of the names listed in
  `acikmedya-newsletters.json` (repo root) — otherwise `400`/`404`.
- Same body rules as `/hooks/acikmedya`: self-contained HTML, starts with
  `<!doctype html`/`<html`, max 256 KB, same bearer token, same 10 req/min rate
  limit (shared limiter, not a separate bucket per folder).
- The write is saved as `index<DDMMYY>.html` (e.g. `index060926.html`), where the
  date is "today" in the `Europe/Istanbul` timezone — NOT taken from the request.
  Publishing again on the same day overwrites that day's file; publishing on a new
  day creates a new dated file alongside older ones (nothing is deleted
  automatically).
- The frontend always shows the latest file whose date is today-or-earlier
  (falls back to the most recent past issue if nothing was published today yet).

Response codes for this route: `200` `{status:'ok', folder, filename, bytes}`,
`400` (bad folder syntax / bad content-type / failed HTML sanity check), `401`
(bad token), `404` (folder not in the allow-list), `413` (too large), `429` (rate
limited), `500` (write failure).
