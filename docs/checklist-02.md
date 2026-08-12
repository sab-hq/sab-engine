# Checklist 02

### `sab-engine` — Continuation of `pre-development-checklist.md`

**Status:** Living document, currently empty — this is where new items go once they come up **after** `pre-development-checklist.md`'s PD-1 through PD-30 scope (including PD-28/PD-29, which stay in that original doc, not here). Same structure and conventions as `pre-development-checklist.md`.

**Ordering:** Items are listed in the order they actually need to happen — each one generally depends on the ones above it. IDs are assigned once and don't get renumbered as items complete; if the order needs to change later, move the row and add a note rather than reassigning IDs.

**Status key:**
- ⬜ **Not Started**
- 🟡 **In Progress**
- 🔒 **Blocked** — waiting on something else, noted in the item
- ✅ **Done**

---

| ID | Item | Why it's here / depends on | Status |
|---|---|---|---|
| PD-31 | Wire `CredentialHandleResolver` (PD-25) into per-module execution — each module in a workflow resolves its own appropriately-scoped credential and opens its own connection, instead of one connection/credential covering an entire workflow run | Depends on the orchestration engine actually calling modules in sequence, per a workflow definition — a gap flagged since PD-4 and still open. `CredentialHandleResolver` itself is real and tested (PD-25); nothing calls it per-module yet. | ⬜ Not Started |

---

### Notes
- This doc continues `pre-development-checklist.md`'s numbering and format (starting from PD-31) for anything that comes up after that checklist's PD-1 through PD-30 scope — kept separate so the original doc doesn't grow indefinitely.
- `pre-development-checklist.md` remains the source of truth for PD-1 through PD-30, PD-28 and PD-29 included — nothing was moved out of it.
- IDs are permanent once assigned, same rule as the original checklist — if priorities shift and the order changes, move the row and note why, rather than renumbering everything.
- Update status as items move. When an item is genuinely done, leave it marked ✅ rather than deleting it — the history of what's been completed is useful context on its own.
