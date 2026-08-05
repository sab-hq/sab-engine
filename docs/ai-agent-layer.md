# AI Agent Layer

*A beginner's guide to what SAB's AI agent actually does, and how it fits into the bigger picture.*

---

## In one sentence

The **AI agent layer** is the part of SAB that looks at a real situation, figures out exactly what should happen, and proposes a plan with its reasoning — but it never touches a real server itself; that's always someone else's job (see `recommend-and-approve-mode.md` and `orchestration-engine.md`).

## The problem it solves

A workflow (see `workflows.md`) is a generic recipe — "patch this server" — but no two servers are in exactly the same situation. One might have an unusually long uptime. One might have failed a patch attempt before. One might be sitting right up against a maintenance window deadline. A recipe that's identical no matter the situation isn't actually very smart.

Someone needs to look at the specific server, the specific history, and the specific constraints, and work out the *specific* plan that makes sense right now — which patches, in what order, with what settings, and whether anything about this particular run looks unusual enough to flag. That's the AI agent's whole job.

## What it actually is

The AI agent is the "what and why" layer of SAB — it doesn't execute anything directly. It reads the situation, thinks it through, and hands off a proposal. Nothing more, nothing less.

**What it looks at before proposing anything:**
- The workflow you picked (the recipe itself)
- The current state of the actual target system — patch level, last patch date, anything unusual going on
- Real history — has this workflow run against this server (or similar servers) before, and how did that go? (This comes from SAB's memory — see Section 4.5 of the design doc.)
- Constraints — maintenance windows, blackout periods, anything else that matters

**What it hands back:**
- Which modules to run, in what order, with what specific settings
- A plain-language explanation of *why* — not just "here's the plan," but "here's the reasoning," since the person approving it needs to actually understand it, not just click a button blind
- A flag if anything about this particular run looks unusual compared to past runs

## Where it fits in the bigger picture

```
You pick a workflow
      ↓
AI agent looks at the situation and proposes a plan   ← (this is this document)
      ↓
You approve it   ← (this is recommend-and-approve-mode.md)
      ↓
Orchestration engine runs it   ← (this is orchestration-engine.md)
      ↓
Modules actually do the work, one at a time   ← (this is modules.md)
```

The AI agent sits right at the front of this whole chain — it's the thinking part. Everything after it is either a human decision (approve or decline) or reliable, predictable execution (the engine and the modules). The agent's proposal never skips ahead to actually happening — see `recommend-and-approve-mode.md` for exactly why that boundary exists and why it matters.

## A useful mental model

**The AI agent is like a resident who writes up a treatment plan for the attending physician to sign off on.**

A resident can be genuinely skilled — good at reading the chart, spotting something unusual, working out a solid plan — but there's still a senior physician who reviews the reasoning and signs off before anything actually happens to the patient. That's not a lack of trust in the resident's ability; it's just how you build a system where mistakes get caught before they cause real harm, no matter how good any individual proposal usually is.

## Why this design choice matters (not just how it works)

- **The agent can never call a module directly.** This is a hard boundary built into the architecture, not a policy someone has to remember to follow. No matter what the agent decides, the most it can ever do is put a proposal in front of a human — it structurally cannot skip straight to action.
- **Its plan is a real, structured object — not just a paragraph of text.** This matters because it's what lets the orchestration engine double-check the plan automatically (does every proposed module actually have a tested rollback path? Section 4.1/4.2's rule) before a human even sees it, and it's what makes a human's "approve" click mean something specific and inspectable, not a leap of faith.
- **It gets smarter about a specific server over time, based on real history — not vibes.** Because it can look at SAB's memory of past runs (Section 4.5), a workflow that's run cleanly against a class of server forty times shows up differently than one being tried somewhere completely new. That's real earned context, not guesswork.

## Getting familiar with the AI agent layer — where to look next

- **`workflows.md`** — the recipe the agent is working from when it proposes a plan.
- **`recommend-and-approve-mode.md`** — what happens immediately after the agent proposes something, and why that step can never be skipped.
- **`orchestration-engine.md`** — what actually happens once a human approves the agent's plan.
- **`SAB_Design_Document_v0.1.2.md`, Section 4.3** — the technical version of everything above, including how the agent's reasoning is structured (via Microsoft's Semantic Kernel) and what "unusual" means as a first, concrete definition.

---

*This document is a plain-language companion to the technical design doc — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with `SAB_Design_Document_v0.1.2.md`, the design doc wins; flag the mismatch so this file can be corrected.*
