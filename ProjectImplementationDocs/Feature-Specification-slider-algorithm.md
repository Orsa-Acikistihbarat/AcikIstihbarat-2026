# Feature-Specification-slider-algorithm

**Date:** 2026-08-01
**Author:** (workshop between user and Think agent)
**Status:** Draft

---

## 1. Overview

The main slider (manset) on the front page currently only displays items from the `Haber` (News) table that have the `Mansetmi` flag set to true. To ensure the slider always feels fresh and fully populated—even during slow news cycles—we are changing the backend algorithm. The new system will dynamically fill up to 12 slider slots by first grabbing any News marked as "Manset" published within the last 10 days. Any remaining empty slots will be backfilled with randomly selected Articles (`Yazi`), ensuring maximum content utilization and keeping readers engaged.

---

## 2. Goals & Non-Goals

### Goals
- Ensure the slider always attempts to display the maximum number of items (currently 12).
- Prioritize recent "Manset" News (published within the last 10 days).
- Backfill remaining capacity using randomly selected Articles (`Yazi`).
- Unify the data structure sent to the frontend so the UI can easily route users to either `/haber/:id` or `/yazi/:id`.
- Show the Author's Name as the badge label when an Article is displayed in the slider.

### Non-Goals
- We are not changing the visual design of the slider itself, only the data feeding it.
- We are not changing the behavior of the "Son Haberler" (Latest News) grid.

---

## 3. User Stories

- As a **Reader**, I want to see the most recent top news in the slider, so that I stay informed on current events.
- As a **Reader**, I want to see interesting, randomly selected articles in the slider if the news is slow, so that I have engaging content to discover.
- As an **Admin**, I want the system to automatically handle slider backfilling, so that I don't have to manually ensure exactly 12 items are always marked as "Manset".

---

## 4. Functional Requirements

| # | Requirement | Priority |
|---|-------------|----------|
| FR-1 | The backend `/Haberler/manset` endpoint must return a unified `SliderItemDto` list. | Must Have |
| FR-2 | The endpoint must query `Haber` records where `Mansetmi == true` AND `Tarih >= [10 days ago]`. | Must Have |
| FR-3 | The endpoint must calculate `RemainingSlots = 12 - [Recent Manset Count]`. | Must Have |
| FR-4 | If `RemainingSlots > 0`, query the `Yazi` table to randomly select that many articles. | Must Have |
| FR-5 | The unified DTO must include a `Tip` (Type) flag (e.g., `"Haber"` or `"Yazi"`). | Must Have |
| FR-6 | The unified DTO must include a `BadgeLabel` property (Category name for News, Author name for Articles). | Must Have |
| FR-7 | The frontend slider must read the `Tip` flag to generate the correct href link (`/haber/{id}` vs `/yazi/{id}`). | Must Have |

---

## 5. Non-Functional Requirements

| # | Requirement | Category |
|---|-------------|----------|
| NFR-1 | The random selection query on `Yazi` should be optimized (e.g., using `ORDER BY NEWID()`) to maintain fast API response times. | Performance |

---

## 6. Chosen Technical Approach

**Architecture decision:** Server-Side Aggregation
We will update the backend `HaberService.cs` to perform all necessary queries and math. It will return a new `SliderItemDto` that abstracts away the differences between a News record and an Article record. 

**Key technologies / patterns:**
- **Entity Framework Core**: We will use EF Core's `ORDER BY NEWID()` equivalent (`EF.Functions.Random()` or direct SQL) to fetch random rows efficiently.
- **Unified DTO Pattern**: By mapping two different database entities (`Haber` and `Yazi`) to a single `SliderItemDto`, the frontend remains incredibly thin and ignorant of complex business logic.

**Fits into existing stack because:** 
This adheres strictly to our current layered architecture. The `HaberService` encapsulates the business rules, and the `Next.js` frontend simply consumes the JSON representation.

---

## 7. Data Model 

We will introduce a new Data Transfer Object (DTO) in the C# backend:
`SliderItemDto`
- `Id` (integer)
- `Tip` (string - "Haber" or "Yazi")
- `Baslik` (string)
- `Spot` (string - used for the short summary)
- `Tarih` (datetime)
- `ThumbnailUrl` (string)
- `BadgeLabel` (string - populated with KategoriAd for News, and YazarAd for Articles)

---

## 8. UI / Layout Notes 

The `app/page.tsx` slider component will be updated to:
1. Fetch `SliderItemDto[]` instead of `HaberListesiItem[]`.
2. Change the `href` on the `Link` element dynamically: `href={item.tip === 'Haber' ? '/haber/' + item.id : '/yazi/' + item.id}`
3. Change the badge span to display `{item.badgeLabel}`.

---

## 9. Suggested Next Steps

1. Review this spec to ensure it accurately captures the desired functionality.
2. Instruct the agent to create an **Implementation Plan** based on this specification, then proceed to development.
