/**
 * Flags imported names that never appear again in the file. tsconfig sets `noUnusedLocals`, so
 * these fail `npm run build` — this catches them without waiting on a full `tsc`.
 *
 *   node scripts/check-unused-imports.mjs
 *
 * Heuristic (identifier occurrences outside the import statement), so treat hits as candidates.
 */
import fs from 'node:fs';
import path from 'node:path';

function walk(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.posix.join(dir, entry.name);
    if (entry.isDirectory()) return walk(full);
    return /\.tsx?$/.test(entry.name) ? [full] : [];
  });
}

const suspects = [];

for (const file of walk('src')) {
  const code = fs.readFileSync(file, 'utf8');
  const importStatements = [...code.matchAll(/^import\s+(?:type\s+)?([\s\S]*?)\s+from\s+['"][^'"]+['"];?$/gm)];

  for (const [statement, clause] of importStatements) {
    // Default / namespace / named bindings, minus `type` markers and `as` aliases.
    const names = [...clause.matchAll(/(?:^|[{,\s])(?:type\s+)?([A-Za-z_$][\w$]*)(?:\s+as\s+([A-Za-z_$][\w$]*))?/g)]
      .map((m) => m[2] ?? m[1])
      .filter((name) => name && name !== 'type' && name !== 'as');

    const withoutStatement = code.replace(statement, '');
    for (const name of new Set(names)) {
      const used = new RegExp(`\\b${name.replace(/\$/g, '\\$')}\\b`).test(withoutStatement);
      if (!used) suspects.push({ file, name });
    }
  }
}

if (suspects.length === 0) {
  console.log('no unused imports found');
  process.exit(0);
}
console.error(`possible unused imports (${suspects.length}):`);
for (const s of suspects) console.error(`  ${s.file}  ->  ${s.name}`);
process.exit(1);
