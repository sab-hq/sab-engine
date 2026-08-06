# Phase 2: Expand Module Library and Workflows

*What this phase is, what's in scope, and how you'll know it's actually done.*

---

## In one sentence

Phase 2 proves the module/workflow model actually generalizes beyond Windows Server patching — by building at least one meaningfully different workflow using the same architecture Phase 1 proved out, without needing to redesign the core contracts to do it.

## Why this phase exists

Phase 1 proves the architecture works for *one* use case. That's necessary but not sufficient — a design that only works for the exact thing it was first built around isn't really a general architecture, it's a one-off. Phase 2 is where the module contract, the workflow model, and the orchestration engine get tested against something genuinely different from patching, and where any gaps that patching happened not to expose get found. See `../design/SAB_Design_Document_v0.1.2.md`, Section 9.

## What's in scope

- **A second workflow, adjacent to patching, using the same architecture.** Natural candidates named in the design doc: service restarts, basic provisioning tasks, backup verification — exact scope still to be decided (`../design/SAB_Design_Document_v0.1.2.md`, Section 9 notes this as TBD).
- **Validating the module contract against that second use case.** Phase 1's modules were written *while* the contract was being proven out — Phase 2 is the first real test of whether that contract holds up for something the contract wasn't originally shaped around.
- **Starting to flesh out the Engine State Store (ESS) into its fuller role.** Phase 1's ESS was deliberately minimal (just enough to log run history); Phase 2 is where there's enough real history — across two different workflows now — to make the ESS's "learn from the past" role actually meaningful, per `../learn/engine-state-store.md`.
- **Beginning to consider approve-by-exception** for the most-proven, lowest-risk parts of the *patching* workflow specifically, if Phase 1's track record actually supports it (`../learn/ai-agent-layer.md`, "Autonomy levels"). This is a "start considering," not a commitment to ship it in this phase.

## What's explicitly out of scope

- Cloud or hybrid execution environments (Phase 3)
- The Marketplace, community contribution at scale, or public open source launch (Phase 4)
- Full autonomy for any workflow — approve-by-exception, if it happens at all this phase, only applies to the most-proven slice of Phase 1's own patching workflow, not a general capability
- A third or fourth workflow — Phase 2's bar is *one* meaningfully different workflow, not a library

## Exit criteria

A second, meaningfully different workflow running in production-adjacent conditions, without needing to redesign core contracts. "Without needing to redesign" is the real test here — if proving out a second workflow forces a breaking change to the module or connector contract, that's a signal Phase 1's design wasn't as general as it looked, and it's better to find that out now than after a Marketplace and community exist depending on that contract (`../design/SAB_Design_Document_v0.1.2.md`, Section 9).

## Current status

Not started — Phase 2 depends on Phase 1's exit criteria actually being met first (see `phase01.md`). `../pre-development-checklist.md` currently only covers Phase 1's tasks (PD-1 through PD-29); Phase 2 doesn't have its own itemized checklist yet. One will be created once Phase 1 is far enough along that Phase 2's specific workflow choice and task breakdown can be made concretely rather than speculatively.

## Getting familiar with Phase 2 — where to look next

- **`phase01.md`** — the phase this one depends on; check its current status first.
- **`../learn/modules.md`**, **`../learn/workflows.md`** — the contracts this phase is specifically testing for generality.
- **`../learn/engine-state-store.md`** — what "fleshing out the ESS's fuller role" actually means.
- **`../design/SAB_Design_Document_v0.1.2.md`, Section 9** — the full roadmap, including how Phase 2 relates to Phases 1, 3, and 4.

---

*This document explains what the phase is and why — it's not a build tracker. A Phase 2-specific checklist will be created once Phase 1 is further along; until then, `../pre-development-checklist.md` covers Phase 1 only. If anything here ever seems to disagree with the design doc itself, `../design/SAB_Design_Document_v0.1.2.md` wins.*
