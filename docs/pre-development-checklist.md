# Pre-Development Checklist

### `sab-engine` — What's Left Before Writing Code

**Status:** Living document — update as items move, and add new items as they surface. Not a design document; everything here assumes the design decisions in `SAB_Design_Document_v0.1.2.md` and `open-questions.md` are already settled.

**Ordering:** Items are listed in the order they actually need to happen — each one generally depends on the ones above it. IDs are assigned once and don't get renumbered as items complete; if the order needs to change later, move the row and add a note rather than reassigning IDs.

**Status key:**
- ⬜ **Not Started**
- 🟡 **In Progress**
- 🔒 **Blocked** — waiting on something else, noted in the item
- ✅ **Done**

---

| ID | Item | Why it's here / depends on | Status |
|---|---|---|---|
| PD-1 | Decide: does `sab-engine` build work start now, or does `sab-kb` still take priority per the Section 9 sequencing note (`open-questions.md` RC-5)? | Nothing below can meaningfully start until this is answered | ⬜ Not Started |
| PD-2 | Scaffold the `sab-engine` C#/.NET solution structure (per TS-1, confirmed) | The base project everything else gets built inside | ⬜ Not Started |
| PD-3 | Stand up PostgreSQL and implement the Section 4.1/4.5 schema — `workflow_runs`, `plans`, `approvals`, `execution_results`, `target_state`, `notes`, `AuditEntry` (per TS-3, confirmed) | The orchestration engine is stateless by design (AR-1) — this has to exist before the engine has anywhere to persist state | ⬜ Not Started |
| PD-4 | Implement the orchestration engine's state machine (`Requested → PlanDrafted → PendingApproval → Approved/Declined → Executing → Completed/Failed → RolledBack`, Section 4.1) | Depends on PD-2/PD-3 existing to persist against | ⬜ Not Started |
| PD-5 | Implement the claim/lease concurrency pattern for stateless workers (Section 4.1's concurrency model) | Extends the state machine (PD-4) to support more than one worker safely | ⬜ Not Started |
| PD-6 | Integrate Microsoft Semantic Kernel for the AI agent layer (per TS-2, confirmed) | Needs the solution scaffold (PD-2) in place first | ⬜ Not Started |
| PD-7 | Set up PowerShell interop via `System.Management.Automation` (native to the .NET/TS-1 choice) | Needs the solution scaffold (PD-2); required before any module can actually run PowerShell | ⬜ Not Started |
| PD-8 | Implement audit logging — every `AuditEntry` write tied to a real state transition, append-only/hash-linked per the tamper-evidence approach in Section 7 | Hooks directly into the state machine (PD-4) — cheapest to build alongside it, not bolted on after | ⬜ Not Started |
| PD-9 | Choose and stand up the Phase 1 secrets backend — HashiCorp Vault or native OS credential store, per SE-1 | Required before the execution environment (PD-16+) can resolve any real credential | ⬜ Not Started |
| PD-10 | Set up a CI pipeline for `sab-engine` (build + run tests on push) | Best set up now, before real code accumulates, so everything built from here on gets tested continuously | ⬜ Not Started |
| PD-11 | Stand up a lab/low-stakes Windows Server environment to test against — required before anything touches production, per Section 2's reliability principle | Doesn't block earlier items, but needs to exist before any real module or connector testing (PD-19+) happens — worth starting in parallel with PD-2–PD-10 | ⬜ Not Started |
| PD-12 | Implement the module metadata manifest format/parser (per the Section 4.2 YAML schema) | Needed before any real module can be written and recognized by the engine | ⬜ Not Started |
| PD-13 | Set up a CI pipeline for `sab-modules` (the OSML) that validates new/changed modules against the contract (unique ID, required fields, rollback present, tests present) | Depends on PD-12's manifest format existing to validate against | ⬜ Not Started |
| PD-14 | Write the `pre-flight-check` module (checks a server is healthy enough to patch) | First real module — the workflow's first step, and the one with the least risk since it only reads | ⬜ Not Started |
| PD-15 | Write the `stage-patches` module | Second step in the patching workflow | ⬜ Not Started |
| PD-16 | Write the `apply-patches` module | Third step — the highest-risk module, which is why everything above (audit logging, secrets, state machine) needs to exist first | ⬜ Not Started |
| PD-17 | Write the `validate` module (confirms the server came back up correctly) | Fourth step, confirms PD-16 actually worked | ⬜ Not Started |
| PD-18 | Write tested rollback procedures for `stage-patches` and `apply-patches` (`pre-flight-check` and `validate` don't need one, since they only read) | Required before any of these modules can be marked usable, per Section 2's non-negotiable rollback rule | ⬜ Not Started |
| PD-19 | Assign each module (PD-14–PD-17) a unique ID per AR-5/AR-6 (confirmed) and set initial `validation_status: lab-validated` | Can't be skipped — the engine won't recognize a module without one | ⬜ Not Started |
| PD-20 | Write the `lab_suite` test file for each module (per the Section 4.2 metadata schema's `tests` field) | Depends on the modules (PD-14–PD-18) actually existing to write tests against | ⬜ Not Started |
| PD-21 | Write the first real workflow definition — "Patch Windows Server" — stringing PD-14–PD-17 together in order (Section 4.2, "Workflows as a separate layer") | Depends on all four modules existing first | ⬜ Not Started |
| PD-22 | Give the workflow its own unique ID per AR-5 (confirmed) | Same requirement as PD-19, applied to the workflow itself | ⬜ Not Started |
| PD-23 | Build the WinRM connector implementing the Section 4.4 interface (`connect`, `execute`, `disconnect`, `health_check`) | Needed before any module can actually run against a real (or lab) target | ⬜ Not Started |
| PD-24 | Implement `credential_handle` resolution against the secrets backend (PD-9) at connection time (never exposing raw credentials to modules or the AI agent) | Depends directly on PD-9 and PD-23 both existing | ⬜ Not Started |
| PD-25 | Scope least-privilege credentials per module/target rather than one standing credential (Section 4.4) | Refines PD-24 once basic credential resolution works | ⬜ Not Started |
| PD-26 | Implement per-target connection isolation (a hang on one target can't affect others running in parallel) | Extends the connector (PD-23) — matters once more than one target is ever run against at once | ⬜ Not Started |
| PD-27 | Implement Docker-based sandboxing for module execution, per SE-2 (confirmed) | Wraps module execution (PD-14–PD-17 running through PD-23) in isolation before real testing begins | ⬜ Not Started |
| PD-28 | Run and pass the Phase 1 exit criteria end-to-end: SAB reliably patches a lab server (PD-11), with a human approving each run and a tested rollback path proven to actually work — not just documented (Section 9, Phase 1) | The integration point — depends on everything above being in place | ⬜ Not Started |
| PD-29 | Promote validated modules (PD-14–PD-17) from `lab-validated` to `production-approved` | Only happens once PD-28 actually passes | ⬜ Not Started |

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

### Notes
- This is a living tracking doc, in the same spirit as `open-questions.md` — pulls concrete pre-code tasks out of the design doc and roadmap so they're trackable in one place, in the order they actually need to happen.
- IDs are permanent once assigned — if priorities shift and the order changes, move the row and note why, rather than renumbering everything.
- Update status as items move. When an item is genuinely done, leave it marked ✅ rather than deleting it — the history of what's been completed is useful context on its own.
- New items surfacing during development belong here too, inserted at the point in the sequence where they actually apply, with a new PD-N ID.
