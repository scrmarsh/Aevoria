param([string]$BuildDirectory='M:\Aevoria\Builds\Alpha-v0.0.9-TerrainCandidate1',[Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$MiniOnly,[switch]$HighRelief,[switch]$WaterOnly,[switch]$CoastOnly,[int]$RiverNode=-1,[ValidateSet('','Miniature','Small','Medium','Large','Colossal')][string]$WorldSize='',[string]$Seed='shaping-continents')
$ErrorActionPreference='Stop'
$output=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $output){throw 'Choose fresh evidence output; preserve previous captures.'}
New-Item -ItemType Directory -Path $output | Out-Null
$testRoot=Join-Path 'D:\AevoriaValidation' ('Terrain009-'+[guid]::NewGuid().ToString('N'))
$arguments=@('-aevoriaAudit009Terrain','-aevoriaTestRoot',$testRoot,'-aevoriaOutput',$output,'-screen-width','2560','-screen-height','1440','-screen-fullscreen','0','-logFile',(Join-Path $output 'player.log'))
if($MiniOnly){$arguments+='-aevoria009MiniOnly'}
if($HighRelief){$arguments+='-aevoria009HighRelief'}
if($WaterOnly){$arguments+='-aevoria009WaterOnly'}
if($CoastOnly){$arguments+='-aevoria009CoastOnly'}
if($WorldSize){$arguments+=@('-aevoria009Size',$WorldSize)}
$arguments+=@('-aevoria009Seed',$Seed)
if($RiverNode -ge 0){$arguments+=@('-aevoria009RiverNode',$RiverNode.ToString())}
$quoted=$arguments | ForEach-Object {'"'+$_+'"'}
$player=Start-Process -FilePath (Join-Path $BuildDirectory 'Aevoria.exe') -ArgumentList $quoted -PassThru
$launch=[ordered]@{playerId=$player.Id;isolatedSaves=$testRoot;miniOnly=[bool]$MiniOnly;highRelief=[bool]$HighRelief;build=(Get-Content -LiteralPath (Join-Path $BuildDirectory 'build-info.json') -Raw | ConvertFrom-Json);presentationSha256=(Get-FileHash -LiteralPath (Join-Path $BuildDirectory 'Aevoria_Data\Managed\Aevoria.Presentation.dll')).Hash;worldSha256=(Get-FileHash -LiteralPath (Join-Path $BuildDirectory 'Aevoria_Data\Managed\Aevoria.World.dll')).Hash}
$launch | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'launch.json') -Encoding UTF8
$launch | ConvertTo-Json -Depth 5
