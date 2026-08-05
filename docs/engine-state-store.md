# Engine State Store

*A beginner's guide to what the Engine State Store is, and how it fits into the bigger picture.*

---

## In one sentence

The **Engine State Store** is SAB's memory — the record of what's been run, against what, and with what result — so the AI agent, the orchestration engine, and the humans using SAB are all working from the same real history instead of starting fresh every time.

## The problem it solves

Imagine SAB had no memory at all. Every single time you asked it to patch a server, the AI agent would be starting completely blind — no idea whether this exact workflow has run cleanly against similar servers a hundred times before, or whether it's caused problems in the past. You'd have to trust every single run equally, with no way to say "this one has a real track record behind it" versus "this one's never been tried before."

That's a bad foundation for the kind of gradual, earned trust SAB is built around (see `recommend-and-approve-mode.md`). The Engine State Store exists specifically to give SAB real memory to work from, so trust can be based on an actual track record instead of a leap of faith every time.

## What it actually is

The Engine State Store is where SAB's own history lives — everything that's happened during past workflow runs, kept in one place that different parts of the system can read from and write to.

**What it likely keeps track of:**
- **Run history** — every workflow that's ever run: what was run, against which target, what happened, whether a rollback was needed
- **Target system state** — what SAB currently knows about each managed server (patch level, when it last ran, anything relevant) so the AI agent isn't guessing
- **Learnings and patterns** — things like "this specific patch has caused a rollback on this class of server before" — the kind of institutional knowledge that normally only lives in one experienced person's head
- **Human notes** — a place for an SA/SE to add their own context about a system or a past incident, the kind of thing an automated log can't infer on its own

## Where it fits in the bigger picture

```
Orchestration engine runs a workflow   ← (orchestration-engine.md)
      ↓
The result gets written into the Engine State Store   ← (this is this document)
      ↓
Next time, the AI agent reads that history back out   ← (ai-agent-layer.md)
      ↓
...before proposing a new plan
```

This is the piece that closes the loop. Without it, every run would be a one-off event that disappears the moment it's over. With it, every run becomes part of a growing, queryable history that makes the *next* run smarter than the last one.

## A useful mental model

**The Engine State Store is like a patient's medical chart.**

A doctor doesn't treat every visit as if they're meeting the patient for the first time — they check the chart first: what's been tried before, what worked, what didn't, any relevant history. That context changes the decision. The Engine State Store plays the same role for SAB: before the AI agent proposes anything, it can check "what happened the last several times this workflow ran against this server, or one like it" — and that real history shapes a better, more specific proposal than starting from zero every time.

## Why this design choice matters (not just how it works)

- **It's what makes "earned trust" possible instead of just a nice phrase.** SAB's longer-term goal is to gradually allow more autonomy for workflows that have a long, clean track record (see the "Autonomy levels" section of `ai-agent-layer.md`) — but that's only possible if there's a real record to point to. A system with no memory has to be trusted blindly every single time; one that can say "this exact workflow has succeeded cleanly forty times" gives both the AI agent and the human approver something real to base a decision on.
- **It's scoped narrowly on purpose.** This store only holds what `sab-engine` itself needs — its own run history and target state. It's a smaller, more focused thing than SAB's separate knowledge-and-documentation product (`sab-kb`), which is built for a different job entirely: capturing broader tribal knowledge from things like email and Teams conversations, across a whole MSP's client base. The two are kept intentionally separate for now.
- **Humans can add to it too, not just the system.** The "human notes" piece matters because there's real context a person knows that no automated log ever could — like "this particular server is flaky during business hours, don't ask me why." Giving that context a real place to live, right alongside the automated history, is part of what makes the record actually useful.

## Getting familiar with the Engine State Store — where to look next

- **`ai-agent-layer.md`** — the main reader of this history; see how it uses past runs to shape a new proposal.
- **`orchestration-engine.md`** — the main writer of this history, recording what happens as each workflow run plays out.
- **`SAB_Design_Document_v0.1.2.md`, Section 4.5** — the technical version of everything above, including the actual data model (`workflow_runs`, `plans`, `approvals`, `execution_results`, `target_state`, `notes`) and how the AI agent's queries against it work.

---

*This document is a plain-language companion to the technical design doc — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with `SAB_Design_Document_v0.1.2.md`, the design doc wins; flag the mismatch so this file can be corrected.*
