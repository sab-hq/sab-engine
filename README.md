# sab-engine
Core orchestration engine and AI agent layer for SAB — executes sysadmin workflows with human-approved plans and built-in rollback.

## New here? Start with these

Plain-language guides to the core concepts — no prior context needed:

- [**What is SAB?**](docs/learn/what-is-sab.md) — the short answer, start here
- [**Workflows**](docs/learn/workflows.md) — the recipe SAB follows to get a job done
- [**Modules**](docs/learn/modules.md) — the individual, reusable steps a workflow is built from
- [**AI agent layer**](docs/learn/ai-agent-layer.md) — how SAB decides what to propose, and why
- [**Recommend-and-approve mode**](docs/learn/recommend-and-approve-mode.md) — why nothing runs against a real server without a human saying yes
- [**Orchestration engine**](docs/learn/orchestration-engine.md) — the piece that actually carries out an approved plan
- [**Execution environment**](docs/learn/execution-environment.md) — how SAB actually reaches a real server, on-prem or otherwise
- [**Engine State Store (ESS)**](docs/learn/engine-state-store.md) — SAB's memory of past runs, so it isn't starting blind every time
- [**ESS vs. SAB-KB**](docs/learn/ess-vs-sab-kb.md) — why SAB-KB isn't a paid upgrade of ESS, but covers a gap ESS can never reach
- [**What is SAB-KB?**](docs/learn/what-is-sab-kb.md) — a separate, paid SAB product, explained for anyone curious even though it's not part of this repo

## Want to contribute?

- [**Community contribution framework**](docs/learn/community-contribution-framework.md) — how anyone can build a module or connector for SAB, and how it gets trusted
- [**Open Source Module Library (OSML)**](docs/learn/open-source-module-library.md) — where every module and workflow actually lives, and how it fits alongside `sab-engine`

## Before development starts

- [**Pre-development checklist**](docs/pre-development-checklist.md) — living tracker of what's left to do before writing code, with unique IDs and status (PD-1 through PD-30)
- [**Checklist 02**](docs/checklist-02.md) — continuation of the above, for anything that comes up after PD-30
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
