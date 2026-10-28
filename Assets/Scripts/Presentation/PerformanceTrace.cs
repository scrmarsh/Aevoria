using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using Aevoria.Core;
using UnityEngine;

namespace Aevoria.Presentation
{
    // Opt-in, preallocated frame evidence. Counters describe the last completed
    // profiler frame; GPU timing can lag further. Correlation is not causation.
    internal sealed class PerformanceTrace : IDisposable
    {
        private struct Sample
        {
            public float Frame,Stream;
            public long Alloc,Collect,Wait,Present,Hud,Veg,Upload,HudBytes,VegBytes,UploadBytes;
            public int Collections,Meshes;
        }
        private readonly List<Sample> samples=new List<Sample>(100000);
        private readonly Dictionary<string,ProfilerRecorder> recorders=new Dictionary<string,ProfilerRecorder>();
        private int previousCollections;
        public PerformanceTrace()
        {
            WorkCounter.Enabled=true;
            WorkCounter.ValidateAllocationCounter();
            DiscoverCounters();
        }
        private void DiscoverCounters()
        {
            var handles=new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
            foreach(var handle in handles)
            {
                var d=ProfilerRecorderHandle.GetDescription(handle);
                if(d.Name=="GC Allocated In Frame" || d.Name=="GC.Collect" || d.Name=="Gfx.WaitForPresentOnGfxThread" ||
                   d.Name=="Gfx.WaitForGfxCommandsFromMainThread" || d.Name=="Aevoria.HUD" || d.Name=="Aevoria.Vegetation" || d.Name=="Aevoria.Upload")
                {
                    if(!recorders.ContainsKey(d.Name)) recorders.Add(d.Name,ProfilerRecorder.StartNew(d.Category,d.Name,1));
                }
            }
        }
        private long Read(string name)=>recorders.TryGetValue(name,out var r)&&r.Valid?r.LastValue:-1;
        public void Reset() {DiscoverCounters();samples.Clear();previousCollections=GC.CollectionCount(0);}
        public void Record(float ms,double stream,int meshes)
        {
            int count=GC.CollectionCount(0);
            samples.Add(new Sample {Frame=ms,Stream=(float)stream,Meshes=meshes,Collections=count-previousCollections,
                Alloc=Read("GC Allocated In Frame"),Collect=Read("GC.Collect"),Wait=Read("Gfx.WaitForGfxCommandsFromMainThread"),
                Present=Read("Gfx.WaitForPresentOnGfxThread"),Hud=WorkCounter.Hud.NsAt(Time.frameCount-1),Veg=WorkCounter.Vegetation.NsAt(Time.frameCount-1),Upload=WorkCounter.Upload.NsAt(Time.frameCount-1),
                HudBytes=WorkCounter.Hud.BytesAt(Time.frameCount-1),VegBytes=WorkCounter.Vegetation.BytesAt(Time.frameCount-1),UploadBytes=WorkCounter.Upload.BytesAt(Time.frameCount-1)});
            previousCollections=count;
        }
        public void Write(string path)
        {
            using(var writer=new StreamWriter(path))
            {
                writer.WriteLine("frame,engineLoopMs,streamUpdateMs,allocatedBytes,gcMarkerNs,gcCollectionDelta,renderWaitNs,presentWaitNs,hudNs,vegetationNs,uploadNs,ownedTerrainMeshes,hudAllocatedBytes,vegetationAllocatedBytes,uploadAllocatedBytes");
                for(int i=0;i<samples.Count;i++)
                {
                    var s=samples[i];
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1:F5},{2:F5},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}",
                        i,s.Frame,s.Stream,s.Alloc,s.Collect,s.Collections,s.Wait,s.Present,s.Hud,s.Veg,s.Upload,s.Meshes,s.HudBytes,s.VegBytes,s.UploadBytes));
                }
            }
            using(var writer=new StreamWriter(Path.ChangeExtension(path,"counters.txt")))
            {
                writer.WriteLine("current-thread-allocation-counter="+(WorkCounter.AllocationCounterSupported?"self-test passed":"unavailable: positive allocation self-test failed"));
                foreach(var pair in recorders) writer.WriteLine(pair.Key+"="+(pair.Value.Valid?"available":"unavailable"));
            }
        }
        public void Dispose() {WorkCounter.Enabled=false;foreach(var r in recorders.Values) r.Dispose();}
    }
}
