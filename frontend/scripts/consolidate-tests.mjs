/**
 * One-off: moves every *.test.ts(x) file into a single src/test/ tree that mirrors the source
 * layout, alongside the existing shared test infrastructure (msw, buildFakeJwt, route helpers).
 * Rewrites relative imports so specifiers still resolve. Run from `frontend/`:
 *
 *   node scripts/consolidate-tests.mjs [--apply]
 *
 * Without --apply it only reports what it would do.
 */
import { execSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const SRC = 'src';
const apply = process.argv.includes('--apply');

/** oldPathRelativeToRepoFrontend -> newPath */
const MOVES = {};

function move(from, to) {
  MOVES[path.posix.normalize(from)] = path.posix.normalize(to);
}

// ---- shared modules: colocated tests -> src/test/<mirror> -----------------
move(`${SRC}/api/client.test.ts`, `${SRC}/test/api/client.test.ts`);
move(`${SRC}/api/errors.test.ts`, `${SRC}/test/api/errors.test.ts`);
move(`${SRC}/auth/AuthContext.test.tsx`, `${SRC}/test/auth/AuthContext.test.tsx`);
move(`${SRC}/auth/ProtectedRoute.test.tsx`, `${SRC}/test/auth/ProtectedRoute.test.tsx`);
move(`${SRC}/auth/jwt.test.ts`, `${SRC}/test/auth/jwt.test.ts`);
move(`${SRC}/components/AppHeader.test.tsx`, `${SRC}/test/components/AppHeader.test.tsx`);
move(
  `${SRC}/components/toast/ToastProvider.test.tsx`,
  `${SRC}/test/components/toast/ToastProvider.test.tsx`,
);
move(`${SRC}/queryClient.test.tsx`, `${SRC}/test/queryClient.test.tsx`);

// ---- features/auth ----------------------------------------------------
for (const page of ['LoginPage', 'RegisterPage', 'VerifyEmailPage']) {
  move(
    `${SRC}/features/auth/__tests__/${page}.test.tsx`,
    `${SRC}/test/features/auth/${page}.test.tsx`,
  );
}

// ---- features/destinations ---------------------------------------------
const D = `${SRC}/features/destinations/__tests__`;
const DT = `${SRC}/test/features/destinations`;
for (const t of [
  'AttractionCard.test.tsx',
  'DestinationDetailPage.test.tsx',
  'DiscoverBackNavigation.test.tsx',
  'SearchPage.test.tsx',
]) {
  move(`${D}/${t}`, `${DT}/${t}`);
}
move(`${D}/humanizeKind.test.ts`, `${DT}/humanizeKind.test.ts`);

// ---- features/trips ------------------------------------------------------
const T = `${SRC}/features/trips/__tests__`;
const TT = `${SRC}/test/features/trips`;
for (const t of [
  'AddToTripControl.test.tsx',
  'TripPlannerPage.test.tsx',
  'TripsPage.test.tsx',
  'useMoveTripDestination.test.tsx',
]) {
  move(`${T}/${t}`, `${TT}/${t}`);
}
for (const t of ['dragDrop.test.ts', 'moveDestination.test.ts']) {
  move(`${T}/${t}`, `${TT}/${t}`);
}

// -------------------------------------------------------------------------

function allSourceFiles(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.posix.join(dir, entry.name);
    if (entry.isDirectory()) return allSourceFiles(full);
    return /\.tsx?$/.test(entry.name) ? [full] : [];
  });
}

/** Resolve a relative specifier from `fromDir` to an on-disk file path, trying TS extensions. */
function resolveSpecifier(fromDir, spec, existing) {
  const base = path.posix.normalize(path.posix.join(fromDir, spec));
  for (const candidate of [base, `${base}.ts`, `${base}.tsx`, `${base}/index.ts`, `${base}/index.tsx`]) {
    if (existing.has(candidate)) return candidate;
  }
  return null;
}

const before = new Set(allSourceFiles(SRC));
// Idempotent: a prior partial run may have already moved some files (fs.existsSync since `to`
// won't be in `before` until this pass re-walks the tree).
for (const from of Object.keys(MOVES)) {
  if (!before.has(from) && !fs.existsSync(MOVES[from])) {
    throw new Error(`Move source missing: ${from}`);
  }
  if (!before.has(from)) delete MOVES[from];
}

// Where every file ends up (unmoved files map to themselves).
const finalPathOf = new Map();
for (const file of before) finalPathOf.set(file, MOVES[file] ?? file);

const edits = [];
for (const oldPath of before) {
  const newPath = finalPathOf.get(oldPath);
  const code = fs.readFileSync(oldPath, 'utf8');
  const oldDir = path.posix.dirname(oldPath);
  const newDir = path.posix.dirname(newPath);

  const rewritten = code.replace(
    /(from\s+|import\s+|vi\.mock\(\s*)(['"])(\.[^'"]*)\2/g,
    (whole, prefix, quote, spec) => {
      const targetOld = resolveSpecifier(oldDir, spec, before);
      if (!targetOld) {
        if (oldDir !== newDir) edits.push({ file: oldPath, unresolved: spec });
        return whole;
      }
      const targetNew = finalPathOf.get(targetOld);
      let next = path.posix.relative(newDir, targetNew).replace(/\.tsx?$/, '');
      if (!next.startsWith('.')) next = `./${next}`;
      return `${prefix}${quote}${next}${quote}`;
    },
  );

  if (rewritten !== code || oldPath !== newPath) {
    edits.push({ file: oldPath, to: newPath, changed: rewritten !== code, content: rewritten });
  }
}

const unresolved = edits.filter((e) => e.unresolved);
if (unresolved.length) {
  console.error('Unresolved specifiers:', unresolved);
  process.exit(1);
}

console.log(`files touched: ${edits.length}  (moves: ${Object.keys(MOVES).length})`);
if (!apply) {
  for (const e of edits.slice(0, 200)) {
    console.log(`${e.changed ? 'R' : ' '}${e.to !== e.file ? 'M' : ' '}  ${e.file}${e.to !== e.file ? ` -> ${e.to}` : ''}`);
  }
  process.exit(0);
}

for (const e of edits) {
  if (e.to !== e.file) {
    fs.mkdirSync(path.posix.dirname(e.to), { recursive: true });
    execSync(`git mv "${e.file}" "${e.to}"`, { stdio: 'pipe' });
  }
  fs.writeFileSync(e.to, e.content);
}
console.log('applied');
