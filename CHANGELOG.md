# Changelog

All notable changes to `sab-engine` are recorded here, in the order they happened — short and scannable on purpose. For full technical detail, exact test counts, and the real bugs found along the way, see `docs/pre-development-checklist.md`; this file is the summary, that file is the source of truth.

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
- **PD-30** — `Microsoft.NET.Sdk.Web` auto-including `appsettings.json`, duplicating an explicit `<Content>` block left over from the old `Sdk`; CSS blocks using the wrong brace-escaping convention for interpolated raw string literals (`CS9006`).

### Changed
- Corrected Section 9's Sequencing Note, which had overstated that `sab-kb` must ship strictly before `sab-engine`'s Phase 1 — the actual resolution is both in parallel.
- Confirmed "OSML" and "ESS" as official shorthand for the module library and Engine State Store, respectively.
- Confirmed Docker containers as the module sandboxing mechanism (SE-2) and Windows Credential Manager as the Phase 1 secrets backend (SE-1).
