import esbuild from 'esbuild';

esbuild.build({
  entryPoints: ['src/index.tsx'],
  outdir: 'dist',
  bundle: true,
  sourcemap: true,
  minify: false, // might want to use true for production build
  format: 'cjs', // needs to be CJS for now
  platform: 'neutral',
  target: ['es2020'], // don't go over es2020 because quickjs doesn't support it
  external: [], // We're bundling everything for now since React will be minimal
  jsx: 'automatic', // Enable JSX support
  jsxImportSource: 'react', // Use React as JSX import source
  define: {
    'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV),
  },
});
