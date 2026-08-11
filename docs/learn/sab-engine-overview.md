# sab-engine
### `github.com/sab-hq/sab-engine` — Core Orchestration Engine

**Status:** Actively being built. The repo exists, the solution scaffold is real and builds cleanly, and Phase 1 development is underway — see `pre-development-checklist.md` for the authoritative, item-by-item build tracker (PD-1 through PD-30). This document consolidates everything decided about this repo across the design doc and open-questions tracker into a single reference; `pre-development-checklist.md` is the one to check for "what's actually built right now."

---

## What This Repo Is

`sab-engine` is the "how and when" layer of SAB (see `SAB_Design_Document_v0.1.2.md`, Section 3). It doesn't decide *what* to do — that's the AI agent's job, which also lives in this repo — it takes an approved plan and reliably carries it out against target systems.

**License:** Apache 2.0 (confirmed, `open-questions.md` LM-1) — open source, permissive, no restriction on rebuilding or rehosting, consistent with the early-Red-Hat business model direction.

**CI:** `.github/workflows/ci.yml` runs on every push/PR — two jobs split by OS (Linux for everything except the Windows-only Credential Manager tests, using a real Postgres service container; Windows for the full `SabEngine.Execution.Tests` project). Verified green on GitHub Actions (PD-10, done). A separate CI pipeline lives in the `sab-modules` repo itself (PD-13), validating every module manifest against this repo's parser on every push there.

## Tech Stack

| Component | Choice | Status |
|---|---|---|
| Orchestration engine | C#/.NET 8 | 🟢 Resolved (TS-1) — real solution exists, builds cleanly (PD-2, done) |
| AI agent layer | C#/.NET + Microsoft Semantic Kernel | 🟢 Resolved (TS-2) — Semantic Kernel integrated and tested (PD-6, done). Not yet wired to a real model — no API key configured, deliberately (see below). |
| State persistence | PostgreSQL | 🟢 Resolved (TS-3) — real schema implemented via EF Core + Npgsql, migration applied (PD-3, done) |
| PowerShell interop | `System.Management.Automation` (via `Microsoft.PowerShell.SDK`) | 🟢 Resolved (TS-1's corollary) — real local execution wired and tested (PD-7, done). Remote (WinRM) execution not yet built (PD-17–PD-20).
| Secrets backend | Windows Credential Manager (Phase 1 default) | 🟢 Resolved (SE-1) — real P/Invoke-backed implementation wired and tested (PD-9, done), behind a pluggable `ISecretStore` contract so Vault can swap in later without touching callers.

## What Lives Here — Current Implementation Status

### 1. Orchestration Engine (Section 4.1)
- **State machine — done (PD-4).** `WorkflowRunStateMachine` (`src/SabEngine.Orchestration`) enforces the exact allowed-transitions map from Section 4.1 in code — an illegal transition throws rather than silently succeeding. Covered by 5 passing tests in `tests/SabEngine.Orchestration.Tests`.
- **Audit trail — done (PD-8).** Every transition writes an immutable, hash-linked `AuditEntry` in the same operation as the state change (Section 7's tamper-evidence design) — not a separate logging pass.
- **State tracking — done, as part of PD-3/PD-4.** `WorkflowRun` state persists to PostgreSQL, not memory, so it survives crashes/restarts (AR-1).
- **Concurrency (claim/lease pattern) — done (PD-5).** `WorkflowRunClaimService` atomically claims the oldest eligible run via a real Postgres `UPDATE ... WHERE`, so two workers can never both win the same run, and a crashed worker's claim simply expires and gets picked up by someone else. Notably, this is the first place EF Core's InMemory test provider proved insufficient (it can't translate the atomic update this depends on) — those 7 tests now run against a real, disposable-per-test Postgres database instead, which means **Docker must be running locally for `dotnet test` to fully pass**, not just for the PD-3 migration step.
- **Sequencing / failure handling / rollback triggering — not yet built.** The `OrchestrationEngine` class itself (as opposed to the state machine) is still a stub — wiring it to actually call modules in order via `IExecutionConnector`, and to trigger a module's rollback automatically on failure, isn't itemized as its own `pre-development-checklist.md` item yet.

### 2. AI Agent Layer (Section 4.3)
- **Done — real plan-drafting via Semantic Kernel (PD-6).** `SabAgent` (`src/SabEngine.Agent`) builds a prompt from a workflow, target, and available-module list, calls the Kernel's chat completion service, and parses the response into a real `Plan`. Covered by 7 passing tests in `tests/SabEngine.Agent.Tests`, using a hand-written fake chat-completion service so the tests run fully offline.
- **Section 4.1/4.2's hard rule enforced here too, as defense in depth.** The agent validates every proposed module against the candidate list before ever returning a plan — rejects unknown modules, anything not `production-approved`, and anything without a tested rollback. A model proposing something unsafe never even reaches a human; it's refused right here.
- **Deliberately not done: wiring to a real model.** Only the core `Microsoft.SemanticKernel` package is referenced — no OpenAI/Azure OpenAI connector, no API key. Needs a real credential Brock supplies; not something to hardcode into this repo.
- **Deliberately not done: a real module catalog.** No real modules exist yet (PD-14–PD-17), so the agent takes its available-module list as an input parameter rather than loading one itself. A `ModuleCandidate` type in `SabEngine.Core` makes that list constructible for now.
- **Phase 1 constraint: recommend-and-approve only,** already reflected in the state machine itself — `PendingApproval → Approved/Declined` is a real, enforced transition, not just a plan.

### 3. Module Contract & Connector Contract (Sections 4.2, 4.4)
- **Done, as C# interfaces (PD-2).** `IModuleContract` and `IExecutionConnector`/`IExecutionSession` exist in `SabEngine.Core`, matching the design doc's contracts exactly — unique ID (AR-5, confirmed), typed inputs/outputs, required rollback status, the `connect`/`execute`/`disconnect`/`health_check` shape.
- **The manifest parser — done (PD-12).** New project `SabEngine.Modules` reads and validates the Section 4.2 YAML manifest format via `YamlDotNet`, enforcing every required field (rollback procedure, test suite, etc.) at parse time — a manifest missing one is rejected outright, not discovered broken later. A `ToModuleCandidate()` extension bridges a parsed manifest directly into the shape `SabAgent.ProposePlanAsync` (PD-6) already consumes, closing a loop PD-6 deliberately left open. Caught a real bug in the design doc's own reference example along the way — `{ type: enum[success, failure] }` isn't actually valid YAML inside a flow mapping; fixed in both the doc and the parser's test fixture.
- **A validator CLI — done (PD-13).** New project `SabEngine.Modules.Cli`, a thin wrapper around the parser above, walks a directory for every `manifest.yaml` and reports OK/FAIL with a non-zero exit code on any failure — this is what actually lets `sab-modules`' own CI (a separate pipeline, in that repo) validate real modules against this repo's parser. The cross-repo design (checkout-and-run-from-source rather than a published NuGet tool) is a deliberate Phase 1 simplification, documented in `pre-development-checklist.md`, PD-13, along with the reasoning for revisiting it once the OSML has real external contributors.
- The first real module written against this contract, `pre-flight-check`, lives in `sab-modules` (PD-14, done) — real module implementations always live there, per design, not here. A real catalog loader that reads every manifest from an OSML checkout and builds the agent's available-module list automatically is still ahead, not itemized as its own `pre-development-checklist.md` item yet.

### 4. WinRM Connector (Section 4.4)
- **PowerShell interop — done, local execution only (PD-7).** `PowerShellExecutor` (`src/SabEngine.Execution`) runs a script via `Microsoft.PowerShell.SDK` and returns a structured result (output, errors, a `Succeeded` flag) — correctly handles both PowerShell's non-terminating errors (`Write-Error`) and terminating ones (`throw`), which turned out to need different handling entirely. Covered by 5 passing tests in `tests/SabEngine.Execution.Tests`, run against a real local PowerShell engine (no fakes — there's no meaningful way to fake whether PowerShell interop actually works).
- **Secrets backend — done (PD-9).** `WindowsCredentialManagerSecretStore` (`src/SabEngine.Execution`) implements `ISecretStore` (Core) against real Windows Credential Manager via P/Invoke — this is what a future connector resolves a `credential_handle` against at connection time (Section 7), so modules and the AI agent never see a raw secret. The riskiest code in the project so far (hand-written Win32 struct marshaling), verified clean on the first attempt across 6 tests including a Unicode round-trip.
- **The actual WinRM connector — not yet built (PD-17–PD-20).** `PowerShellExecutor` and `WindowsCredentialManagerSecretStore` are both interop primitives the connector will use together; neither is wired into an `IExecutionConnector` implementation yet, and neither points at a *remote* session/target. Building that is still ahead.

### 5. CLI / API
- **A real human approval UI — done (PD-30).** `SabEngine.Api` is now a working ASP.NET Core web app (switched from a plain console `Sdk` to `Microsoft.NET.Sdk.Web`), serving plain server-rendered HTML with no JavaScript framework or build step — matching the same lightweight philosophy `sab-kb`'s own UI already uses. A pending-approvals list, a review page showing the plan's steps and the agent's reasoning, and Approve/Decline buttons that genuinely drive `WorkflowRunStateMachine` and record a real `Approval` row. This is the human-facing half of recommend-and-approve mode (Section 2) made real — not a mockup. A `POST /demo/create` route seeds an obviously-labeled demo run for testing, since the orchestration engine doesn't yet call modules in sequence to create a real one.
- **A broader CLI/API surface — still open (AR-4), correctly deferred to Phase 3/4** per the roadmap. PD-30 covers the minimum needed for a human to approve a plan; a fuller API (for external tools, ChatOps, etc.) is separate, later scope.

## What Does *Not* Live Here

- **Modules and workflow definitions themselves** — those live in `sab-modules`, the Open Source Module Library (OSML), referenced by contract
- **SAB-KB's schema and query implementation** — lives in `sab-kb`; this repo does not currently integrate with it at all (see `ess-vs-sab-kb.md` and `open-questions.md` RC-1)
- **Marketplace, managed hosting, enterprise connectors** — all `sab-commercial`

## Phase 1 Scope (Minimum Viable Version)

Per the roadmap (`SAB_Design_Document_v0.1.2.md`, Section 9), Phase 1 doesn't need the full vision of this repo — just enough to prove the architecture end-to-end on Windows Server patching. Real progress so far:
- ✅ Solution scaffold, database schema, a production-quality state machine with a real audit trail, multi-worker-safe concurrency (claim/lease), a real Semantic-Kernel-backed AI agent with hard-rule enforcement, working local PowerShell interop, a real Windows Credential Manager-backed secrets store, a working two-job CI pipeline (Linux + Windows, both verified green), a real module manifest parser, a validator CLI feeding `sab-modules`' own CI, and a real, working human approval web UI (PD-30) — further along than "minimum viable" already, not a stripped-down placeholder
- ✅ All four real modules are done: `pre-flight-check` (PD-14), `stage-patches` (PD-15), `apply-patches` (PD-16), and `validate` (PD-17) — written, tested (29 tests total across the set), and confirmed valid against the real parser (`4/4 manifest(s) valid`). `apply-patches` is also the first module with a genuine, non-trivial rollback (real patch uninstall via `wusa.exe`), unlike the justified no-ops the other three got. The actual "Patch Windows Server" workflow stringing all four together is next (PD-21)
- ⬜ Wiring the agent to a real model (an actual OpenAI/Azure OpenAI connector + API key) — not yet done, needs a real credential from Brock
- ⬜ The actual WinRM connector, combining PowerShell interop and the secrets store to reach a remote server — not yet built (PD-23–PD-27)
- ✅ A lab/low-stakes Windows Server VM in Azure (`sabengine-labwin01`), confirmed reachable and properly deallocated between sessions with a budget alert in place — ready for PD-23+ connector/module testing once that work starts

`pre-development-checklist.md` is the authoritative, up-to-date tracker for exactly where this stands — this section is a summary, not the source of truth.

## Open Items Specific to This Repo

- **AR-4** (third-party integration API surface) — deferred until Phase 3/4, not needed for Phase 1
- **SE-2** (sandboxing model) — 🟢 confirmed: Docker containers. Not yet implemented (PD-27).
- **SE-1** (secrets backend) — 🟢 confirmed and implemented: Windows Credential Manager for Phase 1, behind a pluggable `ISecretStore` contract (PD-9, done).
- CLI/API surface design — not yet started, and correctly deferred (AR-4)

---

*This is a consolidation document, not new design work — everything here traces back to `SAB_Design_Document_v0.1.2.md` and `open-questions.md` for design decisions, and `pre-development-checklist.md` for build status. Update those sources of truth first if anything here needs to change, then reflect the change back into this file.*
