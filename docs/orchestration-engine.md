# Orchestration Engine

*A beginner's guide to what the orchestration engine is, and how it fits into the bigger picture.*

---

## In one sentence

The **orchestration engine** is the part of SAB that actually carries out an approved plan — it's the only piece of the system that touches real servers, and its whole job is running the right modules in the right order, tracking exactly what's happening, and automatically cleaning up if something goes wrong.

## The problem it solves

By the time a plan reaches this point, a lot has already happened: the AI agent has proposed a plan (see `recommend-and-approve-mode.md`), and a human has approved it. But approving a plan and actually *executing* it reliably are two very different problems. Real execution against real infrastructure has to survive things like:

- What happens if the process running SAB crashes halfway through a multi-step patch job?
- What happens if step 3 of 5 fails — does the server get left in a half-patched, broken state?
- What happens if you're patching 50 servers at once — does SAB get confused about which one is on which step?

The orchestration engine exists specifically to answer all of that reliably, every time, without a human having to babysit the process.

## What it actually is

Think of the orchestration engine as the "how and when" layer of SAB — it doesn't decide *what* to do (that's the AI agent's job) and it doesn't *contain* the actual work (that's what modules are for, see `modules.md`). It's the reliable machinery in between: it takes an approved plan and works through it, one module at a time, keeping track of exactly where it is.

Using the Windows Server patching workflow as the example again, here's what the orchestration engine is actually doing behind the scenes once you've approved a plan:

1. **It runs `pre-flight-check`**, waits for the result, and records that this step happened and what it found.
2. **If pre-flight passes, it moves on to `stage-patches`** — it won't jump ahead out of order, and it won't skip a step just because it seems obvious.
3. **It runs `apply-patches`**, tracking that this specific module is now "in progress" for this specific server.
4. **It runs `validate`** to confirm the server came back up correctly.
5. **If any step along the way fails**, the engine automatically triggers that module's rollback procedure — nobody has to notice the failure and manually kick off an undo. This happens as part of the engine's job, not as a separate manual process.
6. **Once everything's done (success or failure)**, it writes down exactly what happened — every step, every result — so there's a real record afterward (see Section 4.5 of the design doc for where that record lives).

## Where it fits in the bigger picture

Here's the full chain of custody, with the orchestration engine's specific piece highlighted:

```
You pick a workflow
      ↓
AI agent proposes a plan
      ↓
You approve it   ← (this is recommend-and-approve-mode.md)
      ↓
Orchestration engine runs it   ← (this is this document)
      ↓
Modules actually do the work, one at a time   ← (this is modules.md)
      ↓
Results get recorded
```

The orchestration engine sits right after the human approval gate and right before the individual modules — it's the reliable "runner" that turns an approved plan into a real sequence of actions, without ever deciding on its own what those actions should be.

## A useful mental model

**The orchestration engine is like a very literal, very reliable stage manager for a play.**

A stage manager doesn't write the script (that's the AI agent's job) and doesn't act in the scenes (that's the modules' job) — but nothing happens on stage without going through them. They call each cue at exactly the right moment, in exactly the right order, and if an actor misses their line, the stage manager is the one who knows exactly how to get the scene back on track. They also keep a detailed record of every performance, so if something goes wrong on opening night, there's a clear account of exactly what happened and when.

## Why this design choice matters (not just how it works)

A couple of specific engineering choices about the orchestration engine are worth knowing, since they're not obvious just from watching it run:

- **It doesn't keep everything in its own memory.** Every workflow run's status gets written to a real database (PostgreSQL) as it happens, rather than only living inside the running program. That's *why* a crash mid-run isn't a disaster — the engine can pick back up exactly where it left off, because "where it left off" was never only in memory to begin with.
- **It's built to run many workflows at once, not just one.** Because state is tracked externally rather than inside one running process, patching 50 servers at the same time isn't fundamentally different from patching one — it's just more workers pulling from the same shared, reliable record of what's happening.
- **Rollback isn't a manual afterthought.** Because every module the engine is allowed to run already comes with a tested undo procedure (see `modules.md`), the engine can trigger that rollback automatically the moment something fails — it doesn't need a human to notice the problem first.

## Getting familiar with the orchestration engine — where to look next

- **`recommend-and-approve-mode.md`** — what happens right *before* the orchestration engine gets involved; read this if you want to understand where an approved plan actually comes from.
- **`modules.md`** — the individual steps the orchestration engine calls, one at a time, as it works through a workflow.
- **`workflows.md`** — the recipe the orchestration engine is following, and how it's structured.
- **`SAB_Design_Document_v0.1.2.md`, Section 4.1** — the technical version of everything above, including the exact state machine (`Requested → PlanDrafted → PendingApproval → Approved → Executing → Completed/Failed → RolledBack`) a workflow run actually moves through.

---

*This document is a plain-language companion to the technical design doc — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with `SAB_Design_Document_v0.1.2.md`, the design doc wins; flag the mismatch so this file can be corrected.*
