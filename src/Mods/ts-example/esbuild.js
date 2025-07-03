const esbuild = require("esbuild");
// include this if you need some node support:
// npm i @esbuild-plugins/node-modules-polyfill --save-dev
// const { NodeModulesPolyfillPlugin } = require('@esbuild-plugins/node-modules-polyfill')

esbuild.build({
  // supports other types like js or ts
  entryPoints: ["src/index.ts"],
  outdir: "dist",
  bundle: true,
  sourcemap: true,
  //plugins: [NodeModulesPolyfillPlugin()], // include this if you need some node support
  minify: false, // might want to use true for production build
  format: "cjs", // needs to be CJS for now
  platform: "neutral",
  target: ["es2020"], // don't go over es2020 because quickjs doesn't support it
});
