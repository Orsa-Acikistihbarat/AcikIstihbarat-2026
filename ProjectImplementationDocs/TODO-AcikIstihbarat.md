# TODO — AcikIstihbarat

This file tracks deferred tasks, known issues, and future action items identified during development sessions.

---

## Add visual loading indicators to search bars during navigation

**Created:** 2026-08-08 22:50 UTC
**Priority:** 🟡 Medium
**Context:** Planned during the post-deployment cleanup session. The current Next.js HTML form submission causes a hard page reload which leaves the user waiting with no visual feedback.

**Description:**
- Extract the Header search bar into a Client Component (`components/HeaderSearchBar.tsx`).
- Extract the Arama page search bar into a Client Component (`components/AramaSearchBar.tsx`).
- In both Client Components, use `useRouter` from `next/navigation` and `useState`/`useTransition` to track `isPending`.
- Intercept the `onSubmit` event (`e.preventDefault()`), trigger the `isPending` state, and push the new route (`router.push('/arama?q=' + query)`).
- While `isPending` is true, swap the search icon to a spinning loader (`<Loader2 className="animate-spin" />` from lucide-react).

---

## Implement Footer

**Created:** 2026-08-08 22:50 UTC
**Priority:** 🟡 Medium
**Context:** Pending item from the original project checklist.

**Description:**
Design and implement the missing global footer component for the public frontend, matching the site's dark/light mode aesthetics.

---
