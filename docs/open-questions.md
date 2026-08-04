# SAB Open Questions
### Tracking document for unresolved design decisions

**Status key:**
- 🔴 **Open** — not yet discussed / no direction chosen
- 🟡 **Exploring** — discussed, leaning a direction, not decided
- 🟢 **Resolved** — decided, needs to be reflected in the design doc

---

## Architecture

| ID | Question | Status |
|---|---|---|
| AR-1 | Should the orchestration engine be a single long-running service, or a stateless task runner invoked per-workflow with state persisted externally (e.g. a database)? Affects scalability and multi-server/parallel orchestration later. | 🟢 Resolved — **Stateless task runner, state persisted externally.** This follows directly from Section 2's reliability principle: state must survive a crash/restart, which a stateless design gets almost for free since nothing lives only in memory. It also sets up SC-1/SC-2 (scaling to multiple servers/concurrent workflows) cleanly — you scale by running more stateless workers, not by making one long-running process more complex. Reflected in 4.1. |
| AR-2 | Should environment differences (Windows vs. Linux vs. cloud) live in the connector/execution-environment layer, or get pushed into environment-specific module variants (e.g. `apply-patch-windows` vs. `apply-patch-linux`)? Affects how much the module library needs to grow per new environment. | 🟢 Resolved — **Split by what kind of difference it is.** Connection/protocol differences (WinRM vs. SSH vs. cloud API) belong in the connector layer, per the original design intent — that's what makes the "where" invisible upstream. But genuinely different implementations of the same action (e.g. `apt` vs. `yum` vs. Windows Update APIs) need environment-specific module variants under one logical module ID, since the actual commands aren't the same. The module contract stays identical either way — only the implementation and declared supported-environment metadata differ. Reflected in 4.2. |
| AR-3 | Is SAB-KB a distinct storage system/service in its own right (its own database, possibly its own repo), or a logical layer built on top of state/logging the orchestration engine and module library already produce? Affects the repo structure decided earlier. | 🟢 Resolved — **SAB-KB gets its own repo.** The GitHub structure is now four repos (core engine, module library, SAB-KB, commercial layer), not three — SAB-KB's cross-cutting role (used by humans, the AI agent, and the engine alike) earns it first-class status with independent code, schema, and release cadence. This doesn't necessarily mean a separate running service on day one — the Phase 1 logical-layer starting point can still apply architecturally within that repo — but the repo boundary itself is settled. Reflected in Section 4.5 and the Section 9 roadmap. |
| AR-4 | What does the third-party integration API surface look like (beyond modules/connectors) — e.g. for ticketing systems, monitoring, ChatOps tools to trigger workflows or read status? What authentication model does it use? | 🔴 Open — genuinely premature to resolve now; this is Phase 3/4 scope per the roadmap. Worth revisiting once there's a real external integration candidate (e.g. a specific ticketing system) rather than designing this in the abstract. |

---

## Marketplace

| ID | Question | Status |
|---|---|---|
| MP-1 | What's the revenue share model once the marketplace moves to paid listings (Phase 2)? | 🔴 Open — **needs your call**, this is a real business decision. For reference/benchmarking: mobile app stores typically take 15–30%, the VS Code Marketplace charges nothing, and the Terraform Registry doesn't take a cut on free modules but monetizes separately via its paid registry tier. Worth deciding closer to when Phase 2 is actually approaching rather than now. |
| MP-2 | What does the vetting/certification process look like for marketplace content, given modules can execute against production infrastructure (higher stakes than a typical app store)? | 🟢 Resolved at the structural level — **a tiered trust model**: "Community" (submitted, meets the module contract, unreviewed beyond that), "Verified" (passed additional review/testing by SAB maintainers), and "Certified" (vendor-backed, e.g. for compliance packs, with support commitments attached). Exact review criteria per tier still TBD, but the tier structure itself gives a clear framework to build toward. |
| MP-3 | Does the marketplace live in the commercial layer repo, or somewhere else structurally? | 🟢 Resolved — **commercial layer repo**, consistent with what Section 5.1 already states (marketplace is the commercial expression of extensibility, tied to the monetization strategy even during its free phase). |

---

## Security

| ID | Question | Status |
|---|---|---|
| SE-1 | What secrets store/vault technology handles credential resolution for the execution environment? | 🟡 Exploring — **Recommended: don't build custom secrets infrastructure.** Support HashiCorp Vault as a pluggable backend for organizations that already run it (common in the target audience), with native OS credential stores (Windows Credential Manager, Linux keyring/systemd-creds) as a simpler default for smaller self-hosted deployments that don't want to stand up Vault just for SAB. |
| SE-2 | What does the module/execution sandboxing model actually look like technically (containers, VMs, something else) — especially given third-party marketplace modules will eventually run through the same path? | 🟡 Exploring — **Recommended: containers (Docker).** This also matches a pattern already seen in the research — Kestra's language-agnostic task model runs arbitrary scripts inside containers for exactly this isolation reason. Containers give process/resource isolation without the overhead of a full VM per module run, and scale down cleanly to a single self-hosted box for smaller users. |
| SE-3 | What compliance frameworks (SOC2, HIPAA, etc.) should audit logging/retention be designed to satisfy, and does that vary per industry-specific pack? | 🔴 Open — genuinely depends on which industries you target for compliance packs (Section 5.1/9), which isn't decided yet. Worth revisiting once there's a specific target industry rather than designing against every framework speculatively. |

---

## Positioning & Partnership

| ID | Question | Status |
|---|---|---|
| PO-1 | What does a concrete Microsoft partnership/integration path actually look like? **Direction set:** connectors work *with* WSUS/SCCM (read status/catalog data, standardize workflow around them) rather than replacing them — see Section 6. Still open: exact technical spec, and any direct outreach to Microsoft once there's a working proof point. | 🟡 Exploring |
| PO-2 | What would make SAB attractive for UiPath/Workato to integrate with rather than compete against? **Direction set:** design module/workflow contracts to be externally callable, so SAB can plausibly become a connector/plugin *within* their platforms (e.g. a Kestra plugin, a UiPath connector) rather than an alternative to them — see Section 6. Still open: which platform to approach first, and what a pilot integration looks like. | 🟡 Exploring |
| PO-3 | Does public-facing messaging need explicit "built on Microsoft technologies" / "works alongside" language from the start, or does this come later once there's something concrete to point to? | 🟢 Resolved — **use accurate, low-key framing from day one** ("built on PowerShell," "works alongside WSUS/SCCM") since it's simply true and costs nothing to say early. Save more deliberate partnership messaging/outreach for Phase 4 (per Section 9's roadmap), once there's a working proof point (the WSUS-read connector) to actually point to — premature outreach without something concrete to show tends to land worse than none. |
| PO-4 | What's the concrete technical spec for the WSUS-read connector as the first partnership-oriented proof point (what data is read, how often, what SAB does with it)? | 🟡 Exploring — **first draft complete**, see `wsus-connector-spec.md`. Uses the native `UpdateServices` PowerShell module (not direct DB access), polling-based (WSUS has no push mechanism), read-only in this phase. A few sub-questions remain open within the spec itself (polling interval default, exact permission level needed, multi-WSUS-server support) — see the spec's own "Open Sub-Questions" section. |

---

## SAB-KB / SAB-Engine Reconciliation (new — see `temp` folder discovery)

| ID | Question | Status |
|---|---|---|
| RC-1 | Does `sab-kb`'s agent1/4A/4B architecture ever connect to or feed `sab-engine`'s AI agent layer, or are they fully independent products that happen to share a parent org? | 🟢 Resolved — **fully independent for now.** No assumed integration. Whether `sab-engine`'s AI agent ever queries `sab-kb` for broader context is left as a genuine future question, not decided now, since `sab-engine` isn't being actively built yet (see RC-5). |
| RC-2 | `sab-engine`'s Section 4.5 describes a narrow "shared knowledge base" for run history/target state — does `sab-engine` need its own small internal store for this now that `sab-kb` is a separate commercial product, or does it eventually consume from `sab-kb`? | 🟢 Resolved — **`sab-engine` gets its own small internal store**, scoped only to what the orchestration engine and AI agent need. Renamed to "Engine State Store" to avoid the naming collision with the real `sab-kb` product (see RC-3). Lives inside the `sab-engine` repo, not its own repo — reverses the earlier AR-3 decision now that "SAB-KB" as a name belongs to a different, separately-developed product. |
| RC-3 | Naming collision: both `sab-engine` (Section 5.1) and `sab-kb` (its own docs) use "SAB Marketplace" for different things — engine's module/connector marketplace vs. KB's data-source connector framework. Needs a rename on one side before it ships in code or messaging. | 🟢 Resolved — **renamed on the `sab-engine` side.** Section 4.5 "Shared Knowledge Base (SAB-KB)" → "Engine State Store." Section 5.1 "SAB Marketplace" → "SAB Engine Marketplace." This frees "SAB-KB" and "SAB Marketplace" to refer exclusively to the real `sab-kb` product's own concepts, without needing to touch `sab-kb`'s own docs. |
| RC-4 | Should the `sab-kb` local `.git` history be checked for accidentally-committed `.env` secrets before this content moves to a real remote repo? | 🟢 Resolved — **credentials rotated**: Azure AD app client secret, Teams bot app password, and OpenRouter API key were all regenerated after being exposed. Postgres local-dev credentials were confirmed not real secrets, left as-is. **Still open, lower priority:** whether the original values were found via git history (`git log --all --full-history -- .env`) or were just the live gitignored file — matters for knowing if they were ever actually pushed anywhere retrievable, but doesn't change that rotation already mitigates the practical risk either way. |
| RC-5 | Does `sab-kb`'s roadmap (its own detailed Phase 0 build-readiness doc) supersede `sab-engine`'s Phase 1 in `SAB_Design_Document.md` Section 9 — i.e., does the KB engine ship *before* Windows patching, given it has more validation behind it? | 🟢 Resolved — **yes.** `sab-kb` has working code, a resolved business model, and real market validation; `sab-engine` has none of that yet. `sab-engine`'s phased roadmap stays as the plan for *that repo specifically*, but real near-term build effort should go to `sab-kb` first. Reflected in `SAB_Design_Document.md` Section 9 with an explicit sequencing note above Phase 1. |

---

## Tech Stack

| ID | Question | Status |
|---|---|---|
| TS-1 | What language/framework for the orchestration engine itself? | 🟡 Exploring — **Recommended: C#/.NET.** Three reasons: (1) native interop with PowerShell via `System.Management.Automation`, which matters a lot since the engine constantly invokes PowerShell modules; (2) it's cross-platform via modern .NET, so this doesn't lock out the Linux/cloud future in Section 4.4; (3) it directly supports the partnership positioning in Section 6/8 — building on Microsoft's own stack (and potentially Microsoft's Semantic Kernel for the AI agent layer, see TS-2) is a concrete, credible signal of "built to work with Microsoft," not just messaging. This is a strong recommendation, not a final decision — your comfort/experience with the language matters too. |
| TS-2 | What language/framework for the AI agent layer? | 🟡 Exploring — **Recommended: also .NET, using Microsoft's Semantic Kernel framework**, for the same partnership reasoning as TS-1, plus it keeps the whole engine+agent stack in one language (simpler for contributors, one build/deploy story). The tradeoff: Python's LLM/agent tooling ecosystem (LangChain, etc.) is larger and more battle-tested. If the AI agent layer ends up needing capabilities Semantic Kernel doesn't cover well, Python is the fallback worth reconsidering. Worth a small prototype/spike before fully committing. |
| TS-3 | What's the state persistence layer (database choice)? | 🟢 Resolved — **PostgreSQL.** Open source, extremely well understood, handles both structured state (workflow runs) and the more flexible querying SAB-KB will eventually need. This is a "boring and reliable" choice deliberately, per Section 2's own stated design philosophy — no reason to pick something exotic here. |

---

## Licensing & Monetization

| ID | Question | Status |
|---|---|---|
| LM-1 | What open source license for the core engine and module library (MIT, Apache 2.0, AGPL, etc.)? | 🟡 Exploring — **Recommendation updated: Apache 2.0 (or plain GPL), not AGPL.** Verified against actual early Red Hat history (not just the general "Red Hat model" phrase): Red Hat used standard permissive/copyleft licensing and never tried to legally prevent rebuilding or rehosting — CentOS ran for years as a free RHEL clone with Red Hat's blessing, and Red Hat even acquired the CentOS project in 2014 rather than fighting it. Their value capture came entirely from the subscription relationship (support, updates, certification, stability), not license enforcement. Notably, Red Hat Network Satellite — their own patch/lifecycle management product, directly analogous to SAB — was itself open-sourced in 2008 (Project Spacewalk) and a competitor built a rival product on that code; Red Hat still thrived. AGPL was the earlier recommendation based on the MongoDB/Elastic playbook, which is a genuinely different lineage from Red Hat's. Apache 2.0 is the more direct match for "follow early Red Hat" specifically, and adds patent protection language GPL lacks. |
| LM-2 | How exactly is the line drawn between what's free (open source) vs. paid (commercial layer) as the module library grows? | 🟢 Resolved, refined — **the Fedora/RHEL split is a better model than feature-gating.** Rather than gating specific features behind a paywall, mirror how Red Hat separates fast-moving community innovation (Fedora) from a hardened, curated, supported snapshot (RHEL): the open module library stays fully free and community-driven, bleeding-edge included, while a paid tier offers a **hardened/certified module and workflow set** — tested, stable, support-backed — for production use. This fits the open-core ethos better than gating functionality, and gives Marketplace's tiered trust model (MP-2) a natural monetization hook: Certified-tier content is where the subscription value concentrates. |

---

## Scalability & Performance

| ID | Question | Status |
|---|---|---|
| SC-1 | How does the system handle orchestrating across multiple servers at once (vs. single-server patching)? | 🟡 Exploring — largely follows from AR-1's resolution: since state is externally persisted rather than living in one long-running process, scaling to multiple concurrent server targets is mostly a matter of running more stateless workers against the shared state store, not a separate design problem. Still needs concrete implementation detail once Phase 2 approaches. |
| SC-2 | How does state/execution scale when many workflows run concurrently? | 🟡 Exploring — same reasoning as SC-1. Worth a real load-testing pass once Phase 1's proof of concept exists, rather than designing for a scale that isn't validated yet. |
| PR-1 | What are the specific reliability/uptime targets for the orchestration engine? | 🔴 Open — **needs your call.** This depends on who you're targeting first (a solo SA's lab environment has very different expectations than an MSP's client SLA) — worth deciding once there's a clearer first-customer picture rather than picking a number speculatively. |
| PR-2 | What's an acceptable rollback time for a failed module/workflow? | 🔴 Open — **needs your call**, likely varies by workflow risk level rather than being one system-wide number. Reasonable to defer until Phase 1's actual patching workflow gives real rollback timing data to anchor a target against. |

---

### Notes
- This is a living tracking doc — pulls questions out of the main design document so they don't get lost in prose.
- Update status as questions get discussed and resolved, and reflect resolved decisions back into `SAB_Design_Document.md`.
