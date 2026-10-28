using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aevoria.World
{
    public enum GenerationOutcome { Idle,Running,Completed,Cancelled,Failed }
    public sealed class GenerationStageRecord
    {
        public readonly string Module,Detail;public readonly int Version;public readonly double Seconds;public readonly long Completed,Total;
        public GenerationStageRecord(string module,int version,string detail,double seconds,long completed,long total){Module=module;Version=version;Detail=detail;Seconds=seconds;Completed=completed;Total=total;}
    }
    public sealed class GenerationDiagnosticSnapshot
    {
        public readonly string Module,Detail,Error;public readonly int Version;public readonly long Completed,Total,Revision;
        public readonly double ElapsedSeconds,ModuleSeconds,SecondsSinceHeartbeat;public readonly GenerationOutcome Outcome;
        public readonly IReadOnlyList<GenerationStageRecord> RecentStages;
        internal GenerationDiagnosticSnapshot(string module,int version,string detail,string error,long completed,long total,long revision,double elapsed,double moduleSeconds,double silent,GenerationOutcome outcome,GenerationStageRecord[] recent)
        {Module=module;Version=version;Detail=detail;Error=error;Completed=completed;Total=total;Revision=revision;ElapsedSeconds=elapsed;ModuleSeconds=moduleSeconds;SecondsSinceHeartbeat=silent;Outcome=outcome;RecentStages=Array.AsReadOnly(recent);}
    }
    // Diagnostics have their own monotonic clock and never participate in seeds,
    // geographic products or persistent simulation fingerprints.
    public sealed class GenerationDiagnostics
    {
        public const int RecentStageLimit=256;
        private readonly object gate=new object();private readonly Stopwatch clock=Stopwatch.StartNew();private readonly List<GenerationStageRecord> recent=new List<GenerationStageRecord>();
        private string module="",detail="",error="";private int version;private long completed,total,revision;private double moduleStarted,heartbeat,finished;
        private GenerationOutcome outcome=GenerationOutcome.Idle;private bool active;
        public double ElapsedSeconds=>clock.Elapsed.TotalSeconds;
        public void Begin(string id,int moduleVersion=1,string message="")
        {
            if(string.IsNullOrWhiteSpace(id)||moduleVersion<1)throw new ArgumentException("Invalid diagnostic stage.");
            lock(gate){if(active)Close();module=id;version=moduleVersion;detail=message??"";completed=total=0;moduleStarted=heartbeat=ElapsedSeconds;outcome=GenerationOutcome.Running;active=true;revision++;}
        }
        public void Pulse(string message=null,long workCompleted=0,long workTotal=0)
        {
            if(workCompleted<0||workTotal<0||workTotal>0&&workCompleted>workTotal)throw new ArgumentException("Invalid generation work counts.");
            lock(gate){if(!active)return;if(message!=null)detail=message;completed=workCompleted;total=workTotal;heartbeat=ElapsedSeconds;revision++;}
        }
        public void CompleteModule(){lock(gate){if(active)Close();}}
        private void Close(){recent.Add(new GenerationStageRecord(module,version,detail,ElapsedSeconds-moduleStarted,completed,total));if(recent.Count>RecentStageLimit)recent.RemoveAt(0);active=false;revision++;}
        public void Finish(GenerationOutcome result,string message="")
        {if(result!=GenerationOutcome.Completed&&result!=GenerationOutcome.Cancelled&&result!=GenerationOutcome.Failed)throw new ArgumentException("A terminal outcome is required.");lock(gate){if(active)Close();outcome=result;finished=ElapsedSeconds;error=message??"";revision++;}}
        public GenerationDiagnosticSnapshot Snapshot()
        {lock(gate){double now=ElapsedSeconds,elapsed=outcome==GenerationOutcome.Completed||outcome==GenerationOutcome.Cancelled||outcome==GenerationOutcome.Failed?finished:now;return new GenerationDiagnosticSnapshot(module,version,detail,error,completed,total,revision,elapsed,active?now-moduleStarted:0,now-heartbeat,outcome,recent.ToArray());}}
    }
}
