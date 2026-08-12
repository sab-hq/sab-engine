# docs/learn

Plain-language companion guides to how SAB actually works — no prior context needed. Each one follows the same shape: one sentence explaining the idea, the real problem it solves, how it actually works, and a mental model to anchor it. Where something's already built, the doc says so plainly; where it's still just a decided design or an open gap, it says that too.

These are companions to the technical sources of truth, not replacements for them — `SAB_Design_Document_v0.1.2.md` for architecture and design decisions, `pre-development-checklist.md` (closed, historical) and `checklist-02.md` (active) for exact build status. If anything here ever disagrees with those, they win.

---

## Start here

- [**What is SAB?**](what-is-sab.md) — the short answer, start here
- [**Workflows**](workflows.md) — the recipe SAB follows to get a job done
- [**Modules**](modules.md) — the individual, reusable steps a workflow is built from
- [**AI agent layer**](ai-agent-layer.md) — how SAB decides what to propose, and why
- [**Recommend-and-approve mode**](recommend-and-approve-mode.md) — why nothing runs against a real server without a human saying yes

## Ready to try it?

- [**Start here: actually using SAB**](start-here.md) — read the concepts above already? This is the hands-on guide: what you can actually build, run, and click through today, and an honest list of what still isn't ready

## Execution and reliability

- [**Orchestration engine**](orchestration-engine.md) — the piece that actually carries out an approved plan
- [**Concurrency and claiming**](concurrency-and-claiming.md) — how multiple SAB workers safely share one queue without racing each other
- [**Audit trail**](audit-trail.md) — how SAB keeps a tamper-evident record of everything it does
- [**Crash recovery**](crash-recovery.md) — how SAB handles an interrupted workflow: reconnect and check reality, never guess
- [**Rollback scoping**](rollback-scoping.md) — how SAB decides exactly what to undo when a rollback is needed, and nothing more
- [**Execution environment**](execution-environment.md) — how SAB actually reaches a real server, on-prem or otherwise
- [**Least-privilege credentials**](least-privilege-credentials.md) — why SAB never uses one all-powerful credential for everything
- [**Module sandboxing**](module-sandboxing.md) — how SAB can safely run an untrusted module in isolation before trusting it for real

## Memory and knowledge

- [**Engine State Store (ESS)**](engine-state-store.md) — SAB's memory of past runs, so it isn't starting blind every time
- [**ESS vs. SAB-KB**](ess-vs-sab-kb.md) — why SAB-KB isn't a paid upgrade of ESS, but covers a gap ESS can never reach
- [**What is SAB-KB?**](what-is-sab-kb.md) — a separate, paid SAB product, explained for anyone curious even though it's not part of this repo

## Community and ecosystem

- [**Community contribution framework**](community-contribution-framework.md) — how anyone can build a module or connector for SAB, and how it gets trusted
- [**Open Source Module Library (OSML)**](open-source-module-library.md) — where every module and workflow actually lives, and how it fits alongside `sab-engine`

## Reference

- [**sab-engine overview**](sab-engine-overview.md) — a consolidated, single-file reference for this repo specifically: tech stack, current implementation status section by section, and what deliberately doesn't live here

---

*New to the project? Read top to bottom, in the order above — later docs generally assume you've read the ones before them. Ready to stop reading and start doing? Jump straight to `start-here.md`. Already oriented and looking for something specific? Any entry stands alone.*
