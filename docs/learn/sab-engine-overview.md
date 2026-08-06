# sab-engine
### `github.com/sab-hq/sab-engine` — Core Orchestration Engine

**Status:** Actively being built. The repo exists, the solution scaffold is real and builds cleanly, and Phase 1 development is underway — see `pre-development-checklist.md` for the authoritative, item-by-item build tracker (PD-1 through PD-29). This document consolidates everything decided about this repo across the design doc and open-questions tracker into a single reference; `pre-development-checklist.md` is the one to check for "what's actually built right now."

---

## What This Repo Is

`sab-engine` is the "how and when" layer of SAB (see `SAB_Design_Document_v0.1.2.md`, Section 3). It doesn't decide *what* to do — that's the AI agent's job, which also lives in this repo — it takes an approved plan and reliably carries it out against target systems.

**License:** Apache 2.0 (confirmed, `open-questions.md` LM-1) — open source, permissive, no restriction on rebuilding or rehosting, consistent with the early-Red-Hat business model direction.

## Tech Stack

| Component | Choice | Status |
|---|---|---|
| Orchestration engine | C#/.NET 8 | 🟢 Resolved (TS-1) — real solution exists, builds cleanly (PD-2, done) |
| AI agent layer | C#/.NET + Microsoft Semantic Kernel | 🟢 Resolved (TS-2) — project scaffolded (PD-2), Semantic Kernel integration itself not yet started (PD-6) |
| State persistence | PostgreSQL | 🟢 Resolved (TS-3) — real schema implemented via EF Core + Npgsql, migration applied (PD-3, done) |
| PowerShell interop | `System.Management.Automation` | Native to the .NET choice; not yet wired in (PD-7) |

## What Lives Here — Current Implementation Status

### 1. Orchestration Engine (Section 4.1)
- **State machine — done (PD-4).** `WorkflowRunStateMachine` (`src/SabEngine.Orchestration`) enforces the exact allowed-transitions map from Section 4.1 in code — an illegal transition throws rather than silently succeeding. Covered by 5 passing tests in `tests/SabEngine.Orchestration.Tests`.
- **Audit trail — done (PD-8).** Every transition writes an immutable, hash-linked `AuditEntry` in the same operation as the state change (Section 7's tamper-evidence design) — not a separate logging pass.
- **State tracking — done, as part of PD-3/PD-4.** `WorkflowRun` state persists to PostgreSQL, not memory, so it survives crashes/restarts (AR-1).
- **Concurrency (claim/lease pattern) — done (PD-5).** `WorkflowRunClaimService` atomically claims the oldest eligible run via a real Postgres `UPDATE ... WHERE`, so two workers can never both win the same run, and a crashed worker's claim simply expires and gets picked up by someone else. Notably, this is the first place EF Core's InMemory test provider proved insufficient (it can't translate the atomic update this depends on) — those 7 tests now run against a real, disposable-per-test Postgres database instead, which means **Docker must be running locally for `dotnet test` to fully pass**, not just for the PD-3 migration step.
- **Sequencing / failure handling / rollback triggering — not yet built.** The `OrchestrationEngine` class itself (as opposed to the state machine) is still a stub — wiring it to actually call modules in order via `IExecutionConnector`, and to trigger a module's rollback automatically on failure, isn't itemized as its own `pre-development-checklist.md` item yet.

### 2. AI Agent Layer (Section 4.3)
- Domain types exist (`Plan`, `ProposedModuleStep` in `SabEngine.Core`), but the agent itself — reading target state and history, producing a proposal via Semantic Kernel — is not yet built (PD-6).
- **Phase 1 constraint: recommend-and-approve only,** already reflected in the state machine itself — `PendingApproval → Approved/Declined` is a real, enforced transition, not just a plan.

### 3. Module Contract & Connector Contract (Sections 4.2, 4.4)
- **Done, as C# interfaces (PD-2).** `IModuleContract` and `IExecutionConnector`/`IExecutionSession` exist in `SabEngine.Core`, matching the design doc's contracts exactly — unique ID (AR-5, confirmed), typed inputs/outputs, required rollback status, the `connect`/`execute`/`disconnect`/`health_check` shape.
- Real module implementations (PowerShell scripts) still live in the separate `sab-modules` (OSML) repo, per design — not here.

### 4. WinRM Connector (Section 4.4)
- **Not yet built (PD-17–PD-20).** `SabEngine.Execution` project exists as a scaffold with a comment pointing to where this goes; no real implementation yet.

### 5. CLI / API
- `SabEngine.Api` exists as the composition root and currently just registers the database connection via DI (PD-3). Real CLI/API surface design is still open (AR-4, deferred to Phase 3/4 per the roadmap).

## What Does *Not* Live Here

- **Modules and workflow definitions themselves** — those live in `sab-modules`, the Open Source Module Library (OSML), referenced by contract
- **SAB-KB's schema and query implementation** — lives in `sab-kb`; this repo does not currently integrate with it at all (see `ess-vs-sab-kb.md` and `open-questions.md` RC-1)
- **Marketplace, managed hosting, enterprise connectors** — all `sab-commercial`

## Phase 1 Scope (Minimum Viable Version)

Per the roadmap (`SAB_Design_Document_v0.1.2.md`, Section 9), Phase 1 doesn't need the full vision of this repo — just enough to prove the architecture end-to-end on Windows Server patching. Real progress so far:
- ✅ Solution scaffold, database schema, a production-quality state machine with a real audit trail, and multi-worker-safe concurrency (claim/lease) — further along than "minimum viable" already, not a stripped-down placeholder
- ⬜ The first real modules (`pre-flight-check`, `stage-patches`, `apply-patches`, `validate`) — not yet written (PD-14–PD-17)
- ⬜ AI agent in recommend-and-approve mode — the state machine enforces the *shape* of this already; the agent itself doesn't exist yet (PD-6)
- ⬜ WinRM connector for on-prem Windows — not yet built (PD-17–PD-20)
- ⬜ A lab/low-stakes test environment to actually validate against (PD-11)

`pre-development-checklist.md` is the authoritative, up-to-date tracker for exactly where this stands — this section is a summary, not the source of truth.

## Open Items Specific to This Repo

- **AR-4** (third-party integration API surface) — deferred until Phase 3/4, not needed for Phase 1
- **SE-2** (sandboxing model) — 🟢 confirmed: Docker containers. Not yet implemented (PD-27).
- **SE-1** (secrets backend) — HashiCorp Vault vs. native OS credential store, not yet decided which to actually stand up for Phase 1 (PD-9)
- CLI/API surface design — not yet started, and correctly deferred (AR-4)

---

*This is a consolidation document, not new design work — everything here traces back to `SAB_Design_Document_v0.1.2.md` and `open-questions.md` for design decisions, and `pre-development-checklist.md` for build status. Update those sources of truth first if anything here needs to change, then reflect the change back into this file.*
