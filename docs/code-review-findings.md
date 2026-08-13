# sab-engine — Code Review Findings

**Date:** 2026-08-12
**Scope:** Full read-through of the solution (`src/`, `tests/`, `docs/`, CI) plus a verification pass: `dotnet build` and every test suite that can run without Docker.
**Status:** Findings recorded below. Nothing in this doc changes the source of truth in `checklist-02.md` — these are observations and recommendations, not new PD- items. Whether any become tracked items is a judgment call for whoever picks them up.

---

## Verification results (as of this review)

| Check | Result |
|---|---|
| `dotnet build` (whole solution) | ✅ 0 errors, 14 warnings (all `CA1416`, Windows-only Credential Manager tests — expected, matches the CI split) |
| `SabEngine.Core.Tests` | ✅ 1/1 passing |
| `SabEngine.Modules.Tests` | ✅ 10/10 passing |
| `SabEngine.Agent.Tests` | ✅ 7/7 passing |
| `SabEngine.Orchestration.Tests` (state machine only) | ✅ 5/5 passing |
| `SabEngine.Execution.Tests` (excluding Windows Credential Manager + Docker sandbox) | ✅ 18/18 passing |
| Not run locally | Postgres claim/lease tests (need Docker), Windows Credential Manager tests (need Windows — verified by CI), Docker sandbox tests (need Linux containers) |

Summary: **47/47 locally-runnable tests pass**, and the build is clean. This is consistent with the repo's own tracking (`pre-development-checklist.md` closed at 66/66 through PD-27/PD-30).

---

## What's strong

- **Documentation discipline is genuinely good.** Every code file links back to the design doc and PD- item it implements; `checklist-02.md` is an honest, living tracker; `docs/learn/` gives 19 plain-language guides. Unverified assumptions are flagged in comments rather than hidden (e.g. `WinRmExecutionSession.cs:24` on first use of `PowerShell.Stop()`).
- **Layering is clean.** `SabEngine.Core` is dependency-free; `SabEngine.Modules` is the only project with a first-class external package (YamlDotNet); the connectors and secrets store are behind real interfaces (`IExecutionConnector`, `ISecretStore`).
- **Core mechanics are real, not stubs.** The state machine (`WorkflowRunStateMachine.cs`) enforces transitions in code and writes a hash-linked audit chain in the same operation; the claim service (`WorkflowRunClaimService.cs`) does genuinely atomic conditional `UPDATE`s via `ExecuteUpdateAsync`; the approval web UI in `SabEngine.Api/Program.cs` drives real transitions and records real `Approval` rows.
- **Test quality is high.** Tests for the agent use a fake `IChatCompletionService` rather than a live model; claim-service tests deliberately run against a real disposable Postgres because InMemory can't translate `ExecuteUpdateAsync` (documented reasoning in `PostgresTestDatabase.cs`).

---

## Significant gaps

### 1. `OrchestrationEngine` is still an empty stub
`src/SabEngine.Orchestration/OrchestrationEngine.cs:16` contains only a TODO comment. The core value of the product — carrying an approved plan through execution, calling modules in sequence, auto-triggering rollback on failure — doesn't exist. This is tracked as **PD-32**, and both **PD-28** (Phase 1 exit criteria) and **PD-29** (module promotion) are blocked on it.

### 2. `Executing` runs have no crash-recovery path in code
`WorkflowRunClaimService.cs:33` claims `Requested`, `Declined`, `Approved`, and `Failed` — deliberately **not** `Executing`. The design for how to handle a worker that dies mid-execution is fully worked out (PD-33: reconnect, health-check, branch on reality), but the mechanism itself — a lease-expiry re-claim of `Executing` runs, mirroring the PD-5 pattern — doesn't exist anywhere yet. Today a worker crash mid-run leaves the run permanently orphaned. This is the concrete, in-code half of PD-33 and it isn't itemized as its own task.

### 3. Security hygiene issues
- **Stored XSS in the approval UI.** `SabEngine.Api/Program.cs:151` renders the agent's `plan.Reasoning` (model-produced, arbitrary text) into HTML with no encoding. A model response containing markup would render as markup in an approver's browser. The workflow ID, target, and other DB-backed values interpolated in the same templates carry the same risk class, though those are at least not model-produced.
- **Unicode escape bugs in claim service messages.** `WorkflowRunClaimService.cs:23` and three other doc comments there contain literal `\u2014` in place of an em dash. These leak into runtime strings (the "nothing to do right now" guidance) instead of rendering as `—`. Cosmetic, but a real defect introduced when the code was authored.
- **Dev DB credentials hardcoded as the code fallback.** `SabEngineDbContextFactory.cs:22` embeds the docker-compose credentials directly in the design-time factory. They're explicitly local-dev-only (correctly flagged as not real secrets), but the hardcoded fallback means any future path that instantiates the factory without the env var quietly uses them.
- **Log-injection surface (minor).** `SabAgent` rethrows `PlanValidationException` messages that embed raw model output (and the run's `target`/`workflowId`), so an exception message can carry model-controlled text into logs.

### 4. Approver is shown a possibly-changed plan
`SabEngine.Api/Program.cs:172` re-fetches the *latest* plan for a run at decision time rather than binding to the plan that was actually rendered to the approver. For Phase 1 with one plan per run this is fine; it becomes a real correctness issue as soon as a declined plan is revised (Declined → PlanDrafted → PendingApproval), since the decision endpoint would then show/reason about the newest plan even if the human approved an older one.

---

## Smaller issues / hygiene

- **`tests/modules-test-output.txt` is committed** at the repo root of `tests/` — a stray UTF-16 output artifact, not covered by `.gitignore` (which is otherwise good: `bin/`, `obj/` correctly ignored).
- **CI depends on unverified package pins.** `SabEngine.Modules.csproj:19` (YamlDotNet 16.2.1) is a "best-effort guess from memory" per its own comment; Semantic Kernel and PowerShell.SDK versions are in the same category per PD-6/PD-7 notes. These resolved and built fine here, but should be re-verified deliberately.
- **Missing pieces already flagged internally** (confirmed still true): no workflow YAML parser/validator (only modules have one, PD-12/13); no module-catalog loader (the agent's available-module list is still hand-supplied, PD-6 note); no real model API key wired; the real WinRM network path (port 5985) is untested against the lab VM; `pre-flight-check` doesn't snapshot pre-existing installed patches (PD-37); `DockerSandboxedExecutor` can't sandbox the real Windows-specific modules yet (needs a Windows container image).
- **`WinRmExecutionConnector.ConnectAsync` builds a `SecureString` from a managed `string`** (`WinRmExecutionConnector.cs:55`) — a transient, unavoidable copy of the plaintext password in managed memory. Acceptable for the interface as designed, but worth noting given the project's own strict least-privilege stance.

---

## Recommendations (in priority order)

1. **Add an explicit item for the `Executing` re-claim gap** in `checklist-02.md` — it's a small, well-understood piece of PD-33 (lease-expiry re-claim mirroring the PD-5 pattern) and it's the only thing standing between a crashed worker and a permanently stuck run.
2. **Fix the stored-XSS surface in the approval UI** before anyone uses it against a real server — encode interpolated values in the `Program.cs` HTML templates (or route the UI through a templating path that HTML-encodes by default).
3. **Fix the `\u2014` leaks** in `WorkflowRunClaimService.cs` (cosmetic, one-line each).
4. **Bind the decision endpoint to the exact plan shown**, or add a guard so a revised plan can't be silently decided against.
5. Delete `tests/modules-test-output.txt` and add `tests/*.txt`-style ignores if it was a one-off.

None of these are blockers for the existing green test suite — they're the difference between a solid Phase 1 foundation and a shippable one.