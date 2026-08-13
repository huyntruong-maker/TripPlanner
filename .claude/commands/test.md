---
description: Add or fix tests and QA a change using the test-driven-development skill (Red-Green-Refactor, test pyramid, DAMP).
argument-hint: "<feature, bug, or area to test>"
---

Apply the `test-driven-development` skill to: $ARGUMENTS

Use Red-Green-Refactor: write a failing test for the behavior, make it pass minimally, refactor under green. Keep a healthy pyramid (~80% unit / 15% integration / 5% e2e) and DAMP tests. For bugs, reproduce with a failing regression test first. Use xUnit for .NET (`dotnet test`) and Vitest + Testing Library for React (`npx vitest run`); reserve Playwright for critical e2e flows. Run the full suite before finishing.

Verification: new/changed behavior has tests that failed before and pass after, the full suite is green with output captured, coverage sits at the right pyramid level, and bug fixes include a regression test.
