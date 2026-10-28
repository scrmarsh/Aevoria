param([string]$OutputDirectory='M:\Aevoria\Docs\Validation\Alpha-v0.0.8\Rendered-final',[switch]$ArtifactProbe,[string]$BuildDirectory="Builds\Alpha-v0.0.8",[switch]$Addendum,[switch]$ColossalOnly,[switch]$HorizonProbe)
$ErrorActionPreference='Stop'
$workspace=Split-Path $PSScriptRoot -Parent
$output=[IO.Path]::GetFullPath($OutputDirectory)
$binaryRoot=if([IO.Path]::IsPathRooted($BuildDirectory)){$BuildDirectory}else{Join-Path $workspace $BuildDirectory}
if(Test-Path -LiteralPath $output){throw 'Choose a new output directory; existing evidence is preserved.'}
New-Item -ItemType Directory -Path $output | Out-Null
$testRoot=Join-Path $env:LOCALAPPDATA ('Temp\Aevoria008Audit-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
$arguments=@('-aevoriaAudit008','-aevoriaTestRoot',$testRoot,'-aevoriaOutput',$output,'-screen-width','2560','-screen-height','1440','-screen-fullscreen','0','-logFile',(Join-Path $output 'player.log'))
if($ArtifactProbe){$arguments+='-aevoria008ArtifactProbe'}
if($Addendum){$arguments+='-aevoria008Addendum'}
if($ColossalOnly){$arguments+='-aevoria008ColossalOnly'}
if($HorizonProbe){$arguments+='-aevoria008HorizonProbe'}
$quoted=$arguments | ForEach-Object {'"'+$_+'"'}
$player=Start-Process -FilePath (Join-Path $binaryRoot 'Aevoria.exe') -ArgumentList $quoted -WorkingDirectory $workspace -PassThru
$player.Id | Set-Content -LiteralPath (Join-Path $workspace 'Temp\alpha008-final.pid')
$samplerArgs=@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $workspace 'Tools\SampleAevoriaProcess.ps1'),'-PlayerId',[string]$player.Id,'-OutputFile',(Join-Path $output 'process-memory.csv'))
$quotedSampler=$samplerArgs | ForEach-Object {'"'+$_+'"'}
$sampler=Start-Process powershell -ArgumentList $quotedSampler -WindowStyle Hidden -PassThru
$sampler.Id | Set-Content -LiteralPath (Join-Path $workspace 'Temp\alpha008-memory.pid')
$worldAssembly=Join-Path $binaryRoot 'Aevoria_Data\Managed\Aevoria.World.dll'
$presentationAssembly=Join-Path $binaryRoot 'Aevoria_Data\Managed\Aevoria.Presentation.dll'
$launch=[pscustomobject]@{playerId=$player.Id;samplerId=$sampler.Id;output=$output;isolatedSaves=$testRoot;artifactProbe=[bool]$ArtifactProbe;build=(Get-Content -LiteralPath (Join-Path $binaryRoot 'build-info.json') -Raw | ConvertFrom-Json);worldAssemblySha256=(Get-FileHash -LiteralPath $worldAssembly -Algorithm SHA256).Hash;presentationAssemblySha256=(Get-FileHash -LiteralPath $presentationAssembly -Algorithm SHA256).Hash}
$launch | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'audit-launch.json') -Encoding UTF8
$launch | ConvertTo-Json -Depth 5
