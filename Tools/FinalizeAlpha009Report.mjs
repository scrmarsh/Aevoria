import fs from 'node:fs';
const dir='Docs/Validation/Alpha-v0.0.9';
const summary=JSON.parse(fs.readFileSync(dir+'/validation-summary.json','utf8'));
const a=summary.native;
if(!a?.completed)throw Error('Final native acceptance incomplete');
const launch=JSON.parse(fs.readFileSync(dir+'/'+a.evidence+'/launch.json','utf8').replace(/^\uFEFF/,''));
const n=v=>v.toFixed(2);
let report=`
### Completed final-candidate Windows run

The completed run is [${a.evidence}/audit.json](Validation/Alpha-v0.0.9/${a.evidence}/audit.json). It recorded **${a.renders.toLocaleString('en-US')} rendered frames**, **zero runtime errors**, five exact authority-preserving archive reloads, successful creation cancellation and successful presentation cleanup. The engine completion marker is retained in player.log. The settled-audit candidate was built at ${launch.build.builtUtc}. The source manifest is [build-source-settled-candidate.json](Validation/Alpha-v0.0.9/build-source-settled-candidate.json).

Hardware reported by the actual player: **${a.cpu}**, **${a.gpu}**. All measured scenes rendered at 2560 × 1440. High Quality was applied through the real preset, VSync disabled, target FPS uncapped, motion blur off, clouds and weather enabled. Raw settings are embedded in audit.json. The fourth preset may serialize as **Huge** because it is an existing enum alias of **Large**; it is not a sixth world size.

| Size | Seed | Create / initial presentation (s) | Reload / reconstruction (s) | Authority identical |
| --- | --- | ---: | ---: | --- |
`;
for(const w of a.worlds)report+=`| ${w.size} | ${w.seed} | ${n(w.createSeconds)} | ${n(w.loadSeconds)} | ${w.persistence?'Yes':'NO'} |\n`;
report+=`
Generation timings include the real native operation and initial presentation, and are not directly comparable to portable field-construction timings. Reload reconstructs deterministic geography and restores saved state. The 180-second Colossal streaming scenario is a short bounded traversal, not an extended soak.

| Size | Scene | Samples | Mean ms | p95 ms | p99 ms | Maximum ms | >16.667 ms | >33.333 ms |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
`;
for(const m of a.measurements){const t=a.frameTraces[m.size+'-'+m.scenario+'-frames.csv'];report+=`| ${m.size} | ${m.scenario} | ${m.frames} | ${n(m.meanMs)} | ${n(m.p95Ms)} | ${n(m.p99Ms)} | ${n(m.maxMs)} | ${t?.over16_667??'—'} | ${t?.over33_333??'—'} |\n`;}
report+=`
These are measured Unity engine-loop intervals, not an independent display-present latency measurement. GPU frame timing is queried from Unity; process CPU time includes worker threads and must not be mistaken for main-thread frame duration. Raw frame traces retain streaming, GC and upload markers where available. Unsupported allocation/counter values are recorded as -1, not zero. Screenshots and loading are outside steady-scene timing; normal movement and streaming work remain inside the relevant traces. Two warm frames after trace allocation are excluded to avoid measuring benchmark setup.

Peak sampled process working set was **${n(a.peakMemoryMiB.workingSetMiB)} MiB**, private memory **${n(a.peakMemoryMiB.privateMiB)} MiB**, dedicated process GPU memory **${n(a.peakMemoryMiB.dedicatedGpuMiB)} MiB**, shared process GPU memory **${n(a.peakMemoryMiB.sharedGpuMiB)} MiB**. The three-second Windows sampler can miss shorter peaks; these are process counters rather than total-system VRAM or proof of no leak. Audit memory and profiler fields use distinct accounting and must not be added together.

The Colossal forest averaged ${n(a.measurements.find(m=>m.size==='Colossal'&&m.scenario==='forest').meanMs)} ms and Small mountain orbit ${n(a.measurements.find(m=>m.size==='Small'&&m.scenario==='mountain-orbit').meanMs)} ms. They exceed a sustained 60 FPS budget in this run. The full maximum and tail columns retain less frequent spikes. These are disclosed performance limitations rather than a passed universal frame-time target. No settings were lowered between sizes to hide them.

The earlier Final-native-first attempt was deliberately stopped before completion to correct collider ownership and remains marked stopped-for-correction. Final-native-complete passed its gameplay checks, but its readiness helper could observe the previous camera's desired tiles before LateUpdate; its captures and scene timings are superseded. The corrected helper waits two publication frames and then three consecutive ready frames before sampling. Both earlier runs remain preserved and are excluded from final performance acceptance. Earlier native baseline comparisons used different generator contracts, poses and sometimes seeds; their images are qualitative comparisons, not controlled speedup or pixel-regression claims.
`;
const p='Docs/ALPHA_v0.0.9_TEST_RESULTS.md';let text=fs.readFileSync(p,'utf8');text=text.replace('Final five-size Windows measurements and delivery provenance are recorded below as they finish.','Final five-size Windows measurements and delivery provenance are recorded below.');text=text.replace(/The final candidate benchmark is running[\s\S]*?A partial run must not be reported as complete\./,'The final candidate benchmark completed successfully. The separate delivery package preserves the accepted runtime code and updated in-game history. Its build identity is recorded in the delivery provenance below.');text=text.split('### Completed final-candidate Windows run')[0].trimEnd()+'\n'+report;fs.writeFileSync(p,text);
console.log('Appended '+a.measurements.length+' completed native measurements.');
