from pathlib import Path
import csv, json, re, statistics, sys

run=Path(sys.argv[1]);frames={}
def quantile(values,q):
    ordered=sorted(values);at=(len(ordered)-1)*q;lo=int(at);hi=min(lo+1,len(ordered)-1)
    return ordered[lo]+(ordered[hi]-ordered[lo])*(at-lo)
for path in run.glob('*-frames.csv'):
    with path.open(encoding='utf-8-sig') as f: rows=list(csv.DictReader(f))
    times=[float(x['engineLoopMs']) for x in rows]
    if times: frames[path.stem]=dict(samples=len(times),medianMs=statistics.median(times),p95Ms=quantile(times,.95),p99Ms=quantile(times,.99),maximumMs=max(times))
memory={}
path=run/'process-memory.csv'
if path.exists():
    with path.open(encoding='utf-8-sig') as f: rows=list(csv.DictReader(f))
    for key in ('workingSetMiB','privateMiB','dedicatedGpuMiB','sharedGpuMiB'):
        values=[float(x[key]) for x in rows if float(x[key])>=0]
        if values: memory[key]=max(values)
log=(run/'player.log').read_text(encoding='utf-8-sig',errors='replace')
completed=re.findall(r'elapsed=([0-9.]+)s .*?outcome=Completed',log)
result=dict(run=str(run),exit=json.loads((run/'process-exit.json').read_text(encoding='utf-8-sig')),
    frameWindows=frames,peakProcessMemory=memory,generationSeconds=float(completed[0]) if completed else None,
    screenshots=len(list(run.glob('*.png'))),failureFile=(run/'failures.txt').exists(),
    timingScope='Short stabilized engine frame-loop windows, not isolated CPU or GPU timestamps. Capture/streaming work outside these windows is separate. Final sustained camera/GPU profiling remains required.',
    memoryScope='External process working set and Windows per-PID GPU process memory counters; not total system VRAM or a graphics profiler allocation trace.')
(run/'summary.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
