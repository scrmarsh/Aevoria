param([Parameter(Mandatory=$true)][string]$Directory)
$ErrorActionPreference='Stop'
$run=Get-Content -LiteralPath (Join-Path $Directory 'audit.json') -Raw | ConvertFrom-Json
$outliers=@();$summary=@()
foreach($scenario in $run.samples){
    $frames=@(Import-Csv -LiteralPath (Join-Path $Directory ($scenario.scenario+'-frames.csv')))
    $bad=@($frames | Where-Object {[double]$_.engineLoopMs -gt 16.6667})
    foreach($row in $bad){$row | Add-Member -NotePropertyName scenario -NotePropertyValue $scenario.scenario;$outliers+=$row}
    $summary+=[pscustomobject]@{scenario=$scenario.scenario;frames=$scenario.frames;rendered=$scenario.rendered;width=$scenario.width;height=$scenario.height;meanMs=$scenario.meanMs;p95Ms=$scenario.p95Ms;p99Ms=$scenario.p99Ms;maxMs=$scenario.maxMs;over16ms=$bad.Count;aggregateCpuMsPerFrame=$scenario.cpuMs;gpuMs=$scenario.gpuMs;managedMiB=$scenario.managedMiB;unityMiB=$scenario.unityMiB}
}
$summary | Export-Csv -NoTypeInformation -LiteralPath (Join-Path $Directory 'summary.csv')
if($outliers.Count){$outliers | Export-Csv -NoTypeInformation -LiteralPath (Join-Path $Directory 'outliers.csv')}
$summary | Format-Table scenario,frames,meanMs,p99Ms,maxMs,over16ms -AutoSize
if(Test-Path -LiteralPath (Join-Path $Directory 'process-memory.csv')){
    $memory=Import-Csv -LiteralPath (Join-Path $Directory 'process-memory.csv')
    $memory | Measure-Object workingSetMiB,privateMiB,dedicatedGpuMiB,sharedGpuMiB -Maximum | Select-Object Property,Maximum | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Directory 'memory-maxima.json')
}
