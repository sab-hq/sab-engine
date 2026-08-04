# SAB System Design Document
### System Administration Builder — Design Foundation

**Version:** v0.1.1
**Document Status:** Living Document — Open for Iteration and Refinement
**Last Updated:** August 2, 2026
**Purpose:** High-level design exploration and framework planning for SAB, an open system that standardizes and automates system administration workflows using reusable modules and AI agent orchestration.

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [System Requirements and Goals](#2-system-requirements-and-goals)
3. [Core Architecture Overview](#3-core-architecture-overview)
4. [Component Breakdown](#4-component-breakdown)
   - 4.1 Orchestration Engine
   - 4.2 Module Library System
   - 4.3 AI Agent Layer
   - 4.4 Execution Environment
   - 4.5 Engine State Store *(renamed from "Shared Knowledge Base (SAB-KB)" — see RC-3)*
5. [Extensibility and Plugin System](#5-extensibility-and-plugin-system)
   - 5.1 SAB Engine Marketplace *(renamed from "SAB Marketplace" — see RC-3)*
6. [Integration with Existing Enterprise Tools](#6-integration-with-existing-enterprise-tools)
7. [Security and Compliance Considerations](#7-security-and-compliance-considerations)
8. [Existing Solutions and Learnings](#8-existing-solutions-and-learnings)
9. [Development Roadmap and Next Steps](#9-development-roadmap-and-next-steps)
10. [Open Questions and Future Considerations](#10-open-questions-and-future-considerations)

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

**Core Design Principle: Reliability and Gradual Autonomy**
The system must be solid and predictable before it is trusted to act autonomously. This is non-negotiable given the system touches production infrastructure. Implications:
- The AI agent layer should start in a **recommend-and-approve** mode — it proposes an action and reasoning, a human approves before execution. Full autonomy is a later capability, not a launch requirement.
- Every module should have a well-tested rollback/undo path from day one. Cheap, reliable recovery from failure is what eventually earns the trust needed for more autonomy.
- New workflows and modules should be validated against low-stakes/lab environments before being trusted against production systems.
- "Boring and reliable" is a feature, not a limitation — especially for early open source adoption, where sysadmins need to trust the core before they'll run it against anything that matters.

> *(Further requirements to be filled in)*

---

## 3. Core Architecture Overview
*How all the pieces fit together at a 30,000-foot level — data flow, component relationships, overall design philosophy.*

**Key Concept: Workflow vs. Module**
- A **module** is a single reusable unit of work (e.g. `check-patch-status`, `apply-patch`). Modules are "dumb" and reliable — they do one job, don't make decisions, and don't know about the bigger picture.
- A **workflow** is the ordered recipe that strings modules together to accomplish a real-world use case (e.g. "patch this server" = pre-flight check → stage → apply → validate → rollback-if-needed). Workflows capture the process an SA/SE would normally walk through manually.

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

**Tech Stack Direction (see `open-questions.md`, TS-1/TS-2/TS-3)**
Current recommendation, still open for confirmation: **C#/.NET** for both the orchestration engine and AI agent layer (the latter potentially using Microsoft's Semantic Kernel), and **PostgreSQL** for state persistence. The .NET choice is deliberate beyond technical merit — native PowerShell interop and building on Microsoft's own stack directly support the partnership positioning in Sections 6 and 8. Modules themselves stay in PowerShell/Bash regardless, per the module contract (4.2) — this only affects what the engine and agent are written in.

> *(Further architectural detail — diagrams, data flow specifics — to be added)*

---

## 4. Component Breakdown
*Deep dive into each major component individually.*

### 4.1 Orchestration Engine
- How does it coordinate tasks and workflows?
- How does it manage state and execution flow?
- What are the inputs and outputs?

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

> *(Further detail — e.g. exact state machine, concurrency model — to be added)*

### 4.2 Module Library System
*See Section 3 for the foundational workflow-vs-module distinction: modules are atomic, reusable units of work; workflows are the recipes that string modules together for a specific use case.*
- How are reusable components structured and catalogued?
- How do modules interact with the orchestration engine?
- Standards and conventions for building modules?
- How are workflows (recipes) defined, stored, and versioned separately from the modules they call?

**Role in the System**
The module library is where the actual work lives — it's the "do the work" layer (see Section 3). Modules are deliberately dumb: they don't make decisions, they just perform one well-defined action reliably and report back what happened.

**The Module Contract**
For the orchestration engine to call any module interchangeably, every module needs to follow the same standard shape, regardless of what it actually does or what language it's written in underneath (PowerShell to start, Bash/IaC later). At minimum, each module needs:
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

**Community contribution implications**
Since this repo is open source (see business model discussion), the module contract *is* the contribution guideline — anyone submitting a module knows exactly what's required (metadata, rollback, tests) for it to be accepted and trusted by the engine.

> *(Further detail — e.g. exact metadata schema, module versioning/compatibility rules — to be added)*

### 4.3 AI Agent Layer
- How do AI agents decide which tasks to run?
- How do they interact with the orchestration engine?
- What information do they need to operate effectively?

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

> *(Further detail — e.g. how the agent's model/reasoning is built, what "unusual" means quantitatively — to be added)*

### 4.4 Execution Environment
- Where and how do scripts actually execute?
- How do we handle on-prem, cloud, and hybrid scenarios?
- Connection management and security.

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

> *(Further detail — e.g. exact connector interface spec — to be added)*

### 4.5 Engine State Store
*(Renamed from "Shared Knowledge Base (SAB-KB)" — see `open-questions.md` RC-3. The name "SAB-KB" now belongs exclusively to the separate `sab-kb` commercial product; this component keeps `sab-engine`'s original narrow scope under a name that doesn't collide.)*
- What is the Engine State Store and why does it exist as its own component?
- What information lives there, and who/what reads and writes it?
- How does it relate to the other components?

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

> *(Further detail — e.g. data model, query interface for the AI agent, human-facing UI — to be added)*

---

## 5. Extensibility and Plugin System
*How do new tools, frameworks, and capabilities get added?*
- API design for third-party integrations
- Module development patterns and guidelines
- Community contribution framework

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

> *(Further detail — e.g. exact API surface, authentication for third-party integrations — to be added)*

---

### 5.1 SAB Engine Marketplace
*(Renamed from "SAB Marketplace" — see `open-questions.md` RC-3. `sab-kb`'s docs use "SAB Marketplace" for a different thing — its Email/Teams/PSA connector framework. This is `sab-engine`'s marketplace specifically, for modules and connectors that plug into the orchestration engine.)*

**What it is**
A dedicated place where users — community members, third-party developers, and eventually vendors — can publish and (later) sell add-ons: modules, connectors, and full workflow packs built on the extensibility contracts above. It's the commercial expression of the extensibility model, and ties directly into the business model established earlier (open-core, monetization via the commercial layer).

**Relationship to the rest of the system**
- The marketplace doesn't replace the open source module library (4.2) — it sits alongside/on top of it. Community modules can still be free and live in the open source repo; the marketplace is specifically for discoverability and (eventually) commerce.
- Anything listed in the marketplace still has to satisfy the module or connector contract — the marketplace is a distribution and monetization layer, not a different technical standard. This keeps quality/trust consistent regardless of whether something is free or paid.
- Likely lives in or alongside the **commercial layer repo** (per the four-repo structure — core engine, module library, `sab-kb`, commercial layer) since it's part of the monetization strategy, even in its free starting phase.
- Maps naturally onto the tiered trust model (MP-2, `open-questions.md`): **Community** and **Verified** tiers stay free, and the **Certified** tier — hardened, tested, support-backed — is where the Fedora/RHEL-style paid value concentrates (see Section 1's business model). This gives the marketplace a monetization mechanism consistent with early Red Hat's actual approach, rather than an app-store-style paywall on functionality itself.

**Phased rollout**
1. **Phase 1 — Free add-ons only.** Focus on discoverability and adoption: a catalog where people can browse and pull in community-built modules, connectors, and workflow packs. No money changes hands yet. This builds the supply side (contributors) and demand side (users who trust and rely on marketplace content) before monetization enters the picture.
2. **Phase 2 — Full marketplace.** Introduce paid listings — developers/vendors can sell modules, connectors, or workflow packs (e.g. industry-specific compliance packs). SAB likely takes a revenue share, similar to how app stores or the Terraform/VS Code marketplace models work.

**Trust and safety implications**
Because modules can execute against production infrastructure, a marketplace introduces a real trust question beyond typical app-store concerns — a malicious or poorly-tested module isn't just annoying, it's a risk to someone's servers. This connects directly to Section 2's reliability principle and Section 7 (Security). **Design direction (see `open-questions.md`, MP-2):** a three-tier trust model — **Community** (meets the module contract, otherwise unreviewed), **Verified** (passed additional review/testing by SAB maintainers), and **Certified** (vendor-backed, with support commitments attached — likely where compliance packs live). Exact review criteria per tier still TBD, but this gives a clear structure to build toward.

> *(Further detail — revenue share model (MP-1, needs your input), review criteria per tier, marketplace UI/discovery — to be added)*

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

> *(Further detail — concrete technical spec for the WSUS-read connector as a first proof point, outreach strategy — to be added)*

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
- **Design direction (see `open-questions.md`, SE-2):** containers (Docker) as the sandboxing mechanism — process/resource isolation without full-VM overhead, and it matches the pattern seen in comparable tools (Kestra runs arbitrary tasks in containers for the same reason).

> *(Further detail — compliance framework mappings once a target industry is clearer — to be added)*

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

> *(Further detail — hands-on trial of Kestra/JetPatch if useful, ongoing tracking of new entrants — to be added as needed)*

---

## 9. Development Roadmap and Next Steps
*How do we build this incrementally?*
- **Phase 1:** Windows Server patching proof of concept
- **Phase 2:** Expand module library and add more workflows
- **Phase 3:** Cloud and hybrid support
- **Phase 4:** Community and open source launch

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

  **2. `sab-hq/sab-modules` (module library) — open source**
  - PowerShell/Bash modules following the module contract, starting with the patching set (pre-flight check, stage, apply, validate, rollback)
  - Workflow definitions (recipes stringing modules together, per Section 3's workflow/module distinction)
  - Community-contributed connectors beyond WinRM (SSH, cloud) likely land here once they exist, alongside modules, since both follow the same contract-based contribution pattern
  - The `wsus-connector-spec.md` implementation (the WSUS-read integration) likely also lives here, since it's a data-source integration feeding SAB-KB rather than an execution-environment connector

  **3. `sab-hq/sab-kb` — closed/commercial (MSP knowledge & documentation engine, per-seat subscription)**
  - **Correction from earlier draft:** previously described here as open source ("logical layer on top of orchestration run-history") — that was wrong. `sab-kb` is not a thin support component for `sab-engine`; it's the actual near-term, sellable v1 product, with its own agents (orchestrator + capture/curation agents), Email/Teams connectors, and a real, already-researched business model (per-technician-seat MSP subscription, free on-ramp tier). Full design lives in its own doc set (currently in the local `temp` folder pending migration) — not duplicated here.
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

**⚠️ Sequencing Note (resolves `open-questions.md` RC-5):** The phases below describe `sab-engine`'s own build path, but they are **not** necessarily what to work on next in real time. `sab-kb` — a separate product — already has working code (agents, connectors, a frontend), a resolved business model, and real market validation behind it, none of which `sab-engine` has yet. The honest read: **`sab-kb` should ship before `sab-engine`'s Phase 1**, not after. Treat `sab-engine`'s phases below as the roadmap for *this repo specifically*, and treat `sab-kb`'s own build-readiness docs (in the `temp` folder, pending migration) as the actual near-term priority for where build effort goes first.

### Phase 1: Windows Server Patching Proof of Concept
*Prove the core architecture works end-to-end on the narrowest possible slice.*
- Core orchestration engine: sequencing, state tracking, rollback triggering (Section 4.1) — minimum viable version
- A small set of patching modules: pre-flight check, stage, apply, validate, rollback (Section 4.2)
- AI agent layer in recommend-and-approve mode only — no autonomy stretch goals at this phase (Section 4.3)
- Execution environment: WinRM connector for on-prem Windows only (Section 4.4)
- SAB-KB: minimal version — enough to log run history and feed the agent's recommendations, not the full shared-knowledge vision yet (Section 4.5, currently pinned for deeper design)
- WSUS-read connector as the first partnership-oriented integration proof point (Section 6, PO-4)
- **Exit criteria:** SAB can reliably patch a lab/low-stakes Windows Server end-to-end, with a human approving each run and a tested rollback path proven to work, not just documented

### Phase 2: Expand Module Library and Workflows
*Prove the module/workflow model generalizes beyond patching.*
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
1. **`sab-kb` build work takes priority over `sab-engine` Phase 1** — per the sequencing note above (RC-5). This is the realistic next step, not what follows.
2. For `sab-engine` specifically, once attention returns to it: resolve the highest-leverage open architecture questions before writing code — particularly TS-1/TS-2 (tech stack), since these affect almost everything built in Phase 1
3. ~~Create the four repos under `sab-hq`~~ — **done**, all four repos exist, are connected, and (except `sab-kb`, still pending its migration) have real content pushed
4. ~~Draft the concrete technical spec for the WSUS-read connector (PO-4) as a first buildable artifact~~ — **done**, see `wsus-connector-spec.md` (a few sub-questions remain within it, to be resolved during implementation)

> *(Further detail — target dates, resourcing, specific workflow prioritization within Phase 2 — to be added as they become clearer)*

---

## 10. Open Questions and Future Considerations
*Parking lot for ideas and uncertainties that need exploration.*
- Tech stack decisions
- Licensing and monetization model refinement
- Scalability considerations
- Performance and reliability targets

**Note:** Open questions are now tracked in a dedicated file, `open-questions.md`, organized by category with IDs and status (🔴 Open / 🟡 Exploring / 🟢 Resolved) so they're easy to reference and update without cluttering this document. As of this writing, that tracker includes questions spanning Architecture (AR-1 to AR-4), Tech Stack (TS-1 to TS-3), Licensing & Monetization (LM-1 to LM-2), Scalability (SC-1 to SC-2), Performance & Reliability (PR-1 to PR-2), Marketplace (MP-1 to MP-3), Security (SE-1 to SE-3), and Positioning & Partnership (PO-1 to PO-4).

Refer to `open-questions.md` as the living source of truth for unresolved decisions; resolved questions get reflected back into the relevant section of this document (as already done for the reliability principle in Section 2, the workflow/module distinction in Section 3, and the partnership stance in Sections 6 and 8).

---

### Notes
- This document is intentionally kept flexible and exploratory.
- No section is final; all are subject to revision as we learn more.
- Next step: begin filling in sections, starting wherever makes the most sense.
