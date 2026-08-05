# ESS vs. SAB-KB

*A beginner's guide to two similarly-named things that solve genuinely different problems — and why one of them can never do the other's job.*

---

## In one sentence

**SAB-KB exists because there's a whole category of knowledge the Engine State Store (ESS) can never capture, no matter how much you use it** — ESS only knows what happened inside an automated `sab-engine` workflow; SAB-KB is built to capture everything else, the knowledge that never touches automation at all.

## The problem this doc solves

"Engine State Store" and "SAB-KB" have a real history of sounding like the same thing — the ESS used to actually be called "SAB-KB" early in SAB's design, before that name got reassigned to the real, separate product (see `open-questions.md`, RC-3). That history left an easy trap behind: someone reading about the free ESS could reasonably think "oh, SAB already remembers things for me, I don't need to pay for SAB-KB too." That would be a mistake — not because SAB-KB is a nicer version of the same thing, but because **ESS is structurally incapable of capturing the kind of knowledge SAB-KB exists for.**

## What ESS can never capture

ESS only ever knows about things that happened *inside* an approved, executed `sab-engine` workflow run. That's its entire universe: which workflow ran, against which server, with what result.

It has zero visibility into anything that happens outside that box — and a huge amount of real institutional knowledge lives entirely outside that box:

- A senior tech explaining in a Teams thread *why* a specific client's firewall is configured the weird way it is
- An email chain where someone worked out a one-off fix to a problem that never became a formal, repeatable workflow
- The "don't reboot that server on Fridays, trust me" knowledge that exists purely in one person's head
- Any troubleshooting, judgment call, or workaround that happened by talking to a person, not by running a module

No amount of `sab-engine` usage will ever cause any of that to show up in the ESS — it's not a smaller version of that knowledge, it's a completely different category that ESS was never built to see.

## What SAB-KB actually captures

SAB-KB exists specifically to close that gap. It pulls tribal knowledge out of the places it actually lives — email, Teams conversations, and other real communication — so it doesn't disappear the moment the person who knows it is out sick, busy, or gone. This is the exact "brain debt" problem the design doc talks about (Section 1): a small number of people carrying disproportionate institutional knowledge that was never written down anywhere durable.

## The comparison, side by side

| | **ESS** | **SAB-KB** |
|---|---|---|
| **What it can ever know about** | Only what ran through an automated `sab-engine` workflow | Anything captured from real human communication — email, Teams, and more |
| **Can it capture a Teams conversation explaining a one-off fix?** | No — structurally can't, it never sees that conversation | Yes — this is exactly what it's built for |
| **Who it's for** | The AI agent and orchestration engine inside `sab-engine` | MSPs looking to capture institutional knowledge across their whole client base |
| **License / cost** | Free — part of open source `sab-engine` | Paid — per-technician-seat subscription, with a free on-ramp tier |
| **Scope** | Deliberately narrow — only automated run history | Broad by design — this is the actual product |

## Do customers need both?

For most customers using `sab-engine`, **ESS alone is enough** — it's free, it comes with the engine, and it does exactly what it's supposed to: give the AI agent a track record to work from for the workflows it runs. That's not a stripped-down trial of SAB-KB; it's simply a different, narrower job.

**SAB-KB is worth it specifically for the knowledge ESS can never touch.** If an organization's real risk is "the knowledge that keeps this place running lives in a few people's heads and in scattered email threads," that's a gap ESS cannot close at any level of usage — it isn't watching those conversations, and it never will, because that's not what it's for. That's the actual case for SAB-KB: not "a better memory," but "a memory of things ESS structurally cannot see."

## A useful mental model

**ESS is like a car's onboard trip computer. SAB-KB is like the mechanic's personal notebook of quirks, tricks, and hard-won lessons that never show up on any dashboard.**

The trip computer is free, comes with the car, and faithfully logs exactly what the car itself did — mileage, fuel used, trips taken. It will never, ever contain the mechanic's note that says "this model's transmission makes a weird noise in cold weather, it's normal, don't worry about it" — that knowledge exists in a completely different place, learned through experience and conversation, not through anything the car's own systems could log. You don't upgrade a trip computer into a mechanic's notebook. They're just different things, and a shop that wants both kinds of knowledge needs both tools.

## Why this design choice matters (not just how it works)

- **This isn't an upsell — it's coverage of a real gap.** SAB-KB's value doesn't come from being "more" of what ESS does; it comes from reaching knowledge ESS structurally cannot reach. That's a much more honest (and durable) case to make to a customer than "pay more for a bigger memory."
- **Keeping them separate keeps each one honest about what it actually does.** ESS stays small, fast, and free because it only has to serve `sab-engine`'s own narrow needs. SAB-KB gets to be genuinely ambitious about the much harder problem of capturing human, conversational knowledge, without being tangled up in orchestration-engine internals.
- **The free tier isn't a trojan horse for the paid one.** A customer who only ever needs automated workflow history has no real reason to pay for SAB-KB — and that's fine. SAB-KB earns its subscription from organizations with the specific problem it solves, not from artificially limiting what ESS is allowed to do.

## Getting familiar with ESS vs. SAB-KB — where to look next

- **`engine-state-store.md`** — the full picture on ESS specifically, including what it stores and how the AI agent queries it.
- **`what-is-sab-kb.md`** — the full picture on SAB-KB specifically, even though it's a separate product from everything else in this repo.
- **`ai-agent-layer.md`** — how the AI agent actually uses ESS when proposing a plan.
- **`SAB_Design_Document_v0.1.2.md`, Section 4.5** — the technical version of ESS, plus the naming history (RC-3) behind this whole distinction.
- **`SAB_Design_Document_v0.1.2.md`, Section 1** — the "brain debt" research that SAB-KB is specifically built to address.
- **`open-questions.md`, RC-1 through RC-3** — the tracked decisions behind why these stay separate, and what (if anything) about their relationship is still genuinely open.

---

*This document is a plain-language companion to the technical design doc — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with `SAB_Design_Document_v0.1.2.md`, the design doc wins; flag the mismatch so this file can be corrected.*
