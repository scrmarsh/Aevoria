param([Parameter(Mandatory=$true)][int]$PlayerId,[Parameter(Mandatory=$true)][string]$OutputFile)
$ErrorActionPreference='Stop'
$rows=New-Object System.Collections.Generic.List[object]
$player=Get-Process -Id $PlayerId -ErrorAction Stop
if($player.ProcessName -ne 'Aevoria'){throw 'Sampler requires the actual Aevoria process'}
# Keep a handle to this exact process so PID reuse cannot hide a shutdown crash.
$null=$player.Handle
while(!$player.HasExited){
    $player.Refresh()
    $dedicated=-1.0;$shared=-1.0
    try{
        $samples=(Get-Counter -Counter "\GPU Process Memory(pid_$($PlayerId)_*)\Dedicated Usage","\GPU Process Memory(pid_$($PlayerId)_*)\Shared Usage" -ErrorAction Stop).CounterSamples
        $d=@($samples | Where-Object {$_.Status -eq 0 -and $_.Path -like '*dedicated usage'});$s=@($samples | Where-Object {$_.Status -eq 0 -and $_.Path -like '*shared usage'})
        if($d.Count){$dedicated=($d | Measure-Object CookedValue -Sum).Sum/1MB};if($s.Count){$shared=($s | Measure-Object CookedValue -Sum).Sum/1MB}
    }catch{}
    $rows.Add([pscustomobject]@{utc=(Get-Date).ToUniversalTime().ToString('o');pid=$PlayerId;workingSetMiB=$player.WorkingSet64/1MB;privateMiB=$player.PrivateMemorySize64/1MB;dedicatedGpuMiB=$dedicated;sharedGpuMiB=$shared})
    $rows | Export-Csv -NoTypeInformation -LiteralPath $OutputFile
    Start-Sleep -Seconds 3
}
$player.WaitForExit()
[ordered]@{pid=$PlayerId;exitCode=$player.ExitCode;exitedUtc=[DateTime]::UtcNow.ToString('O')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path (Split-Path $OutputFile -Parent) 'process-exit.json') -Encoding UTF8
