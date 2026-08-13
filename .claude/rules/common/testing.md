# Testing (Common)

- Every behavior change ships with tests; bug fixes start with a failing test.
- Follow the test pyramid: many fast unit tests, fewer integration, few E2E.
- Tests must be deterministic: no reliance on time, order, network, or shared state.
- Name tests for behavior (`Method_State_ExpectedResult`); one logical assertion focus.
- Use real dependencies for integration tests (Testcontainers) over fakes that hide bugs.
- Mock only collaborators you own; never assert solely on mock internals.
- Cover the unhappy paths: validation failures, errors, empty and boundary inputs.
- Keep tests fast enough to run on every commit; quarantine and fix flaky tests immediately.
- Coverage is a signal, not a goal; do not write assertion-free tests to inflate it.
- CI runs the full suite; PRs do not merge with failing or skipped-without-reason tests.

If it is not tested, assume it is broken.
