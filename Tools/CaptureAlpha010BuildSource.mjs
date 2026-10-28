import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const [cache, build, label] = process.argv.slice(2);
if (!cache || !build || !/^[A-Za-z0-9-]+$/.test(label ?? '')) throw Error('Pass validation project, build directory and fresh label');
const root = path.resolve('.');
const output = path.join(root, 'Docs/Validation/Alpha-v0.0.10', `build-source-${label}.json`);
const snapshot = path.join(root, 'Temp', `Alpha010-${label}-Source`);
if (fs.existsSync(output) || fs.existsSync(snapshot)) throw Error('Choose a fresh label');
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex').toUpperCase();
const files = [];
function walk(relative) {
  for (const entry of fs.readdirSync(path.join(cache, relative), { withFileTypes: true })) {
    const file = `${relative}/${entry.name}`;
    if (entry.isDirectory()) walk(file);
    else files.push({ file, sha256: hash(path.join(cache, file)) });
  }
}
for (const folder of ['Assets', 'Packages', 'ProjectSettings']) walk(folder);
fs.mkdirSync(snapshot, { recursive: true });
for (const folder of ['Assets', 'Packages', 'ProjectSettings']) fs.cpSync(path.join(cache, folder), path.join(snapshot, folder), { recursive: true });
const authoringDifferences = files.filter(entry => /^Assets\/(Scripts|Shaders)\//.test(entry.file) && /\.(cs|shader|hlsl|asmdef)$/.test(entry.file) && (!fs.existsSync(entry.file) || hash(entry.file) !== entry.sha256)).map(entry => entry.file);
const manifest = {
  capturedUtc: new Date().toISOString(), validationProject: path.resolve(cache), buildDirectory: path.resolve(build), snapshot,
  build: JSON.parse(fs.readFileSync(path.join(build, 'build-info.json'), 'utf8').replace(/^\uFEFF/, '')),
  worldSha256: hash(path.join(build, 'Aevoria_Data/Managed/Aevoria.World.dll')),
  presentationSha256: hash(path.join(build, 'Aevoria_Data/Managed/Aevoria.Presentation.dll')),
  gitIndexSha256: hash('.git/index'), authoringDifferences,
  files: files.sort((a, b) => a.file.localeCompare(b.file)),
};
fs.writeFileSync(output, JSON.stringify(manifest, null, 2));
console.log(`Preserved ${files.length} built source/configuration files; ${authoringDifferences.length} source files have subsequent authoring changes.`);
