# React Testing Rules

- Test with Vitest (or Jest) + React Testing Library; query by role/label/text, not test-ids
  where a semantic query exists.
- Test behavior from the user's perspective; do not assert on implementation details/state.
- Use `userEvent` for interactions; `findBy*` for async UI.
- Mock the network at the boundary with MSW; do not mock React Query itself.
- Cover loading, empty, error, and success states for data-driven components.
- Validate forms: invalid input shows messages, valid input submits expected payload.
- Assert accessibility basics: roles, labels, and focus management; consider axe checks.
- Keep tests deterministic: fake timers for debounce/animations, no real network.
- Name tests by behavior; one user-facing assertion focus per test.
- `npm test`, `npm run typecheck`, and `npm run lint` must pass before merge.

Test what the user experiences, not the internals.
