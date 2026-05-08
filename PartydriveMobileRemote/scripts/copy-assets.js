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
