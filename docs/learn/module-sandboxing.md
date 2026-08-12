# Module Sandboxing

*A beginner's guide to how SAB isolates module execution, and how it fits into the bigger picture.*

---

## In one sentence

Before a module is fully trusted to touch a real server, SAB can run it inside a disposable, isolated container instead — so even something broken or malicious in that script can't reach anything outside the box it's running in.

> **Current status:** the sandboxing mechanism itself is real, working code — `DockerSandboxedExecutor`, verified in `pre-development-checklist.md`, PD-27. **There's an important, honestly-flagged limitation:** it currently only sandboxes generic PowerShell scripts correctly, not SAB's actual four patching modules — see below for why.

## The problem it solves

A module (see `modules.md`) is just a PowerShell script. That's the whole point of the design — modules are meant to be written, reviewed, and eventually contributed by a community, not locked inside SAB itself (see `open-source-module-library.md`). But "anyone can write one" also means SAB can't blindly trust that every module is safe just because its manifest looks correct.

Before a module has earned real trust — or any time someone just wants to safely see what a script actually does without risking the machine running it — there needs to be a way to execute it somewhere that genuinely can't cause damage if something goes wrong, whether that's a bug or something worse.

## How it actually works

**Every run happens inside a disposable container, with two real isolation guarantees, both directly tested rather than just assumed:**

- **No network access at all.** The container is started with networking disabled entirely — not restricted, not filtered, genuinely absent. A script running inside can't reach out anywhere, accidentally or otherwise.
- **The script's own directory is mounted read-only.** Whatever the script tries to do, it cannot modify the files it was given to work with, let alone anything else on the host machine.

Both of these are proven with real tests — a script that tries to make a network call gets a confirmed failure, and one that tries to write a file back to its own folder gets a confirmed rejection — not just described in a comment and hoped for.

## The real limitation, stated plainly

This sandboxing runs inside a standard Linux container by default — the same kind Docker Desktop runs without any special configuration, and the same kind everything else in this project (like the Postgres database used in development) already uses.

**SAB's actual four modules — `pre-flight-check`, `stage-patches`, `apply-patches`, `validate` — are all Windows-specific.** They use Windows Update's COM API, `Get-Service`, and `wusa.exe`. None of that exists inside a Linux container. So today, this sandboxing genuinely works correctly for *any* PowerShell script — but it does not yet sandbox SAB's own real modules, because they need a Windows-based container image to run at all.

Making that work for real would mean switching Docker Desktop to Windows-container mode — a real, disruptive reconfiguration, not something to silently assume or force as a default. That's a deliberate, separate decision, still open, not itemized as its own tracked item yet (see `checklist-02.md`'s "Other Known Open Threads").

## A useful mental model

**Think of it like a quarantine room, not a locked cabinet.** A locked cabinet keeps something contained but you still can't watch what it does. A quarantine room lets you actually run the thing and observe its behavior directly, while the walls guarantee nothing gets out — no network, no way to write outside its own space — regardless of whether what's inside turns out to be perfectly safe or genuinely dangerous.

## Getting familiar with module sandboxing — where to look next

- **`modules.md`** — what a module actually is, and why they're meant to be community-writable in the first place.
- **`execution-environment.md`** — how modules actually get run against a real target once they're trusted, which is a genuinely different code path than this local sandboxing.
- **`open-source-module-library.md`** — the broader vision this sandboxing partly exists to support: trusting modules written by people outside the core team.

---

*This document is a plain-language companion to the technical design doc and `pre-development-checklist.md`/`checklist-02.md` — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with those sources, they win; flag the mismatch so this file can be corrected.*
