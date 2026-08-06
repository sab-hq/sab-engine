# SAB System Design Document
### System Administration Builder — Design Foundation

**Version:** v0.1.2
**Document Status:** Living Document — Open for Iteration and Refinement
**Last Updated:** August 4, 2026
**Purpose:** High-level design exploration and framework planning for SAB, an open system that standardizes and automates system administration workflows using reusable modules and AI agent orchestration.

---

## Changelog

- **v0.1.2** (Aug 4–6, 2026) — Added RMM/PSA/MCP connector research to Section 6 (specific integration priorities: NinjaOne, ConnectWise Manage, Autotask PSA, Ansible Automation Platform/AWX, Hudu, plus partner-program notes and a phased rollout) and to Section 8 (2026 MCP ecosystem findings, including documented security risks that reinforce the recommend-and-approve principle). Also closed out most remaining "to be filled in" placeholders across Sections 2–9 with concrete first-draft detail (state machine, module metadata schema, connector interface, Engine State Store data model, API surface) — items that genuinely need Brock's input (revenue share %, compliance framework target, target dates) are explicitly flagged rather than invented, and collected in Section 10. Reformatted headers throughout so the label and its following text are visually separated, not run together. Confirmed Docker containers as the module sandboxing mechanism (SE-2). Confirmed every module and every workflow requires a unique ID (new AR-5); reflected in Sections 3 and 4.2. Confirmed "Open Source Module Library (OSML)" as the official name for the module library / `sab-hq/sab-modules` repo (new AR-6); reflected in Sections 4.2, 5.1, and 9. Confirmed "ESS" as the official shorthand for the Engine State Store (new AR-7); reflected in Section 4.5. Added a full set of plain-language companion docs (`what-is-sab.md` through `open-source-module-library.md`) and cross-linked them throughout this document. Added `pre-development-checklist.md`, a dependency-ordered pre-code task list (PD-1 through PD-29). **Corrected Section 9's Sequencing Note**, which had overstated — on this document's own initiative, not Brock's instruction — that `sab-kb` must ship strictly before `sab-engine`'s Phase 1; the actual, twice-confirmed resolution (RC-5 originally, PD-1 again this session) is that the two are worked on in parallel. **Phase 1 development began (Aug 6):** real C#/.NET code now exists and is verified — the solution scaffold, the PostgreSQL/EF Core schema, and a working, tested state machine with hash-linked audit logging (PD-1 through PD-4 and PD-8, all done and confirmed by Brock). This is the first point where `sab-engine` has actual code, not just design. **Concurrency (PD-5) is also done** — multiple workers can now safely claim work from the same queue via an atomic Postgres `UPDATE ... WHERE`, verified with 20/20 tests passing. This surfaced the project's first real testing-strategy correction: EF Core's InMemory provider can't translate the atomic update this depends on, so those specific tests now run against a real, disposable-per-test Postgres database — meaning **Docker must be running locally for `dotnet test` to fully pass from here on**, not just for the PD-3 migration step. `pre-development-checklist.md` and `sab-engine-overview.md` are the up-to-date sources for exact build status — not duplicated here. **This design document itself moved into `docs/design/`** (Aug 6) — `open-questions.md` and `pre-development-checklist.md` stay at `docs/` root; the two phase docs live at `docs/phases/`; and the full plain-language companion series, now including `sab-engine-overview.md` too, lives in `docs/learn/`. Every link in this file to a sibling doc has been updated to `../learn/filename.md` for the companion docs, and `../filename.md` or `../phases/filename.md` for everything else, accordingly.
- **v0.1.1** (Aug 2, 2026) — Full document buildout: Executive Summary, workflow/module architecture, all of Section 4 including the new Engine State Store (4.5), Marketplace (5.1), Integration (6), Security (7), Existing Solutions (8), and the Phase 0–4 roadmap (9). Tech stack, licensing, and repo structure confirmed via `open-questions.md`.
- **v0.1.0** — Initial document. Section 2 (System Requirements and Goals) established as the founding principle; all other sections were placeholders.

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [System Requirements and Goals](#2-system-requirements-and-goals)
3. [Core Architecture Overview](#3-core-architecture-overview)
4. [Component Breakdown](#4-component-breakdown)
   - 4.1 Orchestration Engine
   - 4.2 Module Library System (OSML)
   - 4.3 AI Agent Layer
   - 4.4 Execution Environment
   - 4.5 Engine State Store (ESS) *(renamed from "Shared Knowledge Base (SAB-KB)" — see RC-3)*
5. [Extensibility and Plugin System](#5-extensibility-and-plugin-system)
   - 5.1 SAB Engine Marketplace *(renamed from "SAB Marketplace" — see RC-3)*
6. [Integration with Existing Enterprise Tools](#6-integration-with-existing-enterprise-tools)
7. [Security and Compliance Considerations](#7-security-and-compliance-considerations)
8. [Existing Solutions and Learnings](#8-existing-solutions-and-learnings)
9. [Development Roadmap and Next Steps](#9-development-roadmap-and-next-steps)
10. [Open Questions and Future Considerations](#10-open-questions-and-future-considerations)

---

## Companion Beginner Documentation

Each core concept in this document also has a plain-language companion doc, written for beginners and cross-linked to each other — useful for onboarding new contributors or anyone who wants the short version before the technical one. If anything in these ever disagrees with this document, this document wins.

| Doc | Covers |
|---|---|
| [`what-is-sab.md`](../learn/what-is-sab.md) | The overall elevator pitch — start here |
| [`workflows.md`](../learn/workflows.md) | Section 3/4.2 — the recipe concept |
| [`modules.md`](../learn/modules.md) | Section 4.2 — the individual step/building-block concept |
| [`ai-agent-layer.md`](../learn/ai-agent-layer.md) | Section 4.3 — how SAB decides what to propose |
| [`recommend-and-approve-mode.md`](../learn/recommend-and-approve-mode.md) | Section 2 — the human approval gate |
| [`orchestration-engine.md`](../learn/orchestration-engine.md) | Section 4.1 — what actually runs an approved plan |
| [`execution-environment.md`](../learn/execution-environment.md) | Section 4.4 — how SAB reaches a real server |
| [`engine-state-store.md`](../learn/engine-state-store.md) | Section 4.5 — SAB's memory of past runs (ESS) |
| [`ess-vs-sab-kb.md`](../learn/ess-vs-sab-kb.md) | Section 4.5 / `open-questions.md` RC-1–RC-3 — why SAB-KB covers a gap ESS structurally can't reach, not a "bigger" version of it |
| [`what-is-sab-kb.md`](../learn/what-is-sab-kb.md) | Section 1 — what the separate, paid SAB-KB product actually is |
| [`community-contribution-framework.md`](../learn/community-contribution-framework.md) | Section 5 — how anyone can contribute |
| [`open-source-module-library.md`](../learn/open-source-module-library.md) | Section 4.2/9 — the OSML, where modules and workflows actually live |

Also see [`phases/phase01.md`](../phases/phase01.md) and [`phases/phase02.md`](../phases/phase02.md) for what each development phase actually covers and how far along it is.

---

## 1. Executive Summary
*High-level overview of what SAB is, the problem it solves, and why it matters. Define the vision and core value proposition.*

**What SAB Is**

SAB (System Administration Builder) is an open system that standardizes and automates the manual, repetitive work system administrators and system engineers do every day — starting with Windows Server patching — by turning that work into reusable, standardized modules that an AI agent orchestrates on the SA/SE's behalf.

**The Problem**

Much of system administration is process that exists mostly in people's heads and in one-off scripts: a specific SA knows how to safely patch a server, but that knowledge is rarely captured in a standardized, reusable, auditable way. This makes the work slow to onboard new people into, inconsistent across teams, and risky to automate naively — a bad script run against production can cause real damage. Existing automation and RPA tools tend to be either too broad and generic, or expensive and enterprise-locked, without being purpose-built for the way sysadmins actually think about their work.

This isn't a hypothetical problem — it's a documented, current one. Industry research (see `market-research.md`) shows IT teams lose roughly a third of their working capacity to manual, repetitive work, and sysadmin burnout runs close to 49%. More specifically, a 2026 industry survey identified "brain debt" as a defining pattern: a small number of senior people end up carrying disproportionate institutional knowledge and decision weight, becoming the default escalation path for anything risky — this is the exact knowledge-silo problem the separate `sab-kb` product (SAB's Knowledge & Documentation Engine) is built to address, by capturing that tribal knowledge from Email/Teams before it walks out the door. `sab-engine` — described in the rest of this document — is a related but distinct product: it automates the execution side of sysadmin work rather than the knowledge-capture side. See Section 9 and `open-questions.md` (RC-1 through RC-5) for how these two products currently relate.

There's also a live opportunity in the market: Microsoft deprecated WSUS in 2024, and most organizations still running it haven't fully migrated to a replacement as of mid-2026. Rather than positioning against WSUS, SAB's first connector is designed to work alongside it — reading existing WSUS/SCCM data and standardizing the workflow around it — which is both a practical, low-friction entry point for organizations not ready to rip out existing infrastructure, and a foundation for treating Microsoft as a partner rather than something SAB is trying to replace (see Section 6).

**The Approach**

SAB is built around a simple distinction: **modules** are small, reusable, well-tested units of work (built on tools SAs already trust — PowerShell, Bash, IaC), and **workflows** are the ordered recipes that string modules together to accomplish a real task, mirroring the process an SA would normally walk through manually. An AI agent sits on top, proposing what to run and why — but every action starts in a **recommend-and-approve** mode, with a human in the loop, and every module ships with a tested rollback path from day one. Reliability is earned in stages, not assumed.

Within `sab-engine` itself, an internal state store (Section 4.5) keeps the AI agent, the orchestration engine, and humans working from the same information about this engine's own runs — what's been executed, what's worked, what hasn't — so trust and autonomy can grow over time based on an actual track record, not a leap of faith. This is a narrower thing than the `sab-kb` product; see Section 4.5 for the current scope and RC-1/RC-2 for how (or whether) the two connect in the future.

**Why It Matters**

For SAs and SEs, SAB turns tribal knowledge into standardized, shareable, auditable process — without asking anyone to trust a black box on day one. For the broader community, an open core means the module library grows through real-world contribution rather than one company's roadmap alone.

This also lines up with what's actually blocking AI-driven infrastructure automation industry-wide: 2026 research shows the top-cited barrier to adopting agentic AI for autonomous infrastructure actions is trust, not capability — and there are real documented incidents of AI agents causing serious, irreversible production damage when deployed without that trust being earned first. SAB's recommend-and-approve model and mandatory rollback path aren't just cautious defaults; they're a direct answer to the exact reason similar efforts stall elsewhere.

**The Business Model**

SAB follows an open-core model, deliberately modeled on *early* Red Hat specifically — not just "open source with a paid tier" in the abstract. Two things define that model and are worth carrying forward precisely: Red Hat used permissive licensing and never tried to legally prevent competitors from rebuilding or rehosting their code (CentOS ran for years as a free RHEL clone with Red Hat's blessing); their value capture came entirely from the subscription relationship — support, updates, certification, and stability — not from license enforcement. Notably, Red Hat's own patch/lifecycle management product (Red Hat Network Satellite) was itself open-sourced in 2008, and a competitor built a rival product on that code — Red Hat still thrived, because the subscription relationship was the actual moat.

SAB follows the same shape: the core orchestration engine and module library are open source under a permissive license, driving adoption, trust, and community contribution without legal restriction on reuse. Monetization mirrors Red Hat's Fedora/RHEL split — rather than gating features, the free module library stays fast-moving and community-driven (like Fedora), while a paid tier offers a hardened, tested, support-backed set of modules and workflows for production use (like RHEL). Additional revenue comes from managed hosting, enterprise connectors, and compliance/industry-specific workflow packs, plus a marketplace (Section 5.1) where developers and vendors can publish — and over time sell — content built on the same open contracts.

**Starting Point**

The first proof of concept is Windows Server patching on-premises — chosen because it's routine, high-value, and heavily PowerShell-based already, making it a clean, well-understood place to prove the architecture before expanding to other workflows and environments (Linux, cloud, hybrid).

---

## 2. System Requirements and Goals
*What must the system do? What are the non-negotiable requirements versus stretch goals?*

> 📖 Plain-language companion: [`recommend-and-approve-mode.md`](../learn/recommend-and-approve-mode.md)

**Core Design Principle: Reliability and Gradual Autonomy**

The system must be solid and predictable before it is trusted to act autonomously. This is non-negotiable given the system touches production infrastructure. Implications:
- The AI agent layer should start in a **recommend-and-approve** mode — it proposes an action and reasoning, a human approves before execution. Full autonomy is a later capability, not a launch requirement.
- Every module should have a well-tested rollback/undo path from day one. Cheap, reliable recovery from failure is what eventually earns the trust needed for more autonomy.
- New workflows and modules should be validated against low-stakes/lab environments before being trusted against production systems.
- "Boring and reliable" is a feature, not a limitation — especially for early open source adoption, where sysadmins need to trust the core before they'll run it against anything that matters.

**Non-negotiable vs. stretch (v0.1.2 pass).** Reviewing the document as a whole, the non-negotiables are consistently the same four bullets above — every other section either implements them or defers to them. Everything else in this document (cloud/hybrid support, the marketplace, approve-by-exception autonomy, specific compliance frameworks) is explicitly a stretch goal, sequenced later in Section 9's roadmap. This section doesn't need a longer requirements list so much as it needs to keep being the one place that list is confirmed unchanged as the rest of the document grows — which, as of v0.1.2, it is.

> *(A more traditional functional requirements list — specific things the system must be able to do, e.g. "must support N concurrent workflow runs" — genuinely needs your input rather than being inferred from what's already written; flagged as PR-1/PR-2 in `open-questions.md` rather than duplicated here.)*

---

## 3. Core Architecture Overview
*How all the pieces fit together at a 30,000-foot level — data flow, component relationships, overall design philosophy.*

> 📖 Plain-language companions: [`what-is-sab.md`](../learn/what-is-sab.md), [`workflows.md`](../learn/workflows.md), [`modules.md`](../learn/modules.md)

**Key Concept: Workflow vs. Module**

- A **module** is a single reusable unit of work (e.g. `check-patch-status`, `apply-patch`). Modules are "dumb" and reliable — they do one job, don't make decisions, and don't know about the bigger picture.
- A **workflow** is the ordered recipe that strings modules together to accomplish a real-world use case (e.g. "patch this server" = pre-flight check → stage → apply → validate → rollback-if-needed). Workflows capture the process an SA/SE would normally walk through manually.
- **Every module and every workflow has its own unique ID** (see `open-questions.md`, AR-5, confirmed) — this is how they're referenced unambiguously throughout the rest of the system; see Section 4.2 for the full detail.

**End-to-End Flow**

The system is designed around how an SA/SE actually works: they have a job to do, they select the matching workflow, and the system takes it from there.

1. **Workflow Selection** — The SA/SE identifies the task they need to do and selects the corresponding pre-built workflow from SAB's library (e.g. "patch this server").
2. **AI Agent Layer** — Looks at the current state of the target system (patch history, known risk factors, etc.) and proposes a plan: which modules to run, in what order, and why.
3. **Human Approval Gate** — In line with the reliability-first principle (Section 2), a person reviews the proposed plan and approves or rejects it before anything executes.
4. **Orchestration Engine** — Once approved, drives execution: calls each module in sequence, tracks state (running/succeeded/failed), and triggers rollback if something breaks.
5. **Module Library** — Supplies the actual units of work the engine calls. Modules are reusable and environment-agnostic where possible.
6. **Execution Environment** — Reaches out and runs the work against the actual target system (e.g. WinRM for on-prem to start), abstracted so cloud/hybrid connectors can slot in later without changing anything upstream.
7. **Feedback Loop** — Results and logs flow back into the orchestration engine's state, into an audit log, and into the **Engine State Store** (see Section 4.5) — which keeps the AI agent, orchestration engine, and humans all working from the same information about this engine's own runs, so future recommendations improve over time.

**Design Philosophy**

Each layer has one job and doesn't reach into another layer's responsibility:
- The **AI Agent** decides *what and why*.
- The **Orchestration Engine** decides *how and when*.
- **Modules** just *do the work*, dumbly and reliably.
- The **Execution Environment** abstracts *where* — so on-prem vs. cloud is invisible to everything above it.

**Tech Stack (see `open-questions.md`, TS-1/TS-2/TS-3 — confirmed)**

**C#/.NET** for both the orchestration engine and AI agent layer (the latter using Microsoft's Semantic Kernel), and **PostgreSQL** for state persistence. The .NET choice is deliberate beyond technical merit — native PowerShell interop and building on Microsoft's own stack directly support the partnership positioning in Sections 6 and 8. Modules themselves stay in PowerShell/Bash regardless, per the module contract (4.2) — this only affects what the engine and agent are written in. **License (LM-1, confirmed): Apache 2.0.**

**Data flow, made concrete (v0.1.2 detail).** The seven-step flow above maps directly onto the state machine introduced in Section 4.1: step 1 creates a `WorkflowRun` in `Requested`; step 2 produces a `Plan` and moves it to `PlanDrafted`/`PendingApproval`; step 3 is the `Approved`/`Declined` transition; step 4 is `Executing`; steps 5–6 are what happens inside `Executing` (the engine calling into the module library, which calls into the execution environment); step 7 is the `Completed`/`Failed`(/`RolledBack`) transition plus the write into the Engine State Store. Nothing here changes the architecture already described — it's the same diagram, just with the state names attached so Section 4.1's data model and this section's flow description don't drift apart as both get implemented.

> *(A literal box-and-arrow diagram would help once this moves toward implementation — worth generating directly against the state machine above rather than freehand, so the two never disagree.)*

---

## 4. Component Breakdown
*Deep dive into each major component individually.*

### 4.1 Orchestration Engine
- How does it coordinate tasks and workflows?
- How does it manage state and execution flow?
- What are the inputs and outputs?

> 📖 Plain-language companion: [`orchestration-engine.md`](../learn/orchestration-engine.md)

**Role in the System**

The orchestration engine is the "how and when" layer (see Section 3). It doesn't decide *what* to do — that's the AI agent's job — it takes an approved plan and reliably carries it out.

**Inputs**

- An **approved workflow plan**: the specific workflow (recipe) selected, the target system(s), and any parameters the AI agent proposed and a human approved (e.g. which patches, which maintenance window).
- **Module definitions**: pulled from the Module Library, including each module's expected inputs, outputs, and rollback procedure.

**Core Responsibilities**

1. **Sequencing** — Executes modules in the order the workflow defines, respecting dependencies (e.g. don't apply patches until pre-flight check passes).
2. **State tracking** — Maintains a live record of where a workflow run is: which module is executing, which have succeeded/failed, and what the target system's last-known state was. This state needs to be durable (survives a crash/restart) since infrastructure operations can be long-running.
3. **Failure handling / rollback** — If a module fails, the engine is responsible for invoking that module's (or the workflow's) rollback path automatically, per the reliability principle in Section 2. This should not require the AI agent or a human to intervene manually just to trigger an undo.
4. **Logging / audit trail** — Every action taken, by which module, with what result, gets logged — this feeds both Section 7 (Security and Compliance) and the feedback loop back to the AI agent.

**Outputs**

- Real-time status (for whoever kicked off the workflow to monitor)
- A final result: success, partial failure + rollback completed, or failure requiring manual intervention
- Structured logs/audit record of the entire run

**Design Decision (see `open-questions.md`, AR-1):** the engine is a stateless task runner, not a single long-running service — state persists externally (PostgreSQL, per TS-3) so it survives crashes/restarts, and scaling to multiple concurrent targets is a matter of running more workers against shared state rather than making one process more complex.

**State machine (v0.1.2 detail).** Each workflow run moves through a fixed set of states, persisted so a crashed worker can resume rather than lose track of where it was:

`Requested → PlanDrafted → PendingApproval → Approved | Declined → Executing → Completed | Failed → RolledBack (if Failed)`

This is what makes "what's currently pending my approval" a simple query rather than a guess, and what makes the audit trail (Section 7) a record of real state transitions rather than free-text log lines. Concretely, this suggests a `WorkflowRun` table (id, current state, timestamps, target) plus child records for the `Plan` (proposed modules/params/reasoning), the `Approval` (who, when, which plan version), and the `ExecutionResult` (per-module outcome, whether rollback fired) — each state transition writes an immutable `AuditEntry`.

**Concurrency model.** Because the engine is stateless (AR-1), concurrency is handled by running multiple workers against the same PostgreSQL-backed state rather than adding threading complexity inside one process. A worker claims a `Requested` or `Approved` run (e.g. via a row-level lock or a claim/lease pattern), executes it, and releases it on completion or failure — this is the same pattern SC-1/SC-2 already point toward for scaling across concurrent targets, just made concrete here rather than left implicit.

> *(Further detail — exact locking/claim mechanism, worker health/timeout handling — to be added once Phase 1 implementation starts)*

### 4.2 Module Library System (OSML)
*See Section 3 for the foundational workflow-vs-module distinction: modules are atomic, reusable units of work; workflows are the recipes that string modules together for a specific use case. This system — conceptually and as the actual `sab-hq/sab-modules` repository — is officially named the **Open Source Module Library (OSML)**, per `open-questions.md` AR-6.*

> 📖 Plain-language companions: [`modules.md`](../learn/modules.md), [`workflows.md`](../learn/workflows.md), [`open-source-module-library.md`](../learn/open-source-module-library.md)

- How are reusable components structured and catalogued?
- How do modules interact with the orchestration engine?
- Standards and conventions for building modules?
- How are workflows (recipes) defined, stored, and versioned separately from the modules they call?

**Role in the System**

The module library is where the actual work lives — it's the "do the work" layer (see Section 3). Modules are deliberately dumb: they don't make decisions, they just perform one well-defined action reliably and report back what happened.

**The Module Contract**

For the orchestration engine to call any module interchangeably, every module needs to follow the same standard shape, regardless of what it actually does or what language it's written in underneath (PowerShell to start, Bash/IaC later). At minimum, each module needs:
- **A unique ID** — required, not optional (see `open-questions.md`, AR-5, confirmed). This is how the orchestration engine, the AI agent, workflow definitions, and eventually the Marketplace all refer to this exact module without ambiguity — no two modules, from any source, can share one.
- **Metadata** — name, description, version, risk level, what environment(s) it supports
- **Inputs** — a defined, typed set of parameters it expects
- **Outputs** — a defined, typed result (success/failure + relevant data)
- **Rollback procedure** — required, not optional, per the Section 2 reliability principle. If a module can't be safely undone, that's itself important metadata the AI agent needs to know before proposing it.
- **Tests** — validating the module works as expected, ideally runnable against a lab/low-stakes environment before being trusted

**Cataloguing**

Modules need to be discoverable — by the AI agent (to know what's available to propose) and by humans (browsing the library, contributing new ones). Likely needs categorization by:
- Task domain (patching, provisioning, backup, etc.)
- Target environment (on-prem Windows, Azure, etc.)
- Risk level

**Workflows as a separate layer**

Workflows (recipes) reference modules by their contract, not their implementation — so a workflow definition says "run the module tagged `pre-flight-check` for this environment," and the library resolves which actual module that maps to. This keeps workflows portable across environments as long as an equivalent module exists.

Like modules, **every workflow also has its own unique ID** (see `open-questions.md`, AR-5, confirmed) — this is what a `WorkflowRun` (Section 4.1) actually points to when it records which recipe it followed, and what lets the same workflow be triggered reliably by a schedule, a human, or an external system (Section 6's MCP findings) without ambiguity about which recipe is meant.

**Community contribution implications**

Since this repo is open source (see business model discussion), the module contract *is* the contribution guideline — anyone submitting a module knows exactly what's required (metadata, rollback, tests) for it to be accepted and trusted by the engine.

**Metadata schema, first draft (v0.1.2 detail).** Concretizing the bullet list above into an actual manifest shape — one file per module, sitting alongside its script:

```yaml
id: apply-patch-windows
name: Apply Windows Update patch
version: 1.2.0
risk_level: medium
environments: [windows-server-2019, windows-server-2022]
validation_status: production-approved   # lab-validated | production-approved
inputs:
  patch_ids: { type: array<string>, required: true }
  maintenance_window: { type: string, required: false }
outputs:
  result: { type: enum[success, failure] }
  applied_patch_ids: { type: array<string> }
rollback:
  procedure: rollback-patch-windows.ps1
  tested: true
tests:
  lab_suite: apply-patch-windows.tests.ps1
```

`validation_status` is worth calling out specifically: it's the field that lets the orchestration engine (4.1) enforce Section 2's reliability principle in code — a module starts at `lab-validated` and only becomes eligible for production `WorkflowRun`s once it's actually been promoted to `production-approved`, rather than that distinction living only in a human's memory of "oh yeah, we tested that one."

**Versioning/compatibility (a starting rule, open to revision).** A `WorkflowRun` should pin to the exact module version active when its `Plan` was drafted (Section 4.1), not "whatever the latest version is by the time it executes" — otherwise a module update mid-flight could silently change behavior for a run already in progress. Workflow definitions reference a module by its logical `id` plus a minimum compatible version, similar in spirit to semver ranges in a package manager.

> *(Further detail — full type system for inputs/outputs, exact workflow-definition file format — to be added once the first real modules for Windows Server patching get built, Section 9 Phase 1)*

### 4.3 AI Agent Layer
- How do AI agents decide which tasks to run?
- How do they interact with the orchestration engine?
- What information do they need to operate effectively?

> 📖 Plain-language companion: [`ai-agent-layer.md`](../learn/ai-agent-layer.md)

**Role in the System**

The AI agent is the "what and why" layer (see Section 3). It doesn't execute anything directly — it interprets the situation, proposes a plan, and hands that plan off for human approval before the orchestration engine ever touches a target system. This is a direct application of the reliability-first principle in Section 2.

**Inputs the agent needs**

- The **selected workflow** (the recipe) the SA/SE chose
- **Current target system state** — e.g. current patch level, last patch date, uptime requirements, known issues
- **Historical data** — past runs of this workflow against this system or similar systems (successes, failures, rollbacks triggered), queried from the **Engine State Store** (see Section 4.5)
- **Constraints** — maintenance windows, blackout periods, compliance requirements

**What the agent produces (the "proposal")**

- Which modules to run, in what order, with what parameters
- A plain-language explanation of *why* — this is critical for trust; the SA/SE approving the plan needs to understand the reasoning, not just see a black-box "approve/reject" button
- A risk/confidence indicator — flagging anything unusual about this run vs. a routine one

**Interaction with the Orchestration Engine**

The agent and engine are decoupled: the agent produces a proposed plan, a human approves it, and only the *approved* plan is handed to the engine for execution. The agent does not call modules directly — this boundary is what keeps the recommend-and-approve principle enforceable at the architecture level, not just a policy.

**Autonomy levels (maps to Section 2's gradual autonomy principle)**

1. **Recommend-and-approve** (launch state) — agent proposes, human approves every run.
2. **Approve-by-exception** (future) — agent runs routine/low-risk workflows automatically, only escalates unusual cases for approval.
3. **Full autonomy** (long-term, narrow scope) — reserved for workflows with a long track record of reliable, low-risk execution and well-tested rollback.

**How the agent's reasoning is structured (v0.1.2 detail).** Rather than free-text reasoning the orchestration engine has to parse and trust, the agent should produce its plan via Semantic Kernel's structured function-calling — a typed `Plan` object (module sequence, parameters, explanation, risk indicator), not a paragraph. This matters for two reasons: it lets the engine validate the plan deterministically (does every proposed module have a tested rollback and clear `production-approved` status? Section 4.1/4.2's hard rule), and it's what keeps a human's "approve" click meaningful — they're approving a specific, inspectable structure, not trusting a summary of one.

**What "unusual" means, a starting definition.** Given the historical-data input above, a reasonable first cut: a run is flagged unusual if it targets a system class or module combination with limited or no prior successful history in the Engine State Store, or if the target's current state differs meaningfully from what past successful runs looked like (e.g. patching a server with an atypically long uptime, or one flagged from a prior incident). This is deliberately conservative to start — false positives (flagging something actually routine) cost a human a few extra seconds of review; false negatives (treating something risky as routine) are the failure mode Section 2 is built to avoid.

> *(Further detail — how exactly "differs meaningfully" gets scored, whether that becomes a tunable threshold per organization — to be added once there's real run history to calibrate against, Section 9 Phase 2)*

### 4.4 Execution Environment
- Where and how do scripts actually execute?
- How do we handle on-prem, cloud, and hybrid scenarios?
- Connection management and security.

> 📖 Plain-language companion: [`execution-environment.md`](../learn/execution-environment.md)

**Role in the System**

This is the "where" layer (see Section 3) — it abstracts away the difference between environments so nothing upstream (engine, modules, agent) needs to know or care whether a target is on-prem, cloud, or hybrid.

**Starting point: On-Prem Windows via WinRM**

Per the earlier decision to start with on-prem Windows Server patching, the first execution environment connector uses WinRM to reach target servers and run PowerShell against them. This needs to handle:
- **Connection management** — establishing, authenticating, and tearing down remote sessions reliably, including retry/timeout behavior for flaky connections
- **Credential handling** — how the engine authenticates to target servers without modules or the AI agent ever seeing raw credentials directly (see also Section 7, Secrets Management)
- **Isolation** — a failure or hang in execution against one target shouldn't affect others running in parallel

**Designed for Extensibility**

The connector itself should be a pluggable interface — a defined contract (similar in spirit to the module contract in 4.2) that any environment-specific implementation can fulfill. This is what lets cloud (Azure, AWS) and hybrid connectors get added later without redesigning the engine or module library:
- On-prem Windows → WinRM (first)
- On-prem Linux → SSH (natural next step given Bash module support)
- Cloud → provider-native APIs (Azure Run Command, AWS SSM, etc.)

**Design Decision (see `open-questions.md`, AR-2):** connection/protocol differences (WinRM vs. SSH vs. cloud API) stay in the connector layer. Genuinely different implementations of the same action (e.g. `apt` vs. `yum` vs. Windows Update APIs) need environment-specific module variants under one logical module ID — the module contract stays identical, only the implementation and declared supported-environment metadata differ.

**Connector interface, first draft (v0.1.2 detail).** Concretizing "a defined contract" above into actual method shapes any connector implementation (WinRM today, SSH/cloud APIs later) needs to fulfill:

```
connect(target, credential_handle) -> Session
execute(session, module_payload) -> ExecutionResult
disconnect(session) -> void
health_check(target) -> bool
```

`credential_handle` deliberately isn't the raw credential itself — it's a reference the connector resolves against the secrets store at connection time (Section 7), so neither the module nor the AI agent layer ever holds a real secret. `health_check` exists mainly to support the isolation requirement above: the engine can probe a target's reachability before committing a worker to a potentially-hanging connection attempt, rather than discovering a dead target mid-execution.

> *(Further detail — exact timeout/retry policy, how `ExecutionResult` structures partial-failure information for the rollback path — to be added once the WinRM connector is actually implemented, Section 9 Phase 1)*

### 4.5 Engine State Store (ESS)
*(Renamed from "Shared Knowledge Base (SAB-KB)" — see `open-questions.md` RC-3. "ESS" is the official shorthand, per `open-questions.md` AR-7. The name "SAB-KB" now belongs exclusively to the separate `sab-kb` commercial product; this component keeps `sab-engine`'s original narrow scope under a name that doesn't collide.)*
- What is the Engine State Store and why does it exist as its own component?
- What information lives there, and who/what reads and writes it?
- How does it relate to the other components?

> 📖 Plain-language companions: [`engine-state-store.md`](../learn/engine-state-store.md), [`ess-vs-sab-kb.md`](../learn/ess-vs-sab-kb.md)

**Resolution (see `open-questions.md` RC-1, RC-2):** `sab-engine` and `sab-kb` are independent products for now, with no assumed integration. `sab-engine` gets its own small internal store — described below — scoped only to what the orchestration engine and AI agent need (run history, target state). Whether `sab-engine`'s AI agent ever queries `sab-kb` for broader context is a real future question, deliberately left open rather than assumed, since `sab-engine` isn't being built yet (see RC-5 below).

**Role in the System**

The Engine State Store is `sab-engine`'s own memory — the record of what's been run, against what, and with what result, that the AI agent, orchestration engine, and humans all read from and write to within this repo alone. Where Sections 4.1–4.4 describe components that *do* things, this is the component that *remembers* things and makes that memory usable.

This makes the "feedback loop" described in Section 3 concrete: instead of results simply generating logs that disappear into storage, they flow into this store, and the AI agent actively queries it before proposing a plan.

**What it likely contains**

- **Run history** — every workflow execution: what was run, against what target, what the outcome was, whether rollback was triggered
- **Target system state** — current known state of each managed system (patch level, last run, configuration facts) so the AI agent isn't guessing
- **Module & workflow catalog** — the discoverable metadata from the Module Library (4.2), so this may be the system of record the library publishes into rather than a separate store
- **Learnings / patterns** — e.g. "this patch has caused rollback on this class of server before" — the kind of institutional knowledge that normally lives in a senior SA's head and gets lost when they leave
- **Human-authored documentation/notes** — SAs and SEs annotating systems, workflows, or past incidents with context an AI agent or automated log can't infer on its own

**Who/what interacts with it**

- **AI Agent Layer (4.3)** — queries this store as a primary input before proposing a plan; writes back its reasoning and outcomes after a run
- **Orchestration Engine (4.1)** — writes execution results and state as a workflow runs
- **Humans (SAs/SEs)** — browse it directly, add context/notes, and use it to understand *why* the system behaved a certain way — this is also what makes the audit trail in Section 7 meaningful rather than just raw logs
- **Module Library (4.2)** — module/workflow metadata may be published into or synced with this store for discoverability

**Why this matters for trust and gradual autonomy**

Per the reliability-first principle (Section 2), this store is arguably what *earns* the system more autonomy over time — a system with no memory has to be trusted blindly each time, but one that can point to "this exact workflow has succeeded cleanly against this exact class of system 40 times" gives both the AI agent and the human approver a real basis for confidence.

**Repo Structure:** Lives *inside* `sab-engine` — **not** its own repo. This reverses the earlier AR-3 decision (which gave "SAB-KB" its own repo) now that the name and the actual product it referred to have diverged: the thing that earned a dedicated repo was always the real `sab-kb` product, not this narrow internal store. A small state store scoped to one repo's own needs doesn't need independent versioning the way a whole separate product does.

**Data model, first draft (v0.1.2 detail).** This store is largely the persistence layer already implied by Section 4.1's state machine, viewed from the querying side rather than the writing side:

- `workflow_runs` — one row per `WorkflowRun`: id, workflow id, target, state, timestamps
- `plans` — the AI agent's proposed module sequence + reasoning, linked to a run
- `approvals` — who approved/declined, when, which plan version they saw
- `execution_results` — per-module outcome, rollback-fired flag, linked to a run
- `target_state` — current known facts per managed system (patch level, last run timestamp), updated as runs complete
- `notes` — free-text human annotations, linked to a target or a specific run, separate from the structured tables above so a human's "watch out, this one's flaky" comment doesn't have to force a schema change

**Query interface for the AI agent.** At minimum, the agent needs: "give me the last N runs of workflow X against target Y," "give me the last N runs of workflow X against systems similar to Y" (for the cold-start case where this exact target has no history), and "give me any human notes attached to target Y." A human-facing UI is a thinner layer on top of the same queries — a run history view, a target detail view, and a notes/annotation form — rather than a separate design problem.

> *(Further detail — how "systems similar to Y" gets defined technically for the cold-start case, exact UI wireframes — to be added once Phase 1's proof of concept generates real run history to design against)*

---

## 5. Extensibility and Plugin System
*How do new tools, frameworks, and capabilities get added?*
- API design for third-party integrations
- Module development patterns and guidelines
- Community contribution framework

> 📖 Plain-language companion: [`community-contribution-framework.md`](../learn/community-contribution-framework.md)

**Foundation: Two Contracts Already Defined**

Extensibility isn't a separate mechanism bolted on — it falls directly out of two contracts already established:
- The **module contract** (Section 4.2) — anyone can build a new module (patching, provisioning, backup, anything) as long as it follows the standard shape: metadata, typed inputs/outputs, required rollback procedure, tests.
- The **connector contract** (Section 4.4) — anyone can add support for a new execution environment (a new cloud provider, a different remote-management protocol) by implementing that interface, without touching the orchestration engine or module library.

Together, these two contracts are what let SAB grow outward — new workflows, new environments, new integrations — without changing its core.

**Community Contribution Framework**

Since the core engine and module library are open source, the contracts themselves function as the contribution guidelines: a submission is straightforward to validate (does it meet the contract? does it have tests and a rollback path?) rather than requiring subjective review of "does this fit the architecture." This keeps the bar for contribution clear and consistent as the community grows.

**Third-Party Integration API**

Beyond modules and connectors, there's likely a need for a broader integration API — allowing external tools (ticketing systems, monitoring platforms, ChatOps tools like Slack) to trigger workflows or read status/results. This would sit alongside the Engine State Store (4.5) as a read/write surface, but scoped for external, non-core consumers.

**Positioning Implication**

Per the partnership-not-competitor stance established in Section 8, these contracts (module, connector, and integration API) should be treated as the primary path for platforms like Microsoft or UiPath to eventually integrate with SAB, rather than assuming SAB needs to out-build them. Keeping these interfaces clean, stable, and well-documented is worth prioritizing specifically because it's what makes future partnership technically easy, not just possible in principle.

**API surface, first draft (v0.1.2 detail).** Given Section 4.5's query interface and Section 6's MCP-consumption/exposure findings, a reasonable minimum external surface:

- `POST /workflows/{id}/trigger` — start a workflow run against a target (this is the same "Trigger" input a schedule or a human uses internally, just exposed externally)
- `GET /runs/{id}` — status of a specific run (maps to the `workflow_runs` query above)
- `GET /runs?target=&workflow=` — history query (maps to Section 4.5's "last N runs" query)
- `POST /runs/{id}/approve` / `POST /runs/{id}/decline` — the human approval gate, made externally callable (e.g. from a ChatOps tool rather than only a web UI)

Authentication for third-party integrations is most naturally scoped-API-key or OAuth2 per external system, consistent with how Section 7 already treats "marketplace contributors/third parties" as their own identity tier distinct from core humans, the AI agent, and the orchestration engine itself. Per Section 6's MCP findings, this same surface is also the natural basis for an MCP server exposing SAB's workflows as callable tools — the REST surface and the MCP tool definitions should describe the same underlying actions rather than drifting into two separate APIs.

> *(Further detail — exact auth flow, rate limiting, versioning strategy for this API — to be added once there's a first real external integration candidate to design against, per AR-4)*

---

### 5.1 SAB Engine Marketplace
*(Renamed from "SAB Marketplace" — see `open-questions.md` RC-3. `sab-kb`'s docs use "SAB Marketplace" for a different thing — its Email/Teams/PSA connector framework. This is `sab-engine`'s marketplace specifically, for modules and connectors that plug into the orchestration engine.)*

**What it is**

A dedicated place where users — community members, third-party developers, and eventually vendors — can publish and (later) sell add-ons: modules, connectors, and full workflow packs built on the extensibility contracts above. It's the commercial expression of the extensibility model, and ties directly into the business model established earlier (open-core, monetization via the commercial layer).

**Relationship to the rest of the system**

- The marketplace doesn't replace the open source module library (4.2, the OSML) — it sits alongside/on top of it. Community modules can still be free and live in the open source repo; the marketplace is specifically for discoverability and (eventually) commerce.
- Anything listed in the marketplace still has to satisfy the module or connector contract — the marketplace is a distribution and monetization layer, not a different technical standard. This keeps quality/trust consistent regardless of whether something is free or paid.
- Likely lives in or alongside the **commercial layer repo** (per the four-repo structure — core engine, module library, `sab-kb`, commercial layer) since it's part of the monetization strategy, even in its free starting phase.
- Maps naturally onto the tiered trust model (MP-2, `open-questions.md`): **Community** and **Verified** tiers stay free, and the **Certified** tier — hardened, tested, support-backed — is where the Fedora/RHEL-style paid value concentrates (see Section 1's business model). This gives the marketplace a monetization mechanism consistent with early Red Hat's actual approach, rather than an app-store-style paywall on functionality itself.

**Phased rollout**

1. **Phase 1 — Free add-ons only.** Focus on discoverability and adoption: a catalog where people can browse and pull in community-built modules, connectors, and workflow packs. No money changes hands yet. This builds the supply side (contributors) and demand side (users who trust and rely on marketplace content) before monetization enters the picture.
2. **Phase 2 — Full marketplace.** Introduce paid listings — developers/vendors can sell modules, connectors, or workflow packs (e.g. industry-specific compliance packs). SAB likely takes a revenue share, similar to how app stores or the Terraform/VS Code marketplace models work.

**Trust and safety implications**

Because modules can execute against production infrastructure, a marketplace introduces a real trust question beyond typical app-store concerns — a malicious or poorly-tested module isn't just annoying, it's a risk to someone's servers. This connects directly to Section 2's reliability principle and Section 7 (Security). **Design direction (see `open-questions.md`, MP-2):** a three-tier trust model — **Community** (meets the module contract, otherwise unreviewed), **Verified** (passed additional review/testing by SAB maintainers), and **Certified** (vendor-backed, with support commitments attached — likely where compliance packs live). Exact review criteria per tier still TBD, but this gives a clear structure to build toward.

> *(Revenue share model (MP-1) genuinely needs your call rather than mine — for reference/benchmarking, mobile app stores typically take 15–30%, the VS Code Marketplace charges nothing, and the Terraform Registry doesn't take a cut on free modules but monetizes separately via a paid registry tier. Review criteria per tier and marketplace UI/discovery are reasonable to design once Phase 1's free tier is closer, per Section 9's roadmap — left as forward-looking rather than filled in speculatively now.)*

---

## 6. Integration with Existing Enterprise Tools
*How does SAB work alongside or replace existing tools?*
- WSUS, SCCM, Windows Admin Center compatibility
- Cloud platform integration (Azure, AWS, etc.)
- RPA and orchestration platform comparisons

**Guiding Principle: Connectors Are Built to Deepen Partnership, Not to Compete**

Per the positioning stance established in Section 8, integrations with existing enterprise tools are designed explicitly to make SAB a good complement to those tools — something that makes them more valuable, not something aimed at displacing them. This shapes what gets built and how it's talked about publicly.

**WSUS / SCCM**

Rather than positioning SAB as a WSUS/SCCM replacement, the first integration connector should let SAB *work with* an organization's existing WSUS/SCCM infrastructure — e.g. reading patch approval status and catalog data from WSUS, and using SAB's orchestration/AI agent layer to standardize and safely execute the workflow around it, rather than replacing the underlying patch distribution mechanism. (JetPatch's bidirectional WSUS sync, see `market-research.md`, is a useful reference pattern for what "integrate, don't replace" looks like technically.) This also gives organizations still on WSUS — which per the market research is most of them as of mid-2026 — a low-friction way to adopt SAB without having to rip out existing infrastructure first. **A first-draft technical spec for this connector now exists — see `wsus-connector-spec.md`.**

**Windows Admin Center**

Worth exploring as an integration surface rather than an alternative — e.g. SAB workflow status or approval actions surfaced inside Windows Admin Center, which is itself a Microsoft-first-party tool. This is the kind of integration that could plausibly get Microsoft's attention as a complementary tool rather than a competing one.

**Cloud Platform Integration (Azure, AWS)**

Per the execution environment design (Section 4.4), cloud connectors are a natural extensibility point. For Azure specifically, building a clean, well-behaved connector (e.g. using Azure Run Command or similar first-party mechanisms rather than working around them) is both a technical necessity for hybrid/cloud support and a positioning opportunity — it signals SAB works *with* Azure rather than around it.

**RPA and Orchestration Platform Integration (UiPath, Workato, Kestra)**

Rather than comparing SAB against these platforms as competitors, the more useful long-term question is what it would take for SAB's module/workflow library to be *callable from* or *composable with* these platforms — e.g. a UiPath or Workato connector that lets their users invoke a SAB workflow as a step in a broader automation, or a Kestra plugin that wraps SAB's sysadmin-specific modules for use inside a general-purpose orchestrator. This reframes SAB's relationship to these platforms from "alternative" to "specialized extension," which is a much easier pitch for a partnership conversation.

**Practical Implication**

This means the module and connector contracts (Section 4.2, 4.4, 5) should be designed with external callability in mind from early on — not just "can a human or the AI agent call this," but "could an external platform's automation call this too." That's a real technical constraint worth carrying into implementation, not just a marketing framing decision.

### RMM/PSA and MCP Connector Priorities (v0.1.2 — new research)

*This subsection adds specific connector-level findings on top of the general integration principles above. It applies mainly to `sab-kb`'s MSP audience, with the AWX/Ansible finding also directly relevant to `sab-engine`. Source: connector-landscape research conducted August 2026, folded into `market-research.md` Section 9.*

**Five priority connectors, in order.** Based on API depth, ease of integration, and market reach across both `sab-kb`'s MSP buyers and `sab-engine`'s eventual enterprise IT buyers:

1. **NinjaOne (RMM)** — clean OAuth 2.0 API, endpoints covering devices, ticketing, webhooks, and knowledge base/org documents (directly relevant to `sab-kb`). Multiple community MCP servers already exist. Formal Technology Alliances Program (TAP) for certified partnership. Caveat: NinjaOne now sells its own IT documentation product, so it's likely to view `sab-kb` specifically as partially competitive even while viewing `sab-engine`'s orchestration layer as complementary — a threshold condition worth tracking (see Section 10).
2. **ConnectWise Manage (PSA)** — largest installed PSA base, deep ticket/company/time-entry data that's exactly what `sab-kb`'s tribal-knowledge capture needs. Strong existing community MCP servers. Certified listing requires **ConnectWise Invent**, which includes a mandatory independent security review — worth starting that application early given unknown review timelines.
3. **Autotask PSA** — the #2 PSA. Notably offers a **free API-user license**, header-key auth (no OAuth2 needed), and webhook support. Captures the other half of the MSP PSA market alongside ConnectWise.
4. **Ansible Automation Platform / AWX** — the credible execution substrate for `sab-engine`'s Windows Server patching use case specifically. REST API, job templates, RBAC, credential management, proven for Windows patching, open source (AWX). Rather than `sab-engine` reinventing execution/remoting from scratch, this is a plausible thing to orchestrate on top of, in the same spirit as the "callable from/composable with" framing above for Kestra/UiPath.
5. **Hudu (MSP documentation)** — ships an **official, native OAuth-based MCP server** as a product feature (not just a community add-on), scoped API keys, and built-in webhooks. This is the current technical bar-setter for what a modern MSP documentation platform's API/MCP story looks like, and the most natural interop target for `sab-kb`'s own knowledge base.

**Fast-follow tier:** HaloPSA, Datto RMM, and IT Glue (larger installed base than Hudu, but weaker webhook support and only unofficial community MCP servers today).

**MCP as the concrete mechanism, both directions.** Consistent with Section 5's "external callability" principle: SAB should both *consume* existing MCP servers where they exist (NinjaOne, ConnectWise, Hudu, and others already have official or strong community servers) rather than building bespoke API integrations for everything, and *expose* SAB's own workflows as an MCP server so external agents can invoke them as tools — this is a more meaningful differentiator than being "just another RMM to bolt on," and it's the same externally-callable posture Section 5 already argues for regarding UiPath/Workato/Kestra, just via a newer, faster-standardizing mechanism. Section 5's API surface draft is designed with this in mind — the REST endpoints and an eventual MCP tool definition should describe the same underlying actions.

**Notable market gap, favorable to `sab-kb`:** knowledge base / documentation MCP is repeatedly identified as the single largest *unmet* demand in the current MCP ecosystem — directly relevant to `sab-kb`'s core value proposition.

**Partner program notes.** ConnectWise Invent, Kaseya's Technology Alliance Program (covers Autotask/Datto/IT Glue/VSA), and NinjaOne's TAP are all application-based and none publish cost or revenue-share terms publicly — worth direct inquiry before assuming these are free or cheap to join. Open-source orchestration platforms (Ansible/AWX, and Kestra/Rundeck/StackStorm from Section 8) require no such gatekeeping.

**Suggested phased rollout, layered on top of Section 9's existing roadmap:**

- **Near-term:** read-only-by-default connectors for NinjaOne, ConnectWise Manage, and Ansible/AWX. A reasonable end-to-end proof point: a `sab-engine` plan gets human-approved, an AWX job runs it, and the result writes back to a ConnectWise ticket via MCP.
- **Next:** add Autotask PSA and Hudu; apply to ConnectWise Invent, Kaseya TAP, and NinjaOne TAP in parallel given unknown review lead times.
- **Later:** Datto RMM, HaloPSA, IT Glue, and — if `sab-engine` moves further upmarket toward enterprise IT rather than staying MSP/SMB-focused — ServiceNow or Jira Service Management.

> *(Folded into `market-research.md` Section 9 as of this same session — see that file for the full research with citations. Still open: exact API depth/rate-limit specifics per tool, and the concrete technical spec for the WSUS-read connector as a first proof point — the latter already has a first draft in `wsus-connector-spec.md`.)*

---

## 7. Security and Compliance Considerations
*How do we keep this safe and auditable?*
- Authentication and authorization patterns
- Audit logging and compliance tracking
- Safe execution isolation
- Secrets management

**This section pulls together security concerns already surfaced elsewhere in the design**, rather than introducing an unrelated set of concepts — security here is a property of how the other components are built, not a separate layer bolted on top.

**Secrets Management**

Raised in Section 4.4: the execution environment needs to authenticate to target systems (e.g. via WinRM) without credentials passing through or being visible to modules or the AI agent. Likely approach: credentials are resolved by the execution environment layer at the point of connection, pulled from a dedicated secrets store (vault-style), and never appear in workflow definitions, module code, logs, or the Engine State Store in plaintext. Modules and the AI agent should only ever see a reference/handle, never the secret itself. **Design direction (see `open-questions.md`, SE-1):** support HashiCorp Vault as a pluggable backend for organizations already running it, with native OS credential stores (Windows Credential Manager, Linux keyring) as a simpler default — avoid building custom secrets infrastructure.

**Authentication and Authorization**

Several distinct identities interact with SAB and need appropriately scoped access:
- **Humans (SAs/SEs)** — who can approve workflow runs, who can author/publish modules, who can administer the system
- **The AI agent** — needs read access to the Engine State Store and target state, and the ability to *propose* plans, but per Section 2's reliability principle, should not have standing authorization to execute against production without the human approval gate
- **The orchestration engine** — needs scoped credentials to actually connect to target systems, ideally least-privilege and per-environment rather than one broad standing credential
- **Marketplace contributors/third parties** (Section 5.1) — a separate identity/permission tier, since their trust level starts lower than core-team-authored modules

**Audit Logging and Compliance Tracking**

This is largely already designed — Section 4.1 (orchestration engine) logs every action taken, and Section 4.5 (the Engine State Store) is where that history persists and becomes queryable. What Section 7 adds on top:
- Audit records need to be **tamper-evident** — an audit trail that could be silently edited after the fact isn't trustworthy for compliance purposes
- Records should capture **who/what approved a plan** (a specific human, not just "approved"), which matters both for accountability and for satisfying compliance frameworks that require documented change approval
- Retention requirements likely vary by industry (this connects to the "industry-specific compliance packs" idea from the marketplace/business model discussion) — worth treating retention/format as configurable rather than hardcoded. Which specific frameworks (SOC2, HIPAA, etc.) to design against is still open (SE-3) until a target industry is clearer.

**Safe Execution Isolation**

Raised in Section 4.4: a failure or hang against one target shouldn't affect others running in parallel. Beyond that:
- Module execution should be sandboxed/isolated enough that a misbehaving module (bug, not malice) can't affect the orchestration engine itself or other concurrent runs
- This matters even more once the Marketplace (5.1) introduces third-party-authored modules — isolation is part of what makes a tiered trust model (community vs. verified) technically meaningful rather than just a label
- **Design decision (see `open-questions.md`, SE-2 — confirmed):** containers (Docker) as the sandboxing mechanism — process/resource isolation without full-VM overhead, and it matches the pattern seen in comparable tools (Kestra runs arbitrary tasks in containers for the same reason).

**Tamper-evidence, made concrete (v0.1.2 detail).** The simplest version of "tamper-evident" that fits the existing `AuditEntry` model (Section 4.1): each entry is written once and never updated, and each entry's hash chains to the previous entry's hash (append-only, hash-linked) — a common, low-effort pattern that makes silent post-hoc edits detectable without needing a heavier solution (e.g. a dedicated ledger system) that would be overkill before there's a specific compliance framework driving the requirement.

> *(Compliance framework mappings (SE-3) genuinely need your call on target industry before this can go further — SOC2, HIPAA, and similar frameworks have different, sometimes conflicting retention/access requirements, and picking one to design against speculatively risks building the wrong thing. Left open per Section 9's existing framing: revisit once a target industry is clearer.)*

---

## 8. Existing Solutions and Learnings
*What can we learn from platforms like UiPath, Workato, Kestra, JetPatch, PSAI, AIShell?*
- Patterns worth adopting
- Pitfalls to avoid
- Opportunities to differentiate — and to partner

*Full sourcing and detail in `market-research.md`.*

**Positioning Note: Partner, Not Competitor**

SAB is deliberately positioned to be seen as complementary to major platforms like Microsoft and UiPath, not as a threat to them. SAB is built on top of PowerShell — a Microsoft technology — and its first use case (Windows Server patching) operates in the same space as WSUS, SCCM, and Microsoft's own AI Shell, but at a different layer: SAB standardizes and orchestrates *how* the work gets done using modules and reusable workflows, rather than replacing Microsoft's patch distribution or command-line tooling outright. Similarly, UiPath and Workato operate at a broad, general-purpose RPA/iPaaS layer — SAB isn't trying to win that budget, it's solving a narrower, sysadmin-specific problem those platforms aren't purpose-built for. The goal is for these companies to eventually see SAB as something worth integrating with or building on top of, not something competing for the same deal. This shapes both the technical strategy (favor integration/connector points into their ecosystems over replacement) and the public narrative (frame SAB as "built on" and "works alongside," not "replaces").

*This pulls together research findings, reframed around where partnership — not displacement — makes sense.*

**The Legacy Tools (WSUS/SCCM) Are in a Transition Window**

WSUS was deprecated by Microsoft in 2024 with no confirmed end-of-life date — it still functions and still ships with Windows Server 2025 as of mid-2026, but receives no new development. Most of the officially recommended replacement paths (Intune, Windows Autopatch, Azure Update Manager) are cloud-managed services. Organizations still running WSUS/SCCM broadly know change is coming but haven't committed to a replacement yet — this is a real, current opening rather than a purely hypothetical one.

**Commercial Patch/RMM Tools: Real, Fixable Frustrations**

User reviews of established tools (ManageEngine, Syxsense, and others) surface concrete, avoidable pain: broken reboot ordering causing user login failures, inflexible scan scheduling, slow performance for remote/on-site patching, and features gated behind additional paid add-ons even within already-paid products. These aren't fundamental limitations of automated patching — they're execution gaps a disciplined, standardized workflow model can avoid.

**AI/Agentic Ops Platforms: The Trust Gap Is the Whole Game**

Broader "AgenticOps" and AIOps platforms are moving toward autonomous infrastructure action, but industry data consistently shows trust — not capability — is what's blocking adoption: real incidents exist of agents causing serious, irreversible damage (including deleting production data and backups together) when granted too much autonomy too early. A 2026 survey of 600+ data-center operators found only 14% would trust AI to change equipment configurations, and that trust has declined every year since 2022 — not improved as AI capability has grown. Most competing platforms are racing toward capability and autonomy; relatively few are treating earned, gradual trust as the core design constraint from day one the way SAB does.

**Named Comparables — and Where Partnership Fits**

- **Kestra** — closest architectural cousin: declarative YAML workflows, human-in-the-loop approval, plugin ecosystem, on-prem/cloud/hybrid. General-purpose, not sysadmin-specific. *Partnership angle:* SAB workflows could plausibly run as a specialized layer on top of or alongside a general orchestrator like Kestra rather than needing to reinvent scheduling/execution infrastructure from scratch — worth exploring as SAB matures, rather than treating Kestra as something to out-build.
- **JetPatch** — closest direct competitor for patching: multi-OS remediation, pre-deployment testing, rollback, WSUS integration, enterprise-priced and closed-source. *Partnership angle:* limited — JetPatch is a direct commercial competitor in the patching space specifically. Best strategy here is differentiation (open-core, extensible, community-driven) rather than partnership framing, though this is the one comparable where "competitor" is the honest description.
- **Microsoft AI Shell** — a conversational command-line copilot, not an orchestrator. *Partnership angle:* strong — this is a natural, low-friction integration point rather than competition. SAB's AI agent layer and Microsoft's AI Shell operate at different layers (workflow orchestration vs. interactive command assistance) and could plausibly complement each other; more broadly, since SAB is built on PowerShell and the first use case sits adjacent to WSUS/SCCM, favoring integration points into the Microsoft ecosystem (rather than positioning as a WSUS/SCCM replacement) supports being seen as a partner, not a threat.
- **UiPath / Workato** — general-purpose RPA/iPaaS, expensive, complex pricing, not sysadmin-specific. *Partnership angle:* plausible long-term — SAB's module/workflow contracts could become something these platforms integrate with as a specialized "infrastructure ops" connector, rather than SAB competing for their broader automation budget. Worth keeping the extensibility contracts (Section 5) clean and well-documented specifically so integration is easy if that opportunity arises.

**Implication for Extensibility (Section 5)**

This reinforces the value of investing early in clean, well-documented module and connector contracts — not just for community contribution, but because the same contracts are what would let a platform like Microsoft or UiPath integrate with SAB later, rather than needing to build a competing feature themselves.

**Historical Precedent: Red Hat Network Satellite**

Worth naming directly, since it's the closest historical analog for SAB's actual business model choice (Section 1): Red Hat's own patch/provisioning/lifecycle management product for RHEL, Red Hat Network Satellite, was open-sourced in 2008 as Project Spacewalk. A competitor (Novell/SUSE) subsequently built a rival product, SUSE Manager, directly on top of that open-sourced code. Red Hat didn't attempt to legally prevent this, and the business continued to thrive — because the subscription relationship around RHEL itself, not the management tooling's license terms, was the actual moat. This is direct historical evidence that open-sourcing a systems-management/patching product under a permissive license is a proven, survivable choice, not just a theoretical one.

**MSP Tooling: A Different Shape of Pain**

RMM/PSA platforms serving MSPs face their own well-documented frustrations — vendor lock-in (e.g. multi-year contract terms), integration gaps between tools that create manual reconciliation work, and licensing complexity. This is a distinct problem shape from a single-org SA's workload relief — MSPs care more about consistent, portable standardization across many different client environments without deep lock-in to one vendor's ecosystem.

**Opportunities to Differentiate**

- Open, auditable core vs. black-box automation — directly addresses the trust gap identified above
- On-prem-first, not cloud-mandatory — a real gap left by WSUS's cloud-leaning replacement paths
- Standardized module/workflow contracts as the actual product, not a proprietary scripting language locking users in
- Gradual, earned autonomy as a stated design principle, not a marketing claim — matched by mandatory rollback paths and an auditable state store (Section 4.5)

**Pitfalls to Avoid**

- Don't chase full autonomy to compete on "AI-ness" — the research suggests this is precisely what erodes trust and stalls adoption elsewhere
- Don't let scheduling/execution UX regress into the same rigidity users already complain about in existing patch tools
- Don't assume MSP and single-org SA personas want the same thing — messaging and possibly packaging should account for the difference

### The 2026 MCP Ecosystem (v0.1.2 — new research)

*Adds current-state findings on the Model Context Protocol (MCP) landscape, relevant primarily to Sections 4.3, 5, and 6's integration strategy. Source: same connector-landscape research as the Section 6 addition above, folded into `market-research.md` Section 9.*

- MCP has become the de facto standard for AI-to-tool connectivity in 2026, with rapid, dense adoption across RMM, PSA, and ITSM vendors — NinjaOne, N-able, ServiceNow, Zendesk, HaloPSA, ConnectWise, and Hudu all have official or strong community MCP servers as of mid-2026.
- **Knowledge base / documentation MCP remains the single largest unmet demand** in the ecosystem — directly favorable to `sab-kb`'s positioning, and a data point worth citing alongside the "brain debt" research already in `market-research.md`.
- MSP-specific market data reinforces the same gap from the demand side: roughly half of MSPs rank AI/automation as their clients' #1 need for 2026 — ahead of security and backup — yet only a small fraction currently generate meaningful revenue from it, and documentation-automation adoption specifically sits well under a quarter of surveyed MSPs. This is consistent with, and adds MSP-specific texture to, the general "trust is the barrier, not capability" finding already documented above.
- **Security findings directly reinforce Section 2's recommend-and-approve principle, and should be treated as a marketing point, not just an internal caution.** Documented 2026 benchmarking of live MCP servers found tool-poisoning and prompt-injection attacks succeed at a meaningful average rate, with far higher peaks for some models — and more capable models were not necessarily more resistant, undercutting any assumption that better AI alone solves this. Separately, real 2026 vulnerability disclosures affected a large number of MCP server instances industry-wide. None of this is specific to SAB, but it is a direct, current-dated validation of why the human approval gate (Section 4.1's hard rule) matters even as MCP adoption accelerates — worth stating in the same explicit, prominent way the existing "trust is the #1 barrier" research is already stated above, rather than treating it as a separate afterthought.

> *(Folded into `market-research.md` Section 9 as of this same session, alongside a hands-on trial of Kestra/JetPatch if useful, and ongoing tracking of new entrants — the latter two still open.)*

---

## 9. Development Roadmap and Next Steps
*How do we build this incrementally?*
- **Phase 1:** Windows Server patching proof of concept
- **Phase 2:** Expand module library and add more workflows
- **Phase 3:** Cloud and hybrid support
- **Phase 4:** Community and open source launch

> 📖 Phase-by-phase detail: [`phases/phase01.md`](../phases/phase01.md), [`phases/phase02.md`](../phases/phase02.md)

**Two Axes of Progress**

The roadmap isn't just feature phases — it's also a trust/autonomy maturity ladder (Section 4.3) that runs alongside them. Every phase below starts in recommend-and-approve mode; moving toward approve-by-exception or full autonomy for any given workflow is a separate, later milestone earned through track record (via SAB-KB), not something scheduled by calendar date.

---

### Phase 0: Foundation (Pre-Code)
*Where the project currently stands.*
- Design document and architecture — in progress (this document)
- Market research and positioning — in progress (`market-research.md`)
- Open questions tracked and prioritized (`open-questions.md`)
- GitHub organization confirmed and claimed: **`github.com/sab-hq`**. Four repos planned underneath it (not yet created — deliberately waiting until design solidifies further):

  **1. `sab-hq/sab-engine` (core orchestration engine) — open source**

  - Orchestration engine itself: sequencing, state tracking, rollback triggering (4.1)
  - AI agent layer (4.3) — proposed to live here rather than as a separate repo, since it's tightly coupled to the engine (proposes plans the engine executes) and was never called out as needing independent versioning; revisit if that coupling loosens later
  - Module contract and connector contract definitions (4.2, 4.4) — the interfaces everything else builds against
  - The WinRM connector (4.4) — the first execution environment, shipped as core rather than a community extension since on-prem Windows is the initial primary target
  - CLI/API to trigger and monitor workflows

  **2. `sab-hq/sab-modules` — the Open Source Module Library (OSML) — open source**

  - PowerShell/Bash modules following the module contract, starting with the patching set (pre-flight check, stage, apply, validate, rollback)
  - Workflow definitions (recipes stringing modules together, per Section 3's workflow/module distinction)
  - Community-contributed connectors beyond WinRM (SSH, cloud) likely land here once they exist, alongside modules, since both follow the same contract-based contribution pattern
  - The `wsus-connector-spec.md` implementation (the WSUS-read integration) likely also lives here, since it's a data-source integration feeding SAB-KB rather than an execution-environment connector

  **3. `sab-hq/sab-kb` — closed/commercial (MSP knowledge & documentation engine, per-seat subscription)**

  - **Correction from earlier draft:** previously described here as open source ("logical layer on top of orchestration run-history") — that was wrong. `sab-kb` is not a thin support component for `sab-engine`; it's the actual near-term, sellable v1 product, with its own agents (orchestrator + capture/curation agents), Email/Teams connectors, and a real, already-researched business model (per-technician-seat MSP subscription, free on-ramp tier). Full design lives in `sab-kb`'s own `docs/` folder — migrated from the local `temp` folder and now live at `github.com/sab-hq/sab-kb` — not duplicated here.
  - `sab-engine`'s "shared knowledge base" role (Section 4.5 — run history, target state feeding the AI agent's recommendations) is a much narrower slice than what `sab-kb` the product actually is. **Open question this raises, not yet resolved:** does `sab-engine`'s AI agent layer eventually consume data from `sab-kb`, or does `sab-engine` need its own lightweight internal state/history store separate from the commercial `sab-kb` product? Worth a dedicated pass — see Section 4.5, flagged for rework.
  - **Private repo.** Contains commercial product code and real customer-facing logic — make sure it's created as private, not public, given the earlier mixup with an accidentally-placed repo under the wrong org.

  **4. `sab-hq/sab-commercial` (commercial layer for `sab-engine`) — closed/private, or open-core with paid tiers**

  - Managed hosting infrastructure
  - Enterprise connectors (SCCM, Azure Arc, ServiceNow, etc.)
  - Certified-tier hardened/tested module and workflow packs (the paid half of the Fedora/RHEL-style split, Section 1/LM-2) — distinct from `sab-modules`' free community content
  - The **engine's** marketplace (5.1, MP-3) — Community/Verified tiers surface content from `sab-modules`; Certified tier is native to this repo. **Naming collision flagged:** `sab-kb`'s connector framework (Email/Teams/PSA ingestion) is also referred to as a "SAB Marketplace" in its own docs — these are two different things and need distinct names before this causes real confusion in code or messaging.
  - Support/SLA tooling, multi-tenant management, dashboards

  *(This is a proposed breakdown, not yet a locked decision — the AI agent layer's and connectors' placement in particular are reasonable defaults worth confirming rather than settled facts.)*
- **Exit criteria:** enough architectural clarity (Sections 3–7) that starting to code wouldn't mean immediately contradicting the design

**Sequencing Note (see `open-questions.md` RC-5, and `pre-development-checklist.md` PD-1, confirmed by Brock):** The phases below describe `sab-engine`'s own build path. Earlier drafts of this note incorrectly stated that `sab-kb` should ship *before* `sab-engine`'s Phase 1 as a strict sequencing rule — that was this document's own overstatement, not something Brock ever said, and it's corrected here. The actual resolution, confirmed twice now (RC-5 and PD-1): **`sab-engine` and `sab-kb` are worked on in parallel**, not one-then-the-other. `sab-kb` does have more validation behind it right now (working code, a resolved business model), which may reasonably mean it draws more hands-on coding time day to day — but that's a resourcing reality, not a rule that `sab-engine` work waits. Treat `sab-engine`'s phases below as the roadmap for *this repo specifically*, running alongside `sab-kb`'s own build-readiness docs (now live at `github.com/sab-hq/sab-kb/docs`), not after them.

### Phase 1: Windows Server Patching Proof of Concept
*Prove the core architecture works end-to-end on the narrowest possible slice. Full detail: [`phases/phase01.md`](../phases/phase01.md).*
- Core orchestration engine: sequencing, state tracking, rollback triggering (Section 4.1) — minimum viable version
- A small set of patching modules: pre-flight check, stage, apply, validate, rollback (Section 4.2)
- AI agent layer in recommend-and-approve mode only — no autonomy stretch goals at this phase (Section 4.3)
- Execution environment: WinRM connector for on-prem Windows only (Section 4.4)
- SAB-KB: minimal version — enough to log run history and feed the agent's recommendations, not the full shared-knowledge vision yet (Section 4.5, currently pinned for deeper design)
- WSUS-read connector as the first partnership-oriented integration proof point (Section 6, PO-4)
- **Exit criteria:** SAB can reliably patch a lab/low-stakes Windows Server end-to-end, with a human approving each run and a tested rollback path proven to work, not just documented

### Phase 2: Expand Module Library and Workflows
*Prove the module/workflow model generalizes beyond patching. Full detail: [`phases/phase02.md`](../phases/phase02.md).*
- Add workflows adjacent to patching that reuse the same architecture — natural candidates: service restarts, basic provisioning tasks, backup verification (exact scope TBD)
- Begin validating the module contract (Section 4.2) against a second real use case, not just patching — this is where contract gaps will surface
- Start fleshing out SAB-KB into its fuller shared-knowledge role (Section 4.5) now that there's more than one workflow's worth of history to learn from
- Begin considering approve-by-exception for the most-proven, lowest-risk parts of the patching workflow, if track record supports it
- **Exit criteria:** a second, meaningfully different workflow running in production-adjacent conditions, without needing to redesign core contracts

### Phase 3: Cloud and Hybrid Support
*Prove the "where" abstraction (Section 4.4) actually holds.*
- Add a second execution environment connector (likely Linux/SSH, given Bash module support, and/or Azure given the partnership angle in Section 6)
- Validate that modules/workflows written against the Windows/on-prem case translate reasonably, or clarify where environment-specific module variants are actually needed (open question AR-2)
- Begin community contribution framework in earnest (Section 5) — this is a natural point to open source if not already done, since there's now more than one environment's worth of real usage to attract contributors around

### Phase 4: Community, Open Source Launch, and Marketplace (Free Tier)
*Open the doors.*
- Public open source launch of `sab-engine` and `sab-modules` only — `sab-kb` and `sab-commercial` stay closed/commercial (see the repo breakdown in Phase 0)
- ~~Migrate repos from personal GitHub ID to a dedicated organization~~ — **no longer needed**: the `sab-hq` organization is already claimed, so repos are created there directly from the start rather than migrated later
- SAB Engine Marketplace Phase 1 (free add-ons only, per Section 5.1) — discoverability layer for community-contributed modules/connectors/workflow packs
- Begin outreach on partnership angles identified in Section 6/8 (Microsoft, UiPath, Workato) now that there's a real, working project to point to rather than just a design document

### Later / Ongoing (Not Strictly Sequential)
- Commercial layer: managed hosting, enterprise connectors, compliance/industry-specific workflow packs (business model, Section 1)
- SAB Engine Marketplace Phase 2 (paid listings, vetting/certification tiers — Section 5.1)
- Gradual autonomy expansion for well-proven workflows (Section 4.3), tracked per-workflow via the Engine State Store rather than as a single system-wide switch
- Ongoing security/compliance hardening (Section 7) as adoption grows and stakes rise

**Immediate Next Steps**

1. **`sab-engine` and `sab-kb` are being worked on in parallel** — confirmed by Brock (`pre-development-checklist.md` PD-1, and see the corrected Sequencing Note above). Not a strict either-first order.
2. For `sab-engine` specifically: work through `pre-development-checklist.md` in order — it breaks Phase 1 down into a dependency-ordered sequence of concrete pre-code tasks (project scaffolding, the first real modules, the WinRM connector, and so on), each traceable back to a section of this document.
3. ~~Create the four repos under `sab-hq`~~ — **done**, all four repos exist, are connected, and have real content pushed — including `sab-kb`, fully migrated from `temp` (which has since been deleted)
4. ~~Draft the concrete technical spec for the WSUS-read connector (PO-4) as a first buildable artifact~~ — **done**, see `wsus-connector-spec.md` (a few sub-questions remain within it, to be resolved during implementation)

> *(Target dates and resourcing genuinely need your input — they depend on how much time you're able to put in and whether this stays solo or gets contributors, neither of which I can estimate on your behalf. Specific workflow prioritization within Phase 2 is reasonable to leave until Phase 1's proof of concept is actually done, per the roadmap's own sequencing.)*

---

## 10. Open Questions and Future Considerations
*Parking lot for ideas and uncertainties that need exploration.*
- Tech stack decisions
- Licensing and monetization model refinement
- Scalability considerations
- Performance and reliability targets

**Note:** Open questions are now tracked in a dedicated file, `open-questions.md`, organized by category with IDs and status (🔴 Open / 🟡 Exploring / 🟢 Resolved) so they're easy to reference and update without cluttering this document. As of this writing, that tracker includes questions spanning Architecture (AR-1 to AR-4), Tech Stack (TS-1 to TS-3), Licensing & Monetization (LM-1 to LM-2), Scalability (SC-1 to SC-2), Performance & Reliability (PR-1 to PR-2), Marketplace (MP-1 to MP-3), Security (SE-1 to SE-3), and Positioning & Partnership (PO-1 to PO-7).

Refer to `open-questions.md` as the living source of truth for unresolved decisions; resolved questions get reflected back into the relevant section of this document (as already done for the reliability principle in Section 2, the workflow/module distinction in Section 3, and the partnership stance in Sections 6 and 8).

**Threshold conditions (PO-5, PO-6, PO-7) now tracked in `open-questions.md`** — moved there as of this same session, in the Positioning & Partnership table alongside PO-1 through PO-4. Full text retained here for context:
- If NinjaOne's own documentation product expands further into tribal-knowledge capture, deprioritize deep NinjaOne-KB-specific integration for `sab-kb` in favor of Hudu/IT Glue plus PSA data instead (PO-5).
- If ServiceNow's or Zendesk's official MCP servers gain rapid MSP-market traction, prioritize interoperating with them over building competing first-party connectors (PO-6).
- If Ansible Automation Platform licensing terms (post-IBM ownership) become a barrier for SMB-scale sysadmins, consider defaulting `sab-engine`'s execution substrate to the open-source AWX instead, or to direct PowerShell/WinRM remoting without an Ansible dependency at all (PO-7).

**Items in this document genuinely needing Brock's input rather than further design work (collected from throughout, for visibility):**

- Section 2 — a formal functional requirements list (beyond the four non-negotiables already confirmed)
- Section 5.1 / MP-1 — marketplace revenue share percentage
- Section 7 / SE-3 — target compliance framework(s), once a target industry is clearer
- Section 9 — target dates and resourcing

---

### Notes
- This document is intentionally kept flexible and exploratory.
- No section is final; all are subject to revision as we learn more.
- Next step: begin filling in sections, starting wherever makes the most sense.
