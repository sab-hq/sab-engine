# Audit Trail

*A beginner's guide to how SAB keeps a tamper-evident record of everything it does, and how it fits into the bigger picture.*

---

## In one sentence

Every single state change SAB makes gets written down permanently, cryptographically linked to the entry before it — so the full history of what happened can never be quietly altered after the fact, only appended to.

> **Current status:** this is real, working code — verified alongside the state machine itself in `pre-development-checklist.md`, PD-4 and PD-8.

## The problem it solves

Infrastructure automation that touches production servers needs to be trustworthy not just in the moment, but afterward. If something goes wrong — a patch causes an issue, a compliance auditor asks what happened last Tuesday, a rollback needs investigating — the honest answer has to come from a record that couldn't have been quietly edited after the fact, by a bug, by an attacker, or by someone trying to cover up a mistake.

A normal application log doesn't guarantee that. Log files can be edited, rows can be deleted, and there's usually no way to prove after the fact that nothing was changed. For something that recommends and executes changes to real infrastructure, that's not good enough.

## How it actually works

**Every state transition writes a real audit entry — not as an afterthought, but as part of the same operation.** When a `WorkflowRun` moves from one state to another (see `orchestration-engine.md` for the full state machine), the code that performs that transition writes the corresponding `AuditEntry` in the same database operation, not in a separate logging pass that could lag behind, get skipped, or fail independently of the actual change.

**Each entry is hash-linked to the one before it.** Every `AuditEntry` includes a hash that incorporates the previous entry's hash. That means altering or deleting an entry after the fact would break the chain in a detectable way — you can't quietly rewrite history in the middle without every entry after that point failing to match up anymore. This is the same core idea a blockchain uses for tamper-evidence, applied here at a much smaller, practical scale.

**This isn't a bolt-on feature — it's built directly into the mechanism that changes state at all.** There's no code path that changes a `WorkflowRun`'s state without also writing the audit entry, because they're literally the same operation. That matters: a well-intentioned feature that's easy to accidentally bypass isn't actually a guarantee.

## A useful mental model

**Think of it like a ship's log, sealed page by page.** Each page of a ship's log is meant to be signed and sealed before the next one is written, incorporating something from the page before it. If someone tried to tear out a page and rewrite it, the seals on the surrounding pages wouldn't match up anymore — the tampering would be obvious, even if you never noticed the missing page directly. SAB's audit trail works the same way: you don't have to trust that nobody touched the history, because tampering leaves an unmistakable trace.

## Getting familiar with the audit trail — where to look next

- **`orchestration-engine.md`** — the state machine this audit trail is directly built into; every transition described there writes one of these entries.
- **`recommend-and-approve-mode.md`** — approvals and declines are state transitions too, so they're part of this same tamper-evident record.
- **`crash-recovery.md`** — how SAB uses recorded history (including audit entries) to figure out what actually happened to an interrupted run.

---

*This document is a plain-language companion to the technical design doc and `pre-development-checklist.md`/`checklist-02.md` — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with those sources, they win; flag the mismatch so this file can be corrected.*
