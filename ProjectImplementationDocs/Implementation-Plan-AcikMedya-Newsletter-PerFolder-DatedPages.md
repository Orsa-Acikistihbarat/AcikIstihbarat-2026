# Plan: Per-newsletter dated pages under /acikmedya/<Folder>

**Summary:** Adds multiple independent newsletters under `/acikmedya/<Folder>` (e.g.
`/acikmedya/AcikGazete`, `/acikmedya/AcikKose`), each auto-resolving to today's dated
`index<DDMMYY>.html` static artifact with fallback to the most recent past-dated file if
today's hasn't been published yet. Publishing is done by extending the existing
`acikmedya-webhook` Express service with a new per-folder `POST /hooks/acikmedya/:folder`
route (reusing its existing auth/rate-limit/atomic-write logic), while the Next.js frontend
renders each newsletter inside a shared `AcikMedyaLayout` (own nav/header, no site-wide
Header/Footer) via a minimal `middleware.ts` + root-layout conditional — explicitly NOT via
route-group restructuring, which was proposed then rejected as too invasive. Content is
embedded via a sandboxed, auto-resizing `<iframe srcDoc>`. Includes a shared JSON allow-list
config, Docker Compose wiring, documentation updates, a full local + production verification
checklist, and a two-commit deployment/rollback strategy. Incorporates one approved
architecture-evaluation finding (Suggestion 1: `middleware.ts` matcher narrowed to
`/acikmedya/:path*` instead of a site-wide catch-all).

## Status: DRAFTED - all Further Considerations resolved - awaiting mode switch to implementation-capable agent

## Context / prior state
- Existing feature (shipped, commit 0349cd3, live in prod): `acikmedya-webhook` Express service,
  `POST /hooks/acikmedya` writes a single `index.html` to `acik-istihbarat-public/public/acikmedya/`,
  bind-mounted live into both the webhook and `frontend` containers. `next.config.ts` rewrites
  `/acikmedya` -> `/acikmedya/index.html`. This feature is NOT touched/modified by the plan below.
- Caddyfile already has `handle /hooks/acikmedya* { reverse_proxy acikmedya-webhook:4001 }` - PREFIX
  match, so `/hooks/acikmedya/AcikGazete` already routes correctly, no Caddyfile change needed.
- `acik-istihbarat-public` is Next.js 16.2.12/React 19.2.4 (App Router), Dockerized 3-stage build
  (`deps`->`builder`->`runner`, Node 22-alpine); `public/` is baked into the image at build time,
  but `public/acikmedya` is bind-mounted at compose level (override).
- `acikmedya-webhook` is a small Node/Express service (own `package.json`/`Dockerfile`, separate
  container) - confirm its exact Node version/base image before adding code (check
  `acikmedya-webhook/Dockerfile` `FROM node:X` line) so new syntax (e.g. `Intl.DateTimeFormat`
  options used below) is guaranteed supported.
- Root `docker-compose.yml` already has `acikmedya-webhook` and `frontend` services; both mount
  `./acik-istihbarat-public/public/acikmedya` today (existing single-page feature only).
- Existing dynamic-route pattern: `app/haber/[id]/page.tsx` etc. - async `params` promise,
  `notFound()` from `next/navigation`, NO `generateStaticParams`, NO `dynamic = 'force-dynamic'`
  used anywhere yet (new for this feature - required so content isn't cached/prerendered).
- `components/Header.tsx`/`Footer.tsx` are currently rendered directly inside the ROOT
  `app/layout.tsx` (not a nested layout) - meaning every route today gets the global Header/Footer.
- **User explicitly rejected route-group-based site restructuring** (moving all existing pages into
  an `app/(site)/` group) as too invasive for this feature. The plan below uses a minimal,
  non-invasive alternative instead: conditional rendering inside the EXISTING root `app/layout.tsx`,
  driven by a tiny new `middleware.ts` that stamps the current pathname onto a request header. Zero
  existing page files are moved; zero URLs change; only two files touched.
- **Known recurring bug class in this repo**: bind-mounting a HOST directory over a path a
  Dockerfile `chown`'d only affects the image layer, not the mounted host dir - this exact class of
  bug bit the original webhook feature (`write failed` in prod). Phase 0.3 exists to avoid repeating it.
- No CSP header is currently set anywhere in `next.config.ts` or visible middleware - confirmed no
  existing restriction that would block external image loading inside the iframe; re-verify in 3.4.

## User decisions (confirmed, all Further Considerations resolved)
1. Publishing mechanism: extend the existing webhook (not git-tracked files).
2. Missing today's file: fall back to the most recent past dated file available in that folder.
3. Header content: nav links across newsletters + a placeholder-only "subscribe" input field.
4. Newsletter folder allow-list: shared config file, read by BOTH the webhook and the Next.js app.
5. Embedding: `<iframe srcDoc={html}>`, confirmed fine as long as external images still load.
   Newsletters are Claude-Code-produced static artifacts, guaranteed **no inline `<script>`**.
6. `AcikMedyaLayout` must be **totally separate** from `Header.tsx`/`Footer.tsx`, achieved via a
   pathname-aware conditional in the existing root layout (NOT via moving/restructuring routes -
   that alternative was proposed then explicitly rejected as too invasive).
7. Subscribe input: placeholder only, no backend - real design deferred, out of scope here.
8. Config reload strategy: startup-only read is fine (adding a newsletter = restart both containers).
9. Iframe sizing: no scrollbar ever - auto-resize to content exactly, including after async content
   shifts (external images loading late).

## Design summary
- Shared allow-list config: root file `acikmedya-newsletters.json`, e.g.
  `{ "folders": ["AcikGazete", "AcikKose"] }`. Bind-mounted READ-ONLY into both containers, read
  ONCE at process startup on both sides.
- Data directory: `acik-istihbarat-public/acikmedya-data/<Folder>/index<DDMMYY>.html` - SIBLING of
  `public/`, NOT inside it (never raw-servable, only reachable via the dynamic route). Bind-mounted
  RW into `acikmedya-webhook`, RO into `frontend`.
- Webhook gains `POST /hooks/acikmedya/:folder` (folder allow-listed, filename server-computed,
  same auth/rate-limit/atomic-write logic as the existing route). Existing `POST /hooks/acikmedya`
  untouched.
- Header/Footer opt-out: new `middleware.ts` (matcher scoped to `/acikmedya/:path*` only, per
  approved evaluation Suggestion 1 - avoids running on every site request) stamps `x-pathname`
  request header; root `app/layout.tsx` reads it via `headers()` and conditionally skips
  Header/Footer/`pt-[80px]` for `/acikmedya/*`. Zero existing files moved.
- New route `app/acikmedya/[folder]/page.tsx`, `force-dynamic`, wraps content in
  `<AcikMedyaLayout>`, embeds via `<AcikMedyaIframe>` (auto-resizing).
- iframe `sandbox="allow-same-origin allow-popups allow-popups-to-escape-sandbox"` (no
  `allow-scripts` - unnecessary and would combine dangerously with `allow-same-origin` if content
  ever did contain scripts; confirmed it never will). External image loading is unaffected by any
  sandbox token.

## Steps

### Phase 0 - Shared config & data directory scaffolding *(independent, do first)*
0.1. Create root file `acikmedya-newsletters.json`, exact content:
     `{ "folders": ["AcikGazete", "AcikKose"] }`.
     - Validate it's valid JSON with `python -m json.tool acikmedya-newsletters.json` or
       `node -e "JSON.parse(require('fs').readFileSync('acikmedya-newsletters.json','utf8'))"`
       before moving on - a syntax error here fails BOTH containers' startup in Phase 1/3.
     - Keys are case-sensitive and MUST exactly match the URL segment and directory name (no
       normalization in v1) - document this constraint in 4.1/4.2.
0.2. Create directory scaffolding with `.gitkeep` (empty file) placeholders so the bind-mount
     source paths exist before first webhook write:
     `mkdir -p acik-istihbarat-public/acikmedya-data/AcikGazete acik-istihbarat-public/acikmedya-data/AcikKose`
     then `touch` a `.gitkeep` in each.
0.3. **Permissions (do this on the VPS too, not just locally - see Phase 6.0)**:
     a. `docker exec acik_acikmedya_webhook id webhook` - note the exact `uid=N(webhook) gid=M(webhook)`.
     b. Locally: `chown -R N:M acik-istihbarat-public/acikmedya-data` (Linux/WSL host) matching that
        UID/GID - do NOT rely on the Dockerfile's internal `chown` alone, it does not propagate to a
        bind-mounted host directory (same bug class as the original webhook's `write failed` prod
        incident).
     c. On native Windows dev (no POSIX permission model on the host side) this step is a no-op
        locally - Docker Desktop's bind-mount translation generally allows write regardless; the
        REAL permission risk is exclusively on the Linux VPS in Phase 6.0 - don't skip it there.
     d. Fallback only if precise UID/GID matching isn't practical on the VPS:
        `chmod -R 777 acik-istihbarat-public/acikmedya-data` - flag as a follow-up hardening item if
        used (log it in a code comment or the PR description, not silently accepted long-term).
0.4. Add to root `.gitignore`: `acik-istihbarat-public/acikmedya-data/*/*.html` - verify with
     `git status` afterward that a manually-created test `.html` file inside one of the folders is
     correctly ignored, while the `.gitkeep` files remain tracked (`git add -f` isn't needed since
     `.gitkeep` doesn't match the glob).
0.5. Record the exact `DDMMYY` contract here (shared by Phase 1 writer and Phase 3 reader so they
     never drift): zero-padded, no separators, `DD`=day, `MM`=month, `YY`=2-digit year, e.g.
     6 Sept 2026 -> `index060926.html`. Canonical regex (copy verbatim into BOTH `server.js` and
     `lib/newsletters.ts`, do not paraphrase a slightly different one in either place):
     `^index(\d{2})(\d{2})(\d{2})\.html$`.
0.6. **Definition of done for Phase 0**: `acikmedya-newsletters.json` parses cleanly; both data
     folders exist with `.gitkeep`; `.gitignore` correctly hides future `.html` artifacts but not
     `.gitkeep`; permission plan documented for VPS deploy (6.0).

### Phase 1 - Webhook per-folder publish endpoint *(depends on Phase 0)*
1.1. Add two new env vars read at startup in `acikmedya-webhook/server.js`, both fail-fast
     (`process.exit(1)`, clear stderr message) if unset/unreadable, mirroring the existing
     `WEBHOOK_TOKEN` fail-fast pattern (find that exact code block first and copy its style):
     - `NEWSLETTERS_CONFIG_PATH` (e.g. `/data/acikmedya-newsletters.json`)
     - `NEWSLETTERS_DATA_DIR` (e.g. `/data/acikmedya-newsletters-data`)
1.2. At startup, in this order:
     a. `const raw = fs.readFileSync(NEWSLETTERS_CONFIG_PATH, 'utf8');` - wrap in try/catch,
        `process.exit(1)` with message `FATAL: cannot read NEWSLETTERS_CONFIG_PATH at <path>: <err>`
        on failure (file missing/unreadable).
     b. `const parsed = JSON.parse(raw);` - separate try/catch, exit message
        `FATAL: NEWSLETTERS_CONFIG_PATH is not valid JSON: <err>`.
     c. Shape validation: `Array.isArray(parsed.folders) && parsed.folders.length > 0 && parsed.folders.every(f => typeof f === 'string' && f.length > 0)`
        - exit message `FATAL: acikmedya-newsletters.json must be { "folders": string[] } (non-empty)`
        if this fails. Do not skip this - a malformed-but-valid-JSON config (e.g. `{}` or
        `{"folders": []}`) must not silently start with zero allowed folders.
     d. `const ALLOWED_FOLDERS = new Set(parsed.folders);` - log the final folder count/list once at
        startup (`console.log(JSON.stringify({ event: 'startup', allowedFolders: [...ALLOWED_FOLDERS] }))`)
        so a `docker compose logs` inspection instantly shows what's configured.
1.3. **Security: validate `:folder` BEFORE using it in any filesystem path.** Two independent
     layers, both required:
     (a) `ALLOWED_FOLDERS.has(folder)` - allow-list membership.
     (b) `/^[A-Za-z0-9_-]+$/.test(folder)` - defense-in-depth regex rejecting `/`, `..`, null bytes,
     URL-encoded traversal sequences, etc., even though (a) alone should already exclude those
     (OWASP: never rely on a single validation layer for path construction from user input). Order:
     check (b) FIRST actually (cheaper, catches junk before a `Set.has` lookup) then (a) - either
     order is functionally fine, just be consistent and return the RIGHT status code per branch
     (see 1.4c).
1.4. New route `POST /hooks/acikmedya/:folder`, exact middleware/handler order:
     a. Auth: reuse the existing `Authorization: Bearer` extraction + `crypto.timingSafeEqual`
        comparison helper verbatim (do not re-implement) -> `401` on failure, identical JSON error
        shape to the existing route (check that shape first, match it exactly).
     b. Rate limit: reuse the existing sliding-window limiter function, keyed identically to today
        (same IP/global key scheme - no new per-folder limiting in v1) -> `429` with the existing
        response body shape.
     c. Folder validation (1.3): regex miss -> `400 { error: "invalid folder name" }`; allow-list
        miss -> `404 { error: "unknown newsletter folder" }`. These MUST be distinguishable in logs
        for ops debugging - use different `event` values in the structured log line for each.
     d. `Content-Type: text/html` check -> `400` (reuse existing check function, unchanged).
     e. Body size: rely on the existing `express.text({ limit: '256kb' })` middleware already
        applied globally/at the router level -> triggers the existing `entity.too.large` error
        handler -> `413` (no new code needed here, just confirm it's wired to this route too).
     f. HTML sanity check (`<!doctype html` / `<html` case-insensitive prefix match via the existing
        helper function) -> `400` on failure.
     g. Compute today's date, server-side, MUST be timezone-correct regardless of container TZ:
        ```
        const parts = new Intl.DateTimeFormat('en-GB', {
          timeZone: 'Europe/Istanbul', day: '2-digit', month: '2-digit', year: '2-digit'
        }).formatToParts(new Date());
        const dd = parts.find(p => p.type === 'day').value;
        const mm = parts.find(p => p.type === 'month').value;
        const yy = parts.find(p => p.type === 'year').value;
        const filename = `index${dd}${mm}${yy}.html`;
        ```
        (using `formatToParts` instead of `.format()` + string-splitting avoids locale-separator
        assumptions - more robust than parsing `"06/09/26"` string output). Do NOT use
        `new Date().getDate()` etc. directly - those reflect the container's system TZ, not
        Europe/Istanbul.
     h. `const folderDir = path.join(NEWSLETTERS_DATA_DIR, folder); fs.mkdirSync(folderDir, { recursive: true });`
        then perform the SAME atomic tmp-file-then-rename write as the existing route, targeting
        `path.join(folderDir, filename)` - extract the existing route's write logic into a shared
        function `atomicWriteHtml(targetPath, content)` if it isn't already a standalone function,
        rather than copy-pasting the fs calls a second time.
     i. Success response: `200 { status: "ok", folder, filename, bytes: Buffer.byteLength(content) }`.
     j. Structured JSON log line on EVERY branch (success + every error), same field set as the
        existing route's logs (`timestamp`, `route`, `statusCode`, plus `folder` for this route) -
        never log the request body or the token value, matching the existing route's discipline.
1.5. Existing `POST /hooks/acikmedya` route: zero changes. Verify with
     `git diff acikmedya-webhook/server.js` before committing that this route's original lines are
     untouched (only additions elsewhere in the file).
1.6. Confirm the existing `entity.too.large` error-handling middleware (Express error middleware,
     registered once, applies to all routes by definition) still fires for the NEW route too - add
     an explicit oversized-body test case for `/hooks/acikmedya/:folder` in Phase 5.3, don't just
     assume it "should" work because it's route-agnostic.
1.7. **Definition of done for Phase 1**: `node -c server.js` (or equivalent) has zero syntax errors;
     manual `curl` smoke test of the new route returns `200` locally with a raw `docker run` (before
     even touching compose) if fast iteration is wanted; existing route's behavior is provably
     unchanged (git diff review).

### Phase 2 - Docker Compose wiring *(depends on Phase 0-1)*
2.1. `acikmedya-webhook` service - add to `volumes:`:
     ```
     - ./acikmedya-newsletters.json:/data/acikmedya-newsletters.json:ro
     - ./acik-istihbarat-public/acikmedya-data:/data/acikmedya-newsletters-data
     ```
     (second line has NO `:ro` suffix - omitting it defaults to read-write; double check this isn't
     accidentally typed as `:ro` since that would make every publish attempt fail with EROFS).
2.2. Same service - add to `environment:`:
     `NEWSLETTERS_CONFIG_PATH=/data/acikmedya-newsletters.json`,
     `NEWSLETTERS_DATA_DIR=/data/acikmedya-newsletters-data`. Existing `WEBHOOK_TOKEN`/`TARGET_DIR`
     and the legacy `public/acikmedya` mount: unchanged, same lines, same order (minimize diff
     noise for reviewability).
2.3. `frontend` service - add to `volumes:`:
     ```
     - ./acikmedya-newsletters.json:/app/acikmedya-newsletters.json:ro
     - ./acik-istihbarat-public/acikmedya-data:/app/acikmedya-data:ro
     ```
     (BOTH lines `:ro` here - frontend must never write to either path; this is the inverse mistake
     risk from 2.1, double check).
2.4. Same service - add to `environment:`:
     `NEWSLETTERS_CONFIG_PATH=/app/acikmedya-newsletters.json`, `NEWSLETTERS_DATA_DIR=/app/acikmedya-data`.
2.5. No port/network changes - both services already share `acik-network`; frontend reads the
     filesystem directly and does NOT call the webhook's HTTP API, so no new inter-service
     dependency/`depends_on` edge is needed either.
2.6. No Caddyfile change (prefix match already covers the new sub-route). No
     `.github/workflows/deploy.yml` change (already builds/deploys both services; new bind-mount
     SOURCE paths just need to exist on the VPS checkout via `git pull`, handled in Phase 6).
2.7. Immediately after editing, run `docker compose config` (validates YAML + var interpolation,
     starts nothing) - fix any indentation/syntax errors here before attempting Phase 5's builds.
2.8. **Definition of done for Phase 2**: `docker compose config` exits 0 and the rendered config
     shows all 4 new volume lines and 4 new env vars in the expected services, with correct
     ro/rw suffixes exactly as specified in 2.1/2.3.

### Phase 3 - Header/Footer opt-out + Next.js dynamic route + layout *(depends on Phase 0; 3.0 is a small isolated prerequisite - commit/verify it alone before 3.1-3.5)*
3.0. **Minimal Header/Footer opt-out** (replaces the previously-proposed, user-rejected route-group
     restructuring):
     a. Create `acik-istihbarat-public/middleware.ts`:
        ```
        import { NextResponse } from 'next/server';
        import type { NextRequest } from 'next/server';

        export function middleware(request: NextRequest) {
          const requestHeaders = new Headers(request.headers);
          requestHeaders.set('x-pathname', request.nextUrl.pathname);
          return NextResponse.next({ request: { headers: requestHeaders } });
        }

        export const config = {
          matcher: ['/acikmedya/:path*'],
        };
        ```
        (Scoped to `/acikmedya/*` only, per approved evaluation Suggestion 1 - the `x-pathname`
        header is only ever consumed by the root layout's newsletter-route check, so there's no
        need to run this middleware on every request across the whole site.)
     b. Edit root `app/layout.tsx`:
        - Add `import { headers } from 'next/headers';` at the top.
        - Inside `RootLayout` (already `async` or make it `async` if not): add
          `const hdrs = await headers(); const isNewsletterRoute = (hdrs.get('x-pathname') ?? '').startsWith('/acikmedya');`
        - Replace the existing unconditional `<Header/><main .../>{children}</main><Footer/>` block
          with:
          ```
          {isNewsletterRoute ? (
            <main className="flex-grow">{children}</main>
          ) : (
            <>
              <Header />
              <main className="flex-grow pt-[80px]">{children}</main>
              <Footer />
            </>
          )}
          ```
     c. **Zero existing page files moved or renamed** - `app/page.tsx`, `app/haber/[id]/page.tsx`,
        etc. stay exactly where they are, same imports, same git history/blame.
     d. **Trade-off to explicitly flag to the user during implementation, not silently accept**:
        calling `headers()` in the root layout forces the ENTIRE app into dynamic rendering (no
        static prerendering/ISR for the shell), since the root layout wraps every route. Run
        `npm run build` before and after this change and diff the route-type summary Next.js prints
        (look for `○ (Static)` vs `λ (Dynamic)` markers per route) to make the impact concrete and
        visible, not theoretical - present that diff to the user rather than asserting the trade-off
        abstractly.
     e. Commit this as its own isolated change; build + smoke-test a couple of existing routes
        BEFORE writing any new newsletter code (see 5.10) - any regression is then trivially
        bisectable to exactly these 2 files.
3.1. Create `acik-istihbarat-public/lib/newsletters.ts` with these exact exported signatures:
     - `export function getNewsletterFolders(): string[]`
       - Reads `process.env.NEWSLETTERS_CONFIG_PATH` (throw a descriptive `Error` if the env var
         itself is unset - fail loudly, this is a server misconfiguration, not a user-facing 404).
       - `fs.readFileSync(configPath, 'utf8')` -> `JSON.parse` -> validate `{ folders: string[] }`
         shape (same validation logic as webhook's 1.2c - keep both checks conceptually identical
         even though they're two separate codebases/languages).
       - Memoize in a module-level `let cachedFolders: string[] | null = null;` variable, populated
         on first call - Next.js server processes are long-lived, safe to cache; a config change
         requires a container restart (matches decision #8, be consistent).
     - `export function isValidNewsletterFolder(folder: string): boolean`
       - `return /^[A-Za-z0-9_-]+$/.test(folder) && getNewsletterFolders().includes(folder);` - copy
         the LITERAL regex from 1.3(b)/0.5, do not redefine a slightly different character class.
     - `export function resolveLatestNewsletterFile(folder: string): { filename: string; html: string } | null`
       - `let entries: string[]; try { entries = fs.readdirSync(path.join(dataDir, folder)); } catch (e) { if ((e as NodeJS.ErrnoException).code === 'ENOENT') return null; throw e; }`
         (missing directory -> `null` -> renders as "not yet published"; any OTHER fs error should
         still surface/throw, don't silently swallow permission errors etc.).
       - Filter `entries` with the canonical regex `^index(\d{2})(\d{2})(\d{2})\.html$` (ignores
         `.gitkeep` and anything else automatically, since it won't match the pattern - no special
         casing needed for `.gitkeep`).
       - Parse each match into a real `Date` (careful with 2-digit year -> full year assumption,
         e.g. `20XX`, document the exact century assumption in a code comment since this app will
         presumably run past 2099... unlikely to matter, but note it as a known limitation, not a
         silent bug).
       - Pick the entry whose date equals today (Europe/Istanbul, same TZ contract as the webhook)
         if present; else the maximum date `<= today`; else `return null`.
       - Read+return `{ filename, html: fs.readFileSync(path.join(dataDir, folder, filename), 'utf8') }`
         - NOT cached, must reflect the latest publish on every request (this function runs inside
           a `force-dynamic` route so that's already guaranteed at the route level too - belt and
           suspenders).
3.2. Create `acik-istihbarat-public/components/AcikMedyaLayout.tsx`:
     - `async function AcikMedyaLayout({ children }: { children: React.ReactNode })` - server
       component, calls `getNewsletterFolders()` directly (no prop drilling).
     - Own header markup, own top padding (no global fixed header exists above this route after
       3.0 - pick an explicit top padding value, e.g. `pt-8`, and note it's independently chosen,
       not inherited from the site's `pt-[80px]` convention).
     - Nav: `{getNewsletterFolders().map(f => <Link key={f} href={`/acikmedya/${f}`}>{f}</Link>)}`.
     - Subscribe field: visibly non-functional placeholder - e.g. disabled `<input disabled placeholder="Yakında..." />`
       + a disabled button, OR an enabled-looking input whose `<form onSubmit={(e) => e.preventDefault()}>`
       just no-ops - pick ONE approach consistently, don't mix a disabled look with a working-looking
       submit handler (confusing UX). Recommend: visually enabled but functionally inert (matches
       "will be designed later" framing better than a disabled-looking dead end).
3.3. Create `acik-istihbarat-public/components/AcikMedyaIframe.tsx` (`"use client"` - REQUIRED,
     this file uses `useState`/`useRef`/`useEffect`, will fail to compile/behave correctly as a
     server component):
     - `interface AcikMedyaIframeProps { html: string; title: string }`
     - `const [height, setHeight] = useState<number>(0);` `const iframeRef = useRef<HTMLIFrameElement>(null);`
     - `<iframe ref={iframeRef} srcDoc={html} title={title} onLoad={handleLoad} sandbox="allow-same-origin allow-popups allow-popups-to-escape-sandbox" style={{ width: '100%', border: 'none', display: 'block', height: height || 'auto', overflow: 'hidden' }} />`
       wrapped in a container `<div style={{ width: '100%', overflowX: 'hidden' }}>`.
     - `handleLoad`:
       ```
       function handleLoad() {
         const doc = iframeRef.current?.contentWindow?.document;
         if (!doc) return; // e.g. allow-same-origin somehow blocked - fail safe to height:auto
         const measure = () => setHeight(doc.documentElement.scrollHeight || doc.body.scrollHeight);
         measure();
         const ro = new ResizeObserver(measure);
         ro.observe(doc.body);
         // stash observer on the element/ref for cleanup, or lift into a useEffect keyed on `html`
       }
       ```
       Prefer restructuring this as a `useEffect` keyed on the iframe's `load` event via
       `addEventListener('load', ...)` + returning a cleanup that calls `ro.disconnect()`, rather
       than relying solely on the JSX `onLoad` prop, so the `ResizeObserver` is reliably torn down
       on unmount/re-navigation (avoids leaking observers across client-side route changes).
     - Explicitly test in a real browser (Phase 5.11) that `contentWindow.document` is actually
       accessible given the `sandbox` value chosen - if it ever returns `null`/throws (e.g. a future
       browser change or an unexpected cross-origin quirk with `srcDoc` + sandbox), the fallback
       must be a fixed reasonable height with internal scroll, NOT a crash/blank page - implement
       this fallback explicitly, don't assume the happy path always holds.
3.4. Security/header re-verification pass (quick, ~5 min): grep `next.config.ts` `headers()`,
     `Caddyfile`, and the new `middleware.ts` for any `Content-Security-Policy`, `X-Frame-Options`,
     or `frame-ancestors` directives - expected: none found (confirmed during planning). Also
     re-read `middleware.ts`'s `matcher` (now scoped to `/acikmedya/:path*` per approved evaluation
     Suggestion 1) once more to confirm it doesn't accidentally match/rewrite anything under
     `/hooks/acikmedya*` (moot in practice - different container/port - but cheap to double-check).
3.5. Create `acik-istihbarat-public/app/acikmedya/[folder]/page.tsx`:
     - `export const dynamic = 'force-dynamic';` (module scope, top of file).
     - `export async function generateMetadata({ params }: { params: Promise<{ folder: string }> }): Promise<Metadata>`
       returning `{ title: `${folder} - Açık İstihbarat` }` (confirm this matches how other pages in
       `app/` export metadata before copying the exact pattern/import).
     - `export default async function NewsletterFolderPage({ params }: { params: Promise<{ folder: string }> })`:
       ```
       const { folder } = await params;
       if (!isValidNewsletterFolder(folder)) notFound();
       const resolved = resolveLatestNewsletterFile(folder);
       return (
         <AcikMedyaLayout>
           {resolved
             ? <AcikMedyaIframe html={resolved.html} title={folder} />
             : <p className="text-center py-20">Bu bültenin henüz bir yayını yok.</p>}
         </AcikMedyaLayout>
       );
       ```
3.6. **Definition of done for Phase 3**: `npm run build` succeeds with zero type errors;
     `npm run lint` (if configured) passes on all new/changed files; the pre/post build route-type
     diff from 3.0d has been shown to the user; a manual local click-through of both an existing
     route and the new `/acikmedya/<folder>` route confirms the Header/Footer opt-out works in both
     directions.

### Phase 4 - Documentation *(depends on Phase 1-3; can run in parallel with Phase 5)*
4.1. Update `acikmedya-webhook/README.md`: new route + full response matrix
     (200/400-invalid-name/401/404-unknown-folder/413/429), config file format/location, explicit
     "filename/date is server-computed, not client-controllable" statement.
4.2. Update `acik-istihbarat-public/AGENTS.md`: new publish flow next to the existing one, literal
     curl example:
     `curl -X POST https://www.acikistihbarat.com/hooks/acikmedya/AcikGazete -H "Authorization: Bearer $TOKEN" -H "Content-Type: text/html" --data-binary @today.html`
     - case-sensitivity note on folder names; script-free/self-contained/external-images-ok artifact
       constraint (the exact contract Claude Code should follow generating each newsletter); mention
       the `middleware.ts` + root-layout conditional as the Header/Footer opt-out mechanism.
4.3. Add a short section to root `README.md` ONLY if it already documents the original webhook
     feature (check first; skip if it doesn't - don't introduce new top-level docs unprompted).
4.4. **Definition of done for Phase 4**: a developer unfamiliar with this session could read
     `acikmedya-webhook/README.md` + `AGENTS.md` alone and successfully publish a new dated
     newsletter page without needing to ask any clarifying questions.

### Phase 5 - Local verification *(depends on Phase 1-3; run 5.10 immediately after Phase 3.0)*
5.1. `docker compose build acikmedya-webhook frontend` (scoped, not full-stack `--build`), then
     `docker compose up -d --no-deps acikmedya-webhook frontend`.
5.2. `docker compose logs acikmedya-webhook --tail 30` (confirm the 1.2d startup log line shows the
     expected folder list, no fail-fast exit) and `docker compose ps` (both `Up`, not restarting).
5.3. Curl matrix against `POST http://localhost:<webhook-port>/hooks/acikmedya/AcikGazete`:
     | Case | Expected |
     |---|---|
     | valid token + valid folder + valid HTML | `200`, verify JSON fields |
     | unknown folder (`NotARealFolder`) | `404` |
     | folder with traversal chars (`..`, `%2f`) | `400`, AND confirm no file/dir created anywhere unexpected |
     | bad/missing token | `401` |
     | wrong `Content-Type` | `400` |
     | body missing `<!doctype html>` | `400` |
     | body > 256kb | `413` |
     | > 10 requests in 60s | `429` on the excess |
5.4. `docker exec acik_acikmedya_webhook ls -la /data/acikmedya-newsletters-data/AcikGazete/` +
     `cat` the written file - confirm filename = today's `DDMMYY` and content matches what was POSTed.
5.5. Browser: `http://localhost:14000/acikmedya/AcikGazete` - confirm `AcikMedyaLayout`
     header/nav/subscribe placeholder render, NO global Header/Footer, iframe shows content with
     NO scrollbar and correct height, dark mode (if applicable) doesn't break it.
5.6. Fallback test: `docker exec` in, `mv`/`rm` today's file, refresh (no rebuild needed,
     `force-dynamic`) - confirm previous day's file renders; restore/re-publish afterward.
5.7. Empty-folder test: a configured-but-never-published folder renders the placeholder, no 500 in
     `docker compose logs frontend`.
5.8. Unknown-folder test at the Next.js layer: `http://localhost:14000/acikmedya/NotARealFolder` ->
     real Next.js 404 via `notFound()`, not a crash.
5.9. Concurrency spot-check (optional, cheap): two rapid sequential publishes to the SAME folder
     within one second - confirm atomic write means the second fully replaces the first, no
     corruption/partial file.
5.10. **Header/Footer opt-out regression check (run immediately after Phase 3.0, before any new
      newsletter code)**: `/`, `/haber/<id>` still show global Header/Footer unchanged;
      `/acikmedya/<anything>` (even a 404 at this point) genuinely shows NEITHER - proves the
      pathname conditional works both directions before layering new code on top. Also capture the
      `npm run build` route-type table (Static vs Dynamic per route) referenced in 3.0d.
5.11. Iframe auto-resize test: publish an artifact containing at least one external `https://` image
      URL - confirm (a) it loads (no CSP/sandbox block) and (b) the `ResizeObserver` correctly grows
      the iframe after the image finishes loading (use devtools network throttling to make the
      timing gap observable). Also deliberately test the `contentWindow.document` null-fallback
      path from 3.3 if feasible (e.g. temporarily strip `allow-same-origin` from the sandbox value
      in a throwaway local build to confirm the fallback doesn't crash).

### Phase 6 - Deployment *(depends on Phase 1-5; requires explicit user go-ahead before push)*
6.0. **Before pushing**, remind the user: after `git pull` on the VPS, the new bind-mount source
     paths will exist (via `.gitkeep`) but with whatever default ownership `git pull` gives them -
     re-run 0.3's permission fix ON THE VPS (`docker exec acik_acikmedya_webhook id webhook` then
     `chown -R <uid>:<gid> acik-istihbarat-public/acikmedya-data` on the VPS host) proactively, as
     part of the deploy checklist, not reactively after a `write failed` incident repeats.
6.1. Review full `git diff` locally. Recommended two-commit split (bisectability):
     - Commit A: `middleware.ts` + `app/layout.tsx` conditional only (zero behavior change for
       existing routes) - `git add acik-istihbarat-public/middleware.ts acik-istihbarat-public/app/layout.tsx && git commit -m "..."`.
     - Commit B: everything else (webhook route, config, data dirs, new components/route, compose,
       docs) - `git add <remaining files individually>` (avoid `git add -A`, which could sweep up
       unrelated local changes) `&& git commit -m "..."`.
6.2. **STOP - do NOT push.** Show `git log --oneline -2` and `git show --stat HEAD~1 HEAD` (or
     equivalent) to the user, wait for explicit approval.
6.3. Once approved: `git push`; poll `gh run list --workflow=deploy.yml --limit 1` until the run
     completes; on failure, `gh run view --log-failed` before making further changes.
6.4. Post-deploy SSH guidance for the user (agent has no direct VPS access): confirm
     `acikmedya-newsletters.json` and `acikmedya-data/*/` exist at `/docker/acikistihbarat-2026/`
     post-pull; apply 6.0's permission fix proactively before the first real publish attempt, not
     only if it fails.
6.5. Production verification: repeat 5.3's curl matrix against
     `https://www.acikistihbarat.com/hooks/acikmedya/<folder>`, then load
     `https://www.acikistihbarat.com/acikmedya/<folder>` in a browser, AND re-verify a couple of
     pre-existing routes still render Header/Footer correctly. Do NOT declare this feature "done"
     until file-write, page-render, iframe auto-sizing, AND zero regression on existing routes are
     ALL confirmed in production (the original feature's rollout left webhook-to-page confirmation
     unresolved last time - don't repeat that gap).
6.6. **Rollback plan if production verification fails**: `git revert` the relevant commit(s) (or
     `git reset --hard` to the pre-deploy SHA if revert is impractical, ONLY with explicit user
     approval per the standing destructive-action rule), `git push`, then re-run the deploy workflow
     - identify which of the two commits (A: layout/middleware, B: feature) is implicated first via
     the regression symptom (Header/Footer issue -> commit A; webhook/newsletter issue -> commit B)
     to decide whether a full revert or a more targeted fix is warranted.
6.7. Only after production confirmation: final check that `acik-istihbarat-public/AGENTS.md` (4.2)
     was actually updated and pushed - not a new step, just a closing checklist item.

## Relevant files
- `acikmedya-newsletters.json` - NEW (shared allow-list config, repo root)
- `acik-istihbarat-public/acikmedya-data/AcikGazete/.gitkeep`,
  `acik-istihbarat-public/acikmedya-data/AcikKose/.gitkeep` - NEW
- `acikmedya-webhook/server.js` - MODIFY (new `POST /hooks/acikmedya/:folder`, startup config load,
  shared `atomicWriteHtml` helper extraction if not already reusable)
- `acikmedya-webhook/README.md` - MODIFY
- `acik-istihbarat-public/middleware.ts` - NEW
- `acik-istihbarat-public/app/layout.tsx` - MODIFY (conditional only - see 3.0b) - no other existing
  file touched or moved
- `acik-istihbarat-public/lib/newsletters.ts` - NEW (`getNewsletterFolders`,
  `isValidNewsletterFolder`, `resolveLatestNewsletterFile`)
- `acik-istihbarat-public/components/AcikMedyaLayout.tsx` - NEW
- `acik-istihbarat-public/components/AcikMedyaIframe.tsx` - NEW (`"use client"`)
- `acik-istihbarat-public/app/acikmedya/[folder]/page.tsx` - NEW
- `acik-istihbarat-public/AGENTS.md` - MODIFY
- `docker-compose.yml` - MODIFY (`acikmedya-webhook` + `frontend`: new volumes/env vars)
- root `.gitignore` - MODIFY
- No changes: `Caddyfile`, `.github/workflows/deploy.yml`, existing `POST /hooks/acikmedya` route,
  `components/Header.tsx`/`Footer.tsx`, every existing page under `app/`

## Verification (see Phase 5-6 for full detail)
- Local: `docker compose config` validation, startup log check, full curl response-code matrix
  (200/400x2/401/404/413/429), filesystem inspection via `docker exec`, browser check (no-scrollbar
  auto-sized iframe + external image load + confirmed absence of global Header/Footer), fallback
  test, empty-folder test, unknown-folder 404 test, double-publish race check, pre/post `npm run
  build` route-type diff, Header/Footer regression check.
- Production: same curl matrix + browser check + existing-route regression check against the live
  domain; explicit go/no-go before declaring done.

## Decisions (confirmed with user)
- Extend existing webhook rather than git-tracked files.
- Fallback to most recent past file when today's is missing.
- Header includes nav across newsletters + a placeholder-only subscribe input.
- Allow-list lives in one shared JSON config file, read by both webhook and frontend, startup-only.
- `iframe srcDoc` embedding with `sandbox="allow-same-origin allow-popups allow-popups-to-escape-sandbox"`
  (no `allow-scripts`).
- `AcikMedyaLayout` fully independent from `Header.tsx`/`Footer.tsx` via minimal `middleware.ts` +
  root-layout conditional - explicitly NOT via route-group restructuring (rejected by user).
- Iframe auto-resizes via `onLoad` + `ResizeObserver`, with an explicit fallback path if
  `contentWindow.document` access ever fails.
- Standing rule: agent must NOT push to GitHub without explicit user confirmation.
- Evaluation Suggestion 1 (Performance, approved): `middleware.ts` matcher narrowed to
  `['/acikmedya/:path*']` instead of a site-wide catch-all, since the `x-pathname` header is only
  consumed for that route prefix.

## Further considerations
1. Site-wide dynamic-rendering trade-off from using `headers()` in the root layout (loses static
   prerendering/ISR app-wide) - made CONCRETE via the required pre/post `npm run build` route-type
   diff in 3.0d/5.10, rather than left as an abstract caveat - still a conscious accept-or-revisit
   decision point, not fully "resolved" until that diff is actually shown to and accepted by the user.
2. Exact placeholder copy/styling for the subscribe input, and `AcikMedyaLayout`'s exact top-spacing
   value - implementation-time judgment calls, not blocking.
3. 2-digit year (`YY`) century assumption in filename parsing (`resolveLatestNewsletterFile`) is a
   known, documented limitation (assumes 20XX) - not a near-term concern, flagged for completeness.
