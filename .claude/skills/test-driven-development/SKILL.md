---
name: test-driven-development
description: Guides agents through Red-Green-Refactor TDD, the test pyramid (~80/15/5), and DAMP-over-DRY test design. Use when writing or fixing tests, adding test coverage to a feature, or doing QA on a change. References xUnit (.NET) and Vitest/Testing Library (React) as concrete examples.
---

## Overview

Write the failing test first, make it pass minimally, then refactor under green. Favor a healthy pyramid (~80% unit, ~15% integration, ~5% end-to-end) and keep tests DAMP (Descriptive And Meaningful Phrases) over DRY — readable tests beat clever ones.

## When to Use

- Before or alongside implementing any behavior (see incremental-implementation).
- When fixing a bug — reproduce it with a failing test first.
- During QA to assess coverage and the shape of the pyramid.

## Process

1. Pick one behavior from the task's acceptance criteria.
2. Red: write a test that asserts the desired behavior and watch it fail for the right reason. Checkpoint: failure message matches the missing behavior.
3. Green: write the minimal code to pass. No extra features. Checkpoint: test passes, others still green.
4. Refactor: clean up code and test under green; rerun. Checkpoint: still green.
5. Place the test at the right pyramid level — push logic to fast unit tests; reserve integration for boundaries (DB, HTTP) and e2e for critical flows only.
6. Keep tests DAMP: explicit arrange/act/assert, descriptive names, minimal shared magic. Duplication in tests is acceptable for clarity.
7. Run the full suite before committing.

### Concrete examples

- .NET (xUnit): `[Fact]`/`[Theory]`, arrange-act-assert, `dotnet test`. Mock collaborators (Moq/NSubstitute); integration tests via `WebApplicationFactory`.
- React/TS (Vitest + Testing Library): `test()` / `expect()`, render and query by role/text, assert user-visible behavior not implementation. Run `npx vitest run`. E2E via Playwright for critical journeys.

## Rationalizations

| Excuse | Rebuttal |
|---|---|
| "I'll add tests after." | After means never; you also lose the design feedback. |
| "DRY tests are cleaner." | Over-DRY tests hide intent; DAMP tests fail readably. |
| "Just write an e2e test." | E2e is slow and flaky; push coverage down the pyramid. |
| "It's too simple to test." | Then the test is trivial to write — write it. |

## Red Flags

- Tests written only after the code, asserting whatever the code happens to do.
- An inverted pyramid (mostly slow e2e tests).
- Tests coupled to implementation details, breaking on safe refactors.
- A bug fix with no regression test.

## Verification

- New/changed behavior has tests that failed before the fix and pass after.
- Full suite is green; output captured as evidence (`dotnet test` / `vitest run`).
- Coverage sits at the right pyramid level; no inverted pyramid.
- Bug fixes include a regression test.
