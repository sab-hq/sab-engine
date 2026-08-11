# Modules

*A beginner's guide to what a module is in SAB, and how it fits into the bigger picture.*

---

## In one sentence

A **module** is a single, reliable, reusable unit of work — one specific action SAB knows how to do (like "check if a server is healthy" or "apply a patch"), built so it can be trusted, reused, and safely undone if something goes wrong.

> **Current status:** the manifest format this doc describes is now real, working code as of PD-12 in `pre-development-checklist.md` — `SabEngine.Modules` can read and validate a real module manifest, rejecting one that's missing a required field (like a rollback procedure) before it ever reaches the rest of the system. Three of the four real modules are done: `pre-flight-check` (PD-14), `stage-patches` (PD-15), and `apply-patches` (PD-16), all written, tested, and confirmed valid against that parser — `validate` is the last one still ahead (PD-17). `apply-patches` is also the first module with a genuine, non-trivial rollback (actually uninstalling a patch) rather than the justified no-op the two read/download-only modules got — see the table in "A concrete example" below for how that distinction plays out across all four modules. A CI pipeline in the `sab-modules` repo now validates every module manifest automatically on every push (PD-13). What's not built yet: a real catalog loader that reads every manifest from the OSML automatically and feeds the AI agent's available-module list.

## The problem modules solve

Think about all the individual little scripts a sysadmin ends up writing over the years — one to check disk space, one to restart a service, one to apply patches. They usually don't look anything alike: different naming, different error handling, some have a way to undo their changes and some really don't, some were written by someone who left the team two years ago and nobody's totally sure what they do anymore.

A **module** is SAB's answer to that mess: every single one, no matter who wrote it or what it does, follows the exact same shape. That consistency is what lets SAB's AI agent (and other sysadmins) actually trust and reuse a module without having to read its source code first.

## What a module actually is

A module is one specific, well-defined action — deliberately "dumb" in the sense that it doesn't make decisions or know about the bigger picture. It just does its one job reliably and reports back what happened. All the "which order do these run in, and why" thinking happens one level up, in the **workflow** (see `workflows.md`).

Every module, regardless of what it does, is required to have the same basic parts:

- **A unique ID** — no two modules, ever, share the same ID. This is what lets a workflow say "run *this exact module*" without any ambiguity, even if two different modules happen to have very similar names.
- **A name and description** — what it is, in plain terms
- **Inputs** — what information it needs to do its job (e.g. "which patches to apply")
- **Outputs** — what it reports back when it's done (e.g. "succeeded" or "failed," plus any relevant details)
- **A rollback procedure** — a tested way to *undo* what it did, if something later goes wrong
- **Tests** — proof that it actually works, ideally checked in a safe lab environment before it's ever trusted against a real production server

That **rollback procedure** requirement is the one non-negotiable part when it comes to safety. The **unique ID** requirement is the one non-negotiable part when it comes to just being able to find and trust the right module in the first place — without it, there'd be no reliable way for a workflow, the AI agent, or the orchestration engine to say "I mean *that specific module*, not something that merely looks like it." If a module can't be safely undone, it's not allowed to be part of a workflow that touches production — full stop. This is SAB's core promise, and it's enforced automatically rather than left to a human to remember (see Section 2 of `SAB_Design_Document_v0.1.2.md`).

## A concrete example

Here's what the patching workflow from `workflows.md` looks like broken down into its actual modules:

| Module | What it does | What its rollback looks like |
|---|---|---|
| `pre-flight-check` | Confirms the server is healthy enough to safely patch right now | N/A — it only *checks*, it doesn't change anything |
| `stage-patches` | Downloads and prepares the patches | N/A — downloading doesn't change the server's running behavior, so there's nothing that urgently needs undoing |
| `apply-patches` | Actually installs the patches | Uninstall the specific patches that were applied — a real, tested rollback, not a no-op |
| `validate` | Confirms the server came back up correctly after patching | N/A — it only *checks*, it doesn't change anything |

Notice that `pre-flight-check` and `validate` don't need a "real" rollback, because they never change anything — they just look and report. That's fine; the rollback requirement is about *reversibility of change*, not about every module needing an undo button for the sake of it.

## Where modules fit in the bigger picture

A module never runs on its own initiative — here's the chain of custody, in plain language:

1. **A workflow decides a module needs to run**, and in what order relative to the other modules in that recipe.
2. **SAB's AI agent proposes running it** as part of a larger plan, with specific inputs filled in for this particular situation (e.g. *which* patches, for *this* specific server).
3. **A human approves the plan** before anything actually executes — the module itself never gets to skip this step.
4. **The orchestration engine calls the module**, passing in its inputs, and gets back its outputs.
5. **If the module fails partway**, the engine automatically triggers that module's rollback procedure — nobody has to remember to do this by hand.
6. **What happened gets remembered** (Section 4.5 of the design doc), so the next time this module runs, there's real history behind it instead of a blank slate.

## A useful mental model

**Module = one Lego brick. Workflow = the instructions for what to build with them.**

A single Lego brick doesn't know or care what it's going to be part of — it just needs to be a reliable, standard shape that snaps together predictably with other bricks. That's exactly what a module is: a small, dependable, standard-shaped unit that a workflow can snap into place wherever it's needed.

This is also why building a brand-new workflow gets *easier* over time, not harder — as the module library grows, more and more new workflows are just "arrange existing, already-tested bricks in a new order," rather than building everything from scratch every time.

## Why this design choice matters (not just how it works)

- **Modules are what makes rollback automatic, not something you have to remember.** Because the rollback procedure is a required part of the module itself, there's no situation where SAB runs something against production without already knowing exactly how to undo it if needed.
- **Modules are what makes SAB extensible.** Anyone — including community contributors, once SAB is open source — can build a new module as long as it follows this same shape (metadata, inputs, outputs, rollback, tests). That consistent "contract" is what lets the module library grow safely without a core team having to hand-review every possible way a script could be written.
- **Modules can be written in whatever language actually fits the job** — PowerShell and Bash to start, with infrastructure-as-code (IaC) support coming later. The module *contract* is what's standardized, not the underlying scripting language.

## Getting familiar with modules — where to look next

- **`workflows.md`** — how individual modules get combined into a real, ordered recipe for getting something done. Read this if you haven't already, for the other half of the picture.
- **`SAB_Design_Document_v0.1.2.md`, Section 4.2** — the technical version of everything above, including the actual metadata file format (a YAML example) a real module manifest looks like.
- **`SAB_Design_Document_v0.1.2.md`, Section 5** — how the module contract becomes the foundation for community contribution once SAB's module library is open for outside developers to build against.

---

*This document is a plain-language companion to the technical design doc — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with `SAB_Design_Document_v0.1.2.md`, the design doc wins; flag the mismatch so this file can be corrected.*
