---
name: react-data-and-state
description: Guides agents through managing server and client state in React with TanStack Query, Zustand/Redux Toolkit, and react-hook-form + zod. Use when fetching data, caching, handling forms, or structuring app state.
---

# React Data and State

## Overview
A clear split between server state (TanStack Query) and client state (Zustand or Redux
Toolkit), with typed forms via react-hook-form + zod. Avoid duplicating server data into
global stores.

## When to Use
- Fetching, caching, or mutating API data.
- Building forms with validation.
- Deciding where a piece of state should live.

## Process

1. Separate the two kinds of state:
   - Server state (remote, cached, can go stale) -> TanStack Query.
   - Client state (UI toggles, wizard step, selection) -> Zustand or Redux Toolkit.
   Do not copy server data into a global store.

2. Fetch reads with `useQuery`; key by resource + params.
   ```tsx
   const { data, isPending, error } = useQuery({
     queryKey: ['orders', { status }],
     queryFn: ({ signal }) => api.getOrders(status, signal),
   });
   ```

3. Mutate with `useMutation` and invalidate affected queries on success.
   ```tsx
   const m = useMutation({
     mutationFn: api.createOrder,
     onSuccess: () => qc.invalidateQueries({ queryKey: ['orders'] }),
   });
   ```

4. Use optimistic updates for snappy UX; always provide rollback.
   ```tsx
   onMutate: async (next) => {
     await qc.cancelQueries({ queryKey: ['orders'] });
     const prev = qc.getQueryData(['orders']);
     qc.setQueryData(['orders'], (o) => [...o, next]);
     return { prev };
   },
   onError: (_e, _v, ctx) => qc.setQueryData(['orders'], ctx.prev),
   ```

5. Tune caching: set `staleTime` to reduce refetch noise; rely on background refetch.

6. Keep client state minimal and typed with Zustand; one store per domain slice.
   ```tsx
   const useCart = create<CartState>((set) => ({
     items: [], add: (i) => set((s) => ({ items: [...s.items, i] })),
   }));
   ```

7. Use Redux Toolkit when you need middleware, time-travel devtools, or complex
   cross-cutting flows; create slices with `createSlice`, avoid hand-rolled reducers.

8. Build forms with react-hook-form; validate with a zod schema and `zodResolver`.
   ```tsx
   const schema = z.object({ email: z.string().email() });
   const { register, handleSubmit, formState } =
     useForm({ resolver: zodResolver(schema) });
   ```

9. Derive the TypeScript type from the zod schema (`z.infer`) so form and API stay in sync.

10. Surface query/mutation `error` and `isPending` in the UI; never swallow failures.

## Rationalizations
| Excuse | Reality |
| --- | --- |
| "Put API data in Redux too." | Duplicates source of truth; cache it in Query. |
| "useEffect + fetch is simpler." | Reinvents caching, retries, dedupe poorly. |
| "Validate forms by hand." | zod gives one schema + types + messages. |
| "Skip rollback on optimistic." | A failed mutation leaves the UI lying. |
| "Global store for one toggle." | Use local `useState`; lift only when shared. |

## Red Flags
- Server data copied into Zustand/Redux and manually synced.
- `useEffect`-based fetching with ad-hoc loading flags.
- Forms with manual `onChange` validation and no schema.
- Optimistic updates without `onError` rollback.
- Query keys that are strings without their parameters.

## Verification
- Reads use `useQuery`; mutations invalidate or update the cache.
- No server data mirrored into a global client store.
- Forms validate via zod; types inferred from the schema.
- Optimistic paths roll back on error (test by forcing a 500).
- Loading and error states visible for every data surface.
