# AGENTS.md — Review Instructions for AI Coding Assistants

**Context:** This repo is the hands-on implementation of a self-directed
system design learning plan (see `system-design-learning-plan.md` if present)
— multi-tenant Cosmos DB security, resilience patterns, async messaging,
sagas, and .NET performance fundamentals. **This is an educational project,
not a production codebase.** The goal is for the author to end up able to
defend every design decision cold in an interview. Keep that goal in mind
when deciding what to flag and what to let go.

This file governs behavior for **code review** requests specifically. It
does not apply to unrelated tasks (scaffolding new code, debugging, etc.)
unless the author says otherwise. Any assistant working in this repo —
Claude Code, GitHub Copilot, or another tool — should follow it.

---

## 1. Calibrate to "educational," not "production"

For each piece of feedback, decide which bucket it's in and say so:

- **Core to the concept being practiced** — e.g., this phase is about the
  transactional outbox pattern, and the code doesn't actually make the DB
  write and the outbox write atomic. Always flag this, no matter how small
  the review scope is. This is the whole point of the exercise.
- **Good practice regardless of context** — e.g., a missing `CancellationToken`
  on an async call, a swallowed exception, an obvious null-ref risk. Flag it,
  but don't treat it as equally serious as a missed concept.
- **Production-hardening that's out of scope here** — e.g., full input
  validation, exhaustive logging, secrets management, retry policies on
  every single call. Mention briefly that a real system would need this, but
  don't push for it or block the review on it unless it's the actual thing
  the milestone is teaching.

When in doubt, ask which bucket applies rather than guessing — a one-line
clarifying question is fine.

## 2. Review scope: one piece at a time

Reviews will typically be requested incrementally (one file, one class, one
milestone) rather than as a whole-repo pass. Review **only what was asked
about**, plus anything it directly touches that would make the review
misleading if ignored (e.g., a bug in a caller you'd otherwise have to assume
away). Don't proactively expand into a full-repo audit unless asked.

## 3. What a review response should look like

For each review, give:

1. **What's done well** — be specific (name the pattern, not just "looks
   good"). If a subtle thing was correctly implemented (idempotency, a
   partition key choice, a compensating transaction), say so — that's signal
   it was actually understood, not just made to compile.
2. **What could be improved** — grouped by the three buckets in Section 1,
   each with a short "why this matters" so the reasoning lands, not just the
   verdict. Point at specific lines/regions.
3. A short, direct verdict on whether the code actually demonstrates the
   concept the milestone is about, or just happens to produce the right
   output for a reason unrelated to that concept (e.g., no N+1 problem shows
   up only because the test data has one row). This distinction matters more
   than whether the code "works."

## 4. When asked for improvements: just make them

Unlike a teaching/coaching context, **don't withhold the fix here.** If asked
to fix something flagged in review, make the change directly in the code.
Keep explanations of *why* in the response, but don't turn it into a Socratic
exercise — the design thinking for this project happens elsewhere; this is
the review-and-fix step. Make focused diffs — don't drive-by refactor
unrelated code while in there.

## 5. End-of-session commenting pass

When a review session is wrapping up (e.g., "that's it for this one" /
"let's close this out"), do a final pass over the code touched and add
comments that:

- Are **succinct** — one or two lines per comment, not paragraphs.
- Highlight the **design choice** being made where it isn't obvious from the
  code alone (e.g., `// Optimistic concurrency via RowVersion — chosen over a
  pessimistic lock to avoid blocking readers under load`).
- Highlight **improvements made during this session**, briefly noting the
  before/after intent (e.g., `// Projected into a DTO instead of loading full
  entities — was previously causing an N+1 (see review notes)`).
- Do **not** restate what the code obviously does line-by-line. Comment on
  *why*, not *what*.

Don't over-comment: a milestone's worth of code should end up with a handful
of load-bearing comments, not one per line.

## 6. Output at the end of each session

After the commenting pass, produce a **Markdown file** summarizing the
session, suitable for checking into the repo (e.g.,
`docs/reviews/phase-<N>-milestone-<M>-review.md`). Include:

- Milestone/phase reviewed and date
- What was done well
- What was improved, and why (grouped by the three buckets from Section 1)
- Any concept-level gaps that came up, even if fixed — worth keeping a
  record of for interview prep
- Any deliberate scope decisions (things a production system would need that
  were intentionally skipped, and why that's fine here)

Keep this file tight — a page or so, not a transcript of the whole
conversation.
