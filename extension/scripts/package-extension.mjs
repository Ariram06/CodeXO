import fs from 'node:fs';
import path from 'node:path';
import { execSync } from 'node:child_process';

const distDir = path.resolve('dist/codexo');
const outputZip = path.resolve('dist/codexo-extension-v0.1.0.zip');

if (!fs.existsSync(distDir)) {
  console.error(`[Error] dist/codexo directory does not exist. Run 'npm run build' first.`);
  process.exit(1);
}

// Verify required extension assets exist
const requiredAssets = [
  'manifest.json',
  'background/service-worker.js',
  'content/content-script.js',
  'sidepanel/browser/index.html',
];

for (const asset of requiredAssets) {
  const assetPath = path.join(distDir, asset);
  if (!fs.existsSync(assetPath)) {
    console.error(`[Error] Missing required extension asset: ${asset}`);
    process.exit(1);
  }
}

if (fs.existsSync(outputZip)) {
  fs.unlinkSync(outputZip);
}

console.log('[Codexo] Packaging extension zip artifact...');

try {
  if (process.platform === 'win32') {
    execSync(
      `powershell -NoProfile -Command "Compress-Archive -Path '${distDir}/*' -DestinationPath '${outputZip}' -Force"`,
      { stdio: 'inherit' }
    );
  } else {
    execSync(`cd "${distDir}" && zip -r "${outputZip}" .`, {
      stdio: 'inherit',
    });
  }

  const stats = fs.statSync(outputZip);
  console.log(`[Codexo] Extension packaged successfully: ${outputZip} (${(stats.size / 1024).toFixed(1)} KB)`);
} catch (err) {
  console.error('[Error] Failed to create extension zip artifact:', err);
  process.exit(1);
}
