# Crash Recovery

*A beginner's guide to how SAB handles an interrupted workflow, and how it fits into the bigger picture.*

---

## In one sentence

When something goes wrong mid-workflow — most likely the process running SAB itself crashing, not the server being patched — SAB doesn't guess what to do next. It reconnects to the target, checks what's actually true, and only then decides whether to continue or roll back.

> **Current status:** this is a fully decided design, not yet built — see `checklist-02.md`, PD-33 (the overall principle), PD-35 (how the health re-check works), and PD-37 (a real gap this surfaced in `pre-flight-check`). Nothing described here is running code yet. This document exists so the reasoning behind the design is written down clearly before anyone starts implementing it, not scattered across a conversation history.

## The problem it solves

Say SAB is halfway through patching a server — `pre-flight-check` passed, `stage-patches` finished, `apply-patches` is partway through installing three patches — and the worker process running SAB crashes. Maybe the machine rebooted, maybe the process just died. Either way, when SAB comes back, it has to answer a real question: **what happened to that workflow run, and what should happen next?**

There are two obviously wrong answers, and it's worth naming both:

- **Reflexively rolling everything back**, on the theory that "the run didn't finish cleanly, so undo it." This conflates two completely different things — *the controller crashed* and *the server is broken* — which usually aren't the same problem at all. The target server is probably fine; it's just sitting there mid-patch-cycle. Triggering a rollback (which is itself a real operation, with its own risk) against a server that's actually healthy is often *more* disruptive than just leaving it alone.
- **Blindly resuming** where it left off, assuming everything's fine just because the crash was on SAB's side, not the server's. This skips the one thing that actually matters: confirming the target is really in the state you think it's in before touching it again.

Neither of these is how a real systems engineer would handle it.

## What a real SA/SE actually does — and what SAB copies

If a systems engineer's session drops mid-change, they don't assume anything either way. They **reconnect, check the box's actual current state, and only then decide** what to do — continue the change, or back it out. That's the entire design principle SAB follows here. Not a new invention — just modeling real practice, the same way SAB's recommend-and-approve mode and per-module rollback requirements already do.

## How it actually works

**1. Detecting an interrupted run.**
Every workflow run's state lives in PostgreSQL, not just in the memory of whatever process is running it (see `orchestration-engine.md`) — so a crash doesn't erase the fact that a run was in progress. `WorkflowRunClaimService` already knows how to atomically claim eligible work; a run that's been sitting in `Executing` past a reasonable lease window is the signal that something died mid-run.

**2. Reconnect and check — before deciding anything.**
This happens in two tiers, matching how an SA actually reconnects to a machine they lost contact with:
- **First, just reachability.** Can SAB even get back into the target at all? This reuses the connector's existing health check — a cheap, fast probe, no assumptions yet about what state the server is actually in.
- **Then, if reachable, a real assessment.** SAB reuses the `validate` module (see `modules.md`) directly, rather than inventing a separate, lighter-weight check just for this situation — one trusted checklist, not two versions of "is this server okay" to keep in sync.

**3. Knowing which step to check, without guessing.**
SAB doesn't need to remember or re-derive what it was doing — the original plan already recorded exactly which patches this run intended to apply. SAB just reads that back, the same way an SA would check their own change ticket instead of trying to recall from memory what they were mid-way through.

This also answers a smaller question cleanly: if nothing was ever recorded as having started the risky step (`apply-patches`), there's nothing to re-check — SAB just resumes from wherever it actually left off. No point re-verifying a server that was never touched.

**4. Branch on what's actually true — not on the mere fact of an interruption.**
- **Target healthy** → continue with the remaining steps. This turns out to be safe by design: the patching modules already check what's actually needed before acting (see `modules.md`'s description of `stage-patches` and `apply-patches`), so re-running a step that already partly succeeded doesn't redo work or cause harm.
- **Target unhealthy** → trigger a rollback — but only for what this specific run actually changed, never a blanket "undo everything since the start." See `rollback-scoping.md` for exactly how that gets decided.

## A useful mental model

**This is exactly what a good on-call engineer does when they get paged about something they were already working on.** They don't assume the worst and start undoing things. They don't assume everything's fine either. They log back in, look at what's actually happening right now, and make a decision based on that — not on how the interruption happened.

## Getting familiar with crash recovery — where to look next

- **`rollback-scoping.md`** — the specific mechanics of how SAB decides exactly what to undo, once it's decided a rollback is actually needed.
- **`orchestration-engine.md`** — the broader engine this design lives inside; crash recovery is one part of what it needs to do reliably.
- **`modules.md`** — what `pre-flight-check`, `apply-patches`, and `validate` actually check, since this design leans on all three.

---

*This document is a plain-language companion to real, tracked design decisions — not the tracking mechanism itself. `checklist-02.md` (PD-33, PD-35, PD-37) is where status, exact reasoning, and any changes to this design actually live; this file should be updated to match if that design ever changes.*
