<!-- BEGIN:nextjs-agent-rules -->
# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` before writing any code. Heed deprecation notices.
<!-- END:nextjs-agent-rules -->

## Static /acikmedya artifact
Push self-contained HTML to `public/acikmedya/index.html` (see its own README.md
for rules). It is served raw at `/acikmedya` via a `next.config.ts` rewrite - it
does NOT go through App Router/layout. A normal push to `main` auto-deploys via
`.github/workflows/deploy.yml` - no extra steps required.

