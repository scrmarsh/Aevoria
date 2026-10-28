using System;
using System.Diagnostics;

namespace Aevoria.Core
{
    // Opt-in main-thread scopes complement profiler markers in release players
    // where custom markers/counters may be stripped. No per-scope heap object.
    public sealed class WorkCounter
    {
        public static bool Enabled;
        public static bool AllocationCounterSupported {get;private set;}
        public static void ValidateAllocationCounter()
        {
            long before=GC.GetAllocatedBytesForCurrentThread();var probe=new byte[4096];
            long after=GC.GetAllocatedBytesForCurrentThread();GC.KeepAlive(probe);
            AllocationCounterSupported=after-before>=4096;
        }
        public static readonly WorkCounter Hud=new WorkCounter(),Vegetation=new WorkCounter(),Upload=new WorkCounter();
        public long Nanoseconds,AllocatedBytes;
        private int frame=-1;
        private int previousFrame=-1;
        private long previousNs,previousBytes;
        public Scope Measure(int frameIndex)=>new Scope(this,frameIndex);
        public readonly struct Scope : IDisposable
        {
            private readonly WorkCounter owner;
            private readonly long start,allocated;
            public Scope(WorkCounter counter,int frameIndex)
            {
                owner=Enabled?counter:null;start=allocated=0;
                if(owner==null) return;
                if(owner.frame!=frameIndex) {owner.previousFrame=owner.frame;owner.previousNs=owner.Nanoseconds;owner.previousBytes=owner.AllocatedBytes;owner.frame=frameIndex;owner.Nanoseconds=owner.AllocatedBytes=0;}
                start=Stopwatch.GetTimestamp();allocated=GC.GetAllocatedBytesForCurrentThread();
            }
            public void Dispose()
            {
                if(owner==null) return;
                owner.Nanoseconds+=(Stopwatch.GetTimestamp()-start)*1000000000L/Stopwatch.Frequency;
                owner.AllocatedBytes+=GC.GetAllocatedBytesForCurrentThread()-allocated;
            }
        }
        public long NsAt(int frameIndex)=>frame==frameIndex?Nanoseconds:previousFrame==frameIndex?previousNs:0;
        public long BytesAt(int frameIndex)=>!AllocationCounterSupported?-1:frame==frameIndex?AllocatedBytes:previousFrame==frameIndex?previousBytes:0;
    }
}
