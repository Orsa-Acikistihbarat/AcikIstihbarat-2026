# Feature-Specification-acik-istihbarat-news-portal

**Date:** 2026-07-30
**Author:** (workshop between user and Think agent)
**Status:** Draft

---

## 1. Overview

AcikIstihbarat is a full-featured news portal designed to serve an existing archive of approximately 10,000 news articles stored in a Microsoft SQL Server database (`AcikIstihbaratYeniDB2026`). The system consists of three main components: a **public-facing Next.js website** for visitors to browse and search news, an **isolated React admin panel** for content management, and a **.NET Core Web API** that serves as the central data and business logic layer connecting both frontends to the database and file storage. The public site features category-based navigation, a featured news slider, a document library section, and full-text search. The admin panel allows authorized users to manage news articles, tag featured items, and upload documents into the media library.

---

## 2. Goals & Non-Goals

### Goals
- Expose the existing 10,000+ news archive to the public through a modern, SEO-optimized website.
- Provide category-based and subcategory-based browsing of news articles.
- Display featured news items in a prominent slider on the homepage.
- Allow admin users to create, edit, and manage news articles and documents through a dedicated admin panel.
- Provide a searchable document library section on the public site, displaying documents with appropriate file-type icons and titles.
- Enable full-text search across news articles and document metadata.
- Store all physical media (images, PDFs, documents) in a centralized `MedyaKutuphanesi` project folder, accessible through the API layer.

### Non-Goals
- User registration or public user accounts (this is a read-only portal for visitors).
- Real-time notifications or push alerts.
- Mobile native applications (the site will be responsive but web-only).
- Content versioning or editorial workflow (no draft → review → publish pipeline in V1).
- Commenting or social interaction features on news articles.
- Multi-language / internationalization support in V1.

---

## 3. User Stories

### Public Visitor
- As a **visitor**, I want to see featured news in a slider at the top of the homepage, so that I can quickly catch the most important stories.
- As a **visitor**, I want to browse news by category and subcategory using the navigation bar, so that I can find articles on topics I'm interested in.
- As a **visitor**, I want to click on a news headline and read the full article on a dedicated detail page, so that I can consume the complete story.
- As a **visitor**, I want to search for news articles by keyword, so that I can quickly find specific stories.
- As a **visitor**, I want to browse a document library section, so that I can find and download published documents (PDFs, etc.) with clear icons indicating their file type.
- As a **visitor**, I want to search document titles and metadata, so that I can find specific documents without browsing manually.

### Admin User
- As an **admin**, I want to log in to a secure, isolated admin panel, so that I can manage site content.
- As an **admin**, I want to create and edit news articles (header, summary/spot, body, images), so that new stories appear on the public site.
- As an **admin**, I want to assign a category to each news article, so that it appears under the correct section on the public site.
- As an **admin**, I want to flag a news article as "featured", so that it appears in the homepage slider.
- As an **admin**, I want to upload documents (PDFs, etc.) with a title and metadata, so that they appear in the public document library.

---

## 4. Functional Requirements

| #     | Requirement                                                                                                              | Priority      |
|-------|--------------------------------------------------------------------------------------------------------------------------|---------------|
| FR-1  | The public site shall display a navigation bar listing top-level news categories and their subcategories.                 | Must Have     |
| FR-2  | The homepage shall feature a slider/carousel section displaying the **most recent 10** news articles flagged as featured (`HaberMansetmi`), ordered by date descending. The `HaberMansetmi` flag acts as a candidate pool; the API returns only the 10 newest. | Must Have     |
| FR-3  | News articles shall be browsable by category, determined by the `HaberTuruID` column in the `Haber` table.               | Must Have     |
| FR-4  | Clicking a news headline shall navigate to a dedicated detail page showing the full article: header, spot (summary), body, and associated images. | Must Have     |
| FR-5  | The public site shall include a document library section displaying uploaded documents with their title and file-type icon (PDF, DOCX, etc.). | Must Have     |
| FR-6  | The public site shall include a search bar that searches across news article text and document metadata.                  | Must Have     |
| FR-7  | The admin panel shall require authentication (username/password login) before granting access.                            | Must Have     |
| FR-8  | The admin panel shall allow creating, editing, and deleting news articles.                                                | Must Have     |
| FR-9  | The admin panel shall allow assigning a category (`HaberTuruID`) to each news article.                                   | Must Have     |
| FR-10 | The admin panel shall allow toggling the "featured" flag (`HaberMansetmi`) on news articles.                              | Must Have     |
| FR-11 | The admin panel shall allow uploading documents with title and metadata, storing physical files in the `MedyaKutuphanesi` folder. | Must Have     |
| FR-12 | The admin panel shall allow managing (editing, deleting) existing documents in the media library.                         | Must Have     |
| FR-13 | All frontend-to-backend communication shall pass through the .NET Core Web API layer (no direct database access from any frontend). | Must Have     |
| FR-14 | Images associated with news articles shall be served from the `MedyaKutuphanesi` folder through the API.                 | Must Have     |
| FR-15 | The public site shall implement pagination or infinite scroll for news listing pages.                                     | Should Have   |
| FR-16 | The public site shall be fully responsive across desktop, tablet, and mobile viewports.                                   | Should Have   |
| FR-17 | The API shall use explicit route prefixes to separate public and admin endpoints: `/api/public/*` for unauthenticated read-only access and `/api/admin/*` for authenticated write/management access. Authorization policies shall be applied at the route group level. | Must Have     |
| FR-18 | The .NET API shall configure CORS with an explicit allowlist of the public and admin frontend origins. Wildcard (`*`) origins must not be used in production. | Must Have     |

---

## 5. Non-Functional Requirements

| #      | Requirement                                                                                                    | Category     |
|--------|----------------------------------------------------------------------------------------------------------------|--------------|
| NFR-1  | Public pages shall use **Incremental Static Regeneration (ISR)** with a revalidation interval (e.g., 60 seconds) for listing pages (homepage, category pages). During API outages, the cached version continues to be served. Detail pages shall use SSR with **error boundary components** for graceful degradation. | Performance / SEO |
| NFR-2  | API response time for list queries shall be under 500ms for standard page loads.                                | Performance  |
| NFR-3  | Only authenticated admin users may access the admin panel and its API endpoints.                                | Security     |
| NFR-4  | File uploads shall be validated for allowed MIME types and maximum file size on both client and server.          | Security     |
| NFR-5  | The admin panel shall be deployed as a physically separate application from the public site.                     | Architecture |
| NFR-6  | The system shall support the existing 10,000+ article archive without performance degradation.                  | Scalability  |
| NFR-7  | All API endpoints shall follow RESTful conventions.                                                             | Maintainability |
| NFR-8  | The public site shall achieve a Lighthouse SEO score of 90+ on key pages.                                       | SEO          |
| NFR-9  | The .NET API shall configure CORS with an explicit allowlist of permitted frontend origins. Wildcard (`*`) origins are prohibited in production. | Security     |
| NFR-10 | The `MedyaKutuphanesi` base directory path shall be configurable via `appsettings.json` (never hardcoded). All file read/write operations shall validate that the resolved full path remains within the configured base directory (anti-path-traversal check, OWASP A01). | Security     |

---

## 6. Chosen Technical Approach

**Architecture decision:** A three-tier architecture with a shared .NET Core Web API backend, a public-facing Next.js frontend (for SEO via server-side rendering), and an isolated React (Vite) admin panel for content management.

**Key technologies / patterns:**

- **.NET Core Web API (C#):** The central backend providing RESTful API endpoints for both the public site and the admin panel. Handles all business logic, data access, authentication, and file management.
- **Entity Framework Core:** The ORM (Object-Relational Mapper) used by .NET to interact with the SQL Server database. It maps database tables to C# classes so we can query and manipulate data using familiar code instead of raw SQL.
- **Microsoft SQL Server (MSSQL):** The existing relational database (`AcikIstihbaratYeniDB2026`) hosting the `Haber` table and the new `MedyaKutuphanesi` and `HaberMedyalar` tables.
- **SQL Server Full-Text Search:** The built-in full-text indexing engine in MSSQL, used to power the search feature (FR-6). Full-text indexes will be created on relevant `Haber` text columns and on `MedyaKutuphanesi.Baslik` / `AnahtarKelimeler`. This requires no additional infrastructure and provides near-instant search across 10,000+ articles.
- **Next.js (React):** The public-facing frontend framework. Uses **Incremental Static Regeneration (ISR)** to pre-build and cache pages, serving them instantly to visitors while revalidating in the background. This ensures SEO optimization and resilience during brief API outages.
- **React (Vite):** The admin panel frontend. Built as a standard Single Page Application since SEO is irrelevant for an internal management tool, and Vite provides extremely fast development builds.
- **Tailwind CSS:** A utility-first CSS framework used across both frontends for rapid, consistent styling.
- **Preline UI:** A component library built on top of Tailwind CSS that provides pre-built, accessible UI components (modals, dropdowns, sliders, etc.) to accelerate frontend development.

**Fits into existing stack because:** The existing database is already on MSSQL, and the user has chosen .NET Core + React as their preferred stack. Next.js extends React with SSR/ISR capabilities specifically needed for the public news site.

**API Route Segmentation:**
The API uses explicit route prefixes to enforce authorization boundaries:
- `/api/public/*` — Unauthenticated, read-only endpoints consumed by the Next.js public site.
- `/api/admin/*` — Authenticated endpoints consumed by the React admin panel. All endpoints in this group require a valid authentication token.

**CORS Policy:**
The API configures CORS with an explicit allowlist of permitted frontend origins (public site and admin panel URLs). Wildcard (`*`) origins are prohibited in production to prevent unauthorized cross-origin access to admin endpoints.

**File Storage Configuration:**
The `MedyaKutuphanesi` base directory path is configured via `appsettings.json` (never hardcoded). All file I/O operations validate that the resolved absolute path remains within the configured base directory to prevent path-traversal attacks (OWASP A01).

---

## 7. Data Model

### Existing Table: `Haber` (News Articles)

This table already exists in `AcikIstihbaratYeniDB2026` with ~10,000 records. Key columns include:

| Column          | Description                                                        |
|-----------------|--------------------------------------------------------------------|
| (Primary Key)   | Unique identifier for each news article (exact column name TBD).   |
| `HaberTuruID`   | Foreign key / identifier linking to the news category. The mapping of ID → category name will be provided during development. |
| `HaberMansetmi` | Boolean/flag column indicating whether this article is "featured" and should appear in the homepage slider. |
| (Header)        | The headline/title of the news article (exact column name TBD).    |
| (Spot/Summary)  | A short summary or teaser for the article (exact column name TBD). |
| (Body)          | The full content of the news article (exact column name TBD).      |
| (Date fields)   | Publication date and other temporal fields (exact column names TBD).|

> **Note:** The exact column names for header, body, spot, date, and primary key will be confirmed by the user during development. The .NET EF Core model will be mapped accordingly.

### New Table: `MedyaKutuphanesi` (Media Library / Documents)

This table will be **created new** in `AcikIstihbaratYeniDB2026` to store metadata about uploaded documents.

| Column          | Description                                                        |
|-----------------|--------------------------------------------------------------------|
| `Id`            | Primary key (auto-incrementing integer or GUID).                   |
| `Baslik`        | Title/name of the document as displayed on the public site.        |
| `DosyaAdi`      | The physical filename stored on disk (within the `MedyaKutuphanesi` folder). |
| `DosyaYolu`     | Relative path to the file within the `MedyaKutuphanesi` folder. Resolved against the configurable base directory in `appsettings.json` at runtime (never hardcoded). All resolved paths are validated to remain within the base directory (anti-path-traversal). |
| `DosyaTipi`     | MIME type or file extension (e.g., `application/pdf`, `.docx`) used to determine the icon on the frontend. |
| `DosyaBoyutu`   | File size in bytes.                                                |
| `Aciklama`      | Optional description or metadata about the document.               |
| `AnahtarKelimeler` | Optional search tags/keywords for the document (used in the search feature via SQL Server Full-Text Search). |
| `YuklenmeTarihi`| Upload timestamp.                                                  |
| `YukleyenKullanici` | The admin user who uploaded the document.                      |
| `Aktifmi`       | Boolean flag (default: `true`). When an admin "deletes" a media item, this is set to `false` (soft delete) rather than removing the row. Prevents broken references from linked news articles via `HaberMedyalar`. |

### New Table: `HaberMedyalar` (News–Media Join Table)

This table establishes a **many-to-many relationship** between news articles and media items. A single news article can have multiple associated images/documents, and a single media item can be shared across multiple articles.

| Column          | Description                                                        |
|-----------------|--------------------------------------------------------------------|
| `Id`            | Primary key (auto-incrementing integer).                           |
| `HaberId`       | Foreign key referencing the `Haber` table (the news article).      |
| `MedyaId`       | Foreign key referencing the `MedyaKutuphanesi` table (the media item). |
| `Sira`          | Optional display order for images within the article (e.g., 1, 2, 3). |

> **Note:** This table is essential for FR-4 (displaying associated images on the news detail page) and FR-14 (serving images through the API). Because `MedyaKutuphanesi` uses soft delete (`Aktifmi`), deleting a media item will not break references from this join table.

---

### Category Mapping

The news categories are determined by the `HaberTuruID` column in the `Haber` table. The exact mapping of IDs to category names (and any subcategory hierarchy) will be provided by the user during development. This mapping may be:
- Hard-coded in a configuration file or enum (if the categories are fixed and rarely change), or
- Stored in a new `HaberTuru` lookup table (if the admin needs to manage categories dynamically).

> **Decision needed during development:** Whether categories should be managed dynamically via a table or statically via configuration.

---

## 8. UI / Layout Notes

### Public Site (Next.js + Tailwind + Preline UI)

#### Homepage
```
┌─────────────────────────────────────────────────────┐
│  Navigation Bar (Categories + Subcategories + Search)│
├─────────────────────────────────────────────────────┤
│                                                     │
│           Featured News Slider / Carousel           │
│         (articles where HaberMansetmi = true)       │
│                                                     │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Latest News Grid                                   │
│  ┌──────┐ ┌──────┐ ┌──────┐                        │
│  │ Card │ │ Card │ │ Card │  ...                    │
│  └──────┘ └──────┘ └──────┘                        │
│                                                     │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Document Library Section                           │
│  📄 Document Title 1    📊 Document Title 2         │
│  📋 Document Title 3    📄 Document Title 4         │
│                                                     │
├─────────────────────────────────────────────────────┤
│  Footer                                             │
└─────────────────────────────────────────────────────┘
```

#### Category Page
- Lists all news articles filtered by the selected `HaberTuruID`.
- Uses card layout with thumbnail, headline, spot/summary, and date.
- Includes pagination.

#### News Detail Page
- Full article view: headline, publication date, spot/summary, full body content.
- Associated images displayed inline or in a gallery.
- "Related articles" or "More from this category" section (optional, nice to have).

#### Document Library Page
- Grid or list of all documents.
- Each document displays: file-type icon (PDF, DOCX, etc.), title, and optional description.
- Clicking a document downloads it or opens it in a new tab.

#### Search Results Page
- Combined results from news articles and document metadata.
- Results clearly differentiated (news vs. document).

### Admin Panel (React Vite + Tailwind + Preline UI)

#### Login Page
- Simple username/password form.

#### Dashboard
- Overview statistics (total articles, featured count, total documents).

#### News Management
- Table listing all news articles with columns: title, category, featured flag, date.
- Inline actions: edit, delete, toggle featured.
- Create/Edit form: header, spot, body (rich text or textarea), category selector, featured toggle, image upload.

#### Document Management
- Table listing all documents: title, file type, upload date, uploader.
- Upload form: file picker, title input, description, keywords/tags.
- Actions: edit metadata, delete.

---

## 9. Open Questions

| #    | Question                                                                                                                   | Owner          |
|------|----------------------------------------------------------------------------------------------------------------------------|----------------|
| OQ-1 | What are the exact column names in the `Haber` table (primary key, header, spot, body, date, image reference columns)?      | User           |
| OQ-2 | What is the full mapping of `HaberTuruID` values to category names and subcategory hierarchy?                               | User           |
| OQ-3 | Should categories be managed dynamically (via admin panel + database table) or statically (configuration/code)?             | User / Dev Team |
| OQ-4 | What authentication mechanism should be used for the admin panel? (e.g., JWT tokens, ASP.NET Identity, a simple hardcoded admin user for V1?) | User / Dev Team |
| OQ-5 | Are the existing news article images already stored somewhere on disk, or do they only exist as database references/URLs?    | User           |
| OQ-6 | Should the news article body support rich text / HTML formatting, or is plain text sufficient?                               | User           |
| OQ-7 | What is the maximum file size allowed for document uploads?                                                                 | User           |
| OQ-8 | Are there any specific Preline UI components the user wants to use (e.g., a specific slider/carousel component)?            | User           |

---

## 10. Out of Scope (deferred ideas)

- **Role-based access control (RBAC):** Multiple admin roles (editor, publisher, super-admin) are not needed for V1. A single admin role is sufficient.
- **Content scheduling:** Publishing articles at a future date/time.
- **Analytics dashboard:** Tracking page views, popular articles, or visitor demographics.
- **Newsletter / email subscriptions.**
- **Social media sharing integration.**
- **Multi-language support.**
- **Comment system or user interaction features.**
- **Automated image optimization or thumbnail generation** (could be added later).
- **CI/CD pipeline setup** (deployment automation is out of scope for V1).

---

## 11. Workspace Structure

The workspace root is: `c:\Belgelerim\yazilim\calisma-projeleri\AcikIstihbarat-2026\`

The following projects will be created under this root:

```
AcikIstihbarat-2026/
├── AcikIstihbarat.API/           # .NET Core Web API (C#, EF Core)
│   ├── Controllers/              # API endpoint controllers
│   ├── Models/                   # EF Core entity models (Haber, MedyaKutuphanesi)
│   ├── Data/                     # DbContext and migrations
│   ├── Services/                 # Business logic services
│   └── Program.cs                # Application entry point
│
├── acik-istihbarat-public/       # Next.js public frontend
│   ├── app/                      # Next.js App Router pages
│   ├── components/               # Reusable React components
│   └── public/                   # Static assets
│
├── acik-istihbarat-admin/        # React (Vite) admin panel
│   ├── src/
│   │   ├── pages/                # Admin pages (login, dashboard, news, documents)
│   │   ├── components/           # Reusable admin components
│   │   └── services/             # API client services
│   └── public/
│
├── MedyaKutuphanesi/             # Media storage project
│   └── MedyaKutuphanesi/         # Physical file storage folder
│
└── ProjectImplementationDocs/    # Specifications and documentation
```

---

## 12. Suggested Next Steps

1. **Review this specification** and answer the Open Questions (Section 9) — especially OQ-1 (exact column names) and OQ-2 (category mapping).
2. **Create an implementation plan** using the main agent, breaking the work into phases (e.g., Phase 1: API + Data Layer, Phase 2: Public Frontend, Phase 3: Admin Panel).
3. **Scaffold the workspace** by creating the project folders and initializing each application (.NET Core, Next.js, Vite React).
4. **Reverse-engineer the existing `Haber` table** by connecting EF Core to `AcikIstihbaratYeniDB2026` and scaffolding the model from the live database.
