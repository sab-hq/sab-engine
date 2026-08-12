# sab-engine
Core orchestration engine and AI agent layer for SAB — executes sysadmin workflows with human-approved plans and built-in rollback.

## New here? Start with these

Plain-language guides to the core concepts — no prior context needed. Full index with all 20 docs grouped by topic: [**docs/learn/README.md**](docs/learn/README.md).

- [**What is SAB?**](docs/learn/what-is-sab.md) — the short answer, start here
- [**Workflows**](docs/learn/workflows.md) — the recipe SAB follows to get a job done
- [**Modules**](docs/learn/modules.md) — the individual, reusable steps a workflow is built from
- [**AI agent layer**](docs/learn/ai-agent-layer.md) — how SAB decides what to propose, and why
- [**Recommend-and-approve mode**](docs/learn/recommend-and-approve-mode.md) — why nothing runs against a real server without a human saying yes
- [**Start here: actually using SAB**](docs/learn/start-here.md) — read the concepts already? The hands-on guide: what you can build, run, and click through today, and an honest list of what still isn't ready
- [**Orchestration engine**](docs/learn/orchestration-engine.md) — the piece that actually carries out an approved plan
- [**Concurrency and claiming**](docs/learn/concurrency-and-claiming.md) — how multiple SAB workers safely share one queue without racing each other
- [**Audit trail**](docs/learn/audit-trail.md) — how SAB keeps a tamper-evident record of everything it does
- [**Crash recovery**](docs/learn/crash-recovery.md) — how SAB handles an interrupted workflow: reconnect and check reality, never guess
- [**Rollback scoping**](docs/learn/rollback-scoping.md) — how SAB decides exactly what to undo when a rollback is needed, and nothing more
- [**Execution environment**](docs/learn/execution-environment.md) — how SAB actually reaches a real server, on-prem or otherwise
- [**Least-privilege credentials**](docs/learn/least-privilege-credentials.md) — why SAB never uses one all-powerful credential for everything
- [**Module sandboxing**](docs/learn/module-sandboxing.md) — how SAB can safely run an untrusted module in isolation before trusting it for real
- [**Engine State Store (ESS)**](docs/learn/engine-state-store.md) — SAB's memory of past runs, so it isn't starting blind every time
- [**ESS vs. SAB-KB**](docs/learn/ess-vs-sab-kb.md) — why SAB-KB isn't a paid upgrade of ESS, but covers a gap ESS can never reach
- [**What is SAB-KB?**](docs/learn/what-is-sab-kb.md) — a separate, paid SAB product, explained for anyone curious even though it's not part of this repo

## Want to contribute?

- [**Community contribution framework**](docs/learn/community-contribution-framework.md) — how anyone can build a module or connector for SAB, and how it gets trusted
- [**Open Source Module Library (OSML)**](docs/learn/open-source-module-library.md) — where every module and workflow actually lives, and how it fits alongside `sab-engine`

## Before development starts

- [**Checklist 02**](docs/checklist-02.md) — **the active tracker** for anything currently in progress or not started, with unique IDs and status
- [**Pre-development checklist**](docs/pre-development-checklist.md) — **closed historical record** of everything completed under the original PD-1 through PD-30 scope; see Checklist 02 for what's happening now
- [**Changelog**](CHANGELOG.md) — short, scannable record of what's actually shipped, in order

## Build phases

- [**Phase 1: Windows Server Patching Proof of Concept**](docs/phases/phase01.md) — the current phase; proves the architecture end-to-end on one workflow
- [**Phase 2: Expand Module Library and Workflows**](docs/phases/phase02.md) — proves the module/workflow model generalizes beyond patching

## Deeper reference

- [**Design document**](docs/design/SAB_Design_Document_v0.1.2.md) — the full technical architecture and design decisions
- [**Market research**](docs/market-research.md) — industry research and positioning behind SAB's design choices
- [**Open questions**](docs/open-questions.md) — tracked design decisions, resolved and outstanding
- [**sab-engine overview**](docs/learn/sab-engine-overview.md) — a consolidated reference for this repo specifically
- [**WSUS connector spec**](docs/wsus-connector-spec.md) — technical spec for the first partnership-oriented integration
