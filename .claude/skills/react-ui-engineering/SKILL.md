---
name: react-ui-engineering
description: Guides agents through building React + TypeScript UIs with strong component architecture, accessibility, and responsive design. Use when creating components, design-system work, or front-end UI in a Vite/Next.js app.
---

# React UI Engineering

## Overview
Standards for accessible, responsive React (TypeScript) UIs: component architecture, typed
props, design-system tokens, responsive layout, and WCAG 2.1 AA compliance on Vite or Next.js.

## When to Use
- Building or refactoring React components and pages.
- Establishing or following a design system.
- Auditing accessibility or responsiveness.

## Process

1. Compose small, single-responsibility components. Separate presentational components
   from container/data components. Co-locate component, styles, and test.
   ```
   components/Button/{Button.tsx, Button.test.tsx, index.ts}
   ```

2. Type every prop explicitly; avoid `any`. Prefer discriminated unions over boolean soup.
   ```tsx
   type ButtonProps = {
     variant: 'primary' | 'secondary';
     onClick: () => void;
     children: React.ReactNode;
   };
   ```

3. Consume design tokens (colors, spacing, type scale) instead of hard-coded values.
   Use CSS variables or a theme object; never magic hex codes inline.

4. Build mobile-first, responsive layouts with CSS grid/flex and container/media queries.
   Test at 320px, 768px, 1280px.

5. Make it accessible by default (WCAG 2.1 AA):
   - Semantic HTML first (`button`, `nav`, `main`), ARIA only to fill gaps.
   - Every interactive element keyboard-reachable with visible focus.
   - Labels tied to inputs; images have `alt`; contrast >= 4.5:1.
   ```tsx
   <label htmlFor="email">Email</label>
   <input id="email" type="email" aria-invalid={!!error} />
   ```

6. Keep components pure; derive UI from props/state. Lift state only as far as needed.
   Avoid prop drilling beyond ~2 levels (use context or a state library).

7. Memoize deliberately: `useMemo`/`useCallback`/`React.memo` only where profiling shows
   re-render cost. Premature memoization adds noise.

8. Handle loading, empty, and error states for every async surface. No silent blank screens.

9. Split code by route; lazy-load heavy components with `React.lazy`/dynamic import.

10. On Next.js, prefer Server Components for data-heavy reads; mark interactive leaves
    `'use client'`. On Vite SPA, keep bundles lean and route-split.

## Rationalizations
| Excuse | Reality |
| --- | --- |
| "A div with onClick works." | It is not keyboard/AT accessible; use a button. |
| "I'll add types later." | Untyped props rot fast; type at creation. |
| "Memoize everything to be safe." | Adds complexity; memoize on evidence. |
| "Hard-code the color this once." | Tokens prevent drift across the design system. |
| "Skip the loading state." | Users see jank; always handle async states. |

## Red Flags
- `any` props or untyped event handlers.
- Click handlers on non-interactive elements.
- Inline hex colors / pixel values bypassing tokens.
- No focus styles, missing labels, low contrast.
- Components over ~200 lines doing data + layout + logic.

## Verification
- `tsc --noEmit` passes; no `any`.
- Keyboard-only navigation reaches all controls with visible focus.
- Axe/Lighthouse accessibility audit shows no AA violations.
- Layout holds at 320/768/1280px with no overflow.
- Loading, empty, and error states render for async views.
