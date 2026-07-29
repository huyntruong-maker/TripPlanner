/**
 * Resolves every relative import under src/ and reports the ones that point nowhere.
 * A fast stand-in for `tsc --noEmit` when only module paths changed.
 *
 *   node scripts/check-imports.mjs
 */
import fs from 'node:fs';
import path from 'node:path';

const SRC = 'src';
const CODE_EXTENSIONS = ['', '.ts', '.tsx', '.d.ts', '/index.ts', '/index.tsx'];
// Imports Vite resolves but that aren't TypeScript modules.
const ASSET_PATTERN = /\.(css|svg|png|jpe?g|gif|webp|json)$/;

function walk(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.posix.join(dir, entry.name);
    if (entry.isDirectory()) return walk(full);
    return /\.tsx?$/.test(entry.name) ? [full] : [];
  });
}

const files = walk(SRC);
const broken = [];
let checked = 0;

for (const file of files) {
  const dir = path.posix.dirname(file);
  const code = fs.readFileSync(file, 'utf8');
  const specifiers = [...code.matchAll(/(?:from\s+|import\s+|vi\.mock\(\s*)(['"])(\.[^'"]*)\1/g)];

  for (const [, , spec] of specifiers) {
    checked += 1;
    const base = path.posix.normalize(path.posix.join(dir, spec));
    if (ASSET_PATTERN.test(spec)) {
      if (!fs.existsSync(base)) broken.push({ file, spec, kind: 'asset' });
      continue;
    }
    const found = CODE_EXTENSIONS.some((ext) => fs.existsSync(`${base}${ext}`));
    if (!found) broken.push({ file, spec, kind: 'module' });
  }
}

console.log(`files: ${files.length}  relative imports checked: ${checked}`);
if (broken.length === 0) {
  console.log('all relative imports resolve');
  process.exit(0);
}
console.error(`\nbroken (${broken.length}):`);
for (const b of broken) console.error(`  ${b.file}  ->  ${b.spec}  [${b.kind}]`);
process.exit(1);
