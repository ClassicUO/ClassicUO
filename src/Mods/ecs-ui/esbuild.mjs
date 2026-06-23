import esbuild from 'esbuild';

// Bundle React + the reconciler + the guest entry into ONE ESM module.
// jco/componentize-js (StarlingMonkey) wants ESM with the world's exports as
// named exports (setup/uiStartup/uiTick). The Extism build used CJS for quickjs;
// StarlingMonkey is full SpiderMonkey, so ESM + es2022 is fine.
esbuild.build({
  entryPoints: ['src/index.tsx'],
  outfile: 'dist/index.js',
  tsconfig: 'tsconfig.json',
  bundle: true,
  sourcemap: false,
  minify: false,
  format: 'esm',
  platform: 'neutral',
  target: ['es2022'],
  jsx: 'automatic',
  jsxImportSource: 'react',
  // WIT-imported interfaces are provided by the host at instantiate time — jco
  // resolves them, esbuild must NOT try to bundle them.
  external: ['tinyecs:modding/*', 'cuo:modding/*'],
  define: {
    'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV ?? 'production'),
  },
});
