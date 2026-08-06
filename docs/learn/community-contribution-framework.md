# Community Contribution Framework

*A beginner's guide to how SAB is built to grow — and how you can be part of that.*

---

## In one sentence

The **community contribution framework** is what lets anyone — not just the core team — build a new module or connector for SAB and have it actually trusted and usable, because every contribution just has to follow the same clear, well-defined contract rather than pass a subjective "does this fit our style" review.

## The problem it solves

A lot of open-source projects run into the same wall: contributing feels like a black box. You write something, submit it, and then it's up to whoever's reviewing it to decide — based on taste, familiarity with the codebase, or a hundred unwritten conventions — whether it gets in. That's slow, discouraging, and it doesn't scale past a small core team.

SAB is built to avoid that specific problem. Because every **module** (see `modules.md`) and every execution environment **connector** (see `execution-environment.md`) already has to follow a strict, well-documented shape — the same one the core team itself has to follow — there's no separate, higher bar for outside contributors to clear. If your module meets the contract, it's a real, valid module. Full stop.

## What it actually is

The whole framework rests on two contracts that already exist elsewhere in SAB's design, reused here as the actual contribution guidelines:

- **The module contract** (`modules.md`) — a unique ID, a name and description, typed inputs and outputs, a required tested rollback procedure, and tests. Anyone can build a new module — for patching, provisioning, backups, anything — as long as it follows this shape.
- **The connector contract** (`execution-environment.md`) — anyone can add support for a new execution environment (a new cloud provider, a different remote-management protocol) by implementing the same `connect` / `execute` / `disconnect` / `health_check` interface every connector already has to follow.

Because both of these are already required of the *core* team's own work, there's no double standard for community contributions to clear. Reviewing a submission becomes a checklist question — does it meet the contract? does it have a tested rollback path and real tests? — rather than a judgment call about whether it "fits."

## How contribution actually works, in practice

1. **You build a module or connector** that follows the standard contract — same rules the core SAB modules already follow.
2. **You submit it.** Because the contract is explicit and machine-checkable (does it have all the required fields? does it have a rollback procedure? do the tests pass?), a lot of the review is straightforward rather than subjective.
3. **It becomes discoverable** alongside everything else in the OSML (the Open Source Module Library), available for the AI agent to propose and for other SAs/SEs to use in their own workflows.
4. **Over time, contributions can move up a trust tier.** SAB's planned Marketplace (see Section 5.1 of the design doc) uses three tiers: **Community** (meets the contract, otherwise unreviewed), **Verified** (passed additional review/testing by SAB maintainers), and **Certified** (vendor-backed, with support commitments — this is where the paid, hardened tier of SAB's business model lives). Every tier still has to clear the same base contract; the tiers are about how much additional trust and vetting sits on top of that baseline.

## Where it fits in the bigger picture

```
Module contract + connector contract already exist   ← (modules.md, execution-environment.md)
      ↓
Anyone can build against those same contracts   ← (this is this document)
      ↓
A valid contribution joins the module library
      ↓
The AI agent can now propose it, just like any core module   ← (ai-agent-layer.md)
```

Community contribution isn't a separate system bolted onto SAB — it's the exact same mechanism the core team uses to build modules and connectors in the first place, just opened up to anyone willing to follow the same rules.

## A useful mental model

**The contribution framework is like a standardized shipping container.**

Before standardized containers existed, loading a ship meant handling every single crate, barrel, and sack differently — slow, and it didn't scale. Once every container had to be the same standard shape, any crane, any ship, any port could handle any container, no matter who packed it or what was inside. SAB's module and connector contracts do the same job: because every contribution — whether it's from the core team or a random contributor halfway across the world — has to fit the same standard shape, the rest of the system (the AI agent, the orchestration engine) can handle it exactly the same way, with no special cases.

## Why this design choice matters (not just how it works)

- **It's what lets SAB's module library grow faster than one team could build it alone.** As more real-world workflows and modules get contributed, new workflows increasingly become "arrange already-trusted modules in a new order" rather than building everything from scratch — see `workflows.md` for why that matters.
- **It keeps quality consistent regardless of who wrote something.** A module that meets the contract is trustworthy in the same specific ways (typed inputs/outputs, a tested rollback path) whether it came from the core team or an outside contributor — the safety guarantees don't get watered down just because more people are building.
- **It's designed to make future partnerships easy, not just outside individual contributors.** The same clean, stable contracts that make community contribution possible are also what would let a platform like Ansible, UiPath, or Microsoft's own tooling eventually integrate with SAB — see Section 6 of the design doc for more on that angle.

## Getting familiar with the contribution framework — where to look next

- **`modules.md`** and **`execution-environment.md`** — the two contracts everything here is built on. Read these first if you're thinking about actually contributing something.
- **`SAB_Design_Document_v0.1.2.md`, Section 5** — the technical version of the contribution framework itself, including the third-party integration API.
- **`SAB_Design_Document_v0.1.2.md`, Section 5.1** — the SAB Engine Marketplace and its three trust tiers (Community, Verified, Certified) — where contribution eventually leads as SAB matures.

---

*This document is a plain-language companion to the technical design doc — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with `SAB_Design_Document_v0.1.2.md`, the design doc wins; flag the mismatch so this file can be corrected.*
