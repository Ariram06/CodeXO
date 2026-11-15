/**
 * Codexo Extension Build Script
 *
 * Bundles the content script and service worker as separate entry points
 * using esbuild. These are NOT part of the Angular build — MV3 requires
 * them as standalone scripts (no inline scripts, no remote code).
 *
 * Usage: node scripts/build-extension.mjs
 */

import * as esbuild from "esbuild";
import { resolve, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = resolve(__dirname, "..");

const isWatch = process.argv.includes("--watch");

/** @type {import('esbuild').BuildOptions} */
const sharedOptions = {
  bundle: true,
  format: "esm",
  target: "chrome120",
  sourcemap: true,
  logLevel: "info",
};

const contentScriptOptions = {
  ...sharedOptions,
  entryPoints: [resolve(rootDir, "src/content/content-script.ts")],
  outfile: resolve(rootDir, "dist/codexo/content/content-script.js"),
  // Content scripts run in a page context — no module format needed
  format: "iife",
};

const serviceWorkerOptions = {
  ...sharedOptions,
  entryPoints: [resolve(rootDir, "src/background/service-worker.ts")],
  outfile: resolve(rootDir, "dist/codexo/background/service-worker.js"),
  format: "esm",
};

async function build() {
  try {
    if (isWatch) {
      const ctxContent = await esbuild.context(contentScriptOptions);
      const ctxWorker = await esbuild.context(serviceWorkerOptions);
      await Promise.all([ctxContent.watch(), ctxWorker.watch()]);
      console.log("[esbuild] Watching for changes...");
    } else {
      await Promise.all([
        esbuild.build(contentScriptOptions),
        esbuild.build(serviceWorkerOptions),
      ]);
      console.log("[esbuild] Extension scripts built successfully.");
    }
  } catch (error) {
    console.error("[esbuild] Build failed:", error);
    process.exit(1);
  }
}

build();
