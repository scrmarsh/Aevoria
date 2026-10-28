// Keep all frame outliers; GC timing association is not proof of causation.
import fs from 'node:fs';
import path from 'node:path';
const root=process.argv[2];
if(!root) throw new Error('Usage: node Tools/AnalyzeFrameTrace.mjs <evidence-directory>');
const results=[];
for(const name of fs.readdirSync(root).filter(n=>n.endsWith('-frames.csv')).sort()) {
  const lines=fs.readFileSync(path.join(root,name),'utf8').trim().split(/\r?\n/);
  const columns=lines.shift().replace(/^\uFEFF/,'').split(',');
  const rows=lines.map(line=>Object.fromEntries(line.split(',').map((v,i)=>[columns[i],Number(v)])));
  const spikes=rows.map((r,i)=>r.engineLoopMs>1000/60?i:-1).filter(i=>i>=0);
  const nearby=(i,key)=>rows.slice(Math.max(1,i-1),i+2).some(r=>r[key]>0);
  const result={scenario:name.replace('-frames.csv',''),frames:rows.length,maxMs:Math.max(...rows.map(r=>r.engineLoopMs)),
    over16_67ms:spikes.length,spikesWithNearbyGCMarker:spikes.filter(i=>nearby(i,'gcMarkerNs')).length,
    spikesWithNearbyCollectionCount:spikes.filter(i=>nearby(i,'gcCollectionDelta')).length};
  for(const key of ['gcMarkerNs','presentWaitNs','renderWaitNs','hudNs','vegetationNs','uploadNs','allocatedBytes','hudAllocatedBytes','vegetationAllocatedBytes','uploadAllocatedBytes']) {
    const values=rows.map(r=>r[key]??-1).filter(v=>v>=0).sort((a,b)=>a-b);
    result[key]=values.length?{mean:values.reduce((a,b)=>a+b,0)/values.length,p99:values[Math.floor((values.length-1)*.99)],max:values.at(-1)}:'unavailable';
  }
  result.outliers=spikes.map(i=>({index:i,...rows[i]}));results.push(result);
  console.log(`${result.scenario}: ${rows.length} frames; max ${result.maxMs.toFixed(2)} ms; ${spikes.length} over budget; ${result.spikesWithNearbyGCMarker} near GC marker`);
}
fs.writeFileSync(path.join(root,'trace-analysis.json'),JSON.stringify(results,null,2));
