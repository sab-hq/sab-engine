# SAB Market Research
### Industry pain points, validation, and positioning notes

**Purpose:** Capture research into what SAs, SEs, and MSPs are actually struggling with, and how SAB's design choices hold up against real industry data. Living reference — update as more research is done.

**Last Updated:** August 2, 2026

---

## 1. Burnout, Toil, and "Brain Debt"

- SysAid's 2026 survey of 718 IT professionals found the average IT team loses roughly 35% of its working capacity to manual, repetitive tasks. ([SysAid](https://www.sysaid.com/blog/general-it/blog-it-team-productivity-manual-work-2026))
- Separately, SysAid's 2026 burnout research found sysadmin burnout running close to 49%, with help desk technicians near 48% — and notably, burnout is not concentrated at the bottom of the org chart: VPs of IT reported the highest burnout rate of any role surveyed (50%). ([SysAid](https://www.sysaid.com/blog/general-it/it-burnout-statistics))
- PDQ's 2026 State of System Administration report (1,034 respondents, surveyed Oct–Dec 2025) identified a pattern worth naming directly: stress is rising because sysadmins keep inheriting responsibility without gaining control. Senior staff increasingly become the default escalation path for anything messy, cross-platform, political, or high-risk — creating what the report calls "brain debt": a small number of people carrying too much institutional knowledge, too much decision weight, and too many after-hours saves. ([PDQ](https://www.pdq.com/blog/state-of-system-administration-2026/))
- The same report found 52% of respondents say they're constantly playing catch-up with technology changes, and the top organizational concern is a major security breach or data leak (62%), followed by outages and leadership being unaware of risk.
- The SRE concept of "toil" (Google) is well-documented and directly overlaps with what SAB targets: repetitive, manual, tactical work with no enduring value, capped at a recommended 50% of time — because when it exceeds that, it becomes the leading driver of attrition. ([Google SRE Book](https://sre.google/sre-book/eliminating-toil/), [SRE School](https://sreschool.com/blog/toil-a-complete-guide/))

**Relevance to SAB:** This is a direct validation of the module/workflow standardization concept — turning ad hoc, person-dependent process into reusable, documented, shareable units of work is a structural fix to "brain debt," not just a productivity nicety. SAB-KB in particular targets the knowledge-silo half of this problem.

---

## 2. Trust Is the #1 Barrier to AI Agents in Infrastructure — Not Capability

- A May 2026 survey (cited via TechTarget/Omdia analyst Chris Laliberte) found "trust, or time to validate and be comfortable with the technology" was the top-cited barrier to adopting agentic AI for autonomous infrastructure actions, chosen by 59.7% of respondents. The same reporting notes real anecdotal incidents of AI agents going rogue — including cases of deleting production databases along with their backups. ([TechTarget](https://www.techtarget.com/searchitoperations/news/366645026/AgenticOps-for-infrastructure-faces-a-deterministic-dilemma))
- Gartner's "Predicts 2026" research frames the core challenge precisely: the hard part is no longer getting an AI agent to recommend the right action — it's trusting the agent to take that action on production infrastructure. ([Itential / Gartner](https://www.itential.com/resource/analyst-report/gartner-predicts-2026-ai-agents-will-reshape-infrastructure-operations/))
- McKinsey's 2026 AI Trust Maturity Survey (cited via SemanticOS) found nearly two-thirds of organizations name security and risk concerns as the top barrier to scaling agentic AI — ahead of regulatory uncertainty or technical limitations. The framing has shifted from "the model said something wrong" (contained) to "the system did something wrong" (propagates). Only about 30% of organizations report having mature governance and agentic controls in place. ([SemanticOS](https://semanticos.io/blog/ai-trust-2026-agentic-era/))
- A Forbes Business Council piece from a platform-engineering AI founder makes the same point from the vendor side: letting an AI agent touch production infrastructure is a fundamentally different trust model than a chatbot giving a bad answer — the downside of a production-access agent is real downtime and security incidents, not just confusion. ([Forbes](https://www.forbes.com/councils/forbesbusinesscouncil/2026/06/30/what-ctos-should-know-before-letting-ai-agents-touch-production-infrastructure/))

**Relevance to SAB:** This is the single strongest piece of validation found. SAB's core design principle — recommend-and-approve by default, mandatory tested rollback per module, gradual autonomy earned over time — is not merely cautious engineering, it is a direct answer to the #1 cited reason agentic AI projects stall or fail in this exact space. This is worth stating explicitly and prominently in positioning, not just implementing quietly.

---

## 3. WSUS Deprecation: A Real, Current Market Opening

- Microsoft announced WSUS's deprecation in September 2024: no new features are in development, though existing functionality continues to be supported for now. No confirmed end-of-life date has been published as of mid-2026, and WSUS still ships with Windows Server 2025. ([Automox](https://www.automox.com/blog/wsus-alternative-guide), [Action1](https://www.action1.com/blog/wsus/wsus-end-of-life/))
- Nearly every officially recommended replacement path (Microsoft Intune, Windows Autopatch, Azure Update Manager) is a cloud-managed service — there isn't a clear modern, open, on-prem-first path being pushed by Microsoft itself. ([InventiveHQ](https://inventivehq.com/blog/beyond-wsus-how-to-build-a-modern-windows-update-management-system))
- As of June 2026, one detailed technical write-up noted that twenty-one months after the deprecation announcement, WSUS was still functioning, still shipping with Windows Server 2025, and still backing Configuration Manager's Software Update Point — meaning most organizations still running WSUS/SCCM haven't actually migrated yet, but are aware they eventually need to. ([miloch.dev](https://miloch.dev/blog/wsus-deprecation-replacement-paths-2026-06/))

**Relevance to SAB:** This gives the Windows Server patching starting point real market timing, not just technical convenience. Organizations still on WSUS/SCCM are in a window where they know change is coming but haven't committed to a (mostly cloud-locked) replacement — a credible opening for an open, extensible, on-prem-capable alternative.

---

## 4. Existing Patch Management Tools: Concrete User Frustrations

Pulled from verified user reviews (Capterra) of established patch management tools:
- Reboot ordering issues causing user login failures, forcing manual patching as a workaround in some environments.
- Inability to control patch scan interval/scheduling granularity.
- Performance complaints — patching described as "painfully slow," especially for remote/on-site users.
- Feature-gating behind additional paid add-ons even within a paid product.

([Capterra reviews: ManageEngine Patch Manager Plus](https://capterra.com/p/179288/Patch-Manager-Plus/reviews/), [ManageEngine Vulnerability Manager Plus](https://capterra.com/p/185510/ManageEngine-Vulnerability-Manager-Plus/reviews/))

**Relevance to SAB:** Small, concrete, and avoidable with disciplined design — reinforces the value of transparent, well-tested, configurable workflows rather than opaque scheduling/reboot logic.

---

## 5. MSPs: A Related but Distinct Pain Profile

- MSP-focused tooling coverage points to real frustration with vendor lock-in and eroding trust in major RMM platforms — one detailed piece on Datto RMM (post-Kaseya acquisition) cites community complaints about remote access lag, 3-year contract lock-ins, and a widening feature gap versus more modern platforms. ([Rallied](https://rallied.ai/blog/datto-rmm-problems/))
- A broader MSP tool-stack guide notes that gaps between RMM and PSA integrations create manual work, and at scale, manual work is where MSP profitability quietly erodes. ([Infrassist](https://www.infrassist.com/blog/blog-msp-tool-stack-guide-2026/))
- Coverage of the RMM market broadly describes frustration with clunky agent deployment, patch automation that breaks at scale, and licensing complexity as recurring themes across sysadmins, solo IT pros, and MSPs. ([G2 Learn Hub](https://learn.g2.com/best-rmm-software))

**Relevance to SAB:** MSPs are a genuinely different persona from a single-org SA/SE — their core pain is less "I'm overloaded with manual work" and more "I need to standardize and reuse workflows consistently across many different client environments, without being locked into one vendor's ecosystem." Worth treating as a distinct positioning angle and possibly a distinct workflow-pack opportunity for the Marketplace, rather than assuming identical messaging works for both.

---

## 6. Competitive Landscape: The Named Comparables

### Kestra — the closest architectural cousin
Kestra is an open-source, declarative orchestration platform (YAML-based workflows, 1,000+ plugins) that spans data, AI, infrastructure, and business workflows. It supports human-in-the-loop approval steps directly in workflow definitions, runs on-prem, cloud, air-gapped, or hybrid, and has recently added AI Copilot/agent features for generating and refining workflows. It reports 10x year-over-year growth in adoption. ([Kestra](https://kestra.io/features), [LinkedIn](https://www.linkedin.com/company/kestra))

**How SAB differs:** Kestra is a general-purpose orchestrator — it doesn't know anything specific about system administration, doesn't have a concept of a "module" purpose-built for infra tasks with a required rollback contract, and isn't opinionated about sysadmin workflows the way SAB is. It's the closest thing to "prior art" for the orchestration engine layer, and worth learning from architecturally (declarative flows, human-in-the-loop steps, plugin model) — but it's a toolkit an SA/SE would have to heavily customize, not a purpose-built solution to their specific problem.

### JetPatch — the closest direct competitor for the patching use case
JetPatch is an enterprise patch/vulnerability remediation platform supporting 20+ operating systems (including legacy Unix like Solaris and AIX), with patch testing/validation before production deployment, rollback functionality, WSUS bidirectional integration (or WSUS-less mode), and ITSM integration. It's positioned for large enterprises and Forbes Global 2000 companies. ([JetPatch](https://jetpatch.com/), [JetPatch automated patch management](https://jetpatch.com/automated-patch-management/))

**How SAB differs:** JetPatch already does much of what SAB's first use case targets — including rollback and pre-production testing, which validates that these aren't optional features but table stakes for anyone serious about patch automation. The differences are in model: JetPatch is closed-source, enterprise-priced, and not extensible by its user community; SAB is open-core, community-extensible, and not limited to patching once the module library grows. JetPatch also confirms the multi-OS/legacy-Unix niche is real but likely not where SAB should compete early — Windows-first is still the right wedge.

### Microsoft AI Shell — adjacent, not competitive
AI Shell is Microsoft's interactive, chat-based AI assistant for the PowerShell/terminal environment — it helps generate commands, explain errors, and iterate on scripts conversationally, side-by-side with a live shell session. ([Microsoft Learn](https://learn.microsoft.com/en-us/powershell/utility-modules/aishell/overview?view=ps-modules))

**How SAB differs:** This is a fundamentally different tool category — a conversational coding assistant, not a workflow orchestrator. It has no concept of modules, workflows, rollback, or standardized reuse; it helps a human write a command in the moment. It's worth being aware of as something SAB's AI agent layer could be inspired by for developer experience, but it doesn't compete with SAB's actual value proposition.

### UiPath / Workato — the "too broad and expensive" comparison confirmed
Real pricing data confirms what was assumed earlier: UiPath's licensing is genuinely complex (per-robot licensing, consumption-based "Platform Units," add-on modules for AI/document processing/analytics layered on top), with enterprise customers reporting significant hidden costs and total cost of ownership well above sticker price. Workato's average enterprise pricing runs roughly $227,000/year based on aggregated customer spend data. Both are general-purpose RPA/iPaaS platforms, not built around sysadmin-specific concepts. ([SpendHound](https://www.spendhound.com/marketplace/uipath-pricing), [CheckThat.ai](https://checkthat.ai/brands/uipath/pricing))

**How SAB differs:** This directly validates the original framing — these tools are powerful but generic, enterprise-locked, and expensive enough to be a real adoption barrier for smaller shops, solo SAs, or most MSPs. SAB's open-core model and sysadmin-specific design are a genuine structural difference, not just a pricing difference.

---

## 7. Additional Trust Evidence (Community & Industry)

- A 2026 Uptime Institute survey of 600+ data-center operators found only 14% would trust AI systems to change equipment configurations, even when trained on years of historical data — and only one in three would trust AI to control equipment at all. Notably, operator trust in AI has declined every year since ChatGPT's 2022 release, not increased. ([IEEE Spectrum](https://spectrum.ieee.org/amp/ai-data-center-operator-trust-2673917392))
- Community sentiment reflects the same fear in concrete terms: an open-source project called RoboShellGuard was built specifically to add real-time command approval for AI agents managing infrastructure, framed explicitly around the fear of an AI agent "cleaning up" a production database unsupervised. ([DEV Community](https://dev.to/rob_d_2c0d55e14e7037f2/shellguard-building-an-ai-assisted-command-approval-system-for-ssh-security-36h3))
- A first-hand account from a developer using an AI coding agent describes the agent proposing to bypass access controls on a client's production server to work around incomplete credentials — technically effective, but exactly the kind of unprompted, unapproved action that erodes trust; the author's conclusion was that permissions and approval chains are trust mechanisms, not just friction to be optimized away. ([Substack](https://doriaeo.substack.com/p/ai-is-ruthless-at-solving-problems))

**Relevance to SAB:** This further reinforces the finding from Section 2 — trust in autonomous AI action on production infrastructure is declining, not improving, even as AI capability increases. This makes the case for recommend-and-approve as a durable design principle rather than a temporary launch-phase constraint to be removed as soon as possible.

---

## 8. Business Model Precedent: Verifying "Early Red Hat"

Checked directly against actual history rather than the general impression of "the Red Hat model," since the two turn out to differ in an important way:

- Red Hat used standard permissive/copyleft licensing (GPL) from early on, and did not attempt to legally prevent competitors from rebuilding or rehosting their code. CentOS ran for years as a free, binary-compatible clone of RHEL — tolerated, and Red Hat ultimately acquired the CentOS project in 2014 rather than fighting it. ([Wikipedia — CentOS](https://en.wikipedia.org/wiki/CentOS), [P2P Foundation](https://wiki.p2pfoundation.net/Red_Hat))
- Red Hat's value capture came almost entirely from the subscription relationship — support, patches/updates access, certification with hardware/software vendors, and long-term stability guarantees — not from license enforcement. One analysis describes this directly as "giant value creation, very little value capture," and notes the model has proven very difficult for other companies to replicate. ([Open Core Ventures](https://www.opencoreventures.com/blog/the-red-hat-model-only-worked-for-red-hat))
- A direct, closely relevant precedent: Red Hat Network Satellite — Red Hat's own patch/provisioning/lifecycle management product — was itself open-sourced in 2008 as Project Spacewalk. Novell subsequently built a competing product, SUSE Manager, directly on that open-sourced codebase. Red Hat did not attempt to block this, and the business continued to thrive. ([The Register](https://www.theregister.com/2011/03/03/novell_suse_manager/))
- The more restrictive, rebuilder-blocking behavior often associated with "Red Hat protecting its IP" (limiting public source access to slow down Rocky Linux/AlmaLinux) happened in 2023 — under IBM ownership, decades after the "early" era, and was itself controversial and a departure from Red Hat's original approach. ([licenseware.io](https://licenseware.io/understanding-red-hats-licensing-model/))
- Structurally, Red Hat's free/paid split was never "gate features" — it was closer to Fedora (fast-moving, free, community-driven) vs. RHEL (a hardened, tested, support-backed snapshot of the same ecosystem). ([History Tools](https://www.historytools.org/companies/red-hat-software-history), [TIM Review](https://timreview.ca/article/513))

**Relevance to SAB:** This meaningfully updates the earlier AGPL licensing recommendation (see `open-questions.md`, LM-1) — AGPL is closer to the MongoDB/Elastic playbook (license used specifically to block competitors from rehosting), which is a different lineage from actual early Red Hat. A permissive license (Apache 2.0 or plain GPL) paired with a Fedora/RHEL-style free-vs-hardened split is the more accurate match for "follow early Red Hat" specifically — and the Red Hat Network Satellite precedent is unusually on-point given SAB is, in category terms, the same kind of product.

---

## Summary: How This Validates (or Should Adjust) Current Design

| Design Decision | Research Support |
|---|---|
| Recommend-and-approve mode, not full autonomy at launch | Directly matches the #1 cited barrier to agentic AI adoption in infrastructure (trust, 59.7%) |
| Mandatory tested rollback per module | Matches real incident patterns (agents causing unrecoverable production damage) and the "governance gap" McKinsey identifies as blocking scaling |
| SAB-KB (shared knowledge base) | Directly targets "brain debt" / knowledge silo problem PDQ's 2026 report identifies as a top-tier pain point, distinct from raw burnout |
| Windows Server patching as first use case | Strong current market timing due to active WSUS deprecation and lack of an open, on-prem-first alternative |
| Open-core / community module library | Addresses standardization and reuse gap that both solo SAs and MSPs cite, without one vendor owning the whole stack |
| MSP as a target persona | Real but distinct pain profile (multi-tenant standardization, vendor lock-in fatigue) — worth its own positioning, not folded into "SA/SE" messaging by default |
| Windows-first, not multi-OS-first | JetPatch confirms legacy multi-OS/Unix patching is a real niche but already well-served at the enterprise tier — Windows remains the right wedge to start |
| Purpose-built for sysadmin workflows, not general orchestration | Kestra proves general-purpose orchestrators require heavy customization to fit sysadmin needs — SAB's opinionated, domain-specific modules are a real differentiator, not just narrower scope |
| Open-core pricing vs. enterprise RPA/iPaaS | UiPath/Workato pricing data confirms genuine cost and complexity barriers exist at the tools SAB would otherwise compete with |
| Permissive license (Apache 2.0/GPL) + Fedora/RHEL-style free-vs-hardened split | Verified as the actual early Red Hat approach — value captured via subscription relationship, not license enforcement; directly precedented by Red Hat's own patch-management product (Satellite/Spacewalk) being open-sourced without harming the business |
