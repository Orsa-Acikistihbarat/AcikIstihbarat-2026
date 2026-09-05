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

