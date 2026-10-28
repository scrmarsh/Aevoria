import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const [cache, build, label] = process.argv.slice(2);
if (!cache || !build || !/^[A-Za-z0-9-]+$/.test(label ?? '')) throw Error('Pass validation project, Windows build directory, and a unique label');
const root = path.resolve('.');
const output = path.join(root, 'Docs/Validation/Alpha-v0.0.9', `build-source-${label}.json`);
const snapshot = path.join(root, 'Temp', `Alpha009-${label}-Source`);
if (fs.existsSync(output) || fs.existsSync(snapshot)) throw Error('Choose a fresh source snapshot label');
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
for (const entry of files) {
  if (/^Assets\/(Scripts|Shaders)\//.test(entry.file) && /\.(cs|shader|hlsl|asmdef)$/.test(entry.file) || entry.file === 'Packages/manifest.json') {
    if (hash(path.join(root, entry.file)) !== entry.sha256) throw Error(`Authoring/runtime source mismatch: ${entry.file}`);
  }
}
for (const name of ['Changelog', 'Chronicles']) {
  const file = `Assets/Resources/History/${name}.txt`;
  if (hash(file) !== hash(path.join(cache, file))) throw Error(`Runtime history mismatch: ${name}`);
}
for (const file of ['CHANGELOG.md', 'Docs/THE_CHRONICLES_OF_AEVORIA.md']) {
  const earlier = fs.readFileSync(path.join(root, 'Temp/Alpha009Baseline', file));
  if (!fs.readFileSync(file).subarray(0, earlier.length).equals(earlier)) throw Error(`Earlier history changed: ${file}`);
}
fs.mkdirSync(snapshot, { recursive: true });
for (const folder of ['Assets', 'Packages', 'ProjectSettings']) fs.cpSync(path.join(cache, folder), path.join(snapshot, folder), { recursive: true });
const manifest = {
  capturedUtc: new Date().toISOString(), validationProject: path.resolve(cache), buildDirectory: path.resolve(build), snapshot,
  build: JSON.parse(fs.readFileSync(path.join(build, 'build-info.json'), 'utf8').replace(/^\uFEFF/, '')),
  worldSha256: hash(path.join(build, 'Aevoria_Data/Managed/Aevoria.World.dll')),
  presentationSha256: hash(path.join(build, 'Aevoria_Data/Managed/Aevoria.Presentation.dll')),
  gitIndexSha256: hash('.git/index'),
  files: files.sort((a, b) => a.file.localeCompare(b.file)),
};
fs.writeFileSync(output, JSON.stringify(manifest, null, 2));
console.log(`Preserved ${files.length} built source/configuration files; authoring code, runtime histories and earlier history prefixes verified.`);
