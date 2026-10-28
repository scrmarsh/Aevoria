param([string]$BuildDirectory='M:\Aevoria\Builds\Alpha-v0.0.9-StabilityCandidate',[Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$Colossal,[switch]$SurfaceProbe)
$ErrorActionPreference='Stop'
$output=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $output){throw 'Choose fresh evidence output; prior captures are preserved.'}
New-Item -ItemType Directory -Path $output | Out-Null
$testRoot=Join-Path 'D:\AevoriaValidation' ('Saves009-'+[guid]::NewGuid().ToString('N'))
$arguments=@('-aevoriaAudit009Stability','-aevoriaTestRoot',$testRoot,'-aevoriaOutput',$output,'-screen-width','2560','-screen-height','1440','-screen-fullscreen','0','-logFile',(Join-Path $output 'player.log'))
if($Colossal){$arguments+='-aevoria009Colossal'}
if($SurfaceProbe){$arguments+='-aevoria009SurfaceProbe'}
$quoted=$arguments | ForEach-Object {'"'+$_+'"'}
$player=Start-Process -FilePath (Join-Path $BuildDirectory 'Aevoria.exe') -ArgumentList $quoted -PassThru
$launch=[ordered]@{playerId=$player.Id;isolatedSaves=$testRoot;requestedColossal=[bool]$Colossal;build=(Get-Content -LiteralPath (Join-Path $BuildDirectory 'build-info.json') -Raw | ConvertFrom-Json);presentationSha256=(Get-FileHash -LiteralPath (Join-Path $BuildDirectory 'Aevoria_Data\Managed\Aevoria.Presentation.dll')).Hash;worldSha256=(Get-FileHash -LiteralPath (Join-Path $BuildDirectory 'Aevoria_Data\Managed\Aevoria.World.dll')).Hash}
$launch | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'launch.json') -Encoding UTF8
$launch | ConvertTo-Json -Depth 5
