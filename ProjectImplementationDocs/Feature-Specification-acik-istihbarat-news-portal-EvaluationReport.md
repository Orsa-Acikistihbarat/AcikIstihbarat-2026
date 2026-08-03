# Feature-Specification-acik-istihbarat-news-portal — Evaluation Report

**Evaluator:** Senior Architect (automated)
**Date:** 2026-07-30
**Artifact:** `ProjectImplementationDocs/Feature-Specification-acik-istihbarat-news-portal.md`
**Scope reviewed:** Full specification document (Sections 1–12)

## Summary

The specification is well-structured and covers the core functional requirements comprehensively. However, it has several gaps that would cause real problems during implementation: the API lacks an explicit public/admin route separation strategy (security risk), the data model has no way to link news articles to their images (functionality blocker), and the search feature has no defined implementation strategy for 10,000+ articles (performance risk). Additionally, there are missing safeguards around CORS, file storage path handling, slider performance, soft-delete for media items, and frontend resilience when the API is unavailable.

## Approved Findings

### 1. Missing API endpoint authorization separation between public and admin routes

- **Category / Severity:** Safety / High
- **Location:** Section 5 (NFR-3), Section 6 (Chosen Technical Approach)
- **Observation:** The spec describes a single shared .NET API serving both the public site and the admin panel, but never defines how to distinguish between public (unauthenticated) and admin-only (authenticated) endpoints.
- **Rationale:** Without explicit route segmentation, developers risk accidentally leaving admin endpoints unprotected or locking down public endpoints. The industry standard is to use route prefixes (e.g., `/api/public/*` and `/api/admin/*`) with authorization policies applied at the group level.
- **Suggestion:** Add a functional requirement or a section under "Chosen Technical Approach" that explicitly defines the API route segmentation strategy — a `/api/public/` prefix for read-only unauthenticated access and an `/api/admin/` prefix requiring valid authentication tokens.

---

### 2. No relationship defined between `Haber` and `MedyaKutuphanesi` for news article images

- **Category / Severity:** Functionality / High
- **Location:** Section 7 (Data Model), FR-4, FR-14
- **Observation:** The spec says news articles have "associated images" (FR-4, FR-14) but the data model has no mechanism to link a `Haber` record to its images in `MedyaKutuphanesi`.
- **Rationale:** Without a relationship, it is impossible to determine which media items belong to which news article. FR-4 and FR-14 cannot be implemented as written.
- **Suggestion (approved with modification):** Create a **`HaberMedyalar` join table** with `HaberId` and `MedyaId` columns, enabling many-to-many relationships. `MedyaKutuphanesi` stores **all file types** (images, documents, PDFs, etc.) in a unified manner.

---

### 3. Search strategy undefined for 10,000+ articles

- **Category / Severity:** Performance / High
- **Location:** Section 4 (FR-6), Section 5 (NFR-2, NFR-6)
- **Observation:** FR-6 requires full-text search across news articles and document metadata, but the spec never defines how this search will be implemented. A naive EF Core `LIKE '%keyword%'` approach would cause table scans across 10,000+ articles, easily exceeding the 500ms response target.
- **Rationale:** SQL Server has a built-in Full-Text Search feature that pre-indexes text columns for near-instant lookups, requiring no additional infrastructure beyond the existing MSSQL instance.
- **Suggestion (approved):** Specify **SQL Server Full-Text Search** as the search strategy in Section 6. Full-text indexes should be created on the relevant `Haber` text columns and on `MedyaKutuphanesi.Baslik` / `AnahtarKelimeler`.

---

### 4. No CORS policy defined for a multi-origin API

- **Category / Severity:** Safety / High
- **Location:** Section 6 (Chosen Technical Approach), Section 11 (Workspace Structure)
- **Observation:** Three separate applications (API, public site, admin panel) each run on different ports/domains. The spec never mentions CORS. Without it, browsers will block all API calls from both frontends.
- **Rationale:** The Same-Origin Policy will cause immediate runtime failures. A misconfigured wildcard CORS policy (`AllowAnyOrigin`) is equally dangerous as it opens admin endpoints to any website on the internet.
- **Suggestion:** Add a non-functional requirement specifying that the .NET API must configure CORS with an explicit allowlist of public and admin frontend origins. Wildcard (`*`) origins must not be used in production.

---

### 5. `MedyaKutuphanesi` table missing a soft-delete flag

- **Category / Severity:** Functionality / Medium
- **Location:** Section 7 (Data Model — `MedyaKutuphanesi` table), FR-12
- **Observation:** FR-12 allows admins to "delete" documents. With the `HaberMedyalar` join table now in place, hard-deleting a media item that is still linked to one or more news articles would cause broken images/links on the public site.
- **Rationale:** Soft delete (an `Aktifmi` boolean column defaulting to `true`) is the industry standard for CMS systems. It prevents broken references and provides an implicit "undo" safety net.
- **Suggestion:** Add an `Aktifmi` (boolean, default `true`) column to the `MedyaKutuphanesi` table. "Deleting" sets `Aktifmi = false` rather than removing the row. A future admin feature could provide permanent purge capability.

---

### 6. No pagination or limit defined for the featured news slider

- **Category / Severity:** Performance / Medium
- **Location:** Section 4 (FR-2), Section 8 (UI — Homepage)
- **Observation:** FR-2 displays all articles where `HaberMansetmi = true` with no upper limit. Over time, admins could flag hundreds of articles as featured, causing massive initial page loads and degraded Lighthouse scores.
- **Rationale:** Sliders are designed for a small number of items. Loading excessive hero images on every page load defeats SSR performance gains and the NFR-8 Lighthouse target of 90+.
- **Suggestion (approved with modification):** FR-2 should specify that the slider displays the **most recent 10 articles** flagged as featured, ordered by date descending. The `HaberMansetmi` flag acts as a candidate pool; the API applies `ORDER BY date DESC` and `TAKE(10)`.

---

### 7. File storage path creates tight physical coupling with no abstraction

- **Category / Severity:** Maintainability (intersects Safety) / Medium
- **Location:** Section 7 (Data Model — `DosyaYolu`), Section 11 (Workspace Structure)
- **Observation:** The spec defines `DosyaYolu` as a "relative path" but never specifies relative to what, or how the API resolves it to an absolute path. Without validation, a malicious `DosyaYolu` value like `../../appsettings.json` could expose sensitive server files (OWASP A01: Broken Access Control).
- **Rationale:** Hardcoding the base path in controller logic prevents portability and testability. The base directory should be configurable, and all resolved paths must be validated to stay within the allowed directory.
- **Suggestion:** Add a note specifying that the `MedyaKutuphanesi` base directory path must be configurable via `appsettings.json` (never hardcoded), and all file read/write operations must validate that the resolved full path remains within the configured base directory (anti-traversal check).

---

### 8. Next.js SSR calls to .NET API lack a failure/fallback strategy

- **Category / Severity:** Functionality / Medium
- **Location:** Section 5 (NFR-1), Section 6 (Chosen Technical Approach)
- **Observation:** NFR-1 mandates SSR. If the .NET API is down or slow during a server-side render, Next.js will return a 500 error to visitors — the entire public site goes dark.
- **Rationale:** For a news portal, availability is critical. Next.js supports ISR (Incremental Static Regeneration) which pre-builds and caches pages, serving stale content gracefully during API outages instead of crashing.
- **Suggestion:** Specify the rendering/caching strategy: use **ISR with a revalidation interval** (e.g., 60 seconds) for listing pages so the public site remains functional during brief API outages. Use **error boundary components** for graceful degradation on detail pages.

---

## Out of Scope / Deferred

*No suggestions were declined. All 8 findings were approved.*

## Notes
- This report contains suggestions only. No code was modified by the evaluator.
- Suggestions are intentionally localised and do not propose structural rewrites.
- The specification document should be updated to incorporate these approved findings before proceeding to implementation planning.
