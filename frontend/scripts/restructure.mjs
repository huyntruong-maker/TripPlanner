/**
 * One-off: moves source files into feature-sliced folders and rewrites every relative import so
 * the specifiers still resolve. Run from `frontend/`:  node scripts/restructure.mjs [--apply]
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

// ---- features/auth -------------------------------------------------------
for (const page of ['LoginPage', 'RegisterPage', 'VerifyEmailPage']) {
  move(`${SRC}/features/auth/${page}.tsx`, `${SRC}/features/auth/pages/${page}.tsx`);
  move(`${SRC}/features/auth/${page}.test.tsx`, `${SRC}/features/auth/__tests__/${page}.test.tsx`);
}
move(`${SRC}/features/auth/schemas.ts`, `${SRC}/features/auth/lib/schemas.ts`);

// ---- features/destinations ----------------------------------------------
const D = `${SRC}/features/destinations`;
for (const page of ['SearchPage', 'DestinationDetailPage']) {
  move(`${D}/${page}.tsx`, `${D}/pages/${page}.tsx`);
}
for (const comp of ['AttractionCard', 'MapView', 'PhotoCarousel']) {
  move(`${D}/${comp}.tsx`, `${D}/components/${comp}.tsx`);
}
for (const hook of ['useAttractions', 'useLocationSearch']) {
  move(`${D}/${hook}.ts`, `${D}/hooks/${hook}.ts`);
}
// Only SearchPage consumes it, so it belongs to this feature rather than a global hooks bucket.
move(`${SRC}/hooks/useDebouncedValue.ts`, `${D}/hooks/useDebouncedValue.ts`);
for (const lib of ['humanizeKind', 'discoverSearchStorage']) {
  move(`${D}/${lib}.ts`, `${D}/lib/${lib}.ts`);
}
for (const t of ['AttractionCard.test.tsx', 'DestinationDetailPage.test.tsx', 'SearchPage.test.tsx', 'DiscoverBackNavigation.test.tsx']) {
  move(`${D}/${t}`, `${D}/__tests__/${t}`);
}
move(`${D}/humanizeKind.test.ts`, `${D}/__tests__/humanizeKind.test.ts`);

// ---- features/trips ------------------------------------------------------
const T = `${SRC}/features/trips`;
for (const page of ['TripPlannerPage', 'TripsPage']) {
  move(`${T}/${page}.tsx`, `${T}/pages/${page}.tsx`);
}
for (const comp of ['AddToTripControl', 'QuickSaveControl']) {
  move(`${T}/${comp}.tsx`, `${T}/components/${comp}.tsx`);
}
for (const hook of ['useTrip', 'useTrips', 'useMoveTripDestination']) {
  move(`${T}/${hook}.ts`, `${T}/hooks/${hook}.ts`);
}
for (const lib of ['dragDrop', 'moveDestination', 'schemas']) {
  move(`${T}/${lib}.ts`, `${T}/lib/${lib}.ts`);
}
for (const t of ['AddToTripControl.test.tsx', 'TripPlannerPage.test.tsx', 'TripsPage.test.tsx', 'useMoveTripDestination.test.tsx']) {
  move(`${T}/${t}`, `${T}/__tests__/${t}`);
}
for (const t of ['dragDrop.test.ts', 'moveDestination.test.ts']) {
  move(`${T}/${t}`, `${T}/__tests__/${t}`);
}

// ---- shared folders: tests into __tests__ so every folder reads the same --
move(`${SRC}/api/client.test.ts`, `${SRC}/api/__tests__/client.test.ts`);
move(`${SRC}/api/errors.test.ts`, `${SRC}/api/__tests__/errors.test.ts`);
move(`${SRC}/auth/AuthContext.test.tsx`, `${SRC}/auth/__tests__/AuthContext.test.tsx`);
move(`${SRC}/auth/ProtectedRoute.test.tsx`, `${SRC}/auth/__tests__/ProtectedRoute.test.tsx`);
move(`${SRC}/auth/jwt.test.ts`, `${SRC}/auth/__tests__/jwt.test.ts`);
move(`${SRC}/components/AppHeader.test.tsx`, `${SRC}/components/__tests__/AppHeader.test.tsx`);
move(`${SRC}/components/toast/ToastProvider.test.tsx`, `${SRC}/components/toast/__tests__/ToastProvider.test.tsx`);
move(`${SRC}/queryClient.test.tsx`, `${SRC}/__tests__/queryClient.test.tsx`);

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
for (const from of Object.keys(MOVES)) {
  if (!before.has(from)) throw new Error(`Move source missing: ${from}`);
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
        // Non-code imports (CSS, assets) aren't in the move table. Harmless while the importing
        // file stays put; if it moved, the path would silently break, so fail loudly instead.
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
