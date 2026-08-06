# What Is SAB?

*A beginner's introduction to SAB — start here if this is your first time hearing about it.*

---

## In one sentence

**SAB (System Administration Builder)** is a system that turns the manual, repetitive work sysadmins do every day into reusable, standardized "recipes" that an AI agent can propose and run — but only after a human says it's okay.

## The problem SAB is trying to solve

If you've worked in IT for any length of time, you know this pattern: there's always one person on the team who "just knows" how to safely patch a server, or handle a tricky restart, or work around some quirk in a particular environment. That knowledge is real and valuable — but it usually only lives in that person's head. It's not written down anywhere reliable, it gets done a little differently every time, and it walks out the door the moment that person is unavailable, busy, or gone.

At the same time, a lot of sysadmin work is genuinely repetitive — the same handful of steps, over and over, across dozens or hundreds of servers. That's exactly the kind of work that's tedious for a person to keep doing by hand, but risky to hand off to "just run a script" or "let an AI handle it" without real safeguards. A bad automated change to production infrastructure isn't a minor inconvenience — it can mean real downtime, real damage, and a lot of very early mornings.

SAB exists to solve both problems at once: capture that tribal knowledge in a standardized, reusable form, and let an AI agent help carry it out — without ever skipping the human judgment that makes it safe.

## How SAB actually works, in plain terms

SAB is built out of a small number of core ideas that fit together. Here's the short version of each, with a link to the full explanation if you want to go deeper on any one of them:

- **[Workflows](workflows.md)** — a workflow is the "recipe" for getting a specific job done, like patching a Windows Server. It's an ordered list of steps, written once and reused every time that job needs doing again.
- **[Modules](modules.md)** — a module is one individual step in that recipe — one small, reliable, well-tested action, like "check if the server is healthy" or "apply the patches." Every module comes with a required, tested way to undo itself if something goes wrong.
- **[The AI agent layer](ai-agent-layer.md)** — looks at the specific situation and proposes exactly which modules to run, in what order, and why — but it never acts on its own.
- **[Recommend-and-approve mode](recommend-and-approve-mode.md)** — SAB's AI agent never just acts on its own. It looks at the situation, proposes a plan, and explains its reasoning — but a human has to approve that plan before anything actually happens to a real server.
- **[The orchestration engine](orchestration-engine.md)** — once a plan is approved, this is the part of SAB that actually carries it out: running each module in the right order, tracking exactly what's happening, and automatically rolling back if something fails partway through.
- **[The execution environment](execution-environment.md)** — the piece that actually reaches out and touches a real server, so nothing else in SAB has to know or care whether that server is on-prem, in the cloud, or somewhere in between.
- **[The Engine State Store (ESS)](engine-state-store.md)** — SAB's memory of everything that's happened before, so the AI agent isn't starting blind every single time.

Put together, a typical SAB interaction looks like this: you pick a workflow → the AI agent proposes a specific plan for your situation, informed by real history → you approve it → the orchestration engine runs the plan, module by module, reaching real servers through the execution environment → if anything fails, it rolls back automatically → what happened gets remembered for next time.

## Why it's built this way

The single most important rule behind SAB's whole design is this: **the system has to be solid and predictable before it's ever trusted to act on its own.** In practice, that means three things, all non-negotiable from day one:

1. The AI agent always proposes, never just acts — a human approves every plan before it touches real infrastructure ([recommend-and-approve mode](recommend-and-approve-mode.md)).
2. Every single module is required to have a tested rollback path — nothing gets run against production without already knowing exactly how to undo it if needed ([modules](modules.md)).
3. New workflows and modules get proven out in a safe, low-stakes environment before they're ever trusted against something that actually matters.

"Boring and reliable" is treated as a real feature here, not a limitation — especially early on, since nobody's going to trust an AI agent near their production servers until it's actually earned that trust.

## Who SAB is for

SAB is being built with two related but distinct audiences in mind:

- **System administrators and system engineers** managing their own on-prem or hybrid infrastructure — SAB's first real use case is Windows Server patching, chosen because it's routine, high-value, and a clean place to prove the whole approach.
- **MSPs (Managed Service Providers)**, who deal with the added challenge of standardizing this kind of work consistently across many different client environments at once, rather than just one.

## Where to go next

- New to all of this? Read **[Workflows](workflows.md)** first, then **[Modules](modules.md)** — those two ideas are the foundation everything else builds on.
- Curious about how the AI actually decides what to propose? **[The AI agent layer](ai-agent-layer.md)** covers that.
- Curious about the safety model specifically? **[Recommend-and-approve mode](recommend-and-approve-mode.md)** is the one to read.
- Want to know what actually runs the show behind the scenes? **[The orchestration engine](orchestration-engine.md)** covers that, and **[the execution environment](execution-environment.md)** covers how it actually reaches a real server.
- Want to know how SAB gets smarter over time instead of starting blind every run? **[The Engine State Store (ESS)](engine-state-store.md)** is where that history lives.
- Ready for the full technical picture, not just the plain-language version? Everything here traces back to **`SAB_Design_Document_v0.1.2.md`**, which is the authoritative source if anything in these beginner docs ever seems to disagree.

---

*This document is a plain-language companion to the technical design doc — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with `SAB_Design_Document_v0.1.2.md`, the design doc wins; flag the mismatch so this file can be corrected.*
