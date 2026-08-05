# sab-engine
### `github.com/sab-hq/sab-engine` — Core Orchestration Engine

**Status:** Not yet created. This document consolidates everything already decided about this repo across the design doc and open-questions tracker, into a single reference for when repo creation and initial scaffolding begin.

---

## What This Repo Is

`sab-engine` is the "how and when" layer of SAB (see `SAB_Design_Document.md`, Section 3). It doesn't decide *what* to do — that's the AI agent's job, which also lives in this repo — it takes an approved plan and reliably carries it out against target systems.

**License:** Apache 2.0 (recommended, see `open-questions.md` LM-1) — open source, permissive, no restriction on rebuilding or rehosting, consistent with the early-Red-Hat business model direction.

## Tech Stack

| Component | Choice | Status |
|---|---|---|
| Orchestration engine | C#/.NET | 🟡 Recommended (TS-1), awaiting your confirmation |
| AI agent layer | C#/.NET + Microsoft Semantic Kernel | 🟡 Recommended (TS-2), awaiting confirmation — Python is the fallback if Semantic Kernel proves limiting |
| State persistence | PostgreSQL | 🟢 Resolved (TS-3) |
| PowerShell interop | `System.Management.Automation` | Native to the .NET choice — this is a large part of *why* .NET was recommended |

## What Lives Here

### 1. Orchestration Engine (Section 4.1)
- **Sequencing** — executes modules in workflow-defined order, respecting dependencies (e.g. pre-flight check must pass before patches apply)
- **State tracking** — durable record of workflow run state (which module is executing, succeeded/failed, target system's last-known state); must survive crashes/restarts
- **Failure handling / rollback** — automatically invokes a failed module's rollback path; this must not require manual triggering
- **Logging / audit trail** — every action, by which module, with what result — feeds both the security/compliance audit trail and the feedback loop to the AI agent and SAB-KB

**Architecture:** stateless task runner, not a long-running service (AR-1, resolved). State lives in PostgreSQL, not in memory — this is what makes crash recovery and horizontal scaling (running more workers, per SC-1/SC-2) straightforward rather than a redesign later.

### 2. AI Agent Layer (Section 4.3)
- Reads: selected workflow, target system state, historical data (queried from `sab-kb`), constraints (maintenance windows, blackout periods)
- Produces: a proposed plan (which modules, what order, what parameters), plain-language reasoning for the proposal, a risk/confidence indicator
- **Phase 1 constraint: recommend-and-approve only.** No autonomy stretch goals at launch — every plan requires human approval before the engine executes it. This is a hard architectural boundary, not just a policy: the agent produces proposals, it does not call modules directly.

### 3. Module Contract & Connector Contract (Sections 4.2, 4.4)
- The interface definitions that `sab-modules` builds against — a required unique ID (AR-5, confirmed — no two modules or workflows ever share one), metadata schema, typed inputs/outputs, required rollback procedure, test requirements
- The connector interface that execution environments implement
- These contracts are the actual product surface for extensibility (Section 5) — worth treating interface stability here as a first-class design concern, since `sab-modules` and eventually third-party integrations all depend on them not shifting underneath them

### 4. WinRM Connector (Section 4.4)
- The first execution environment implementation, shipped as core rather than as a community-contributed extension, since on-prem Windows is the initial primary target (Phase 1 scope)
- Handles connection management, credential resolution (via the secrets approach in SE-1 — HashiCorp Vault as a pluggable backend, native OS credential stores as a simpler default), and isolation between concurrent target connections

### 5. CLI / API
- The interface an SA/SE (or eventually an external tool, per AR-4) uses to trigger a workflow and monitor its status
- Exact API surface not yet speced — reasonable next artifact once Phase 1 architecture work starts

## What Does *Not* Live Here

- **Modules and workflow definitions themselves** — those live in `sab-modules`, the Open Source Module Library (OSML), referenced by contract
- **SAB-KB's schema and query implementation** — lives in `sab-kb`; this repo is a *consumer* of that interface, not where it's implemented
- **Marketplace, managed hosting, enterprise connectors** — all `sab-commercial`

## Phase 1 Scope (Minimum Viable Version)

Per the roadmap (`SAB_Design_Document.md`, Section 9), Phase 1 doesn't need the full vision of this repo — just enough to prove the architecture end-to-end on Windows Server patching:
- Sequencing, state tracking, rollback triggering — minimum viable, not the full state-machine design
- AI agent in recommend-and-approve mode only
- WinRM connector for on-prem Windows only — no other execution environments yet
- Enough logging to feed a minimal version of SAB-KB (which itself is only a logical layer at this stage, per AR-3)

## Open Items Specific to This Repo

- **AR-4** (third-party integration API surface) — deferred until Phase 3/4, not needed for Phase 1
- **SE-2** (sandboxing model) — 🟢 confirmed: Docker containers. Relevant once module execution isolation is actually implemented here.
- Exact state machine and concurrency model (flagged in Section 4.1 as still needing detail beyond the AR-1 resolution)
- CLI/API surface design — not yet started

---

*This is a consolidation document, not new design work — everything here traces back to `SAB_Design_Document.md` and `open-questions.md`. Update both sources of truth first if anything here needs to change, then reflect the change back into this file.*
