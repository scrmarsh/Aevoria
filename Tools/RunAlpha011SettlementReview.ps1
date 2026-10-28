param([Parameter(Mandatory=$true)][string]$BuildDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$build=[IO.Path]::GetFullPath($BuildDirectory);$output=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $output){throw 'Choose a fresh settlement evidence directory'}
New-Item -ItemType Directory -Path $output | Out-Null
$save=Join-Path D:/AevoriaValidation ('Settlements011-'+[guid]::NewGuid().ToString('N'))
$arguments=@('-aevoriaAudit011Settlements','-aevoriaTestRoot',$save,'-aevoriaOutput',$output,'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',(Join-Path $output 'player.log'))
$player=Start-Process -FilePath (Join-Path $build 'Aevoria.exe') -ArgumentList ($arguments | ForEach-Object {'"'+$_+'"'}) -PassThru
$sampleArgs=@('-NoProfile','-File',(Join-Path $PSScriptRoot 'SampleAevoriaProcess.ps1'),'-PlayerId',[string]$player.Id,'-OutputFile',(Join-Path $output 'process-memory.csv'))
$sampler=Start-Process powershell -ArgumentList ($sampleArgs | ForEach-Object {'"'+$_+'"'}) -WindowStyle Hidden -PassThru
[ordered]@{playerId=$player.Id;samplerId=$sampler.Id;isolatedSaves=$save;build=$build;arguments=$arguments;presentationSha256=(Get-FileHash (Join-Path $build 'Aevoria_Data/Managed/Aevoria.Presentation.dll')).Hash;worldSha256=(Get-FileHash (Join-Path $build 'Aevoria_Data/Managed/Aevoria.World.dll')).Hash;scope='Actual Windows settlement gameplay. Images require separate visual review.'} | ConvertTo-Json | Set-Content (Join-Path $output 'launch.json') -Encoding UTF8
Get-Content (Join-Path $output 'launch.json')
$player.WaitForExit()
Set-Content (Join-Path $output 'exit-code.txt') $player.ExitCode
if($player.ExitCode -ne 0){throw "Settlement review exited $($player.ExitCode)"}
if(!(Test-Path (Join-Path $output 'result.txt'))){throw 'Settlement review did not complete'}
Get-Content (Join-Path $output 'result.txt')
