# Least-Privilege Credentials

*A beginner's guide to how SAB handles credentials, and how it fits into the bigger picture.*

---

## In one sentence

SAB never uses one all-powerful, standing credential for everything it touches — it resolves a specific, appropriately-scoped credential for each target, and neither a module nor the AI agent ever sees the raw secret.

> **Current status:** the underlying secrets backend and the tiered-credential resolution mechanism are both real, working code — `ISecretStore`/`WindowsCredentialManagerSecretStore` (verified in `pre-development-checklist.md`, PD-9) and `CredentialHandleResolver` (PD-25). **What's not done yet:** nothing currently calls the tiered resolver per-module — a workflow today would still use one connection/credential for its whole run. That wiring is tracked as PD-31 in `checklist-02.md`, waiting on the broader orchestration engine work described in `crash-recovery.md` and `rollback-scoping.md`.

## The problem it solves

The easy, lazy way to build something like SAB would be one admin-level credential, stored once, used for every server and every action. It would work. It would also mean that if that single credential ever leaked — through a compromised machine, a careless log line, anything — an attacker would have full administrative access to every server SAB can reach, for every kind of action, not just the ones that were actually needed.

Real infrastructure security doesn't work that way. A technician who only ever runs health checks shouldn't be holding the same credential as one applying patches. A read-only check like `pre-flight-check` genuinely doesn't need the same access as `apply-patches`, which changes the system. SAB's credential design exists to make that distinction real, not just a policy written down somewhere nobody enforces.

## How it actually works

**The secret itself never travels far.** `ISecretStore` is the pluggable contract for wherever secrets actually live — Phase 1 uses Windows Credential Manager (`WindowsCredentialManagerSecretStore`), with the door deliberately left open to swap in something like HashiCorp Vault later without touching anything that calls it. A module never sees a raw secret. The AI agent that proposes what to do never sees one either. Only the connector that actually opens a connection resolves one, and only for as long as that connection is open.

**Which credential gets used isn't a coin flip — it follows a real convention.** `CredentialHandleResolver` looks for a credential registered specifically for the combination of a target and a tier — something like `"srv-01:elevated"` for the servers and actions that genuinely need it. If nothing's registered at that specific tier, it falls back to a plain `"srv-01"` handle — a single standing credential for that target. This matters in practice: it means an operator can adopt tiered credentials gradually, server by server, rather than needing every target reconfigured before any of this works at all.

**The tier itself is meant to track a module's own declared risk.** A module's manifest already says how risky it is (`risk_level: low` for something like `pre-flight-check`, `risk_level: high` for `apply-patches` — see `modules.md`). The intended mapping is straightforward: low/medium-risk modules use the `Standard` tier, high-risk ones use `Elevated`. That mapping is a decision for whoever calls the resolver, not baked into the resolver itself — a deliberate choice, so the piece that resolves credentials doesn't need to understand the module manifest schema just to make that call.

## A useful mental model

**Think of it like a building's keycard system, not a single master key.** A master key opens every door, which is convenient right up until it's lost or copied. A well-run keycard system instead issues access based on what someone actually needs to do — a technician doing routine walkthroughs gets a different level of access than one doing electrical work. SAB's credential tiers work the same way: the access granted matches the actual risk of the action being performed, not a single blanket level of trust for everything.

## Getting familiar with credentials — where to look next

- **`execution-environment.md`** — the WinRM connector this credential resolution actually feeds into, and the one honest gap that remains around the real network connection.
- **`modules.md`** — where a module's `risk_level` is actually declared, which is meant to inform which credential tier it uses.
- **`crash-recovery.md`** — the broader orchestration work that PD-31 (wiring tiered credentials in per-module) depends on existing first.

---

*This document is a plain-language companion to the technical design doc and `pre-development-checklist.md`/`checklist-02.md` — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with those sources, they win; flag the mismatch so this file can be corrected.*
