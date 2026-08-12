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
| PD-32 | Wire the `OrchestrationEngine` to actually execute a workflow: read a workflow definition, call each module in order via `IExecutionConnector`, transition the `WorkflowRun` through `Executing → Completed/Failed` as it goes, and trigger a module's own rollback automatically on failure | **The single foundational gap PD-28 and PD-31 both depend on** — flagged repeatedly since PD-4, never itemized on its own until now. Placeholder entry, written with as much real detail as could be reasoned out now, so whoever picks this up isn't starting from nothing. | ⬜ Not Started — **not yet designed in code, but here's what's already known:** **What it can build on, all real and already verified:** `WorkflowRunClaimService.TryClaimNextAsync` (PD-5) already claims eligible runs atomically, including the `Approved` state — this is the natural trigger point; a background worker (likely an ASP.NET Core `BackgroundService` hosted in `SabEngine.Api`, since that's already the live host per PD-30) would poll it, and claiming an `Approved` run is what kicks this logic off. `WinRmExecutionConnector`/`WinRmExecutionSession` (PD-23–PD-27) already resolve a target, execute a module script, and return a structured `ExecutionResult` — the actual step-execution primitive already exists and is tested. `WorkflowRunStateMachine` (PD-4) already enforces the transitions this engine would drive. **Three real, unresolved sub-problems worth flagging honestly, not glossed over:** (1) **No workflow parser exists.** `patch-windows-server.yaml` (PD-21) has zero automated reader — this item will likely need to build a minimal one (deserialize the YAML into an ordered list of `module_id`/`module_version` steps) as part of its own scope, since there's nothing to execute against otherwise. (2) **Rollback script resolution is a real, concrete gap in already-built code.** `WinRmExecutionSession.ExecuteAsync` currently resolves `moduleId` → `{modulesRoot}/{moduleId}/{moduleId}.ps1` only — it has no way to invoke a module's *rollback* script, which lives under a different filename (e.g. `rollback-pre-flight-check.ps1`, per each manifest's `RollbackSpec.Procedure`, PD-12). This will need either a new parameter/overload on `ExecuteAsync`/`IExecutionSession`, or another mechanism — genuinely undecided. (3) **Scoping call on `CredentialHandleResolver` (PD-31):** this item should NOT hard-block on PD-31 — it's reasonable to build PD-32 using one connection/credential per workflow run first, and layer in PD-31's per-module tiered credentials as a refinement afterward, matching how PD-25 itself was scoped (the mechanism before the wiring). **Also still needed, not blocking but relevant:** persisting an `ExecutionResult` row per step (the type already exists in Core, matching Section 4.1's data model) as the engine goes, not just at the end. |
| PD-28 | Run and pass the Phase 1 exit criteria end-to-end: SAB reliably patches a lab server (PD-11), with a human approving each run and a tested rollback path proven to actually work — not just documented (Section 9, Phase 1) | Directly depends on **PD-32** (nothing to run end-to-end without it) and on the WinRM connector's real network path being verified against the lab VM (never tested — only RDP was, not WinRM/5985) | ⬜ Not Started |
| PD-29 | Promote validated modules (PD-14–PD-17) from `lab-validated` to `production-approved` | Only happens once PD-28 actually passes | ⬜ Not Started |
| PD-31 | Wire `CredentialHandleResolver` (PD-25) into per-module execution — each module in a workflow resolves its own appropriately-scoped credential and opens its own connection, instead of one connection/credential covering an entire workflow run | Depends on **PD-32** existing first (there's no per-module execution to wire tiered credentials into yet). `CredentialHandleResolver` itself is real and tested (PD-25); nothing calls it per-module yet. Deliberately not a hard blocker for PD-28 — see PD-32's own scoping note. | ⬜ Not Started |

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
- **No real module catalog loader exists.** Flagged since PD-6 — the AI agent's available-module list is still supplied manually rather than loaded automatically from an OSML checkout.
- **No workflow parser/validator exists**, unlike modules (PD-12/13) — though PD-32 above will likely need to build at least a minimal reader as part of its own scope.
- **Wiring the AI agent to a real model** — still needs a real OpenAI/Azure OpenAI API key from Brock, deliberately not hardcoded into the repo. **Open question, not yet resolved:** does PD-28's exit test require the agent to genuinely propose the plan via a real model, or can a fixed/manual plan stand in for that specific test? Worth deciding before PD-28 is attempted, not during it.

---

### Notes
- This doc continues `pre-development-checklist.md`'s numbering and format — PD-28/PD-29 moved here with their IDs unchanged; PD-31 was already here. New genuinely-new items continue from PD-32 onward.
- `pre-development-checklist.md` is now closed — a historical record of PD-1 through PD-27 and PD-30. Nothing further gets added there.
- IDs are permanent once assigned, same rule as the original checklist — if priorities shift and the order changes, move the row and note why, rather than renumbering everything.
- Update status as items move. When an item is genuinely done, leave it marked ✅ rather than deleting it — the history of what's been completed is useful context on its own.
