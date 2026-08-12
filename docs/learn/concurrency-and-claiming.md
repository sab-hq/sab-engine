# Concurrency and Claiming

*A beginner's guide to how SAB safely runs multiple workers at once, and how it fits into the bigger picture.*

---

## In one sentence

Multiple SAB workers can safely pull work from the same queue at the same time, because "claiming" a run is a single atomic operation — two workers can never accidentally grab the same job, and a worker that crashes mid-run doesn't permanently block that run from ever being picked up again.

> **Current status:** this is real, working code — `WorkflowRunClaimService`, verified in `pre-development-checklist.md`, PD-5.

## The problem it solves

Patching one server at a time, sequentially, doesn't scale. A real deployment might need to patch 50 servers, and waiting for each one to finish before starting the next would be needlessly slow. The obvious fix is having multiple workers pull work from the same queue in parallel — but that introduces a real, classic problem: **what stops two workers from grabbing the same run at the same time?**

A naive approach — "check if a run is available, then grab it" — has a race condition built in. Two workers could both check at nearly the same moment, both see the same run as available, and both start working on it. For something that's actually changing infrastructure, that's a real problem, not a theoretical one — the same patch could get attempted twice, or two workers could interfere with each other on the same target.

There's a second, related problem: what happens if a worker crashes while it's holding a claim on a run? If nothing ever notices, that run is stuck forever — claimed by a worker that no longer exists, with the next state for it never determined.

## How it actually works

**Claiming a run is one atomic database operation, not a "check then grab" sequence.** `WorkflowRunClaimService.TryClaimNextAsync` uses a real Postgres `UPDATE ... WHERE` that finds and claims the oldest eligible run in a single step. There's no gap between "checking if it's available" and "marking it claimed" for a race condition to slip into — the database itself guarantees only one caller can win.

**This actually surfaced a real limitation in how the system was being tested, not just built.** Early testing used EF Core's InMemory database provider, which turned out to be unable to correctly simulate this specific kind of atomic update at all. That's not a small detail — it meant those tests needed to run against a real, disposable Postgres database instead to mean anything, which is the standing reason Docker needs to be running locally for the full test suite to pass.

**A claim doesn't last forever.** Each claim comes with a lease — if the worker holding it doesn't finish (or renew it) within a reasonable window, the claim expires, and the run becomes eligible to be picked up again by a different worker. This is what stops a crashed worker from permanently stranding a run in limbo.

**Only certain states are actually claimable.** A run has to be in a state where picking it up makes sense — `Requested`, `Declined`, `Approved`, or `Failed` — not, say, one that's already mid-execution by someone else. (Notably, `Executing` itself is *not* currently one of the claimable states — that's a real, specific gap described in `crash-recovery.md`, since it's exactly what stands between today's code and true crash recovery for a run that dies partway through.)

## A useful mental model

**Think of it like a deli counter's take-a-number system, not a show of hands.** If everyone in line just called out "I'll take the next customer!" at the same time, two people could genuinely believe they were serving the same customer. A physical ticket dispenser makes that impossible — pulling a number is a single, indivisible action, so nobody can ever end up holding the same number as someone else. And if a customer walks away without ever being called, their number eventually gets skipped rather than jamming up the line forever.

## Getting familiar with concurrency and claiming — where to look next

- **`orchestration-engine.md`** — the broader state machine this claim/lease pattern operates on top of.
- **`crash-recovery.md`** — the real gap this surfaces (claiming doesn't currently cover a run stuck in `Executing`), and why that matters for handling an interrupted run correctly.

---

*This document is a plain-language companion to the technical design doc and `pre-development-checklist.md`/`checklist-02.md` — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with those sources, they win; flag the mismatch so this file can be corrected.*
