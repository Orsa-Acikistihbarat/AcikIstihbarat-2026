# /acikmedya static artifact

`index.html` in this folder is served raw (byte-for-byte, no Next.js layout/React
wrapping) at https://www.acikistihbarat.com/acikmedya via a rewrite in
`next.config.ts`.

Rules for anything placed here:
- Must be a single, fully self-contained HTML file: inline or absolute-URL CSS/JS/
  images only. No relative paths assuming Next.js routing/asset pipeline.
- Only `index.html` at this exact path is wired up. Nested pages
  (e.g. `/acikmedya/foo`) are NOT served unless the rewrite in `next.config.ts` is
  also updated to add them.
- After replacing this file, a normal `git add`, `git commit`, `git push origin main`
  is enough - the existing `.github/workflows/deploy.yml` pipeline rebuilds and
  redeploys the `frontend` container automatically. No manual VPS steps needed.
- The route is served with `Cache-Control: no-cache, must-revalidate` (set in
  `next.config.ts`) so updates to this file become visible immediately after deploy,
  without stale browser/CDN caching.
