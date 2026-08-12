# Start Here: Actually Using SAB

*A practical guide for systems administrators who've read the concepts and want to get hands-on.*

---

## In one sentence

If you've read `what-is-sab.md`, `workflows.md`, and `modules.md`, and now want to know "okay, how do I actually run this thing" — here's exactly what you can try today, verified and real, and an honest accounting of what genuinely isn't ready yet.

> **Read this before you get your hopes up:** SAB cannot yet patch a real server end to end. That specific milestone (`checklist-02.md`, PD-28) is still open, blocked on real orchestration wiring that hasn't been built yet. What follows is everything that *is* real and working today — which is substantial — laid out honestly, without pretending it's further along than it is.

## Before you start

You'll need two repos, sitting next to each other as sibling folders (`sab-engine` and `sab-modules` — the code assumes this layout for a few cross-repo commands below):

- **.NET 8 SDK**
- **Docker Desktop**, running
- **PowerShell 7+** with **Pester v5 or later** (`Install-Module Pester -MinimumVersion 5.0 -Force -SkipPublisherCheck` if you don't have it)
- **git**

## What you can actually do today

### 1. Build and test `sab-engine` itself

```powershell
cd sab-engine
docker compose up -d
dotnet build
dotnet test
```

This runs the full test suite across every part of the engine that's been built so far — the state machine, the AI agent's plan-drafting and hard-rule enforcement, PowerShell/WinRM interop, the credential store, module sandboxing, and more. As of this writing that's 66 tests, all passing. This is a genuinely good way to get a feel for the codebase's actual quality before reading a single line of source.

### 2. Validate a real module against the real parser

```powershell
dotnet run --project src/SabEngine.Modules.Cli -- ../sab-modules/modules
```

This walks every module in `sab-modules` and confirms its manifest is valid against the real Section 4.2 schema parser — the same check that runs automatically on every push to `sab-modules`. You should see all four real modules (`pre-flight-check`, `stage-patches`, `apply-patches`, `validate`) reported valid.

### 3. Run the actual module tests

```powershell
cd sab-modules
Invoke-Pester ./modules/pre-flight-check/pre-flight-check.tests.ps1
Invoke-Pester ./modules/stage-patches/stage-patches.tests.ps1
Invoke-Pester ./modules/apply-patches/apply-patches.tests.ps1
Invoke-Pester ./modules/validate/validate.tests.ps1
```

Every external dependency (Windows Update's COM API, disk/service checks) is mocked, so these are completely safe to run on any machine — nothing here touches real Windows Update or installs anything. This is the real, tested logic behind every step of the patching workflow, not a description of what it's supposed to do.

**Worth knowing:** don't run the module `.ps1` files directly (outside of Pester) — `stage-patches.ps1` and `apply-patches.ps1` specifically will trigger genuine Windows Update activity on whatever machine runs them. See `modules.md` for the full detail on what each one does.

### 4. See the human approval flow — the closest thing to "using SAB" today

```powershell
cd sab-engine
dotnet run --project src/SabEngine.Api
```

Open the URL it prints (something like `http://localhost:5000`). Click **"Create a demo run"**, then **"Review,"** then **Approve** or **Decline**.

**Be clear-eyed about what this is and isn't:** the plan you're reviewing is seeded demo data, clearly labeled as such — nothing here actually reaches a real server. But the approval mechanism itself is completely real: your click genuinely drives the workflow's state machine and writes a real, auditable approval record, exactly as it would for a genuine patch job once one exists. This is recommend-and-approve mode (`recommend-and-approve-mode.md`), actually running, not just described.

## What's honestly not ready yet

- **A real, end-to-end patch job.** Nothing yet strings the four modules together into an actual running workflow against a real target — that's `checklist-02.md`, PD-32, still not started. The design for how it should behave (including crash recovery and rollback scoping) is fully settled — see `crash-recovery.md` and `rollback-scoping.md` — but none of it is implemented in code yet.
- **A real AI-proposed plan.** The agent's plan-drafting logic is real and tested, but it isn't wired to an actual language model — that needs a real API key, deliberately not hardcoded into the repo.
- **A confirmed WinRM connection to a real server.** The connector code is built and tested against a substituted local session, but the actual network path to a real Windows Server has never been tried.
- **Sandboxing of SAB's actual modules.** The Docker sandboxing mechanism works correctly, but only for generic scripts — SAB's real modules are Windows-specific and need a Windows container image, which isn't configured.

`checklist-02.md` is the exact, current, authoritative list of what's open — check there for the real state of anything not covered above.

## If you want to go further

- **Stand up your own lab VM** to experiment with, following the same low-cost Azure pattern already used for the project's own lab server — see `pre-development-checklist.md`'s PD-11 entry for the exact recipe (VM size, region, budget alert setup) and the real cost gotchas hit along the way.
- **Read the full design doc** (`SAB_Design_Document_v0.1.2.md`) for the complete technical architecture behind everything above.
- **Pick up open work** — `checklist-02.md` lists exactly what's next, with enough written-out design detail on most items that you shouldn't be starting from nothing.

## Getting familiar — where to look next

- **`what-is-sab.md`** — if you haven't read the basics yet, start there instead of here.
- **`workflows.md`** / **`modules.md`** — what you're actually running in steps 2–4 above.
- **`sab-engine-overview.md`** — a consolidated, section-by-section reference for exactly what's built versus not, across the whole repo.
- **`checklist-02.md`** — the authoritative, current list of what's still open.

---

*This document describes what's actually runnable today, as of the point it was written — it will go stale as more of PD-32 and beyond gets built. If something above no longer works the way it's described, that's a signal this file needs updating, not that you did something wrong.*
