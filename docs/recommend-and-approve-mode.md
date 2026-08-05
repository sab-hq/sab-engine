# Recommend-and-Approve Mode

*A beginner's guide to SAB's core safety principle — what it is, and why it's non-negotiable.*

---

## In one sentence

**Recommend-and-approve mode** means SAB's AI agent never runs anything against your infrastructure on its own — it proposes a plan and explains its reasoning, and a human has to say yes before anything actually happens.

## The problem it solves

Handing an AI agent the ability to make changes to real servers is genuinely useful — but it's also genuinely risky if it's trusted too early. This isn't a hypothetical worry: there are real, documented cases of AI agents causing serious, irreversible damage to production infrastructure — including deleting production databases along with their backups — when they were given too much autonomy before they'd earned it.

Industry research backs this up directly. A 2026 survey found that "trust" — not the AI's actual technical capability — is the single biggest reason companies hesitate to let AI agents take autonomous action on infrastructure. Separately, a 2026 survey of data-center operators found that trust in AI making changes to real equipment has gone *down* every year since 2022, even as AI has gotten smarter. Getting an AI to recommend the right action was never really the hard part — trusting it enough to actually let it act is.

SAB's answer to that problem is recommend-and-approve mode: build the trust-earning step directly into the architecture, rather than hoping people are careful.

## What it actually is

Recommend-and-approve mode is a hard rule in how SAB is built, not just a setting someone could accidentally turn off. Concretely, it means the AI agent's job stops at *proposing* — it can never directly call a module or touch a real system itself (see `modules.md`). Every single workflow run, no matter how routine, has to pass through a human approval step before SAB does anything to a real server.

Here's what that actually looks like in practice, using the Windows Server patching example from `workflows.md`:

1. You tell SAB "patch this server," and its AI agent looks at the situation — this server's patch history, its current health, anything unusual about it.
2. Instead of just doing it, the agent produces a **plan**: exactly which modules it wants to run, in what order, with what specific settings — plus a plain-language explanation of *why*, and a flag if anything about this particular run looks unusual compared to past runs.
3. That plan lands in front of a person. Nothing has happened to the actual server yet.
4. **You decide.** Approve it, and SAB carries out exactly that plan. Decline it, and nothing runs — the agent can revise and try again, but it never gets to just proceed anyway.

That's it. The whole safety mechanism is: propose, explain, wait for a yes.

## Where it fits in the bigger picture

Recommend-and-approve mode is the seam between two parts of SAB that are deliberately kept separate (see Section 3 of `SAB_Design_Document_v0.1.2.md`):

- **The AI agent** decides *what* should happen and *why* — that's its whole job, and it stops there.
- **The orchestration engine** is the only part of SAB that actually *does* anything to a real system — and it will only ever act on a plan a human has already approved.

That separation is deliberate and load-bearing, not incidental. It means that even if the AI agent's reasoning were ever wrong or confused about something, the worst outcome is a bad *suggestion* landing in front of a person — never a bad *action* landing on a real server. The human approval step is the wall between "the AI got something wrong" and "something wrong actually happened."

## A useful mental model

**Recommend-and-approve mode = a junior team member who always checks with you before touching anything important.**

Imagine a capable junior sysadmin who's done real research, has a solid plan, and can explain their reasoning clearly — but who's agreed, as a firm rule, to never actually touch a production server without you nodding first. Even once they've proven themselves on hundreds of routine patch runs, they still check in every single time. That's not because you don't trust their judgment anymore — it's because the check-in itself is cheap, and the cost of being wrong on production infrastructure is not.

## Why this design choice matters (not just how it works)

- **It's not a temporary training-wheels phase to be removed as soon as possible.** SAB does have a longer-term vision of gradually earning more autonomy for specific, well-proven workflows over time (see "Autonomy levels" in Section 4.3) — but that's earned per-workflow, based on an actual track record of successful runs recorded in SAB's memory (see Section 4.5), not assumed on day one or granted system-wide.
- **It's a security control, not just a trust-building nicety.** Separate 2026 research into AI-agent security found that attacks specifically designed to trick AI agents into taking harmful actions succeed at a meaningful rate — and that more capable AI models weren't necessarily more resistant to being tricked. Recommend-and-approve mode isn't just about building user confidence; it's a real, concrete backstop against exactly that category of risk (see Section 7 of the design doc).
- **"Boring and reliable" is treated as a feature, not a limitation.** Especially early on, sysadmins need to actually trust SAB before they'll run it against anything that matters to them — and a system that never skips the human check-in is a lot easier to trust than one that promises to "mostly" ask permission.

## Getting familiar with recommend-and-approve mode — where to look next

- **`workflows.md`** and **`modules.md`** — recommend-and-approve mode is the gate that sits between a proposed workflow plan and it actually running; worth reading those first if you haven't, since this concept sits right in the middle of both.
- **`SAB_Design_Document_v0.1.2.md`, Section 2** — where this principle is first established as one of SAB's four non-negotiable requirements.
- **`SAB_Design_Document_v0.1.2.md`, Section 4.3** — the technical version of how the AI agent structures a proposal, and how "autonomy levels" work as a longer-term concept beyond recommend-and-approve.
- **`SAB_Design_Document_v0.1.2.md`, Section 7** — the security research and reasoning for why this is treated as a real safety control, not just good UX.

---

*This document is a plain-language companion to the technical design doc — it's meant to get you oriented, not to be the authoritative spec. If something here ever seems to disagree with `SAB_Design_Document_v0.1.2.md`, the design doc wins; flag the mismatch so this file can be corrected.*
