# Rollback Scoping

*A beginner's guide to how SAB decides exactly what to undo, and how it fits into the bigger picture.*

---

## In one sentence

When SAB rolls back a failed patching run, it undoes exactly what *this run* actually changed — never patches that were already there before SAB touched the server, and never patches it merely planned to apply but never got to.

> **Current status:** this is a fully decided design, not yet built — see `checklist-02.md`, PD-36 (the scoping formula itself) and PD-37 (a real gap this surfaced in `pre-flight-check`, which this design depends on). Nothing described here is running code yet.

## The problem it solves

Say a rollback gets triggered — `crash-recovery.md` covers how SAB decides *that* a rollback is needed. This document is about a different, easy-to-get-wrong question: **rollback exactly what?**

The naive answer — "undo everything in the original plan" — is a real mistake, for two separate reasons:

1. **The plan might include patches that were already installed *before* this run even started.** If a server already had a patch on it, unrelated to anything SAB did, and that same patch happens to appear in this run's plan (say, it's part of a standard patch set applied to a whole fleet), rolling it back on failure would mean *removing something SAB never actually touched*. That's not fixing a mistake — that's making one.
2. **The plan might include patches the run never actually got to.** If `apply-patches` failed on its second patch out of three, the third one was never attempted at all — there's nothing to undo there.

A real systems engineer never undoes state they didn't create. SAB's rollback scoping follows the same rule.

## How it actually works

**The core idea: don't trust a module's own self-report — verify against the target directly.**

It might seem natural to just ask `apply-patches` "which patches did you successfully install?" and use that list. But a real SA doesn't scope a rollback based on what an installer *said* it did — they check what the system actually shows. SAB does the same: it re-queries the target server directly (the same check `validate` already does, see `modules.md`), rather than trusting a module's self-reported result.

**The actual formula:**

> **Rollback set** = (patches this run planned to apply) **∩** (patches currently installed, confirmed by re-checking the target) **−** (patches that were already installed *before* this run started)

Each piece matters. Here's a concrete example to make it real:

- Before this run, the server already had `KB1111` installed — unrelated history, nothing to do with this workflow.
- This run's plan includes `KB1111`, `KB2222`, and `KB3333`.
- `apply-patches` successfully installs `KB2222`, then fails before reaching `KB3333`.
- SAB re-checks what's actually installed: `KB1111` and `KB2222`. (`KB3333` was never attempted, so it's correctly *not* in this list — nothing to undo there.)
- **Planned ∩ currently installed** = `{KB1111, KB2222, KB3333}` ∩ `{KB1111, KB2222}` = `{KB1111, KB2222}`
- **Minus already-installed-before** = `{KB1111, KB2222}` − `{KB1111}` = **`{KB2222}`**

The rollback set is just `KB2222` — exactly what this run actually did, nothing more and nothing less. `KB1111` is protected because it predates the run; `KB3333` is excluded because it was never actually applied in the first place.

## Why this needed a real fix elsewhere in the system

That formula's third piece — "already installed before this run started" — needs a snapshot of the server's state *before* anything happened. Nothing currently captures that. `pre-flight-check` (see `modules.md`) checks disk space, pending reboot, and the Windows Update service — but it doesn't currently record which patches are already installed.

This is a real, concrete gap in already-designed logic, not a hypothetical — closing it (extending `pre-flight-check` to snapshot installed patches at the very start of a run) is tracked as its own item, **PD-37**, specifically because the rollback-scoping formula above genuinely doesn't work correctly without it.

## A useful mental model

**This is exactly the discipline of a careful SA doing manual rollback work: check the change log for what *this specific change* touched, verify the current state directly rather than trusting a tool's exit message, and never touch anything that was there before you started.**

## Getting familiar with rollback scoping — where to look next

- **`crash-recovery.md`** — how SAB decides *whether* a rollback is needed in the first place; this document only covers *what* gets undone once that decision is made.
- **`modules.md`** — what `pre-flight-check`, `apply-patches`, and `validate` actually do, since this design leans on all three, including a real change (PD-37) `pre-flight-check` still needs.

---

*This document is a plain-language companion to real, tracked design decisions — not the tracking mechanism itself. `checklist-02.md` (PD-36, PD-37) is where status, exact reasoning, and any changes to this design actually live; this file should be updated to match if that design ever changes.*
