# Plan: Fix TOC scrolling in AcikMedyaIframe (add allow-scripts)

## Context / findings
- Single relevant component: `acik-istihbarat-public/components/AcikMedyaIframe.tsx`.
  Renders externally-produced newsletter HTML (read from disk via
  `lib/newsletters.ts` -> `resolveLatestNewsletterFile`, files live in
  `acik-istihbarat-public/acikmedya-data/<Folder>/*.html`) into a `sandbox`ed
  `srcDoc` iframe. TOC markup/anchors/click-handler scripts live INSIDE that
  external HTML, not in this repo — this repo only controls the iframe wrapper.
- Current sandbox value: `allow-same-origin allow-popups allow-popups-to-escape-sandbox`
  — missing `allow-scripts`, so any script in the newsletter HTML (e.g. a
  scrollIntoView click-handler) is inert.
- Existing height-measurement logic (already present, contrary to initial
  assumption that "no resize logic" exists): on iframe `load` (or immediately
  if `contentDocument.readyState === 'complete'` already), reads
  `contentWindow.document.documentElement.scrollHeight` and sets it as iframe
  height; also attaches a `ResizeObserver` on `doc.body` for further growth.
  Falls back to `80vh` + internal scroll (`resizeFailed`) only if
  `contentWindow.document` is inaccessible (e.g. real cross-origin issue).
- Local sample HTML file (`AcikGazete/index070926.html`) is just a placeholder
  smoke-test stub with no TOC/script — cannot be used alone to reproduce/verify
  the real-world clipped-box bug; need a realistic fixture with headings +
  in-page anchor links (and optionally a script-based click handler) for
  verification.
- Security note (discussed with user): `allow-scripts` + `allow-same-origin`
  together is the classically-flagged risky sandbox combo (script can escape
  sandbox restrictions for same-origin access) — acceptable here ONLY because
  `html` is first-party, pipeline-generated content read from local disk, never
  user-submitted/untrusted input. Confirmed via `lib/newsletters.ts` (fs.readFileSync
  from a config-controlled folder, no user input path).
- User decision: proceed with adding `allow-scripts` (original suggestion),
  not the postMessage/no-allow-same-origin alternative.

## Evaluation findings (approved, integrated below)
1. **[Safety / High / DEFERRED — TODO, not part of this change]** The
   "first-party trust" justification above only verifies the *read* path
   (`lib/newsletters.ts`). Traced the *write* path: `acikmedya-webhook/server.js`
   accepts a bearer-token-authenticated POST body and writes it verbatim to
   disk (`fs.writeFileSync`) with **no HTML sanitization**. If the webhook
   token or upstream generator were ever compromised, `allow-scripts` +
   `allow-same-origin` would let that content execute with same-origin
   privileges. **Action deferred**: not addressed by this plan; flagged as a
   follow-up to verify webhook token strength/rotation and/or add HTML
   sanitization at ingestion or read time, independent of this sandbox change.
2. **[Functionality / Medium — integrated into Phase 2 fixture below]** The
   original test fixture only exercised `onclick`-driven `scrollIntoView`
   links, never a plain no-JS `<a href="#id">` anchor. Since the original bug
   diagnosis identified two independent failure modes (blocked scripts, and a
   too-short clipped iframe box), testing only the scripted path can't prove
   the sizing bug is genuinely fixed vs. masked by JS-driven scrolling. Fixed
   by adding a third, non-scripted anchor link to the fixture (see Phase 2).

## Steps

**Phase 1 — Sandbox permission fix** (no dependencies)
1. Open `acik-istihbarat-public/components/AcikMedyaIframe.tsx`, locate the
   `<iframe>` JSX block (currently lines ~55-65, the one with `ref={iframeRef}`,
   `srcDoc={html}`).
2. Change the exact attribute value:
   - Old: `sandbox="allow-same-origin allow-popups allow-popups-to-escape-sandbox"`
   - New: `sandbox="allow-same-origin allow-scripts allow-popups allow-popups-to-escape-sandbox"`
   (insert `allow-scripts` as the 2nd token, right after `allow-same-origin`,
   for readability/consistency — order doesn't affect behavior).
3. Immediately above that line, add a single-line comment, e.g.:
   `// allow-scripts is safe here: html is first-party pipeline output (see lib/newsletters.ts), never user input`
4. Save. Run `get_errors` on this file to confirm no new TS/JSX diagnostics
   were introduced by the edit (should be none — it's a string literal change).

**Phase 2 — Build a realistic test fixture** (*depends on Phase 1*)
1. Do NOT edit the committed `acik-istihbarat-public/acikmedya-data/AcikGazete/index070926.html`
   in place long-term — instead temporarily overwrite its content for local
   testing only, and restore/revert it afterward (or use `git stash`/`git diff`
   to confirm no unintended diff remains before finishing).
2. Replace its content with a minimal but representative fixture that
   reproduces both original symptoms (short content that still needs proper
   sizing, headings for TOC anchors, a `<script>`-driven click handler to
   confirm `allow-scripts` actually works, AND a plain non-scripted anchor to
   isolate the sizing fix from JS-driven scrolling — per evaluation finding #2):
   ```html
   <!DOCTYPE html>
   <html><body style="font-family:sans-serif;padding:1rem;">
     <h1>Test Newsletter</h1>
     <nav id="toc">
       <ul>
         <li><a href="#section-1" onclick="document.getElementById('section-1').scrollIntoView({behavior:'smooth'});return false;">Section 1 (scripted)</a></li>
         <li><a href="#section-2" onclick="document.getElementById('section-2').scrollIntoView({behavior:'smooth'});return false;">Section 2 (scripted)</a></li>
         <li><a href="#section-2">Section 2 (plain anchor, no JS)</a></li>
       </ul>
     </nav>
     <h2 id="section-1">Section 1</h2>
     <p>Repeat filler text to force page height beyond one viewport...</p>
     <div style="height:900px;background:linear-gradient(#eee,#ccc);"></div>
     <h2 id="section-2">Section 2</h2>
     <p>Target content for section 2 — if visible after scroll, TOC works end-to-end.</p>
   </body></html>
   ```
3. Confirm required env vars are set for `npm run dev` in `acik-istihbarat-public`
   (per repo memory): `NEXT_PUBLIC_API_URL`, `NEWSLETTERS_CONFIG_PATH` (point at
   repo-root `acikmedya-newsletters.json`), `NEWSLETTERS_DATA_DIR` (point at
   `acik-istihbarat-public/acikmedya-data`). Without these the route 500s.
4. Start the dev server (`npm run dev` in `acik-istihbarat-public`); note the
   actual printed port (commonly NOT 3000 locally — check the "Local:" line).

**Phase 3 — Manual verification** (*depends on Phase 2*)
1. Open `http://localhost:<port>/acikmedya/AcikGazete` in a browser.
2. Check the iframe box visually: it must render tall enough to show both
   "Section 1" and "Section 2" headings without an internal scrollbar clipping
   content to a small box (confirms height-measurement logic already works
   once scripts aren't blocked from causing side effects — no JS is actually
   required for the *sizing* logic itself, since that runs in the parent frame
   via `contentWindow.document`, but confirm it's not incidentally broken).
3. Click the "Section 1 (scripted)" and "Section 2 (scripted)" TOC links: page
   should smoothly scroll within the iframe's own rendered content to reveal
   the matching `<h2>` heading.
4. Click the "Section 2 (plain anchor, no JS)" link separately: it must also
   scroll to reveal the heading, confirming the sizing fix works independently
   of any script execution (evaluation finding #2).
5. If step 2, 3, or 4 fails (iframe still clipped, or a click does nothing):
   - Open browser DevTools console for the iframe context (right-click inside
     iframe → "Inspect"), check for any CSP/sandbox-related console errors.
   - If height is still wrong: in `AcikMedyaIframe.tsx`'s `handleLoad()`, add a
     second delayed measurement to catch late reflow, e.g. wrap the existing
     `measure()` call with an additional
     `requestAnimationFrame(() => requestAnimationFrame(measure))` right after
     the initial `measure()` call, before attaching the `ResizeObserver`.
   - If a scripted click handler still doesn't fire: confirm the `sandbox`
     string was saved correctly (re-check exact spelling: `allow-scripts`, not
     `allow-script`) and hard-refresh (dev server HMR can sometimes serve a
     stale `srcDoc` string).
6. Only apply the Step 5 code change if Step 2/3/4 genuinely fail after a hard
   refresh — do not add it speculatively.

**Phase 4 — Cleanup & regression check** (*depends on Phase 3*)
1. Revert the temporary fixture content in
   `acik-istihbarat-public/acikmedya-data/AcikGazete/index070926.html` back to
   its original placeholder content (`<h1>Test Newsletter</h1><p>Smoke test
   content.</p>`), or run `git checkout -- acik-istihbarat-public/acikmedya-data/AcikGazete/index070926.html`.
2. Run `git diff acik-istihbarat-public/components/AcikMedyaIframe.tsx` to
   confirm the ONLY remaining diff is the `sandbox` attribute line + the new
   comment line (no leftover fixture/debug code).
3. Run `npm run build` in `acik-istihbarat-public` (create/use a task similar
   to the existing `admin-ts-build-check2` task, but scoped to
   `acik-istihbarat-public`) to confirm a clean production build with no new
   TypeScript/ESLint errors.
4. If a second newsletter folder with real (non-placeholder) content exists,
   load its `/acikmedya/<folder>` route once more to confirm no regressions
   (e.g., any external `target="_blank"` links in real newsletters still open
   via `allow-popups`).

## Relevant files
- `acik-istihbarat-public/components/AcikMedyaIframe.tsx` — only file requiring
  a code change (the `sandbox` attribute on the `<iframe>` element).
- `acik-istihbarat-public/lib/newsletters.ts` — reference only, confirms HTML
  source is trusted/first-party (no edits needed).
- `acik-istihbarat-public/acikmedya-data/AcikGazete/index070926.html` — used
  only as a placeholder; a temporary richer fixture is needed for real
  verification of Phase 2.
- `acikmedya-webhook/server.js` — reference only; traced as the content's
  write/ingestion path for evaluation finding #1 (no edits needed by this plan).

## Verification
1. Manual browser test per Phase 3 (all three TOC links — 2 scripted, 1 plain
   anchor — scroll correctly; iframe not clipped).
2. `npm run build` in `acik-istihbarat-public` (or equivalent dev-server visual
   check) shows no errors after the sandbox attribute change.

## Decisions
- Keep `allow-same-origin` alongside `allow-scripts` (user's explicit choice,
  informed by the security tradeoff discussion) rather than switching to a
  postMessage-based no-allow-same-origin design — content is trusted first-party.
- Scope excludes any changes to the external newsletter-generation pipeline
  (not in this repo) — only the iframe wrapper's sandbox/resize behavior is in
  scope.
- Do not modify resize/ResizeObserver logic unless Phase 3 verification proves
  it's still broken after the sandbox fix alone (avoid speculative changes).
- Webhook sanitization / token-hardening (evaluation finding #1) is explicitly
  OUT OF SCOPE for this change — logged as a deferred follow-up, not a blocker.

## Deferred follow-ups (not part of this change)
- **[Safety / High]** Verify `acikmedya-webhook`'s `WEBHOOK_TOKEN` strength and
  rotation policy; consider adding HTML sanitization at ingestion
  (`acikmedya-webhook/server.js`) or read time (`lib/newsletters.ts`) as
  defense-in-depth for the `allow-scripts` + `allow-same-origin` sandbox combo.
