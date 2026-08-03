# Feature Specification: Full-Text Search Optimization

**Date:** 2026-08-01
**Author:** (workshop between user and Think agent)
**Status:** Draft

---

## 1. Overview

Currently, the search functionality (`AramaService.cs`) uses Entity Framework's `.Contains()` method to search across multiple columns. This translates to `LIKE '%keyword%'` queries in SQL Server, which cannot utilize standard indexes and results in slow Full Table Scans. To optimize performance, we will implement SQL Server's native **Full-Text Search (FTS)**. Additionally, we are changing the scope of the search: Media (`MedyaKutuphanesi`) will no longer be searched, and Articles (`Yazi`) will be added to the search results alongside News (`Haber`).

---

## 2. Goals & Non-Goals

### Goals
- Drastically improve search performance by eliminating `LIKE '%...'` full table scans.
- Enable Full-Text Search catalogs and indexes for the `Haber` and `Yazi` tables in SQL Server.
- Remove Media search from the API and frontend.
- Add Article (`Yazi`) search to the API and frontend.
- Utilize Turkish language analyzers in SQL Server FTS if applicable.

### Non-Goals
- We are not implementing a third-party search engine like Elasticsearch.
- We are not changing the visual design of the search results page, other than accommodating the new "Yazilar" section.

---

## 3. User Stories

- As a **Reader**, I want my search queries to return results instantly, so I don't have to wait for the page to load.
- As a **Reader**, I want to see both News and Author Articles in my search results so I get a complete picture of the site's content.
- As an **Admin**, I want the database to use efficient indexing so that the server CPU is not overwhelmed by simple search queries.

---

## 4. Functional Requirements

| # | Requirement | Priority |
|---|-------------|----------|
| FR-1 | Create an EF Core Migration containing raw SQL to create a `FULLTEXT CATALOG`. | Must Have |
| FR-2 | The migration must create a `FULLTEXT INDEX` on the `Haber` table (columns: `HaberBaslik`, `HaberOnIzlemeMetni`, `HaberTamMetin` or equivalent). | Must Have |
| FR-3 | The migration must create a `FULLTEXT INDEX` on the `Yazi` table (columns: `YaziBaslik`, `YaziOnIzlemeMetni`, `YaziTamMetin`). | Must Have |
| FR-4 | Update `AramaSonucDto.cs` to remove `Medyalar` and add `Yazilar` (`PagedResult<SliderItemDto>` or generic equivalent). | Must Have |
| FR-5 | Update `AramaService.cs` to execute FTS queries using `EF.Functions.FreeText()` instead of `.Contains()`. | Must Have |
| FR-6 | Update the Next.js `app/arama/page.tsx` to display both `Haberler` and `Yazilar` in the search results UI. | Must Have |

---

## 5. Non-Functional Requirements

| # | Requirement | Category |
|---|-------------|----------|
| NFR-1 | The database migration must explicitly define a unique key index (e.g., `PK_Haber`) for the Full-Text Index to bind to. | Architecture |
| NFR-2 | The FTS language should be set to Turkish to support proper word stemming (Language Code 1055). | Localization |

---

## 6. Chosen Technical Approach

**Architecture decision:** SQL Server Full-Text Search (Native)
We will leverage SQL Server's built-in FTS. Because EF Core does not have a fluent API for generating FTS objects, we will generate an empty migration (`AddFullTextSearch`) and inject raw `migrationBuilder.Sql(...)` commands into the `Up` and `Down` methods. 

**Querying:** We will use `EF.Functions.FreeText(h.Baslik, searchTerm)` or `EF.Functions.Contains(...)` in our LINQ queries. This delegates the semantic parsing to SQL Server.

---

## 7. Data Model 

**`AramaSonucDto` changes:**
```csharp
public class AramaSonucDto
{
    public PagedResult<HaberListDto> Haberler { get; set; }
    // Replaced Medyalar with Yazilar
    public PagedResult<SliderItemDto> Yazilar { get; set; } 
}
```

---

## 8. Suggested Next Steps

1. Review this spec to ensure it accurately captures the desired functionality.
2. Instruct the main agent to create an **Implementation Plan** based on this specification, paying close attention to the raw SQL required for the EF Core migration.
