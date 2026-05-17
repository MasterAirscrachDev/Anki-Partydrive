'use strict';
// Copies car and ability icon PNGs from the Unity Assets folder into
// public/cars/ and public/abilityicons/ (for dev use), and writes
// public/image-manifest.js with PNG data embedded as base64 so the pkg exe
// can serve images without any pkg.assets binary-glob issues.

const fs   = require('fs');
const path = require('path');

const root = path.join(__dirname, '..');

const copies = [
    {
        key:  'cars',
        src:  path.join(root, '..', 'Assets', 'Textures', 'Cars'),
        dest: path.join(root, 'public', 'cars'),
    },
    {
        key:  'abilityicons',
        src:  path.join(root, '..', 'Assets', 'Textures', 'AbilityIcons'),
        dest: path.join(root, 'public', 'abilityicons'),
    },
];

const imageData = {};

for (const { key, src, dest } of copies) {
    if (!fs.existsSync(src)) {
        console.warn(`[copy-assets] Source not found, skipping: ${src}`);
        imageData[key] = [];
        continue;
    }
    fs.mkdirSync(dest, { recursive: true });
    const files = [];
    for (const file of fs.readdirSync(src)) {
        if (!file.toLowerCase().endsWith('.png')) continue;
        const srcFile = path.join(src, file);
        fs.copyFileSync(srcFile, path.join(dest, file));
        files.push({ name: file, b64: fs.readFileSync(srcFile).toString('base64') });
    }
    imageData[key] = files;
    console.log(`[copy-assets] Copied ${files.length} PNGs  ${src} → ${dest}`);
}

// Emit a JS module with all PNG data embedded.
// require() is statically detected by pkg — no pkg.assets binary globs needed.
const lines = ["'use strict';", 'module.exports = {'];
for (const [key, files] of Object.entries(imageData)) {
    lines.push(`  ${key}: [`);
    for (const { name, b64 } of files)
        lines.push(`    { name: ${JSON.stringify(name)}, data: Buffer.from(${JSON.stringify(b64)}, 'base64') },`);
    lines.push('  ],');
}
lines.push('};');
fs.writeFileSync(path.join(root, 'public', 'image-manifest.js'), lines.join('\n') + '\n');
console.log('[copy-assets] Written public/image-manifest.js');

