param([Parameter(Mandatory=$true)][string]$BuildDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$output=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $output){throw 'Choose a fresh evidence directory'}
New-Item -ItemType Directory -Path $output | Out-Null
$testRoot=Join-Path 'D:\AevoriaValidation' ('Final009-'+[guid]::NewGuid().ToString('N'))
$arguments=@('-aevoriaAudit009Final','-aevoriaTestRoot',$testRoot,'-aevoriaOutput',$output,'-screen-width','2560','-screen-height','1440','-screen-fullscreen','0','-logFile',(Join-Path $output 'player.log'))
$quoted=$arguments | ForEach-Object {'"'+$_+'"'}
$player=Start-Process -FilePath (Join-Path $BuildDirectory 'Aevoria.exe') -ArgumentList $quoted -PassThru
$samplerArguments=@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'SampleAevoriaProcess.ps1'),'-PlayerId',[string]$player.Id,'-OutputFile',(Join-Path $output 'process-memory.csv'))
$quotedSampler=$samplerArguments | ForEach-Object {'"'+$_+'"'}
$sampler=Start-Process powershell -ArgumentList $quotedSampler -WindowStyle Hidden -PassThru
$launch=[ordered]@{playerId=$player.Id;samplerId=$sampler.Id;isolatedSaves=$testRoot;build=(Get-Content -LiteralPath (Join-Path $BuildDirectory 'build-info.json') -Raw | ConvertFrom-Json);presentationSha256=(Get-FileHash -LiteralPath (Join-Path $BuildDirectory 'Aevoria_Data\Managed\Aevoria.Presentation.dll')).Hash;worldSha256=(Get-FileHash -LiteralPath (Join-Path $BuildDirectory 'Aevoria_Data\Managed\Aevoria.World.dll')).Hash}
$launch | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'launch.json') -Encoding UTF8
$launch | ConvertTo-Json -Depth 5
