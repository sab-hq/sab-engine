# Execution Environment

*A beginner's guide to what the execution environment is, and how it fits into the bigger picture.*

---

## In one sentence

The **execution environment** is the part of SAB that actually reaches out and touches a real server — it's the "hands" that carry out a module's instructions, and it's built so the rest of SAB never has to know or care whether that server is on-prem, in the cloud, or somewhere in between.

> **Current status:** the WinRM connector this doc describes is now real, working code as of PD-23/PD-24 in `pre-development-checklist.md` — it resolves a credential handle against Windows Credential Manager, opens a remote PowerShell session, runs a real module script against it, and returns a structured result. Least-privilege credential scoping is real now too (PD-25) — a resolver that picks a tier-specific credential (`"target:elevated"` vs. a plain `"target"` fallback) instead of one standing credential for everything, verified with 59/59 tests total. **One honest, important gap remains:** the tests substitute a real *local* PowerShell session for the real remote connection, since a genuine WinRM connection can't be meaningfully faked in a test — so the actual network connection to a real server has never been tried yet. Confirming that for real means deliberately starting the lab VM (PD-11) and checking WinRM is reachable there, which hasn't happened. **Also worth knowing:** PD-25's resolver exists, but nothing calls it per-module yet — that wiring depends on orchestration work that doesn't exist yet, now tracked as PD-31 in `checklist-02.md`. What's still not built here: per-target connection isolation (PD-26) and Docker-based sandboxing (PD-27).

## The problem it solves

A module (see `modules.md`) says something like "apply these patches" — but *how* you actually reach a server and run a command on it depends entirely on where that server lives. An on-prem Windows Server gets reached differently than a Linux box, which gets reached differently than a cloud VM. If every module had to know all of that connection detail itself, you'd end up rewriting the same "how do I connect and authenticate" logic over and over, and every module would be tangled up with infrastructure details that have nothing to do with what it's actually trying to accomplish.

The execution environment exists to pull all of that connection complexity into one place, so a module can just say "run this" without needing to know anything about how the connection actually works underneath.

## What it actually is

The execution environment is the "where" layer of SAB — it abstracts away the difference between environments so nothing upstream (the orchestration engine, the modules, the AI agent) needs to think about it. For SAB's first real use case, that means:

**On-prem Windows via WinRM** — SAB's first execution environment connector reaches Windows Server targets using WinRM (Windows Remote Management) and runs PowerShell against them. It handles:

- **Connecting and disconnecting** reliably, including retrying if a connection is flaky
- **Credentials** — the execution environment is the only place that ever actually resolves a real credential to connect with; modules and the AI agent only ever see a reference to a credential, never the real thing (see Section 7 of the design doc for why that separation matters)
- **Isolation** — if something goes wrong connecting to one server, it shouldn't affect anything happening on a different server at the same time

**Built to grow.** WinRM is the first connector, not the only one that'll ever exist. Because the connector is designed as a pluggable interface, adding support for Linux (via SSH) or cloud platforms (via their own APIs) later doesn't require redesigning anything else in SAB — it's just a new implementation of the same basic contract.

## Where it fits in the bigger picture

```
Orchestration engine decides it's time to run a module   ← (orchestration-engine.md)
      ↓
Execution environment connects to the actual target server   ← (this is this document)
      ↓
The module's instructions actually run on that server
      ↓
Results come back up to the orchestration engine
```

The execution environment is the very last link in the chain — everything before it is planning and decision-making; this is where SAB's actions finally become real, physical changes on an actual machine.

## A useful mental model

**The execution environment is like a universal power adapter for travel.**

Your laptop charger doesn't need to know or care whether it's plugged into a US outlet, a UK outlet, or an EU outlet — the adapter handles that difference invisibly, and your laptop just gets power either way. The execution environment plays the same role for modules: a module doesn't need to know whether it's running against on-prem Windows or, eventually, a cloud VM — the execution environment handles that difference, and the module just gets to run.

## Why this design choice matters (not just how it works)

- **Credentials never pass through the module or the AI agent.** The execution environment resolves the actual credential only at the moment of connecting, from a dedicated secrets store — this means even if something else in the system were ever compromised, the raw credentials were never sitting somewhere they could be read from.
- **A failure connecting to one server can't take down a run on a different one.** Because each connection is isolated, a network hiccup reaching one target doesn't ripple out and affect work happening in parallel elsewhere.
- **Growing to a new environment (Linux, cloud) is additive, not a rewrite.** Since the connector is a defined, swappable interface rather than logic baked directly into the orchestration engine, expanding SAB's reach later is a matter of building a new connector — not reworking everything that already exists and is already trusted.

## Getting familiar with the execution environment — where to look next

- **`modules.md`** — what the execution environment is actually carrying out on the target server.
- **`orchestration-engine.md`** — what calls on the execution environment, and when.
- **`SAB_Design_Document_v0.1.2.md`, Section 4.4** — the technical version of everything above, including the actual connector interface (`connect`, `execute`, `disconnect`, `health_check`) any new environment implementation needs to fulfill.
- **`SAB_Design_Document_v0.1.2.md`, Section 7** — more detail on how credentials are kept away from modules and the AI agent specifically.

---

*This document is a plain-language companion to the technical design doc — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with `SAB_Design_Document_v0.1.2.md`, the design doc wins; flag the mismatch so this file can be corrected.*
