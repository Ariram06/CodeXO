import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import sharp from 'sharp';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const ICONS_DIR = path.resolve(__dirname, '../public/icons');
const SIZES = [16, 32, 48, 128];

// Geometric Codexo Mark: refined solid lightning bolt in orange (#D97757) on near-black (#191918) rounded square
const SVG_MARK = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="128" height="128">
  <rect width="128" height="128" rx="28" fill="#191918"/>
  <path d="M72 16L36 68H64L54 112L92 56H64L72 16Z" fill="#D97757"/>
</svg>`;

async function generate() {
  if (!fs.existsSync(ICONS_DIR)) {
    fs.mkdirSync(ICONS_DIR, { recursive: true });
  }

  // Save the master SVG file
  fs.writeFileSync(path.join(ICONS_DIR, 'codexo-mark.svg'), SVG_MARK, 'utf-8');
  console.log('Saved codexo-mark.svg');

  for (const size of SIZES) {
    const outputPath = path.join(ICONS_DIR, `icon-${size}.png`);
    await sharp(Buffer.from(SVG_MARK))
      .resize(size, size)
      .png()
      .toFile(outputPath);
    console.log(`Generated icon-${size}.png (${size}x${size})`);
  }

  console.log('Icon generation complete.');
}

generate().catch((err) => {
  console.error('Error generating icons:', err);
  process.exit(1);
});
