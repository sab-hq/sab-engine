# Workflows

*A beginner's guide to what a workflow is in SAB, and how it fits into the bigger picture.*

---

## In one sentence

A **workflow** is a saved, repeatable recipe for getting a real sysadmin job done — the ordered set of steps SAB runs, in the right order, to accomplish something like "patch this server," the same reliable way every time.

> **Current status:** all four ingredients this recipe needs are done and tested as of `pre-development-checklist.md`, PD-17 — `pre-flight-check`, `stage-patches`, `apply-patches`, and `validate` all exist as real, working modules. The actual "Patch Windows Server" workflow definition described below — stringing those four together in order — is next (PD-21), not yet written.

## The problem workflows solve

If you've ever patched a Windows Server by hand, you already know the "recipe" in your head, even if you've never written it down:

1. Check the server is actually healthy before you touch it
2. Stage the patches
3. Apply them
4. Confirm the server came back up and looks right
5. If something's wrong, undo it

That mental checklist is real, valuable knowledge — but it usually lives only in one person's head, gets done slightly differently every time, and disappears the moment that person is out sick, busy, or leaves the team. SAB's whole starting premise (see `SAB_Design_Document_v0.1.2.md`, Section 1) is that this kind of tribal knowledge should be captured, standardized, and made repeatable instead. A **workflow** is what that looks like once it's captured.

## What a workflow actually is

A workflow is the *recipe*. It doesn't do any of the actual work itself — it's an ordered list that says "run this step, then this step, then this step," where each step is a **module** (see `modules.md` for the full picture on those). Think of it like a recipe card: the recipe doesn't chop the vegetables or preheat the oven, it just tells you the order to do those things in.

Just like modules, **every workflow has its own unique ID** — no two workflows share one, even if their names sound similar. That's what lets SAB reliably say "run *this exact recipe*" whether the trigger is a person clicking a button, a schedule, or an external system (see Section 6 of the design doc) — there's never any ambiguity about which recipe was actually meant.

For SAB's first real workflow — patching a Windows Server — the recipe looks roughly like this:

```
Workflow: "Patch Windows Server"
  1. pre-flight-check   → is this server healthy enough to patch right now?
  2. stage-patches       → download/prepare the patches
  3. apply-patches       → actually install them
  4. validate             → confirm the server is healthy after patching
  5. rollback (if needed) → if step 4 fails, undo everything
```

Each of those steps — `pre-flight-check`, `stage-patches`, and so on — is a separate, reusable **module**. The workflow's whole job is just deciding what order they run in and what information passes between them. That separation matters: it means a module like `pre-flight-check` can potentially be reused across many different workflows, not just this one.

## Where a workflow fits in the bigger picture

Here's how a workflow actually gets run, step by step, in plain language (the technical version of this lives in `SAB_Design_Document_v0.1.2.md`, Section 3):

1. **You pick the workflow.** You (an SA/SE) decide "I need to patch this server" and select the matching workflow from SAB's library — you don't write it from scratch each time.
2. **SAB's AI agent proposes a plan.** It looks at the actual server you picked and figures out exactly how this workflow should run against it — which patches, what maintenance window, anything unusual about this particular server — and explains its reasoning in plain language.
3. **You approve it.** Nothing runs against a real server until a human says yes. This is SAB's core rule, not a suggestion — see the "recommend-and-approve" principle in Section 2 of the design doc.
4. **SAB runs the workflow.** It works through the modules in order — pre-flight check, stage, apply, validate — tracking exactly where it is at every moment.
5. **If something goes wrong, it rolls back.** Every module in a workflow is required to have a tested "undo" procedure. If a step fails, SAB doesn't just stop and leave the server in a broken state — it automatically reverses what it did.
6. **The result gets remembered.** What happened, whether it succeeded, whether a rollback was needed — all of that gets written down so the next time this workflow runs (on this server or a similar one), SAB has real history to draw on instead of guessing blind.

## A useful mental model

**Workflow = recipe. Module = individual step/ingredient.**

You don't need to understand how an oven works to follow a recipe — you just need to trust that "bake at 350°F for 20 minutes" is a reliable instruction. Same idea here: a workflow doesn't need to know *how* `apply-patches` actually talks to Windows Update under the hood. It just needs to know "run `apply-patches` here, with these settings, then move to the next step."

This separation is also *why* SAB can grow safely over time. Adding a brand new workflow (say, for restarting a service, or verifying a backup) doesn't require touching the modules that already exist and are already trusted — you're just writing a new recipe using ingredients that are already proven.

## Why this design choice matters (not just how it works)

A few things about workflows are deliberate, not accidental:

- **Workflows don't skip the human approval step, ever, at launch.** Even a workflow that's run successfully a hundred times still stops and asks a person to confirm before it touches production. Full autonomy is a real long-term goal, but it's *earned* per-workflow over time based on an actual track record — not assumed on day one. (See Section 4.3, "Autonomy levels.")
- **A workflow is only as trustworthy as its weakest module.** Since every module in it needs a tested rollback path, a workflow inherits that safety net automatically — you don't have to think about "what if this specific step fails" separately for every workflow you write, because the requirement is baked in at the module level.
- **Workflows are meant to be shared and reused**, not written once and forgotten. As SAB's module library grows, new workflows increasingly become "arrange existing, already-trusted modules in a new order" rather than writing everything from scratch — this is part of what makes the whole system scale.

## Getting familiar with workflows — where to look next

- **`modules.md`** — the individual building blocks a workflow strings together. Read this next if you want the other half of the picture.
- **`SAB_Design_Document_v0.1.2.md`, Section 3** — the technical version of the end-to-end flow described above, including the exact system states a workflow run moves through.
- **`SAB_Design_Document_v0.1.2.md`, Section 4.2** — how workflows are actually defined, stored, and versioned as files, once you're ready to go past the concept and into the file format itself.

---

*This document is a plain-language companion to the technical design doc — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with `SAB_Design_Document_v0.1.2.md`, the design doc wins; flag the mismatch so this file can be corrected.*
