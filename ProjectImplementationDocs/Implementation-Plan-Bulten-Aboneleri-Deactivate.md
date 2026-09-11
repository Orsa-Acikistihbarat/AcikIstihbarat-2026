# Implementation Plan: Deactivate icon on "Bültenlere Kimler Abone Oldu" page

## Goal
Add a deactivate action icon to each row of the admin "Bültenlere Kimler Abone Oldu"
subscribers table. Clicking it shows a confirm dialog, then calls a new backend
endpoint that sets `IsActive=false` + `UnsubscribedAt=UtcNow` for that subscriber row.
Already-deactivated rows show a disabled/muted icon instead (no action).

Confirmed exact current state of all touched files (read directly, 2026-09-11):
- `MailSubscriberAdminItem` DTO currently has only `Email`, `NewsletterDisplayName`,
  `SubscriptionDate`, `UnsubscriptionDate` — no `Id`/`IsActive` yet.
- `MailAdminController` has exactly one action (`GetSubscribers`); no PUT/PATCH exists yet.
- ASP.NET Core's default controller JSON serialization is camelCase (no custom `JsonOptions`
  in Program.cs), so `Id`→`id`, `IsActive`→`isActive` on the wire — matches frontend's
  existing camelCase reads.
- Existing delete/confirm pattern verified in `HaberlerList.tsx`'s `handleDelete`:
  `window.confirm(...)` → try/catch `api.delete(...)` → refetch → `alert('...başarısız.')`
  on catch. This plan follows the identical shape for deactivate.

## Phase 1 — Backend DTO + read endpoint
1. In `AcikIstihbarat.API/Models/DTOs/MailSubscriberAdminDtos.cs`, add
   `public int Id { get; set; }` and `public bool IsActive { get; set; }` to
   `MailSubscriberAdminItem`.
2. In `AcikIstihbarat.API/Controllers/Admin/MailAdminController.cs`, `GetSubscribers`'s
   `.Select(...)` projection: add `Id = s.Id,` and `IsActive = s.IsActive,`. No change to
   `.Where`/`.OrderByDescending` — still returns both active and inactive rows.

## Phase 2 — Backend deactivate endpoint (depends on Phase 1)
3. In the same `MailAdminController`, add a private static mapping helper first, then the
   new action:
   - `private static MailSubscriberAdminItem ToAdminItem(MailSubscriber s) => new() { Id = s.Id,
     Email = s.Email, NewsletterDisplayName = NewsletterDisplayNames.Resolve(s.TemplateBaseName),
     SubscriptionDate = s.LastConfirmedAt, UnsubscriptionDate = s.UnsubscribedAt,
     IsActive = s.IsActive };`
   - Refactor `GetSubscribers`'s inline projection (from step 2) to `.Select(s => ToAdminItem(s))`
     — **caveat**: EF Core needs to translate this into SQL; if it fails to translate the
     static-method call, keep the inline `.Select(...)` initializer for the EF query as-is,
     and only reuse `ToAdminItem` for the non-EF, in-memory mapping in step 3e below
     (post-materialization). Prefer this safer fallback if translation issues arise — don't
     fight EF's query translator for the sake of DRY.
   - Add the new action directly after `GetSubscribers`:
     - Route/verb: `[HttpPut("subscribers/{id:int}/deactivate")]` →
       `PUT /api/admin/mail/subscribers/{id}/deactivate`.
     - Signature: `public async Task<IActionResult> DeactivateSubscriber(int id, CancellationToken ct)`.
     - Body logic, in order:
       a. `var subscriber = await _db.MailSubscribers.FirstOrDefaultAsync(s => s.Id == id, ct);`
       b. `if (subscriber is null) return NotFound();`
       c. If `subscriber.IsActive` is true: set `IsActive = false`, `UnsubscribedAt = DateTime.UtcNow`,
          then `await _db.SaveChangesAsync(ct);` — mirrors exactly `MailController.UnsubscribeConfirmed`'s
          mutation (`AcikIstihbarat.API/Controllers/Public/MailController.cs`).
       d. If already inactive on entry, skip mutation entirely (idempotent no-op — don't
          overwrite existing `UnsubscribedAt`).
       e. Return `Ok(ToAdminItem(subscriber))`.
     - No new `[Authorize]` needed — already applied at class level. No request body needed
       (id from route).

## Phase 3 — Frontend (depends on Phase 1 + 2)
4. In `acik-istihbarat-admin/src/pages/BultenAboneleri.tsx`, extend `SubscriberRow` interface:
   add `id: number;` and `isActive: boolean;`.
5. Import `Ban` from `lucide-react`: `import { Ban } from 'lucide-react';`.
6. Add `handleDeactivate` function above the `return`:
   - Confirm text: `window.confirm('Bu e-postanın bülten aboneliğini iptal etmek istediğinize emin misiniz?')`.
   - On confirm: `try { await api.put(\`/mail/subscribers/${id}/deactivate\`); fetchSubscribers(); }
     catch (error) { alert('İşlem başarısız.'); }`.
7. Table header: add a 5th `<th>İşlemler</th>` after "Abonelik İptal Tarihi", same classes as
   siblings.
8. Update both `colSpan={4}` placeholder rows (loading/empty state) to `colSpan={5}`.
9. In `rows.map(...)`, add a 5th `<td className="px-6 py-4 whitespace-nowrap text-sm text-center">`:
   - If `r.isActive`: `<button onClick={() => handleDeactivate(r.id)} title="Aboneliği İptal Et"
     className="inline-flex text-red-600 hover:text-red-800"><Ban className="size-4" /></button>`.
   - Else: `<span title="Zaten pasif" className="inline-flex text-gray-300 cursor-not-allowed">
     <Ban className="size-4" /></span>` — no `onClick`, non-focusable.

## Relevant files
- `AcikIstihbarat.API/Models/DTOs/MailSubscriberAdminDtos.cs` — add Id, IsActive
- `AcikIstihbarat.API/Controllers/Admin/MailAdminController.cs` — shared `ToAdminItem` helper,
  projection update, new PUT endpoint
- `acik-istihbarat-admin/src/pages/BultenAboneleri.tsx` — interface, icon import, handler,
  header/column, row rendering

## Verification
1. `dotnet build` in AcikIstihbarat.API.
2. Bring up local docker stack (db/api/admin), open the page, confirm active row → click icon
   → confirm dialog → row updates (icon turns gray, date populates) after refetch.
3. Confirm already-inactive rows render muted/non-clickable from initial load.
4. Call the endpoint with a bogus id → expect 404; call twice on same id → second call is a
   no-op (`UnsubscribedAt` unchanged).

## Decisions
- Identify subscriber by `MailSubscriber.Id`, newly exposed via DTO.
- Reuses exact same deactivation logic/timestamp semantics as the public token-based
  unsubscribe flow.
- `window.confirm()` per existing convention, not a custom modal.
- No reactivate feature — deactivate-only, per original request.
- Shared `ToAdminItem` mapping helper added to avoid duplicating the DTO-construction logic
  between `GetSubscribers` and `DeactivateSubscriber` (approved evaluator finding), with an
  explicit fallback note to keep the EF-query-side projection inline if the helper doesn't
  translate cleanly to SQL.
