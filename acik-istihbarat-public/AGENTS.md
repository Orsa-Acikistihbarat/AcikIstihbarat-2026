<!-- BEGIN:nextjs-agent-rules -->
# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` before writing any code. Heed deprecation notices.
<!-- END:nextjs-agent-rules -->

## Static /acikmedya artifact
Content at `public/acikmedya/index.html` is served raw at `/acikmedya` via a
`next.config.ts` rewrite - it does NOT go through App Router/layout. This file is
gitignored (not git-tracked) and must be published via the `acikmedya-webhook`
service, NOT via `git push` (a normal git push cannot change this route's content
because a `.gitignore`d file is never touched by `git pull` on deploy):

```
curl -X POST https://www.acikistihbarat.com/hooks/acikmedya \
  -H "Authorization: Bearer $ACIKMEDYA_WEBHOOK_TOKEN" \
  -H "Content-Type: text/html" \
  --data-binary @index.html
```

Publish is instant (no CI/build wait). Body must be self-contained HTML (inline or
absolute-URL CSS/JS/images only), max 256 KB, starting with `<!doctype html` or
`<html`. See `acikmedya-webhook/README.md` for full rules and response codes. The
bearer token lives only in the VPS's `.env` - never in this repo.

## Per-folder newsletters: `/acikmedya/[folder]`

Unlike `/acikmedya` above, `app/acikmedya/[folder]/page.tsx` IS a normal App
Router route (`force-dynamic`) — it reads whichever named folders are listed in
`acikmedya-newsletters.json` (repo root) and renders the latest dated HTML issue
under `acikmedya-data/<folder>/` (gitignored, bind-mounted read-only from the
webhook's write target) inside a sandboxed, auto-resizing iframe with no scripts
allowed. Folder/date parsing logic lives in `lib/newsletters.ts` and MUST stay in
sync with the identical regexes in `acikmedya-webhook/server.js`.

`middleware.ts` (matcher scoped to `/acikmedya/:path*` only) injects an
`x-pathname` request header so `app/layout.tsx` can skip the site
Header/Footer/padding for these routes — this is the only place in the app that
depends on request headers, and it forces the whole site into fully dynamic
(non-cached) rendering as a known, accepted tradeoff (see git history /
implementation plan for the evaluation of this decision). Publishing new issues
goes through `acikmedya-webhook`'s `/hooks/acikmedya/:folder` route — see that
service's README for the publish contract (filename format, allow-list, rate
limits).

