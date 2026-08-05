# sab-engine
Core orchestration engine and AI agent layer for SAB — executes sysadmin workflows with human-approved plans and built-in rollback.

## New here? Start with these

Plain-language guides to the core concepts — no prior context needed:

- [**What is SAB?**](docs/what-is-sab.md) — the short answer, start here
- [**Workflows**](docs/workflows.md) — the recipe SAB follows to get a job done
- [**Modules**](docs/modules.md) — the individual, reusable steps a workflow is built from
- [**AI agent layer**](docs/ai-agent-layer.md) — how SAB decides what to propose, and why
- [**Recommend-and-approve mode**](docs/recommend-and-approve-mode.md) — why nothing runs against a real server without a human saying yes
- [**Orchestration engine**](docs/orchestration-engine.md) — the piece that actually carries out an approved plan
- [**Execution environment**](docs/execution-environment.md) — how SAB actually reaches a real server, on-prem or otherwise
- [**Engine State Store**](docs/engine-state-store.md) — SAB's memory of past runs, so it isn't starting blind every time

## Want to contribute?

- [**Community contribution framework**](docs/community-contribution-framework.md) — how anyone can build a module or connector for SAB, and how it gets trusted
- [**Open Source Module Library (OSML)**](docs/open-source-module-library.md) — where every module and workflow actually lives, and how it fits alongside `sab-engine`

## Deeper reference

- [**Design document**](docs/SAB_Design_Document_v0.1.2.md) — the full technical architecture and design decisions
- [**Market research**](docs/market-research.md) — industry research and positioning behind SAB's design choices
- [**Open questions**](docs/open-questions.md) — tracked design decisions, resolved and outstanding
- [**sab-engine overview**](docs/sab-engine-overview.md) — a consolidated reference for this repo specifically
- [**WSUS connector spec**](docs/wsus-connector-spec.md) — technical spec for the first partnership-oriented integration
