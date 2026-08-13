---
name: code-review-and-quality
description: Guides agents through reviewing a diff across five axes with severity labels and sensible change sizing (~100-400 lines). Use when reviewing a PR, branch, or working diff for correctness, design, tests, security, and readability before it ships.
---

## Overview

A structured review across five axes, with each finding given a severity label so authors know what blocks merge. Keep review units sized ~100-400 lines; larger diffs get split or reviewed in passes.

## When to Use

- Reviewing a PR, branch, or staged diff before merge (see `/review`).
- Self-review before opening a PR.
- Assessing overall quality/tech-debt of a change set.

## Process

1. Confirm scope: the diff is ~100-400 lines and one logical change. If larger, ask to split or review in focused passes. Checkpoint: scope is reviewable.
2. Review across five axes, taking notes per axis:
   - Correctness: does it meet the spec/acceptance criteria? Edge cases, error paths.
   - Design: fits the architecture? Right abstraction, no needless coupling.
   - Tests: adequate coverage at the right pyramid level; tests actually assert behavior.
   - Security: input validation, authz/authn, secrets, injection, dependency risk.
   - Readability/maintainability: naming, clarity, comments where intent is non-obvious.
3. Label each finding by severity:
   - Blocker — must fix before merge (bug, security hole, missing tests for new behavior).
   - Major — should fix; significant design/quality issue.
   - Minor — fix if easy; small improvement.
   - Nit — optional/style.
4. Verify the change builds and tests pass (do not rely on author's word).
5. Summarize: overall verdict (approve / approve-with-nits / request changes) and the blocker list.

## Rationalizations

| Excuse | Rebuttal |
|---|---|
| "It's a big PR but it's all related." | 1000-line diffs hide bugs; split or review in passes. |
| "Author says tests pass." | Verify yourself; trust but confirm. |
| "Style nits aren't worth raising." | Label them Nit so they don't block but still get seen. |
| "Looks fine, ship it." | Walk all five axes; "looks fine" misses security + edge cases. |

## Red Flags

- Approving without running the build/tests.
- A review that only comments on style and ignores correctness/security.
- No severity labels, so the author can't tell what blocks merge.
- Diffs far over 400 lines reviewed in one undifferentiated pass.

## Verification

- All five axes were addressed with notes, not just style.
- Every finding carries a severity label; blockers are listed explicitly.
- Build + tests were run by the reviewer with output as evidence.
- A clear verdict is recorded (approve / approve-with-nits / request changes).
