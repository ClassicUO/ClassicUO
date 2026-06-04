import esbuild from 'esbuild';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import path from 'node:path';
import fs from 'node:fs';

const here = path.dirname(fileURLToPath(import.meta.url));
const modSrc = path.resolve(here, '..', 'src');
const req = createRequire(path.join(here, 'package.json'));

// Force a SINGLE react/react-dom copy (reference/node_modules). Mod files under
// ../src would otherwise pull the mod's own node_modules/react -> two React
// instances -> "Invalid hook call".
const reactPlugin = {
  name: 'single-react',
  setup(build) {
    build.onResolve({ filter: /^react(-dom)?(\/.*)?$/ }, (args) => ({ path: req.resolve(args.path) }));
  },
};

// Alias `~/react` -> our DOM shim (so the REAL design-system components render
// through react-dom), and every other `~/x` -> the mod's real src (pure enums,
// theme, types). Order matters: the `~/react` rule must win over the generic one.
const EXTS = ['.tsx', '.ts', '.jsx', '.js'];
function resolveReal(base) {
  for (const e of EXTS) if (fs.existsSync(base + e)) return base + e;
  for (const e of EXTS) {
    const idx = path.join(base, 'index' + e);
    if (fs.existsSync(idx)) return idx;
  }
  if (fs.existsSync(base)) return base;
  return base; // let esbuild report a clear error
}

const aliasPlugin = {
  name: 'tilde-alias',
  setup(build) {
    build.onResolve({ filter: /^~\/react$/ }, () => ({
      path: path.resolve(here, 'clay-dom.tsx'),
    }));
    build.onResolve({ filter: /^~\// }, (args) => ({
      path: resolveReal(path.resolve(modSrc, args.path.slice(2))),
    }));
  },
};

fs.mkdirSync(path.resolve(here, 'dist'), { recursive: true });
fs.copyFileSync(path.resolve(here, 'index.html'), path.resolve(here, 'dist', 'index.html'));

await esbuild.build({
  entryPoints: [path.resolve(here, 'app.tsx')],
  outfile: path.resolve(here, 'dist', 'app.js'),
  bundle: true,
  sourcemap: true,
  format: 'iife',
  platform: 'browser',
  target: ['es2020'],
  jsx: 'automatic',
  jsxImportSource: 'react',
  loader: { '.tsx': 'tsx', '.ts': 'ts' },
  resolveExtensions: ['.tsx', '.ts', '.jsx', '.js'],
  plugins: [reactPlugin, aliasPlugin],
  define: { 'process.env.NODE_ENV': JSON.stringify('development') },
});

console.log('reference build ok -> dist/app.js');
