# Open Source Module Library (OSML)

*A beginner's guide to what the OSML is, and how it fits into the bigger picture.*

---

## In one sentence

The **Open Source Module Library (OSML)** is the actual, real home where every module and workflow SAB knows about actually lives — the open, public repository (`sab-hq/sab-modules`) that anyone can browse, use, and contribute to, following the module and connector contracts described in `modules.md` and `community-contribution-framework.md`.

## The problem it solves

Up to this point, docs like `modules.md` and `workflows.md` explain the *concept* of a module or a workflow — the shape they have to follow, the rules they play by. But a concept needs a real place to actually live. If modules only existed as an abstract idea with nowhere concrete to be stored, browsed, versioned, and pulled into a workflow, none of the rest of SAB would actually work.

The OSML is that concrete place. It's not a separate idea from "modules" and "workflows" — it's simply *where* they physically exist as real files, openly available, rather than scattered across individual people's own scripts and private folders the way sysadmin tooling often ends up.

## What it actually is

The OSML is an open source repository — meaning its contents are public, free to use, and licensed permissively (Apache 2.0), consistent with SAB's whole open-core approach (see Section 1 of the design doc). Concretely, it holds:

- **The actual modules** — the real PowerShell/Bash (and eventually IaC) files, each following the module contract from `modules.md`: a unique ID, typed inputs/outputs, a tested rollback procedure, and tests
- **Workflow definitions** — the actual "recipes" from `workflows.md`, stored as files that reference modules by their contract, not by digging into how they're implemented
- **Community-contributed connectors** beyond the first one SAB ships with — new ways of reaching different environments, submitted the same way a new module would be (see `community-contribution-framework.md`)

Because everything here follows the same required contract, the AI agent layer can discover and propose anything in the OSML with the same level of trust, whether it was written by the core team or contributed by someone in the community.

## Where it fits in the bigger picture

```
sab-engine (the orchestration engine + AI agent)
      ↓  reads modules and workflows from
sab-modules — the OSML   ← (this is this document)
      ↑  anyone can contribute to it, following
community-contribution-framework.md
```

It helps to know what the OSML is *not*, too:

- It's not `sab-engine` — that's the separate repository that actually runs workflows and hosts the AI agent. The OSML is a *library* the engine reads from, not the engine itself (see `sab-engine-overview.md`).
- It's not the commercial/Certified tier — the OSML is the free, community-driven side of SAB's module ecosystem (closer to how Fedora relates to RHEL, see Section 1 of the design doc). A separate, paid tier of hardened, support-backed modules lives elsewhere, in SAB's commercial layer.

## A useful mental model

**The OSML is like a public library's shelves, not the librarian.**

The librarian (`sab-engine`'s orchestration engine and AI agent) is the one who actually knows how to find a book, check it out, and put it to use — but the librarian doesn't *contain* the books. The shelves do. Anyone can walk in, browse what's there, and — because this library also accepts donations that meet its cataloguing standards — anyone can contribute a new book too, as long as it's properly bound and indexed the same way everything else is (that's the module contract, doing the work of a cataloguing standard).

## Why this design choice matters (not just how it works)

- **It's what makes SAB's open-core business model real, not just a talking point.** The OSML being genuinely open source — free to use, free to fork, no legal restriction on rehosting — is the same approach early Red Hat took with its own tooling (see Section 8 of the design doc for the historical precedent). Value gets captured through support, hosting, and the certified tier, not by locking up the library itself.
- **It's the actual growth engine for SAB over time.** As more modules and workflows land in the OSML — from the core team and from contributors alike — building something new increasingly means arranging already-trusted pieces rather than starting from scratch. The size and quality of the OSML is, in a real sense, the size and quality of what SAB can actually do.
- **Its separateness from `sab-engine` is deliberate.** Keeping the library and the engine in different repositories means the engine's code can evolve independently of the module catalog growing — a new module doesn't require touching the engine at all, and vice versa.

## Getting familiar with the OSML — where to look next

- **`modules.md`** and **`workflows.md`** — the contracts that everything living in the OSML has to follow.
- **`community-contribution-framework.md`** — how to actually get something added to the OSML.
- **`sab-engine-overview.md`** — how `sab-engine` (a separate repo) actually reads from and depends on the OSML.
- **`SAB_Design_Document_v0.1.2.md`, Section 9** — the technical breakdown of what specifically lives in the `sab-hq/sab-modules` repository.

---

*This document is a plain-language companion to the technical design doc — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with `SAB_Design_Document_v0.1.2.md`, the design doc wins; flag the mismatch so this file can be corrected.*
