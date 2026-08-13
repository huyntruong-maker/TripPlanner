# React / TypeScript Style

Conventions for the React (TypeScript) frontend.

- `strict: true` in tsconfig; no `any`. Type all props, state, and event handlers.
- Function components only; name in PascalCase, files match the component name.
- Co-locate component, test, and styles in a folder with an `index.ts` barrel.
- Follow the Rules of Hooks: call hooks at the top level, never conditionally; complete
  dependency arrays (rely on `eslint-plugin-react-hooks`).
- Custom hooks start with `use`; extract reusable stateful logic into them.
- Prefer discriminated unions over multiple boolean flags for component variants.
- Keep components small and presentational; push data fetching to query hooks / containers.
- Server state via TanStack Query; client state via Zustand/Redux Toolkit. Do not duplicate
  server data into a global store.
- Forms with react-hook-form + zod; infer types from the zod schema.
- Derive UI from props/state; memoize (`useMemo`/`useCallback`/`memo`) only on profiling evidence.
- Accessibility is mandatory: semantic HTML, labels, keyboard focus, WCAG 2.1 AA contrast.
- Use design tokens, not inline hex/pixel magic values.
- Run ESLint + Prettier and `tsc --noEmit` before committing.

Typed, accessible, small components.
