# Implementation-Plan-acik-istihbarat-news-portal

**Date:** 2026-07-30
**Source Specification:** [Feature-Specification-acik-istihbarat-news-portal.md](./Feature-Specification-acik-istihbarat-news-portal.md)
**Projects Affected:** `AcikIstihbarat.API`, `acik-istihbarat-public`, `acik-istihbarat-admin`, `MedyaKutuphanesi`
**Status:** Draft - Pending Developer Review

---

## Clarifications Resolved During Planning

| # | Topic | Decision |
|---|-------|----------|
| OQ-4 | Authentication | ASP.NET Core Identity + JWT Bearer tokens |
| OQ-6 | Article body format | Rich text HTML - TipTap editor in admin; DOMPurify sanitization in public site |
| OQ-3 | Category management | Dynamic - `HaberKategorileri` table (`Id`, `Ad`, `ParentId`, `Sira`) |
| OQ-5 | Existing article images | `HaberResimAdresi` column used as legacy fallback on public detail page; physical files copied manually into `MedyaKutuphanesi/` folder at a later date |
| OQ-7 | Max file upload size | 50 MB per file, single limit for all file types |
| OQ-8 | Media flow for new articles | GUID-prefixed filenames on disk; `HaberMedyalar.OncuResimMi` identifies thumbnail/hero image; multiple images render as slider on detail page; documents render in right-column panel |

---

## 1. Executive Summary

AcikIstihbarat is a three-application news portal ecosystem serving an existing archive of 10,000+ Turkish-language news articles. A central .NET Core 8 Web API connects to a local MSSQL database (`AcikIstihbaratYeniDB2026`) via Entity Framework Core, exposing two route groups: `/api/public/*` (anonymous) for the Next.js public website and `/api/admin/*` (JWT-authenticated via ASP.NET Core Identity) for an isolated React/Vite admin panel. The public site uses Next.js Incremental Static Regeneration (ISR) for SEO performance and API-outage resilience. The admin panel is a full SPA allowing news article CRUD with TipTap rich-text editing, multi-file media uploads (GUID-prefixed, 50 MB limit), featured image designation, and document management. All physical files live in the `MedyaKutuphanesi/` project folder; the API handles all file I/O with anti-path-traversal protection.

---

## 2. Final Data Model

### Existing: `Haber`
- All columns preserved as-is. EF Core scaffolded from live DB.
- `HaberTuruID` -> FK to `HaberKategorileri.Id` (logical only - no DB constraint added)
- `HaberMansetmi` -> featured flag for slider
- `HaberResimAdresi` -> legacy thumbnail path (read-only; fallback if no `HaberMedyalar` rows exist)

### New: `HaberKategorileri`
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | Auto-increment |
| `Ad` | nvarchar(200) | Category display name |
| `ParentId` | int? | FK self-referencing; NULL = top-level category |
| `Sira` | int | Display order in navbar |

### New: `MedyaKutuphanesi`
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | Auto-increment |
| `Baslik` | nvarchar(500) | Display title |
| `OrijinalDosyaAdi` | nvarchar(500) | Original filename before GUID prefix |
| `FizikselDosyaAdi` | nvarchar(600) | `{GUID}_{OrijinalDosyaAdi}` - actual name on disk |
| `DosyaYolu` | nvarchar(1000) | Relative path from `MedyaKutuphanesi` base folder |
| `DosyaTipi` | nvarchar(100) | MIME type (e.g., `image/jpeg`, `application/pdf`) |
| `DosyaBoyutu` | bigint | File size in bytes |
| `Aciklama` | nvarchar(2000)? | Optional description |
| `AnahtarKelimeler` | nvarchar(1000)? | Comma-separated search tags (FTS indexed) |
| `YuklenmeTarihi` | datetime2 | UTC upload timestamp |
| `YukleyenKullanici` | nvarchar(256) | Identity username of uploader |
| `Aktifmi` | bit | Default 1. Soft delete flag. |

### New: `HaberMedyalar`
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | Auto-increment |
| `HaberId` | int | FK -> `Haber` |
| `MedyaId` | int | FK -> `MedyaKutuphanesi` |
| `Sira` | int | Display order (for image slider ordering) |
| `OncuResimMi` | bit | Default 0. If 1, this is the thumbnail on list pages and hero on detail page. Only one per article. |

### New: ASP.NET Identity Tables
Standard Identity tables created by `IdentityDbContext<IdentityUser>` migration: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens`.

---

## 3. Dependency Map

### NuGet Packages (AcikIstihbarat.API)
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.AspNetCore.Authentication.JwtBearer`

### npm Packages (acik-istihbarat-public)
- `preline`
- `dompurify` + `@types/dompurify`
- `isomorphic-dompurify`

### npm Packages (acik-istihbarat-admin)
- `tailwindcss`, `postcss`, `autoprefixer`
- `preline`
- `axios`
- `react-router-dom`
- `@tiptap/react`, `@tiptap/starter-kit`, `@tiptap/extension-link`, `@tiptap/extension-image`
- `dompurify` + `@types/dompurify`

---

## 4. Integration Points

| # | Component | Connects To | Protocol | Contract |
|---|-----------|------------|---------|---------|
| INT-1 | Next.js server components | `.NET API /api/public/*` | HTTPS REST | Anonymous. Returns typed JSON. Caller handles `null` on error (ISR fallback). |
| INT-2 | React admin SPA | `.NET API /api/admin/*` | HTTPS REST | `Authorization: Bearer {token}` header on every request. 401 -> redirect to `/login`. |
| INT-3 | `.NET API` | `AcikIstihbaratYeniDB2026` | EF Core SqlServer | Connection string in `appsettings.Development.json` (gitignored). |
| INT-4 | `.NET API DosyaService` | `MedyaKutuphanesi/` folder | Local filesystem | Base path from `appsettings.json:MedyaKutuphanesi:BasePath`. Anti-traversal validated. |
| INT-5 | Next.js public site | Media files | Via `GET /api/public/medya/dosya/{id}` | API streams file with correct `Content-Type`. No direct filesystem access from Next.js. |


---

## 5. Phased Implementation Plan

---

### PHASE 1 - Workspace Scaffolding & Project Initialization

**Goal:** Create all projects, install all base dependencies, confirm every application builds and runs. No features - only a clean, verified foundation.
**Estimated Complexity:** Low
**Prerequisites:** None.

---

#### TASK-1.1 - Create workspace folder structure

**What:**
Create the following directories under `c:\Belgelerim\yazilim\calisma-projeleri\AcikIstihbarat-2026\`:
- `AcikIstihbarat.API\` (populated by dotnet CLI)
- `acik-istihbarat-public\` (populated by Next.js CLI)
- `acik-istihbarat-admin\` (populated by Vite CLI)
- `MedyaKutuphanesi\MedyaKutuphanesi\` (nested - outer is project, inner is the file store)
- `ProjectImplementationDocs\` (already exists)

**Command:** `mkdir MedyaKutuphanesi\MedyaKutuphanesi` from workspace root

**Acceptance Criteria:** All four top-level directories exist. Nested `MedyaKutuphanesi\MedyaKutuphanesi\` folder is empty and accessible.

---

#### TASK-1.2 - Scaffold the .NET Core 8 Web API

**What:**
1. Run: `dotnet new webapi -n AcikIstihbarat.API --framework net8.0`
2. Delete auto-generated `WeatherForecast.cs` and `WeatherForecastController.cs`
3. Create empty folders inside the project: `Controllers/Public/`, `Controllers/Admin/`, `Models/`, `Models/DTOs/`, `Services/`, `Data/`, `Data/Migrations/`
4. Verify `AcikIstihbarat.API.csproj` targets `net8.0`

**Where:** `AcikIstihbarat-2026/AcikIstihbarat.API/`

**Acceptance Criteria:** `dotnet build` succeeds with 0 errors. `dotnet run` starts without exceptions. Swagger UI accessible at `/swagger`.

---

#### TASK-1.3 - Install .NET NuGet packages

**What:** From inside `AcikIstihbarat.API/`, run:
```
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

**Where:** `AcikIstihbarat-2026/AcikIstihbarat.API/AcikIstihbarat.API.csproj`

**Acceptance Criteria:** `dotnet restore` completes with no errors. All four packages appear in `.csproj`. `dotnet build` still succeeds.

---

#### TASK-1.4 - Scaffold the Next.js 14 public frontend

**What:**
1. Run: `npx create-next-app@latest acik-istihbarat-public`
2. When prompted: TypeScript=Yes, ESLint=Yes, Tailwind CSS=Yes, src/ directory=No, App Router=Yes, customize import alias=No
3. Replace default `app/page.tsx` with placeholder `<h1>AcikIstihbarat</h1>`
4. Clear `app/globals.css` except Tailwind directives (`@tailwind base/components/utilities`)
5. Create folders: `app/kategori/`, `app/haber/`, `app/belgeler/`, `app/ara/`, `components/`, `lib/`

**Where:** `AcikIstihbarat-2026/acik-istihbarat-public/`

**Acceptance Criteria:** `npm run dev` starts on `localhost:3000`. Placeholder heading visible. No TypeScript or ESLint errors.

---

#### TASK-1.5 - Install and configure Preline UI + DOMPurify in the public frontend

**What:**
1. `npm install preline` inside `acik-istihbarat-public/`
2. In `tailwind.config.ts`: add `require('preline/plugin')` to `plugins` array; add `./node_modules/preline/preline.js` to `content` array
3. In `app/layout.tsx`: add `<Script src="../node_modules/preline/dist/preline.js" strategy="afterInteractive" />` using Next.js `next/script`
4. Run: `npm install dompurify @types/dompurify isomorphic-dompurify`

**Where:** `acik-istihbarat-public/tailwind.config.ts`, `acik-istihbarat-public/app/layout.tsx`

**Acceptance Criteria:** Test Preline dropdown renders and opens correctly. No hydration errors in browser console.

---

#### TASK-1.6 - Scaffold the React/Vite admin panel

**What:**
1. Run: `npm create vite@latest acik-istihbarat-admin -- --template react-ts`
2. `cd acik-istihbarat-admin && npm install`
3. Delete `src/App.css`, replace default `src/App.tsx` with placeholder
4. Create folders: `src/pages/`, `src/components/`, `src/services/`, `src/context/`, `src/hooks/`, `src/types/`

**Where:** `AcikIstihbarat-2026/acik-istihbarat-admin/`

**Acceptance Criteria:** `npm run dev` starts on `localhost:5173`. Placeholder renders. No TypeScript errors.

---

#### TASK-1.7 - Install Tailwind CSS + Preline + all npm packages in the admin panel

**What:** From inside `acik-istihbarat-admin/`, run in order:
```
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
npm install preline
npm install axios
npm install react-router-dom
npm install @tiptap/react @tiptap/starter-kit @tiptap/extension-link @tiptap/extension-image
npm install dompurify
npm install -D @types/dompurify
```

Configure `tailwind.config.js`:
- Add `./src/**/*.{js,ts,jsx,tsx}` and `./node_modules/preline/preline.js` to `content`
- Add `require('preline/plugin')` to `plugins`

Add `@tailwind base/components/utilities` to `src/index.css`.
Add `import 'preline'` to `src/main.tsx`.

**Where:** `AcikIstihbarat-2026/acik-istihbarat-admin/`

**Acceptance Criteria:** Tailwind utility `className="bg-blue-500 text-white p-4"` renders correctly. A Preline modal opens without console errors.

---

#### TASK-1.8 - Create root-level `.gitignore` and `README.md`

**What:**
Create `AcikIstihbarat-2026/.gitignore`:
```
node_modules/
.next/
dist/
bin/
obj/
*.user
.vs/
appsettings.Development.json
appsettings.*.json
!appsettings.json
MedyaKutuphanesi/MedyaKutuphanesi/
```

Create `AcikIstihbarat-2026/README.md` with: project title, one-paragraph description, link to Feature Specification, list of four project directories with purpose.

**Acceptance Criteria:** `git init && git add . && git status` confirms `node_modules/`, `bin/`, `obj/`, `.next/`, `appsettings.Development.json`, and `MedyaKutuphanesi/MedyaKutuphanesi/` contents are NOT staged.

---

#### Phase 1 - Definition of Done
- [ ] `dotnet build` passes with 0 errors in `AcikIstihbarat.API`
- [ ] `npm run dev` starts without errors in both frontends (ports 3000 and 5173)
- [ ] Tailwind CSS and Preline UI verified working in both frontends
- [ ] `MedyaKutuphanesi/MedyaKutuphanesi/` folder exists and is empty
- [ ] `.gitignore` correctly excludes all build artifacts and secrets

---

### PHASE 2 - Database, Data Layer & Identity Setup

**Goal:** Connect API to MSSQL, scaffold the `Haber` model, set up ASP.NET Identity, create all new tables via migrations, enable Full-Text Search, build a complete service skeleton.
**Estimated Complexity:** Medium
**Prerequisites:** Phase 1 complete. Developer must be able to connect to MSSQL and run `SELECT TOP 1 * FROM Haber` to retrieve real column names.

---

#### TASK-2.1 - Configure `appsettings.json` and `appsettings.Development.json`

**What:**
`appsettings.json` (committed) - schema with empty placeholder values:
```json
{
  "ConnectionStrings": { "DefaultConnection": "" },
  "MedyaKutuphanesi": { "BasePath": "" },
  "Jwt": { "Key": "", "Issuer": "AcikIstihbarat", "Audience": "AcikIstihbarat", "ExpiryMinutes": 480 },
  "Cors": { "AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"] },
  "AdminSeed": { "Password": "" }
}
```

`appsettings.Development.json` (gitignored) - real values:
- `DefaultConnection`: `"Server=localhost;Database=AcikIstihbaratYeniDB2026;Trusted_Connection=True;TrustServerCertificate=True;"`
- `BasePath`: absolute path to `MedyaKutuphanesi\MedyaKutuphanesi\` folder
- `Jwt:Key`: a random 64-character string (generate once, never share)
- `AdminSeed:Password`: initial admin user password

**Acceptance Criteria:** `appsettings.Development.json` is NOT tracked by git. The API reads all config values without null reference exceptions at startup.

---

#### TASK-2.2 - Create `AppDbContext` inheriting from `IdentityDbContext`

**What:**
Create `Data/AppDbContext.cs`:
```csharp
public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Haber> Haberler { get; set; }
    public DbSet<HaberKategori> HaberKategorileri { get; set; }
    public DbSet<Medya> Medyalar { get; set; }
    public DbSet<HaberMedya> HaberMedyalar { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // REQUIRED - must be called first for Identity
        // FK configurations added in TASK-2.4, 2.5, 2.6
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**Acceptance Criteria:** `dotnet build` succeeds. `AppDbContext` resolvable via DI.

---

#### TASK-2.3 - Scaffold the `Haber` EF Core entity from the live database

**What:**
Run EF Core scaffold against the existing `Haber` table only:
```
dotnet ef dbcontext scaffold "Server=localhost;Database=AcikIstihbaratYeniDB2026;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --table Haber --output-dir Models --no-onconfiguring --force
```

After scaffolding:
1. Review generated `Haber.cs` - note exact names for: PK column, headline, spot, body, date, category FK, `HaberMansetmi`, `HaberResimAdresi`
2. Delete any auto-generated `DbContext` file (keep only hand-written `AppDbContext`)
3. Add navigation property to `Haber.cs`: `public ICollection<HaberMedya> HaberMedyalar { get; set; } = new List<HaberMedya>();`
4. Configure in `AppDbContext.OnModelCreating()`: map `HaberMedyalar` navigation

**CRITICAL:** Document all discovered column names in `ProjectImplementationDocs/DEVLOG.md` immediately - every subsequent task depends on them.

**Acceptance Criteria:** `await context.Haberler.Take(5).ToListAsync()` returns 5 rows. `HaberResimAdresi` property correctly mapped.

---

#### TASK-2.4 - Create `HaberKategori` entity and migration

**What:**
Create `Models/HaberKategori.cs`:
```csharp
public class HaberKategori
{
    public int Id { get; set; }
    [MaxLength(200)] public string Ad { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public int Sira { get; set; } = 0;
    public HaberKategori? Parent { get; set; }
    public ICollection<HaberKategori> AltKategoriler { get; set; } = new List<HaberKategori>();
}
```

In `AppDbContext.OnModelCreating()`:
```csharp
modelBuilder.Entity<HaberKategori>(entity => {
    entity.ToTable("HaberKategorileri");
    entity.HasOne(k => k.Parent)
          .WithMany(k => k.AltKategoriler)
          .HasForeignKey(k => k.ParentId)
          .OnDelete(DeleteBehavior.Restrict);
});
```

Run:
```
dotnet ef migrations add AddHaberKategorileri
dotnet ef database update
```

**Acceptance Criteria:** `HaberKategorileri` table exists with `Id`, `Ad`, `ParentId`, `Sira` columns. Self-referencing FK visible in SSMS.

---

#### TASK-2.5 - Create `Medya` entity and migration

**What:**
Create `Models/Medya.cs`:
```csharp
public class Medya
{
    public int Id { get; set; }
    [MaxLength(500)] public string Baslik { get; set; } = string.Empty;
    [MaxLength(500)] public string OrijinalDosyaAdi { get; set; } = string.Empty;
    [MaxLength(600)] public string FizikselDosyaAdi { get; set; } = string.Empty;
    [MaxLength(1000)] public string DosyaYolu { get; set; } = string.Empty;
    [MaxLength(100)] public string DosyaTipi { get; set; } = string.Empty;
    public long DosyaBoyutu { get; set; }
    [MaxLength(2000)] public string? Aciklama { get; set; }
    [MaxLength(1000)] public string? AnahtarKelimeler { get; set; }
    public DateTime YuklenmeTarihi { get; set; }
    [MaxLength(256)] public string YukleyenKullanici { get; set; } = string.Empty;
    public bool Aktifmi { get; set; } = true;
    public ICollection<HaberMedya> HaberMedyalar { get; set; } = new List<HaberMedya>();
}
```

In `AppDbContext.OnModelCreating()`:
```csharp
modelBuilder.Entity<Medya>(entity => {
    entity.ToTable("MedyaKutuphanesi");
    entity.Property(m => m.Aktifmi).HasDefaultValue(true);
    entity.Property(m => m.YuklenmeTarihi).HasDefaultValueSql("GETUTCDATE()");
});
```

Run:
```
dotnet ef migrations add AddMedyaKutuphanesi
dotnet ef database update
```

**Acceptance Criteria:** `MedyaKutuphanesi` table exists with all 13 columns and correct default values.

---

#### TASK-2.6 - Create `HaberMedya` join entity and migration

**What:**
Create `Models/HaberMedya.cs`:
```csharp
public class HaberMedya
{
    public int Id { get; set; }
    public int HaberId { get; set; }
    public int MedyaId { get; set; }
    public int Sira { get; set; } = 0;
    public bool OncuResimMi { get; set; } = false;
    public Haber Haber { get; set; } = null!;
    public Medya Medya { get; set; } = null!;
}
```

In `AppDbContext.OnModelCreating()`:
```csharp
modelBuilder.Entity<HaberMedya>(entity => {
    entity.ToTable("HaberMedyalar");
    entity.HasOne(hm => hm.Haber)
          .WithMany(h => h.HaberMedyalar)
          .HasForeignKey(hm => hm.HaberId)
          .OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(hm => hm.Medya)
          .WithMany(m => m.HaberMedyalar)
          .HasForeignKey(hm => hm.MedyaId)
          .OnDelete(DeleteBehavior.Restrict); // Restrict: prevents deleting media still linked to articles
    entity.Property(hm => hm.OncuResimMi).HasDefaultValue(false);
});
```

Run:
```
dotnet ef migrations add AddHaberMedyalar
dotnet ef database update
```

**Acceptance Criteria:** `HaberMedyalar` table exists with both FK constraints. Deleting a `MedyaKutuphanesi` record referenced by `HaberMedyalar` raises FK violation (confirming Restrict). `OncuResimMi` defaults to `false`.

---

#### TASK-2.7 - Set up ASP.NET Core Identity and seed the first admin user

**What:**
In `Program.cs`, BEFORE `builder.Build()`:
```csharp
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
```

Run:
```
dotnet ef migrations add AddIdentity
dotnet ef database update
```

Create `Data/DbSeeder.cs`:
```csharp
public static class DbSeeder
{
    public static async Task SeedAdminUserAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        const string adminUsername = "admin";
        if (await userManager.FindByNameAsync(adminUsername) != null)
        {
            logger.LogInformation("Admin user already exists. Skipping seed.");
            return;
        }

        var adminUser = new IdentityUser { UserName = adminUsername, Email = "admin@acikistihbarat.local" };
        var password = config["AdminSeed:Password"]!;
        var result = await userManager.CreateAsync(adminUser, password);

        if (result.Succeeded)
            logger.LogInformation("Admin user seeded successfully.");
        else
            logger.LogError("Failed to seed admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}
```

Call in `Program.cs` after `app.UseAuthorization()`:
```csharp
await DbSeeder.SeedAdminUserAsync(app.Services);
```

**Acceptance Criteria:** All 7 Identity tables exist in DB. `SELECT * FROM AspNetUsers` shows one row after first run. Subsequent runs log "Admin user already exists."

---

#### TASK-2.8 - Enable SQL Server Full-Text Search

**What:**
Create migration `AddFullTextSearch`. Use raw SQL (replace placeholder column/index names with real values from TASK-2.3):
```csharp
migrationBuilder.Sql(@"
    IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'AcikIstihbaratFTS')
        CREATE FULLTEXT CATALOG AcikIstihbaratFTS AS DEFAULT;

    IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Haber'))
    BEGIN
        -- Replace [HaberBasligi],[HaberSpotu],[HaberIcerigi] with real column names from TASK-2.3
        -- Replace PK_Haber with real PK index name
        CREATE FULLTEXT INDEX ON Haber([HaberBasligi],[HaberSpotu],[HaberIcerigi])
            KEY INDEX PK_Haber ON AcikIstihbaratFTS
            WITH CHANGE_TRACKING AUTO;
    END

    IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('MedyaKutuphanesi'))
    BEGIN
        CREATE FULLTEXT INDEX ON MedyaKutuphanesi(Baslik, AnahtarKelimeler)
            KEY INDEX PK_MedyaKutuphanesi ON AcikIstihbaratFTS
            WITH CHANGE_TRACKING AUTO;
    END
");
```

Also add a Down migration to drop the FTS index and catalog on rollback.

Run: `dotnet ef database update`

**PRE-CHECK:** Before running this migration, verify FTS is installed:
`SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')` -- must return 1.

**Acceptance Criteria:** `SELECT FULLTEXTCATALOGPROPERTY('AcikIstihbaratFTS', 'PopulateStatus')` returns 0 (idle = indexed). `SELECT TOP 5 * FROM Haber WHERE CONTAINS([HeadlineColumn], '"ekonomi"')` returns rows.

---

#### TASK-2.9 - Register all services in the DI container

**What:**
Create empty service interfaces and implementations (with constructor injection of `AppDbContext`, `IConfiguration` as needed):
- `Services/IHaberService.cs` + `Services/HaberService.cs`
- `Services/IKategoriService.cs` + `Services/KategoriService.cs`
- `Services/IMedyaService.cs` + `Services/MedyaService.cs`
- `Services/IAramaService.cs` + `Services/AramaService.cs`
- `Services/IDosyaService.cs` + `Services/DosyaService.cs`

Register all as `Scoped` in `Program.cs`:
```csharp
builder.Services.AddScoped<IHaberService, HaberService>();
builder.Services.AddScoped<IKategoriService, KategoriService>();
builder.Services.AddScoped<IMedyaService, MedyaService>();
builder.Services.AddScoped<IAramaService, AramaService>();
builder.Services.AddScoped<IDosyaService, DosyaService>();
```

**Acceptance Criteria:** `dotnet build` succeeds. No DI resolution errors on startup. Each service constructor-injectable into a controller without exceptions.

---

#### Phase 2 - Definition of Done
- [ ] `context.Haberler.CountAsync()` returns ~10,000
- [ ] `HaberKategorileri`, `MedyaKutuphanesi`, `HaberMedyalar` tables exist with correct schema
- [ ] All 7 Identity tables exist; one admin user seeded
- [ ] FTS catalog and indexes active; test `CONTAINS` query returns results
- [ ] `dotnet build` and `dotnet run` succeed with no errors
- [ ] All service classes registered and resolvable via DI
- [ ] Real `Haber` column names documented in `DEVLOG.md`


---

### PHASE 3 - API Business Logic & All Endpoints

**Goal:** Implement all service methods and controller endpoints. By end of phase, the complete API is functional and testable via Swagger/Postman with no frontend needed.
**Estimated Complexity:** High
**Prerequisites:** Phase 2 complete.

---

#### TASK-3.1 - Configure JWT authentication, CORS, and Kestrel request limits

**What:**
In `Program.cs`, add all middleware configuration BEFORE `builder.Build()`:

JWT Authentication:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
```

CORS (reads allowed origins from config - no hardcoding):
```csharp
builder.Services.AddCors(options => {
    options.AddPolicy("FrontendPolicy", policy => {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()!;
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});
```

Kestrel 50 MB limit:
```csharp
builder.WebHost.ConfigureKestrel(options => {
    options.Limits.MaxRequestBodySize = 52_428_800; // 50 MB
});
```

Middleware pipeline order (CRITICAL - wrong order causes subtle bugs):
```csharp
app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");   // CORS MUST come before UseAuthentication
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

**Acceptance Criteria:**
- `GET /api/admin/haberler` without token returns HTTP 401
- `GET /api/public/haberler` without token returns HTTP 200
- Fetch from `localhost:3000` does not get CORS error
- Fetch from `localhost:9999` gets CORS error

---

#### TASK-3.2 - Implement `AuthController` - JWT login endpoint

**What:**
Create `Controllers/Admin/AuthController.cs` with `[Route("api/admin/auth")]` and `[AllowAnonymous]`:

DTOs - create in `Models/DTOs/`:
```csharp
public class LoginDto { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
public class TokenResponseDto { public string Token { get; set; } = ""; public DateTime ExpiresAt { get; set; } }
```

`POST /api/admin/auth/login` implementation:
```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginDto dto)
{
    var user = await _userManager.FindByNameAsync(dto.Username);
    if (user == null) return Unauthorized(new { message = "Gecersiz kullanici adi veya sifre." });

    var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
    if (!result.Succeeded) return Unauthorized(new { message = "Gecersiz kullanici adi veya sifre." });

    var expiryMinutes = _config.GetValue<int>("Jwt:ExpiryMinutes", 480);
    var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
    var claims = new[] {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.UserName!),
        new Claim(ClaimTypes.Role, "Admin")
    };
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
    var token = new JwtSecurityToken(
        issuer: _config["Jwt:Issuer"],
        audience: _config["Jwt:Audience"],
        claims: claims,
        expires: expiresAt,
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
    );
    return Ok(new TokenResponseDto { Token = new JwtSecurityTokenHandler().WriteToken(token), ExpiresAt = expiresAt });
}
```

**Acceptance Criteria:** Correct credentials return a JWT. Wrong credentials return 401. Token decoded at jwt.io shows correct claims and 8-hour expiry.

---

#### TASK-3.3 - Implement `DosyaService` - GUID file naming + path validation

**What:**
Implement `IDosyaService` with three methods:

```csharp
public interface IDosyaService
{
    Task<(string fizikselDosyaAdi, string dosyaYolu, long dosyaBoyutu)> SaveFileAsync(IFormFile file);
    string GetFullPath(string fizikselDosyaAdi);
    void DeleteFile(string fizikselDosyaAdi);
}
```

Allowed MIME types (allowlist):
```csharp
private static readonly HashSet<string> AllowedMimeTypes = new() {
    "image/jpeg", "image/png", "image/gif", "image/webp",
    "application/pdf",
    "application/msword",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
};
```

`SaveFileAsync` implementation:
1. Validate `file.Length <= 52_428_800` (50 MB) - throw `ArgumentException` if over limit
2. Validate `file.ContentType` is in `AllowedMimeTypes` - throw `ArgumentException` if not
3. Generate: `string fizikselAd = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}"`
4. Resolve: `string basePath = _config["MedyaKutuphanesi:BasePath"]!`
5. Resolve: `string fullPath = Path.GetFullPath(Path.Combine(basePath, fizikselAd))`
6. Anti-traversal: `if (!fullPath.StartsWith(Path.GetFullPath(basePath) + Path.DirectorySeparatorChar)) throw new UnauthorizedAccessException("Gecersiz dosya yolu.");`
7. Save: `using var stream = File.Create(fullPath); await file.CopyToAsync(stream);`
8. Return: `(fizikselAd, fizikselAd, file.Length)`

`GetFullPath` implementation:
1. Resolve full path (same formula)
2. Anti-traversal check (same)
3. `if (!File.Exists(fullPath)) throw new FileNotFoundException("Dosya bulunamadi.", fizikselDosyaAdi);`
4. Return `fullPath`

`DeleteFile` implementation:
1. Resolve and anti-traversal check
2. `if (File.Exists(fullPath)) File.Delete(fullPath);` (idempotent - no exception if missing)

**Acceptance Criteria:**
- Valid JPEG upload creates `{GUID}_{filename}` file in folder
- `.exe` upload returns 400
- `../../appsettings.json` as filename throws `UnauthorizedAccessException`
- 51 MB file returns 400

---

#### TASK-3.4 - Implement `KategoriService` + public and admin controllers

**What:**
`IKategoriService` methods:
- `GetAllAsync()`: returns top-level categories (where `ParentId == null`) with `.Include(k => k.AltKategoriler).OrderBy(k => k.Sira)`
- `CreateAsync(KategoriCreateDto dto)`: creates and saves new `HaberKategori`
- `UpdateAsync(int id, KategoriUpdateDto dto)`: updates `Ad`, `ParentId`, `Sira`
- `DeleteAsync(int id)`: validates no `AltKategoriler` exist and no `Haber.HaberTuruID` references this ID, then deletes

DTOs:
```csharp
public class KategoriDto { public int Id; public string Ad; public int? ParentId; public int Sira; public List<KategoriDto> AltKategoriler = new(); }
public class KategoriCreateDto { public string Ad; public int? ParentId; public int Sira; }
public class KategoriUpdateDto { public string Ad; public int? ParentId; public int Sira; }
```

`Controllers/Public/KategoriController.cs` (route: `api/public/kategoriler`, `[AllowAnonymous]`):
- `GET /api/public/kategoriler` - calls `GetAllAsync()`, maps to `List<KategoriDto>`

`Controllers/Admin/KategoriAdminController.cs` (route: `api/admin/kategoriler`, `[Authorize]`):
- `GET /api/admin/kategoriler` - full list
- `POST /api/admin/kategoriler` - create, returns 201 with created resource
- `PUT /api/admin/kategoriler/{id}` - update, returns 200
- `DELETE /api/admin/kategoriler/{id}` - delete, returns 204 or 400 with error message

**Acceptance Criteria:** `GET /api/public/kategoriler` returns nested tree. `DELETE` fails with 400 if category has subcategories (not a 500 error).

---

#### TASK-3.5 - Implement `HaberService` - public read methods + thumbnail fallback logic

**What:**
`IHaberService` read methods with thumbnail fallback:

`GetFeaturedAsync(int count = 10)`:
```csharp
var haberler = await _context.Haberler
    .Where(h => h.HaberMansetmi == true)
    .OrderByDescending(h => h.[DateColumn]) // replace with real column name
    .Take(count)
    .Include(h => h.HaberMedyalar.Where(hm => hm.OncuResimMi && hm.Medya.Aktifmi))
        .ThenInclude(hm => hm.Medya)
    .ToListAsync();
return haberler.Select(h => MapToListDto(h)).ToList();
```

Thumbnail logic in `MapToListDto(Haber h)`:
```csharp
// OncuResimMi image from HaberMedyalar takes priority; legacy HaberResimAdresi as fallback
var oncuMedya = h.HaberMedyalar.FirstOrDefault(hm => hm.OncuResimMi);
string? thumbnailUrl = oncuMedya != null
    ? $"/api/public/medya/dosya/{oncuMedya.MedyaId}"
    : h.HaberResimAdresi; // legacy column fallback
```

`GetLatestAsync(int page, int pageSize)`: paginated, ordered by date desc. Return `PagedResult<HaberListDto>`.

`GetByKategoriAsync(int kategoriId, int page, int pageSize)`: filter by `HaberTuruID == kategoriId`, paginated.

`GetByIdAsync(int id)`: load full article with ALL associated media:
```csharp
var haber = await _context.Haberler
    .Include(h => h.HaberMedyalar)
        .ThenInclude(hm => hm.Medya)
    .FirstOrDefaultAsync(h => h.[PK] == id); // replace [PK] with real column
if (haber == null) return null;

// Filter inactive media
var activeMedya = haber.HaberMedyalar.Where(hm => hm.Medya.Aktifmi).ToList();
var gorseller = activeMedya.Where(hm => hm.Medya.DosyaTipi.StartsWith("image/"))
                           .OrderBy(hm => hm.Sira).ToList();
var belgeler = activeMedya.Where(hm => !hm.Medya.DosyaTipi.StartsWith("image/"))
                          .OrderBy(hm => hm.Sira).ToList();
// Fallback to legacy image if no HaberMedyalar images
string? legacyResim = gorseller.Count == 0 ? haber.HaberResimAdresi : null;
```

DTOs to create in `Models/DTOs/`:
```csharp
public class HaberListDto { int Id; string Baslik; string Spot; DateTime Tarih; int KategoriId; string? ThumbnailUrl; }
public class HaberDetailDto { int Id; string Baslik; string Spot; string HtmlIcerigi; DateTime Tarih; int KategoriId; string? LegacyResimAdresi; List<HaberMedyaDto> Gorseller; List<HaberMedyaDto> Belgeler; }
public class HaberMedyaDto { int Id; int MedyaId; string Baslik; string DosyaTipi; bool OncuResimMi; int Sira; string DosyaUrl; }
public class PagedResult<T> { List<T> Items; int TotalCount; int Page; int PageSize; int TotalPages; }
```

**Acceptance Criteria:** `GetFeaturedAsync()` never returns more than 10. `GetByIdAsync()` correctly separates images and documents. Inactive media excluded. Legacy `HaberResimAdresi` returned when no `HaberMedyalar` images exist.

---

#### TASK-3.6 - Implement `HaberService` - admin write methods

**What:**
Admin write methods:

`CreateAsync(HaberCreateDto dto, string username)`: create new `Haber` record using DTO fields mapped to real column names. Save, then return `HaberDetailDto`.

`UpdateAsync(int id, HaberUpdateDto dto)`: update existing record fields. Do NOT touch `HaberMedyalar` here (managed separately).

`DeleteAsync(int id)`: hard delete. EF Cascade removes `HaberMedyalar` join rows automatically. Does NOT delete `MedyaKutuphanesi` records or physical files.

`ToggleFeaturedAsync(int id)`: flip `HaberMansetmi`. Return new boolean value.

`AddMedyaToHaberAsync(int haberId, int medyaId, int sira, bool oncuResimMi)`:
```csharp
if (oncuResimMi)
{
    // Clear any existing OncuResimMi for this article first (enforce single featured image)
    var existing = await _context.HaberMedyalar
        .Where(hm => hm.HaberId == haberId && hm.OncuResimMi).ToListAsync();
    existing.ForEach(hm => hm.OncuResimMi = false);
}
_context.HaberMedyalar.Add(new HaberMedya { HaberId = haberId, MedyaId = medyaId, Sira = sira, OncuResimMi = oncuResimMi });
await _context.SaveChangesAsync();
```

`RemoveMedyaFromHaberAsync(int haberId, int medyaId)`: delete the `HaberMedya` join record only.

`ReorderMedyaAsync(int haberId, List<(int medyaId, int sira)> order)`: batch update `Sira` values.

DTOs:
```csharp
public class HaberCreateDto { string Baslik; string Spot; string HtmlIcerigi; int KategoriId; bool Mansetmi; }
public class HaberUpdateDto { string Baslik; string Spot; string HtmlIcerigi; int KategoriId; bool Mansetmi; }
public class HaberMedyaEkleDto { int MedyaId; int Sira; bool OncuResimMi; }
public class MediaReorderItem { int MedyaId; int Sira; }
```

**Acceptance Criteria:** Setting new `OncuResimMi = true` clears the previous one for the same article. Deleting an article removes `HaberMedyalar` rows but leaves `MedyaKutuphanesi` intact.

---

#### TASK-3.7 - Implement `MedyaService`

**What:**
`IMedyaService` methods:

`GetAllActiveAsync()`: `context.Medyalar.Where(m => m.Aktifmi).OrderByDescending(m => m.YuklenmeTarihi)`

`GetAllAsync()` (admin - includes soft-deleted): `context.Medyalar.OrderByDescending(m => m.YuklenmeTarihi)`

`GetByIdAsync(int id)`: returns single record regardless of `Aktifmi`.

`GetFilePathAsync(int id)`: fetch `FizikselDosyaAdi` from DB, call `_dosyaService.GetFullPath(fizikselDosyaAdi)`.

`UploadAsync(MedyaUploadDto dto, IFormFile file, string username)`:
1. Call `_dosyaService.SaveFileAsync(file)` - let exceptions propagate (400 handled by controller)
2. Create `Medya` entity with all fields populated from result and dto
3. `YuklenmeTarihi = DateTime.UtcNow`
4. Save to DB, return `MedyaDto`

`UpdateMetadataAsync(int id, MedyaUpdateDto dto)`: update only `Baslik`, `Aciklama`, `AnahtarKelimeler`. Never touch path/file columns.

`SoftDeleteAsync(int id)`: set `Aktifmi = false`. Save. Physical file untouched. `HaberMedyalar` records untouched.

`RestoreAsync(int id)`: set `Aktifmi = true`. Save.

`MedyaDto` include computed `DosyaUrl = $"/api/public/medya/dosya/{Id}"`.

**Acceptance Criteria:** Upload creates GUID-prefixed physical file and DB record. Soft delete sets `Aktifmi = false` without touching file. Restore sets `Aktifmi = true`.

---

#### TASK-3.8 - Implement `AramaService` using Full-Text Search

**What:**
```csharp
public async Task<SearchSonucDto> SearchAsync(string query)
{
    if (string.IsNullOrWhiteSpace(query))
        return new SearchSonucDto { Haberler = new(), Medyalar = new(), ToplamSonuc = 0 };

    // Sanitize: remove double quotes to prevent FTS injection, then wrap in quotes for phrase search
    var sanitized = query.Replace("\"", "").Trim();
    var ftsQuery = $"\"{sanitized}\"";

    // Run both searches in parallel
    var haberTask = _context.Haberler
        .FromSqlRaw($"SELECT * FROM Haber WHERE CONTAINS(([HeadlineCol],[SpotCol],[BodyCol]), {{0}})", ftsQuery)
        .Take(50)
        .ToListAsync();
    var medyaTask = _context.Medyalar
        .FromSqlRaw("SELECT * FROM MedyaKutuphanesi WHERE CONTAINS((Baslik,AnahtarKelimeler), {0}) AND Aktifmi = 1", ftsQuery)
        .Take(20)
        .ToListAsync();

    await Task.WhenAll(haberTask, medyaTask);

    var haberDtos = (await haberTask).Select(MapToListDto).ToList();
    var medyaDtos = (await medyaTask).Select(MapToMedyaDto).ToList();

    return new SearchSonucDto {
        Haberler = haberDtos,
        Medyalar = medyaDtos,
        ToplamSonuc = haberDtos.Count + medyaDtos.Count
    };
}
```

Replace `[HeadlineCol],[SpotCol],[BodyCol]` with real Haber column names from TASK-2.3.

`SearchSonucDto`:
```csharp
public class SearchSonucDto { public List<HaberListDto> Haberler; public List<MedyaDto> Medyalar; public int ToplamSonuc; }
```

**Acceptance Criteria:** `GET /api/public/ara?q=ekonomi` returns results in under 500ms. Empty `q` returns 200 with empty lists. Special characters in query do not cause SQL errors.

---

#### TASK-3.9 - Implement all public API controllers

**What:**
All controllers: `[ApiController]`, `[AllowAnonymous]`.

`Controllers/Public/HaberController.cs` (route: `api/public/haberler`):
- `GET /api/public/haberler?page=1&pageSize=12` -> `GetLatestAsync(page, pageSize)` -> 200
- `GET /api/public/haberler/manset` -> `GetFeaturedAsync(10)` -> 200
- `GET /api/public/haberler/{id:int}` -> `GetByIdAsync(id)` -> 200 or 404
- `GET /api/public/haberler/kategori/{kategoriId:int}?page=1&pageSize=12` -> `GetByKategoriAsync(...)` -> 200

`Controllers/Public/MedyaController.cs` (route: `api/public/medya`):
- `GET /api/public/medya` -> `GetAllActiveAsync()` -> 200
- `GET /api/public/medya/dosya/{id:int}` -> stream file:
  ```csharp
  var medya = await _medyaService.GetByIdAsync(id);
  if (medya == null || !medya.Aktifmi) return NotFound();
  var fullPath = await _medyaService.GetFilePathAsync(id);
  var contentType = medya.DosyaTipi;
  var disposition = medya.DosyaTipi.StartsWith("image/") ? "inline" : "attachment";
  Response.Headers.Append("Content-Disposition", $"{disposition}; filename=\"{medya.OrijinalDosyaAdi}\"");
  return PhysicalFile(fullPath, contentType);
  ```

`Controllers/Public/AramaController.cs` (route: `api/public/ara`):
- `GET /api/public/ara?q={query}` -> `SearchAsync(q)` -> 200

`Controllers/Public/KategoriController.cs` (route: `api/public/kategoriler`):
- `GET /api/public/kategoriler` -> `GetAllAsync()` -> 200

**Acceptance Criteria:** All endpoints return 200 + correct JSON from Swagger without a token. File streaming returns correct MIME type and binary. Non-existent IDs return 404.

---

#### TASK-3.10 - Implement all admin API controllers

**What:**
All controllers: `[Authorize]`, `[ApiController]`.

`Controllers/Admin/HaberAdminController.cs` (route: `api/admin/haberler`):
- `GET /api/admin/haberler?page=1&pageSize=20` -> 200 paginated
- `GET /api/admin/haberler/{id}` -> 200 or 404
- `POST /api/admin/haberler` -> `CreateAsync(dto, User.Identity!.Name!)` -> 201 Created with location header
- `PUT /api/admin/haberler/{id}` -> `UpdateAsync(id, dto)` -> 200
- `DELETE /api/admin/haberler/{id}` -> `DeleteAsync(id)` -> 204 No Content
- `PATCH /api/admin/haberler/{id}/manset` -> `ToggleFeaturedAsync(id)` -> 200 with `{ "mansetmi": true/false }`
- `POST /api/admin/haberler/{id}/medya` -> `AddMedyaToHaberAsync(id, dto)` -> 200
- `DELETE /api/admin/haberler/{haberId}/medya/{medyaId}` -> `RemoveMedyaFromHaberAsync(...)` -> 204
- `PUT /api/admin/haberler/{id}/medya/sirala` -> `ReorderMedyaAsync(id, dto)` -> 200

`Controllers/Admin/MedyaAdminController.cs` (route: `api/admin/medya`):
- `GET /api/admin/medya` -> `GetAllAsync()` (includes soft-deleted) -> 200
- `POST /api/admin/medya/yukle` -> accepts `[FromForm] MedyaUploadDto dto, IFormFile file`. Add `[RequestSizeLimit(52_428_800)]`. Calls `UploadAsync(dto, file, User.Identity!.Name!)` -> 201
- `PUT /api/admin/medya/{id}` -> `UpdateMetadataAsync(id, dto)` -> 200
- `DELETE /api/admin/medya/{id}` -> `SoftDeleteAsync(id)` -> 204
- `PATCH /api/admin/medya/{id}/restore` -> `RestoreAsync(id)` -> 200

`Controllers/Admin/KategoriAdminController.cs` (route: `api/admin/kategoriler`):
- Full CRUD as defined in TASK-3.4

**Acceptance Criteria:** All admin endpoints return 401 without token. With token: full CRUD works. File upload via multipart/form-data creates physical file and DB record. `PATCH /manset` toggles correctly.

---

#### TASK-3.11 - Configure Swagger with JWT authorization

**What:**
In `Program.cs`, configure Swagger (Development only):
```csharp
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AcikIstihbarat API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "JWT token giriniz: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AcikIstihbarat API v1"));
}
```

**Acceptance Criteria:** `/swagger` shows all endpoints organized by controller. "Authorize" button in Swagger UI accepts a Bearer token. Admin endpoints testable directly from Swagger UI after pasting token.

---

#### Phase 3 - Definition of Done
- [ ] `GET /api/public/haberler/manset` returns <=10 articles without a token
- [ ] `GET /api/public/ara?q=ekonomi` returns results in under 500ms
- [ ] `POST /api/admin/auth/login` returns a valid JWT token
- [ ] All admin endpoints return 401 without token, correct data with valid token
- [ ] File upload creates GUID-prefixed physical file and DB record
- [ ] File streaming endpoint delivers correct binary with correct MIME type
- [ ] Soft delete sets `Aktifmi = false` without touching physical file
- [ ] Zero entity classes exposed directly from any controller - only DTOs


---

### PHASE 4 - Next.js Public Frontend Implementation

**Goal:** Build the SEO-friendly public face of the news portal using Next.js App Router and Tailwind CSS, fetching data from the public API endpoints.
**Estimated Complexity:** Medium
**Prerequisites:** Phase 3 complete. API must be running.

---

#### TASK-4.1 - Setup global API client configuration

**What:**
Create `lib/apiClient.ts`:
```typescript
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api/public";

export async function fetchApi<T>(endpoint: string, options?: RequestInit): Promise<T | null> {
    try {
        const url = `${API_BASE_URL}${endpoint}`;
        const res = await fetch(url, { ...options, next: { revalidate: 60, ...options?.next } });
        if (!res.ok) {
            console.error(`API Error: ${res.status} on ${url}`);
            return null; // Return null on error so Next.js doesn't crash the whole page
        }
        return await res.json() as T;
    } catch (err) {
        console.error("Fetch failed:", err);
        return null;
    }
}
```

Create `.env.local`:
```
NEXT_PUBLIC_API_URL=http://localhost:5000/api/public
```

**Acceptance Criteria:** `fetchApi` wraps native fetch with default 60-second revalidation (ISR) and swallows non-200 responses returning `null`.

---

#### TASK-4.2 - Layout and Navigation Bar component

**What:**
Create `components/Navbar.tsx`:
1. Uses `fetchApi` to load `KategoriDto[]` from `/kategoriler`.
2. Map over categories to create navigation links. Preline UI dropdown used for `AltKategoriler`.
3. Add a search input box in navbar that redirects to `/ara?q={value}` on submit.

Update `app/layout.tsx`:
Add `<Navbar />` to the top of the body. Add `<Footer />` (static placeholder) to bottom. Ensure `main` content has `min-h-screen`.

**Acceptance Criteria:** Top-level categories visible in navbar. Nested categories appear in Preline dropdowns. Search redirects correctly. Navbar renders on every page.

---

#### TASK-4.3 - Homepage (`/`) - Featured and Latest News

**What:**
Update `app/page.tsx` (Server Component):
1. Parallel fetch:
   ```typescript
   const [mansetler, enYeniler] = await Promise.all([
       fetchApi<HaberListDto[]>("/haberler/manset"),
       fetchApi<PagedResult<HaberListDto>>("/haberler?page=1&pageSize=12")
   ]);
   ```
2. Render `mansetler` (Featured) using a Preline Carousel component at the top of the page. Display `Baslik` over the image. Use `ThumbnailUrl` (or `null` placeholder if missing).
3. Render `enYeniler.Items` (Latest) as a CSS Grid of cards below the carousel. Each card shows image, `Baslik`, `Tarih` formatted, and a link to `/haber/{id}`.

**Acceptance Criteria:** Homepage loads in under 2 seconds. Carousel works (Preline initialized correctly). Grid responsive (1 col mobile, 3 cols desktop).

---

#### TASK-4.4 - Category Page (`/kategori/[id]`)

**What:**
Create `app/kategori/[id]/page.tsx` (Server Component):
1. Read `id` from `params`, `page` from `searchParams`.
2. Fetch `/haberler/kategori/{id}?page={page}&pageSize=12`.
3. Display items as a CSS Grid of cards.
4. Implement simple Next/Prev pagination links at the bottom based on `TotalPages`.

**Acceptance Criteria:** Clicking a category in navbar loads this page. URL `?page=2` correctly fetches page 2 data.

---

#### TASK-4.5 - Article Detail Page (`/haber/[id]`) - Core layout and Image Slider

**What:**
Create `app/haber/[id]/page.tsx` (Server Component):
1. Fetch `/haberler/{id}`. If `null`, `notFound()`.
2. Render page title `<h1>` mapped to `Baslik`, and `<h2>` mapped to `Spot`.
3. Media display logic at the top:
   - If `Gorseller.length > 1`: Render Preline Carousel slider.
   - If `Gorseller.length == 1`: Render standard `<img>` as hero image.
   - If `Gorseller.length == 0` AND `LegacyResimAdresi` exists: Render `<img>` using legacy URL.
   - If none: Render nothing at top.

**Acceptance Criteria:** Correct combination of images is rendered based on array length. Legacy image displays if no new media is attached.

---

#### TASK-4.6 - Article Detail Page - Rich Text and Documents

**What:**
In `app/haber/[id]/page.tsx`:
1. Use `isomorphic-dompurify` to sanitize `HtmlIcerigi` before rendering:
   ```typescript
   import DOMPurify from 'isomorphic-dompurify';
   const cleanHtml = DOMPurify.sanitize(haber.htmlIcerigi);
   // render with dangerouslySetInnerHTML={{ __html: cleanHtml }}
   ```
2. Display Documents: If `Belgeler.length > 0`, render a right-hand sidebar or bottom section titled "Ekli Belgeler".
3. List documents as links with a download icon (Preline icon), using the `DosyaUrl` from DTO. Add `target="_blank"` and `download` attribute where applicable.

**Acceptance Criteria:** TipTap HTML from DB renders correctly without script execution (XSS test: insert `<script>alert(1)</script>` into DB manually; verify it is stripped). Documents list is clickable and triggers file download from API.

---

#### TASK-4.7 - Search Results Page (`/ara`)

**What:**
Create `app/ara/page.tsx` (Server Component):
1. Read `q` from `searchParams`.
2. Fetch `/ara?q={q}`.
3. If `q` is empty, show "Lütfen arama kelimesi girin."
4. If results: show "Haber Sonuçları" section (grid of cards) and "Medya Sonuçları" section (list of matching images/documents with links).

**Acceptance Criteria:** Search for "ekonomi" returns matching articles and media.

---

#### Phase 4 - Definition of Done
- [ ] Navbar loads categories from API
- [ ] Homepage renders slider + latest news
- [ ] Detail page renders slider for multiple images, hero for single, legacy for none
- [ ] Detail page HTML content is sanitized via DOMPurify
- [ ] Detail page lists all attached documents as downloadable links
- [ ] Pagination works on category pages
- [ ] Next.js `next/image` is NOT used for external API images to avoid complex config unless explicitly required (standard `<img>` with Tailwind `object-cover` used).

---

### PHASE 5 - React/Vite Admin Panel Implementation

**Goal:** Build a secure, isolated SPA for content creators to manage articles, categories, and the media library.
**Estimated Complexity:** High
**Prerequisites:** Phase 3 complete. API must be running.

---

#### TASK-5.1 - Setup Axios instance with JWT Interceptor and Auth Context

**What:**
Create `services/api.ts`:
1. Create Axios instance pointing to `http://localhost:5000/api/admin`.
2. Add request interceptor: reads `token` from `localStorage`, appends `Authorization: Bearer {token}` to headers.
3. Add response interceptor: if 401 Unauthorized, call `localStorage.removeItem('token')` and redirect to `/login` via `window.location`.

Create `context/AuthContext.tsx`:
1. State: `isAuthenticated` (boolean), `user` (object from decoded JWT).
2. `login(username, password)` function: POST `/auth/login`, save token, update state.
3. `logout()` function: remove token, update state.

**Acceptance Criteria:** `axios.get('/haberler')` automatically sends token. Expired token automatically forces logout and redirect.

---

#### TASK-5.2 - Create Admin Layout, Login Page, and Protected Routes

**What:**
1. Create `pages/Login.tsx`: simple username/password form using Preline form classes. On submit, call `authContext.login()`. On success, redirect to `/`.
2. Create `components/AdminLayout.tsx`:
   - Left sidebar: Links to "Haber Yönetimi", "Kategori Yönetimi", "Medya Kütüphanesi".
   - Top navbar: User info and "Çıkış Yap" (Logout) button.
   - Main content area: `<Outlet />` (react-router).
3. Create `App.tsx` routes:
   - `<Route path="/login" element={<Login />} />`
   - `<Route element={<ProtectedRoute><AdminLayout /></ProtectedRoute>}>` (wraps all other routes)

**Acceptance Criteria:** Unauthenticated user navigating to `/haberler` is redirected to `/login`. Admin login succeeds and shows dashboard.

---

#### TASK-5.3 - Kategori Management UI

**What:**
Create `pages/KategoriList.tsx`:
1. Fetch and display categories in a table.
2. Add "Yeni Kategori" button opening a Preline Modal.
3. Form inside modal: `Ad` (text), `ParentId` (select dropdown populated from top-level categories), `Sira` (number).
4. Save calls `POST` or `PUT` based on mode.
5. "Sil" (Delete) button calls `DELETE`, catches 400 error and displays it (e.g. "Alt kategorisi olan silinemez").

**Acceptance Criteria:** Can create, edit, delete categories. UI reflects changes immediately without full page reload.

---

#### TASK-5.4 - TipTap Rich Text Editor Component

**What:**
Create `components/RichTextEditor.tsx`:
1. Initialize TipTap with `StarterKit`, `Link`, `Image`.
2. Create a formatting toolbar above the editor: Bold, Italic, H2, H3, Bullet List, Link.
3. Use Tailwind `prose` class for the editor content area to ensure it looks like the final output.
4. Pass `content` in as prop, emit `onChange(html)` when content updates.

**Acceptance Criteria:** Editor loads. Can make text bold. Output HTML is clean.

---

#### TASK-5.5 - Haber CRUD - List and Create/Edit Form (Text Only)

**What:**
Create `pages/HaberList.tsx`: Data table with server-side pagination, search input. Columns: ID, Başlık, Tarih, Manşet?. Edit/Delete actions.
Create `pages/HaberForm.tsx` (handles both Create and Edit):
1. Fields: `Baslik`, `Spot` (textarea), `KategoriId` (dropdown), `Mansetmi` (checkbox), `HtmlIcerigi` (the `RichTextEditor`).
2. Submit calls `POST` or `PUT`. On success, redirect to list.

**Acceptance Criteria:** Can create a new text-only news article and see it in the list.

---

#### TASK-5.6 - Medya Kütüphanesi Management UI

**What:**
Create `pages/MedyaList.tsx`:
1. Grid of media items. Show thumbnail for images, icon for documents.
2. "Yeni Yükle" button opens modal with file input.
3. Form submits via `FormData` (`multipart/form-data`) to `/medya/yukle`.
4. File input has client-side validation for `size <= 50MB` and `AllowedMimeTypes`. Shows visual error if failed.
5. Clicking an item opens details modal to edit `Baslik`, `AnahtarKelimeler`.
6. "Sil" button soft-deletes the media.

**Acceptance Criteria:** 51MB file rejected by UI instantly. 5MB image uploads successfully, appears in grid, and thumbnail renders.

---

#### TASK-5.7 - Haber-Medya Association UI (The final integration)

**What:**
In `pages/HaberForm.tsx` (Edit mode only - save article first before attaching media):
1. Add a "Medyalar" tab or section at the bottom.
2. Left side: "Medyalarım" - grid of attached media. For each image, show radio button for "Öncü Resim Yap" (`OncuResimMi`). Show "Sırayı Değiştir" input. Show "Kaldır" button.
3. Right side: "Medya Seç" - searchable modal/panel pulling from `MedyaList`. Click to attach to the article.
4. Attaching calls `POST /haberler/{id}/medya`. Removing calls `DELETE /haberler/{id}/medya/{medyaId}`. Setting Öncü Resim calls update API.

**Acceptance Criteria:** Can attach 3 images and 1 PDF to an article. Can set one image as Öncü Resim. Order changes save correctly. All changes reflect correctly on the Next.js public detail page.

---

#### Phase 5 - Definition of Done
- [ ] Login screen prevents unauthorized access.
- [ ] CRUD operations work for Categories, News, and Media.
- [ ] TipTap editor produces HTML and updates state.
- [ ] File uploader correctly posts `FormData` and rejects files > 50MB client-side.
- [ ] Article form allows selecting existing media, assigning one as featured, and ordering them.
- [ ] Public site updates perfectly when admin changes are saved (verifying full loop).

---
**END OF PLAN**
