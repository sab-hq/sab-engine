# Changelog

All notable changes to `sab-engine` are recorded here, in the order they happened — short and scannable on purpose. For full technical detail, exact test counts, and the real bugs found along the way, see `docs/pre-development-checklist.md` (closed, historical record of PD-1–PD-27/PD-30) and `docs/checklist-02.md` (active tracker for everything since); this file is the summary, those are the source of truth.

## [Unreleased] — Phase 1 development

### Added
- **PD-2** — Solution scaffold: 6 projects (`Core`, `Data`, `Orchestration`, `Agent`, `Execution`, `Api`), following the design doc's dependency layering.
- **PD-3** — PostgreSQL schema via EF Core + Npgsql, matching Section 4.5's data model exactly (`workflow_runs`, `plans`, `approvals`, `execution_results`, `target_state`, `notes`, `audit_entries`).
- **PD-4** — `WorkflowRunStateMachine`, enforcing the Section 4.1 state machine in code; illegal transitions throw rather than silently succeeding.
- **PD-5** — `WorkflowRunClaimService`, an atomic claim/lease pattern so multiple workers can safely share one queue.
- **PD-6** — `SabAgent`, real Semantic Kernel plan-drafting, with the Section 4.1/4.2 hard rule (no unapproved or rollback-less modules) enforced as defense in depth.
- **PD-7** — `PowerShellExecutor`, real local PowerShell interop via `Microsoft.PowerShell.SDK`.
- **PD-8** — Hash-linked, tamper-evident `AuditEntry` writes, built directly into the state machine (PD-4).
- **PD-9** — `WindowsCredentialManagerSecretStore`, the Phase 1 secrets backend, behind a pluggable `ISecretStore` contract.
- **PD-10** — Two-job CI pipeline (`.github/workflows/ci.yml`), split by OS to satisfy Postgres (Linux) and Windows Credential Manager (Windows) at once.
- **PD-11** — Lab environment: `sabengine-labwin01`, an Azure Windows Server 2022 VM, confirmed reachable.
- **PD-12** — `SabEngine.Modules`, the module manifest parser, matching Section 4.2's YAML schema.
- **PD-13** — `SabEngine.Modules.Cli`, a validator CLI, plus a CI pipeline in `sab-modules` that checks out this repo and runs it on every push.
- **PD-14** — `pre-flight-check`, the first real SAB module (in `sab-modules`), with 8 passing Pester tests.
- **PD-15** — `stage-patches`, the second module (in `sab-modules`), downloading Windows updates via the native WUA COM API, with 5 passing Pester tests.
- **PD-16** — `apply-patches`, the third module (in `sab-modules`), installing staged Windows updates via the native WUA COM API, with a genuine, non-trivial rollback (`wusa.exe /uninstall`) and 10 passing Pester tests. Also closes out PD-18 (rollback procedures for `stage-patches`/`apply-patches`).
- **PD-17** — `validate`, the fourth and final module (in `sab-modules`), confirming specific patches actually installed and the server is still healthy afterward, with 6 passing Pester tests. Also closes out PD-19 (unique IDs/`lab-validated` status) and PD-20 (`tests.lab_suite` files) for all four modules — both were satisfied as each module was written, not separate work. All four patching modules now exist, tested (29 tests total), and verified.
- **PD-21** — `patch-windows-server`, the first real workflow definition (in `sab-modules`), stringing all four modules together in order. Also closes out PD-22 (its unique ID). No formal schema or validator exists for workflow definitions yet, unlike modules — a real, flagged gap.
- **PD-23** — `WinRmExecutionConnector`/`WinRmExecutionSession`, the first real `IExecutionConnector` implementation — resolves a credential, opens a real WinRM/PowerShell-remoting session, runs a module script against it, and returns a structured result. Also closes out PD-24 (credential resolution at connection time). Verified with 55/55 tests passing, using an injectable seam that substitutes a real local PowerShell session for testing.
- **PD-25** — `CredentialHandleResolver`, a real least-privilege credential convention: a tier-specific handle (`"target:elevated"`) is tried first, falling back to a bare `"target"` handle if nothing tier-specific is registered. Verified clean on the first attempt, 59/59 tests total. Nothing calls this per-module yet — that wiring is tracked separately as PD-31 in `checklist-02.md`, since it depends on orchestration engine work that doesn't exist yet.
- **PD-26** — Per-target connection isolation: the blocking PowerShell invocation now runs on a dedicated thread rather than the shared pool, and a configurable timeout (default 10 minutes) calls PowerShell's `Stop()` — used here for the first time — to forcibly interrupt a hung pipeline rather than let it block indefinitely. Verified directly with a real concurrent test (a hanging execution against one target never delays a fast one against another), 61/61 tests total.
- **PD-27** — `DockerSandboxedExecutor`, real module sandboxing via disposable, network-isolated, read-only-mounted containers (`--network none` and a read-only mount, both directly tested). Real, flagged limitation: SAB's actual four modules are Windows-specific and don't run inside the default Linux container this uses — sandboxing them for real needs a Windows container image, a deliberate future step. CI proactively updated to exclude these tests from the Windows job. 66/66 tests total.
- **PD-30** — A real human approval web UI in `SabEngine.Api` — the actual, working implementation of recommend-and-approve mode (Section 2), not a mockup.

### Fixed
- **PD-3** — Missing `using Microsoft.Extensions.Configuration;`; `Microsoft.EntityFrameworkCore.Design` needed on the startup project, not just the one with the `DbContext`; a native Postgres install on port 5432 silently intercepting connections meant for Docker (moved to port 5433).
- **PD-5** — EF Core's InMemory provider can't translate `ExecuteUpdateAsync`; switched those tests to a real, disposable-per-test Postgres database.
- **PD-6** — Missing `using Microsoft.Extensions.DependencyInjection;`.
- **PD-7** — A `Microsoft.CodeAnalysis` version conflict between `Microsoft.PowerShell.SDK` and EF Core Design; PowerShell's `throw` (terminating error) wasn't being caught the way `Write-Error` (non-terminating) already was.
- **PD-11** — B-series Azure VM sizes returned `NotAvailableForSubscription` in two regions; used D2s_v3 instead, at meaningfully higher cost if left running.
- **PD-12** — The design doc's own Section 4.2 YAML example, `{ type: enum[success, failure] }`, wasn't actually valid YAML inside a flow mapping; fixed in both the doc and the parser's test fixture.
- **PD-15** — Two subtle test-only bugs: helper fake-object functions defined outside any Pester block weren't reliably visible in `It`/`BeforeEach` scope under Pester v6 (fixed by moving them into `BeforeAll`); a mocked function returning an *empty* `ArrayList` silently unrolled to zero pipeline output items, since `ArrayList` implements `IEnumerable` (fixed with `Write-Output -NoEnumerate`).
- **PD-16** — One invalid Pester assertion (`Should-Not`, not real syntax) caught and removed during self-review, before it ever reached Brock. Applying the `-NoEnumerate` lesson from PD-15 proactively meant no repeat of that bug.
- **PD-23** — A real bug in `IExecutionSession`'s interface itself (Core, from PD-2): `ExecuteAsync` had no way to receive a `WorkflowRunId`, even though its own return type requires one — fixed at the interface level, not worked around locally.
- **PD-30** — `Microsoft.NET.Sdk.Web` auto-including `appsettings.json`, duplicating an explicit `<Content>` block left over from the old `Sdk`; CSS blocks using the wrong brace-escaping convention for interpolated raw string literals (`CS9006`).

### Changed
- Corrected Section 9's Sequencing Note, which had overstated that `sab-kb` must ship strictly before `sab-engine`'s Phase 1 — the actual resolution is both in parallel.
- Confirmed "OSML" and "ESS" as official shorthand for the module library and Engine State Store, respectively.
- Confirmed Docker containers as the module sandboxing mechanism (SE-2) and Windows Credential Manager as the Phase 1 secrets backend (SE-1).
- Added `checklist-02.md`, a continuation of `pre-development-checklist.md` for anything surfacing after PD-30 (starting at PD-31) — keeps the original checklist from growing indefinitely.
- **Clean break: `pre-development-checklist.md` closed as a historical record; `checklist-02.md` is now the sole active tracker.** PD-28 and PD-29 (both `Not Started`) moved over with their IDs unchanged, along with the previously-deferred items table (AR-4, SC-1/2, PR-1/2, MP-1, SE-3) and every open thread flagged in the closed checklist's entries (the untested WinRM network path, `CredentialHandleResolver`'s per-module wiring, Docker sandboxing's Windows-container gap, the still-stub `OrchestrationEngine`).
