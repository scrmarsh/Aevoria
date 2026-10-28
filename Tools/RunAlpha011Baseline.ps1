param([ValidateSet('Miniature','Small','Medium','Large','Colossal')][string]$WorldSize='Miniature',[string]$Seed='shaping-continents',[switch]$HighRelief,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$evidence=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $evidence){throw 'Choose a fresh native baseline evidence directory'}
New-Item -ItemType Directory -Path $evidence | Out-Null
$build='M:\Aevoria\Builds\Alpha-v0.0.10'
$saveRoot=Join-Path D:\AevoriaValidation ('Baseline011-'+[guid]::NewGuid().ToString('N'))
$arguments=@('-aevoriaAudit009Terrain','-aevoria009Size',$WorldSize,'-aevoria009Seed',$Seed,'-aevoriaTestRoot',$saveRoot,'-aevoriaOutput',$evidence,'-screen-width','2560','-screen-height','1440','-screen-fullscreen','0','-logFile',(Join-Path $evidence 'player.log'))
if($HighRelief){$arguments+='-aevoria009HighRelief'}
$player=Start-Process -FilePath (Join-Path $build 'Aevoria.exe') -ArgumentList ($arguments | ForEach-Object {'"'+$_+'"'}) -PassThru
$sampleArgs=@('-NoProfile','-File',(Join-Path $PSScriptRoot 'SampleAevoriaProcess.ps1'),'-PlayerId',[string]$player.Id,'-OutputFile',(Join-Path $evidence 'process-memory.csv'))
$sampler=Start-Process powershell -ArgumentList ($sampleArgs | ForEach-Object {'"'+$_+'"'}) -WindowStyle Hidden -PassThru
[ordered]@{playerId=$player.Id;samplerId=$sampler.Id;seed=$Seed;requestedSize=$WorldSize;mountainRelief=if($HighRelief){12686.6143}else{3600};savedWorlds=$saveRoot;build=$build;worldSha256=(Get-FileHash (Join-Path $build 'Aevoria_Data/Managed/Aevoria.World.dll')).Hash;presentationSha256=(Get-FileHash (Join-Path $build 'Aevoria_Data/Managed/Aevoria.Presentation.dll')).Hash;scope='Preserved v0.0.10 native terrain baseline. Old high-relief harness uses 12686.6143 m, not 16000 m. Exact 16000 m has a separate portable baseline.'} | ConvertTo-Json | Set-Content (Join-Path $evidence 'launch.json') -Encoding UTF8
Get-Content (Join-Path $evidence 'launch.json')
