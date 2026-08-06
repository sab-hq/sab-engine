# Phase 1: Windows Server Patching Proof of Concept

*What this phase is, what's in scope, and how you'll know it's actually done.*

---

## In one sentence

Phase 1 proves SAB's whole architecture works end-to-end on the narrowest possible slice: reliably patching one Windows Server, with a human approving the plan and a rollback path that's actually been tested, not just documented.

## Why this phase exists

Every idea in `sab-engine`'s design — modules, workflows, recommend-and-approve, the orchestration engine, rollback — is still just a design until something real runs through all of it at once. Phase 1 isn't about building a lot; it's about building the *smallest complete loop* that proves the architecture holds together, using a single, well-understood, high-value use case: Windows Server patching. See `../design/SAB_Design_Document_v0.1.2.md`, Section 1, for why patching specifically was picked as the starting point.

## What's in scope

- **Core orchestration engine** — sequencing, state tracking, rollback triggering (Section 4.1). Minimum viable version, not the full long-term design.
- **A small set of patching modules** — `pre-flight-check`, `stage-patches`, `apply-patches`, `validate`, plus tested rollback procedures (Section 4.2).
- **The AI agent layer, in recommend-and-approve mode only** — no autonomy stretch goals at this phase (Section 4.3).
- **The WinRM connector** — on-prem Windows only, no other execution environments yet (Section 4.4).
- **A minimal Engine State Store (ESS)** — enough to log run history and feed the agent's recommendations, not the full shared-knowledge vision (Section 4.5).
- **The WSUS-read connector** — the first partnership-oriented integration proof point (Section 6).

## What's explicitly out of scope

- Cloud or hybrid execution environments (that's Phase 3)
- Any workflow other than Windows Server patching (that's Phase 2)
- Approve-by-exception or any other autonomy beyond recommend-and-approve
- The Marketplace, community contribution at scale, or public open source launch (Phase 4)
- A full, general-purpose knowledge base — the ESS here is deliberately narrow (see `../learn/ess-vs-sab-kb.md` for why that's a different thing entirely from `sab-kb`)

## Exit criteria

SAB can reliably patch a lab/low-stakes Windows Server end-to-end, with a human approving each run and a tested rollback path proven to actually work — not just documented. This is a real bar, not a checkbox: "documented" and "proven" are treated as different things on purpose (Section 9).

## Current status

Real development on this phase started August 6, 2026. `../pre-development-checklist.md` is the authoritative, item-by-item tracker (PD-1 through PD-29) — this doc explains *what* the phase is, that checklist tracks *exactly where it stands right now*. As of this writing, the solution scaffold, the PostgreSQL/EF Core schema, and a working, tested orchestration state machine with hash-linked audit logging are done and verified (PD-1 through PD-4, PD-8). The first real modules, the WinRM connector, and the AI agent itself are still ahead.

## Getting familiar with Phase 1 — where to look next

- **`../pre-development-checklist.md`** — the live, dependency-ordered build tracker for everything in this phase.
- **`../learn/sab-engine-overview.md`** — current implementation status across every component this phase touches.
- **`../learn/what-is-sab.md`**, **`../learn/workflows.md`**, **`../learn/modules.md`** — the plain-language concepts this phase is built from.
- **`../design/SAB_Design_Document_v0.1.2.md`, Section 9** — the full roadmap, including how Phase 1 relates to Phases 2 through 4.

---

*This document explains what the phase is and why — it's not a build tracker. Check `../pre-development-checklist.md` for current status, and `../design/SAB_Design_Document_v0.1.2.md` if anything here ever seems to disagree with the design doc itself.*
