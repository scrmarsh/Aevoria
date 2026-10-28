param([Parameter(Mandatory=$true)][string]$BuildDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$build=[IO.Path]::GetFullPath($BuildDirectory);$evidence=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $evidence){throw 'Choose a fresh lifecycle evidence directory'}
New-Item -ItemType Directory -Path $evidence | Out-Null
$saveRoot=Join-Path D:\AevoriaValidation ('Loading011-'+[guid]::NewGuid().ToString('N'))
$arguments=@('-aevoriaAudit011Loading','-aevoriaTestRoot',$saveRoot,'-aevoriaOutput',$evidence,'-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-logFile',(Join-Path $evidence 'player.log'))
$player=Start-Process -FilePath (Join-Path $build 'Aevoria.exe') -ArgumentList ($arguments | ForEach-Object {'"'+$_+'"'}) -PassThru
$sampleArgs=@('-NoProfile','-File',(Join-Path $PSScriptRoot 'SampleAevoriaProcess.ps1'),'-PlayerId',[string]$player.Id,'-OutputFile',(Join-Path $evidence 'process-memory.csv'))
$sampler=Start-Process powershell -ArgumentList ($sampleArgs | ForEach-Object {'"'+$_+'"'}) -WindowStyle Hidden -PassThru
[ordered]@{playerId=$player.Id;samplerId=$sampler.Id;savedWorlds=$saveRoot;build=$build;worldSha256=(Get-FileHash (Join-Path $build 'Aevoria_Data/Managed/Aevoria.World.dll')).Hash;presentationSha256=(Get-FileHash (Join-Path $build 'Aevoria_Data/Managed/Aevoria.Presentation.dll')).Hash;scope='Native minimized-window create/load/cancel lifecycle, preference restoration and archive preservation. Uses only the fixture game window and isolated saves.'} | ConvertTo-Json | Set-Content (Join-Path $evidence 'launch.json') -Encoding UTF8
Get-Content (Join-Path $evidence 'launch.json')
