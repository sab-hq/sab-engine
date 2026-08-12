# Checklist 02

### `sab-engine` — Active Tracker

**Status: ACTIVE.** This is now the single source of truth for what's currently in progress or not yet started. `pre-development-checklist.md` is closed — a historical record of PD-1 through PD-27 and PD-30, all done and verified. This document picks up where that one left off: PD-28 and PD-29 (moved here unstarted), the items that were explicitly deferred there, and every open thread flagged along the way (the untested WinRM network path, `CredentialHandleResolver`'s per-module wiring, Docker sandboxing's Windows-container gap).

**Ordering:** Items are listed in the order they actually need to happen — each one generally depends on the ones above it. IDs are assigned once and don't get renumbered as items complete; if the order needs to change later, move the row and add a note rather than reassigning IDs.

**Status key:**
- ⬜ **Not Started**
- 🟡 **In Progress**
- 🔒 **Blocked** — waiting on something else, noted in the item
- ✅ **Done**

---

| ID | Item | Why it's here / depends on | Status |
|---|---|---|---|
| PD-28 | Run and pass the Phase 1 exit criteria end-to-end: SAB reliably patches a lab server (PD-11), with a human approving each run and a tested rollback path proven to actually work — not just documented (Section 9, Phase 1) | The integration point — depends on everything in `pre-development-checklist.md` being in place. In practice, also needs the WinRM connector's real network path verified against the lab VM (never tested — only RDP was, not WinRM/5985) and at least basic orchestration wiring to call modules in sequence (flagged as a gap since PD-4, still open) | ⬜ Not Started |
| PD-29 | Promote validated modules (PD-14–PD-17) from `lab-validated` to `production-approved` | Only happens once PD-28 actually passes | ⬜ Not Started |
| PD-31 | Wire `CredentialHandleResolver` (PD-25) into per-module execution — each module in a workflow resolves its own appropriately-scoped credential and opens its own connection, instead of one connection/credential covering an entire workflow run | Depends on the orchestration engine actually calling modules in sequence, per a workflow definition — a gap flagged since PD-4 and still open. `CredentialHandleResolver` itself is real and tested (PD-25); nothing calls it per-module yet. | ⬜ Not Started |

---

## Explicitly Deferred — Not Blocking Phase 1

These are real open items, but the design doc and `open-questions.md` already establish they don't need to be resolved before development starts. Listed here so they don't get mistaken for missing prerequisites, and so they don't clutter the priority order above.

| Ref | Item | Why it's deferred |
|---|---|---|
| AR-4 | Third-party integration API surface | Phase 3/4 scope, per the roadmap |
| SC-1/SC-2 | Multi-server/concurrent-run scaling detail | Worth a real load-testing pass once Phase 1 exists, not before |
| PR-1/PR-2 | Reliability/uptime and rollback-time targets | Reasonable to set once Phase 1 gives real timing data to anchor against |
| MP-1 | Marketplace revenue share % | Irrelevant until Phase 2's marketplace |
| SE-3 | Compliance framework target (SOC2, HIPAA, etc.) | Blocked on knowing a target industry, not on more design work |

---

## Other Known Open Threads (not yet given their own PD- entry)

Flagged honestly across `pre-development-checklist.md`'s entries but not yet formalized as their own tracked item:

- **The real WinRM network connection is untested.** PD-23's connector code is done and verified against a substituted local session, but nobody has yet started the lab VM and confirmed it actually connects over the network (port 5985) to a real target. Worth doing deliberately, likely as part of or just before PD-28.
- **Docker sandboxing doesn't cover SAB's real modules yet.** `DockerSandboxedExecutor` (PD-27) is real and correctly isolates any PowerShell script, but the actual four patching modules are Windows-specific and won't run inside its default Linux container. Actually sandboxing them needs a Windows container image — a deliberate, disruptive Docker Desktop reconfiguration.
- **The `OrchestrationEngine` class itself is still a stub.** Flagged repeatedly since PD-4 — nothing yet wires the state machine to actually call modules in order via `IExecutionConnector`, or triggers a module's rollback automatically on failure. This is the real, foundational prerequisite both PD-28 and PD-31 above ultimately depend on.
- **No real module catalog loader exists.** Flagged since PD-6 — the AI agent's available-module list is still supplied manually rather than loaded automatically from an OSML checkout.
- **No workflow parser/validator exists**, unlike modules (PD-12/13). `sab-modules/workflows/patch-windows-server.yaml` (PD-21) is real but has zero automated validation.
- **Wiring the AI agent to a real model** — still needs a real OpenAI/Azure OpenAI API key from Brock, deliberately not hardcoded into the repo.

---

### Notes
- This doc continues `pre-development-checklist.md`'s numbering and format — PD-28/PD-29 moved here with their IDs unchanged; PD-31 was already here. New genuinely-new items continue from PD-32 onward.
- `pre-development-checklist.md` is now closed — a historical record of PD-1 through PD-27 and PD-30. Nothing further gets added there.
- IDs are permanent once assigned, same rule as the original checklist — if priorities shift and the order changes, move the row and note why, rather than renumbering everything.
- Update status as items move. When an item is genuinely done, leave it marked ✅ rather than deleting it — the history of what's been completed is useful context on its own.
