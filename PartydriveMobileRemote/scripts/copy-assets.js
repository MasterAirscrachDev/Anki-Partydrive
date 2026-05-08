'use strict';
// Copies car and ability icon PNGs from the Unity Assets folder into
// public/cars/ and public/abilityicons/ so that pkg bundles them.

const fs   = require('fs');
const path = require('path');

const root = path.join(__dirname, '..');

const copies = [
    {
        src:  path.join(root, '..', 'Assets', 'Textures', 'Cars'),
        dest: path.join(root, 'public', 'cars'),
    },
    {
        src:  path.join(root, '..', 'Assets', 'Textures', 'AbilityIcons'),
        dest: path.join(root, 'public', 'abilityicons'),
    },
];

for (const { src, dest } of copies) {
    if (!fs.existsSync(src)) {
        console.warn(`[copy-assets] Source not found, skipping: ${src}`);
        continue;
    }
    fs.mkdirSync(dest, { recursive: true });
    let count = 0;
    for (const file of fs.readdirSync(src)) {
        if (!file.toLowerCase().endsWith('.png')) continue;
        fs.copyFileSync(path.join(src, file), path.join(dest, file));
        count++;
    }
    console.log(`[copy-assets] Copied ${count} PNGs  ${src} → ${dest}`);
}

// Copy public/ web files (html/css/js) next to each build output so that the
// pkg executable can serve them from the real filesystem at runtime.
const publicSrc = path.join(root, 'public');
const buildOutputDirs = [
    path.join(root, '..', 'BUILDS'),           // Windows exe
    path.join(root, '..', 'BUILDS', 'Server'), // Linux binary
];
const webExts = new Set(['.html', '.css', '.js', '.ico', '.png', '.svg', '.json']);

function copyDir(src, dest) {
    fs.mkdirSync(dest, { recursive: true });
    let count = 0;
    for (const entry of fs.readdirSync(src, { withFileTypes: true })) {
        const srcPath  = path.join(src,  entry.name);
        const destPath = path.join(dest, entry.name);
        if (entry.isDirectory()) {
            count += copyDir(srcPath, destPath);
        } else if (webExts.has(path.extname(entry.name).toLowerCase())) {
            fs.copyFileSync(srcPath, destPath);
            count++;
        }
    }
    return count;
}

for (const outDir of buildOutputDirs) {
    if (!fs.existsSync(outDir)) continue;
    const destPublic = path.join(outDir, 'public');
    const n = copyDir(publicSrc, destPublic);
    console.log(`[copy-assets] Copied ${n} web files  ${publicSrc} → ${destPublic}`);
}
