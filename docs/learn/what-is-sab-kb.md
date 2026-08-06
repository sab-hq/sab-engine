# What Is SAB-KB?

*A beginner's introduction to SAB-KB — a separate SAB product, not part of the free open source system.*

---

## In one sentence

**SAB-KB** is a paid product that automatically captures an organization's tribal knowledge — from real email and Teams conversations — into a searchable, standardized knowledge base, so critical know-how doesn't disappear the moment the one person who has it is unavailable, busy, or gone.

## Important context before you read further

SAB-KB is **not** part of `sab-engine`, and it's **not** free or open source. It's a separate commercial product, built and sold on its own, aimed primarily at MSPs (Managed Service Providers). If you're here from the `sab-engine` docs wondering how this fits in, the short version is: it doesn't, technically — they're independent products. See [`ess-vs-sab-kb.md`](ess-vs-sab-kb.md) for the full explanation of how they relate (and don't).

## The problem SAB-KB solves

Most organizations' infrastructure knowledge lives in the heads of one or two people who built things "the way they always do it." When that person leaves, goes on vacation, or is just having a busy week, everyone else is stuck reverse-engineering what they knew — this is sometimes called "bus factor" risk, and most teams already know they're exposed to it, even if they've never fixed it.

The deeper reason it never gets fixed on its own: keeping a knowledge base up to date has always depended on someone remembering to write it up after the fact. That doesn't happen reliably, no matter how good anyone's intentions are. SAB-KB's founding idea flips that: **the system, not a human, is the primary knowledge-base maintainer.** It builds and maintains the knowledge base automatically from data sources it can already reach — without ever depending on someone remembering to log anything.

## How it actually works

SAB-KB captures knowledge from two everyday places almost every organization already uses:

- **Email (Outlook)**
- **Microsoft Teams conversations**

A small set of purpose-built agents does the work:

- **The capture agent** reads incoming email and Teams conversations, looking for real operational knowledge worth keeping — a troubleshooting explanation, a workaround, the reasoning behind an unusual configuration
- **The curation agent** takes what's been captured, checks it against a standard structured format, removes duplicates, and writes it into the knowledge base
- **An orchestrator agent** coordinates the others and maintains a human approval queue for anything higher-stakes — this mirrors the same "human stays in the loop" philosophy behind `sab-engine`'s recommend-and-approve mode, even though these are separate products

If a real gap shows up — something the system genuinely couldn't capture, and why — that gets tracked explicitly rather than silently missed. And when an employee adds a comment or a tag to an existing entry, that human input becomes enrichment feeding back into the curation loop — a bonus that improves the knowledge base further, but never something the system depends on to function in the first place.

## Beyond simple capture: Flightpath

SAB-KB also includes a standardization concept called **Flightpath** — named for the aviation idea that if running IT operations were a flight, there's a correct route, and current practice can be "on path" or "off path." Instead of inventing standards from scratch, Flightpath draws on existing, trusted industry standards (things like CIS Benchmarks and well-architected frameworks) and compares them against what an organization is actually doing. Where practice deviates from the standard, a person reviews it and chooses — adopt the standard now, or knowingly continue the current approach with a documented reason. Nothing gets silently forced into compliance; the point is making the choice visible and recorded, not mandating a single "correct" answer.

## Where to find it

SAB-KB lives in its own separate repository — `sab-hq/sab-kb` — and its own documentation, distinct from everything in `sab-engine`'s `docs/` folder. This doc, and [`ess-vs-sab-kb.md`](ess-vs-sab-kb.md), exist here specifically so someone reading `sab-engine`'s docs isn't left wondering what SAB-KB is or how the two relate.

## Why it costs money

Unlike `sab-engine`, which is open source and free, SAB-KB is a **paid, monthly subscription priced per technician seat**, with a free on-ramp period for new customers before that billing kicks in. This is a deliberate business-model choice, not an accident: `sab-engine`'s open-core approach makes the orchestration engine and module library free to drive adoption and community contribution (see Section 1 of the design doc), while SAB-KB is built and sold as its own standalone commercial product from the start, aimed at a specific buyer (MSPs) with a specific, high-value problem.

## Getting familiar with SAB-KB — where to look next

- **[`ess-vs-sab-kb.md`](ess-vs-sab-kb.md)** — the essential read if you came here from `sab-engine`'s docs: why SAB-KB isn't a bigger version of `sab-engine`'s free Engine State Store, but covers a gap that store can never reach.
- **[`what-is-sab.md`](what-is-sab.md)** — the broader picture of SAB as a whole, including how `sab-engine` (the free, open source side) fits alongside SAB-KB (the paid side).
- **`SAB_Design_Document_v0.1.2.md`, Section 1** — the "brain debt" research and business-model reasoning behind why SAB-KB exists as a separate product.

---

*This document is a plain-language companion to `sab-engine`'s design doc — it's meant to get you oriented, not to be the authoritative spec for SAB-KB itself, which has its own separate documentation in the `sab-hq/sab-kb` repository.*
