param([ValidateSet('prepare','edit','play','build','characters')][string]$Action='edit',[string]$TestFilter='',[string]$ValidationProject='AevoriaPhase1Validation',[string]$ValidationRoot='')
$ErrorActionPreference='Stop'
# TEMP may use an 8.3 alias (SEANMA~1). Unity's TypeDB lookup then compares
# abbreviated assembly paths with expanded paths and can build an invalid player.
if($ValidationProject -notmatch '^Aevoria[A-Za-z0-9]+$'){throw 'Use a simple isolated validation project name'}
if(-not $ValidationRoot){$ValidationRoot=Join-Path $env:LOCALAPPDATA 'Temp'}
$project=Join-Path ([IO.Path]::GetFullPath($ValidationRoot)) $ValidationProject
$source=Split-Path $PSScriptRoot -Parent
foreach($folder in @('Assets','Packages','ProjectSettings')) {
    & robocopy (Join-Path $source $folder) (Join-Path $project $folder) /E /NFL /NDL /NJH /NJS /NP | Out-Null
    if($LASTEXITCODE -ge 8) { throw "Copy failed: $folder" }
}
foreach($file in @('CHANGELOG.md','Docs/THE_CHRONICLES_OF_AEVORIA.md')) {
    $target=Join-Path $project $file; New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null; Copy-Item -LiteralPath (Join-Path $source $file) -Destination $target
}
$unity='C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe'
$arguments=@('-batchmode','-projectPath',$project,'-logFile',(Join-Path $project "phase1-$Action.log"))
$arguments+=@('-aevoriaTestRoot',(Join-Path $source ('Temp/UnityValidation/'+[guid]::NewGuid().ToString('N'))))
if($Action -eq 'edit' -or $Action -eq 'play') {
    $platform=if($Action -eq 'edit'){'EditMode'}else{'PlayMode'}
    $resultPath=Join-Path $project ("phase1-$Action-"+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')+'.xml')
    $arguments+=@('-runTests','-testPlatform',$platform,'-testResults',$resultPath)
    if($TestFilter){$arguments+=@('-testFilter',$TestFilter)}
} else {
    $method=if($Action -eq 'build'){'Aevoria.Editor.Phase1Validation.BuildWindows'}elseif($Action -eq 'characters'){'Aevoria.Editor.CharacterAssetExport.Export'}else{'Aevoria.Editor.Phase1Validation.PrepareAssets'}
    $arguments+=@('-quit','-executeMethod',$method)
}
$quoted = $arguments | ForEach-Object { '"' + $_ + '"' }
$process = Start-Process -FilePath $unity -ArgumentList $quoted -WindowStyle Hidden -PassThru -Wait
if($process.ExitCode -ne 0){throw "Unity exited with code $($process.ExitCode); inspect validation log"}
$log=Join-Path $project "phase1-$Action.log"
if(Select-String -LiteralPath $log -Pattern 'UnityException:|NullReferenceException:|MissingReferenceException:|Shader error in' -Quiet) {
    throw "Runtime/shader exception in $log; rejecting validation even if the runner exited successfully."
}
if($Action -eq 'edit' -or $Action -eq 'play') {
    Write-Output "Test results: $resultPath"
    [xml]$results=Get-Content -LiteralPath $resultPath
    if($results.'test-run'.result -ne 'Passed' -or [int]$results.'test-run'.failed -ne 0 -or [int]$results.'test-run'.total -eq 0) {throw "Unity $Action tests did not pass"}
}
if ($Action -eq 'build' -and (Select-String -LiteralPath (Join-Path $project "phase1-$Action.log") -SimpleMatch 'TypeDB: Assembly index' -Quiet)) {
    throw 'Unity could not match assemblies in its serialization database. Rejecting this build; use a full, non-abbreviated project path.'
}
exit $process.ExitCode

