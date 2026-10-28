using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Aevoria.Persistence;
using Aevoria.World;
using UnityEngine;

namespace Aevoria.Presentation
{
    // A native lifecycle fixture using only its own game window and isolated
    // archive. A worker restores that window after the background lease ends,
    // allowing the main thread to verify that the original preference returned.
    public sealed class Alpha011LoadingAudit:MonoBehaviour
    {
        [DllImport("user32.dll")]private static extern bool ShowWindow(IntPtr handle,int command);
        [DllImport("user32.dll")]private static extern bool ShowWindowAsync(IntPtr handle,int command);
        [Serializable]private sealed class Result
        {public bool createdUnfocused,loadedUnfocused,cancelledUnfocused,backgroundPreferenceRestored,archivesUnchangedAfterCancel;public string seed="aevoria-001";public double createSeconds,loadSeconds,cancelSeconds;}
        private AevoriaBootstrap app;private string output;private IntPtr window;private Timer restore;private int timedOut;private bool cancelWork;private Stopwatch watch;private readonly Result result=new Result();
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        private void Failure(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception){File.AppendAllText(Path.Combine(output,"failures.txt"),message+"\n"+stack+"\n");Application.Quit(4);}}
        private IEnumerator Start()
        {
            Require(Application.platform==RuntimePlatform.WindowsPlayer,"Windows native loading fixture");
            Require(AevoriaBootstrap.Argument("-aevoriaTestRoot")!=null,"Isolated saves required");output=AevoriaBootstrap.Argument("-aevoriaOutput");Require(!string.IsNullOrEmpty(output),"Explicit evidence output");Directory.CreateDirectory(output);
            app=GetComponent<AevoriaBootstrap>();app.ExternalControl=true;Application.logMessageReceived+=Failure;
            using(var process=Process.GetCurrentProcess())window=process.MainWindowHandle;Require(window!=IntPtr.Zero,"Native game window available");
            var settings=app.Settings.Copy();settings.AutosaveSeconds=0;app.ApplySettings(settings,false);yield return null;
            Application.runInBackground=false;watch=Stopwatch.StartNew();
            app.CreateWorld("Unfocused preparation fixture",result.seed,WorldSize.Medium,new GenerationSettings(mountains:16000,landmass:LandmassType.Continents),new ClimateSettings());
            BeginBackgroundOperation(false);while(app.Busy){result.createdUnfocused|=!Application.isFocused;yield return null;}EndBackgroundOperation();
            Require(app.WorldActive&&app.Current!=null,"Unfocused creation completed");Require(result.createdUnfocused,"Creation processed frames while unfocused");Require(!Application.runInBackground,"Creation restored the disabled background preference");app.Clock.Pause();result.createSeconds=watch.Elapsed.TotalSeconds;
            string id=app.Current.Id;app.ReturnToMenu();yield return null;watch.Restart();app.LoadWorld(id);
            BeginBackgroundOperation(false);while(app.Busy){result.loadedUnfocused|=!Application.isFocused;yield return null;}EndBackgroundOperation();
            Require(app.WorldActive&&app.Current?.Id==id,"Unfocused load restored the same archive");Require(result.loadedUnfocused,"Loading processed frames while unfocused");Require(!Application.runInBackground,"Loading restored the disabled background preference");app.Clock.Pause();result.loadSeconds=watch.Elapsed.TotalSeconds;
            app.ReturnToMenu();yield return null;var before=ArchiveHashes();watch.Restart();
            app.CreateWorld("Cancelled unfocused fixture",result.seed,WorldSize.Colossal,new GenerationSettings(mountains:16000,landmass:LandmassType.Continents),new ClimateSettings());
            BeginBackgroundOperation(true);while(app.Busy){result.cancelledUnfocused|=!Application.isFocused;yield return null;}EndBackgroundOperation();
            Require(app.PreparationDiagnostics?.Outcome==GenerationOutcome.Cancelled&&!app.WorldActive,"Unfocused worker cancellation returned to the menu");Require(result.cancelledUnfocused,"Cancellation was exercised while unfocused");Require(!Application.runInBackground,"Cancellation restored the disabled background preference");result.cancelSeconds=watch.Elapsed.TotalSeconds;
            var after=ArchiveHashes();result.archivesUnchangedAfterCancel=before.Count==after.Count;foreach(var pair in before)result.archivesUnchangedAfterCancel&=after.TryGetValue(pair.Key,out var hash)&&hash==pair.Value;
            Require(result.archivesUnchangedAfterCancel,"Cancelling preparation preserved every existing archive byte");result.backgroundPreferenceRestored=!Application.runInBackground;
            File.WriteAllText(Path.Combine(output,"loading-results.json"),JsonUtility.ToJson(result,true));UnityEngine.Debug.Log("AEVORIA011_UNFOCUSED_LOADING_COMPLETE");Application.Quit();
        }
        private Dictionary<string,string> ArchiveHashes()
        {
            var values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(string file in Directory.GetFiles(app.Repository.Root,"*",SearchOption.AllDirectories))values.Add(file,ArchiveCodec.Hash(File.ReadAllBytes(file)));return values;
        }
        private void BeginBackgroundOperation(bool cancel)
        {
            cancelWork=cancel;Interlocked.Exchange(ref timedOut,0);Require(Application.runInBackground,"Preparation holds a background-processing lease before yielding");
            restore=new Timer(_=>
            {
                if(watch.Elapsed.TotalSeconds>180){Interlocked.Exchange(ref timedOut,1);app.CancelPreparation();ShowWindowAsync(window,9);return;}
                var snapshot=app.PreparationDiagnostics;
                if(cancelWork&&snapshot?.Module==WorldLayerIds.Hydrology&&snapshot.Detail.IndexOf("river",StringComparison.OrdinalIgnoreCase)>=0)app.CancelPreparation();
                if(!app.Busy)ShowWindowAsync(window,9);
            },null,50,50);
            ShowWindow(window,6);
        }
        private void EndBackgroundOperation(){if(restore!=null){using(var ended=new ManualResetEvent(false)){restore.Dispose(ended);Require(ended.WaitOne(1000),"Native fixture watchdog stopped");}restore=null;}Require(Volatile.Read(ref timedOut)==0,"Native background preparation stayed within 180 seconds");}
        private void OnDestroy(){restore?.Dispose();Application.logMessageReceived-=Failure;}
    }
}
