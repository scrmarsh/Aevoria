param([Parameter(Mandatory=$true)][string]$BuildDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$CharacterOnly,[string]$Seed="shaping-continents")
$ErrorActionPreference='Stop'
$output=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $output){throw 'Choose a fresh evidence directory'}
New-Item -ItemType Directory -Path $output | Out-Null
$testRoot=Join-Path 'D:\AevoriaValidation' ('Underground009-'+[guid]::NewGuid().ToString('N'))
$arguments=@('-aevoriaAudit009Underground','-aevoriaTestRoot',$testRoot,'-aevoriaOutput',$output,'-screen-width','2560','-screen-height','1440','-screen-fullscreen','0','-logFile',(Join-Path $output 'player.log'))
$arguments+=@('-aevoria011CaveSeed',$Seed)
if($CharacterOnly){$arguments+='-aevoria009CharacterOnly'}
$quoted=$arguments | ForEach-Object {'"'+$_+'"'}
$player=Start-Process -FilePath (Join-Path $BuildDirectory 'Aevoria.exe') -ArgumentList $quoted -PassThru
$launch=[ordered]@{playerId=$player.Id;isolatedSaves=$testRoot;build=(Get-Content -LiteralPath (Join-Path $BuildDirectory 'build-info.json') -Raw | ConvertFrom-Json);presentationSha256=(Get-FileHash -LiteralPath (Join-Path $BuildDirectory 'Aevoria_Data\Managed\Aevoria.Presentation.dll')).Hash;worldSha256=(Get-FileHash -LiteralPath (Join-Path $BuildDirectory 'Aevoria_Data\Managed\Aevoria.World.dll')).Hash}
$launch | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'launch.json') -Encoding UTF8
$launch | ConvertTo-Json -Depth 5
$player.WaitForExit()
Set-Content (Join-Path $output 'exit-code.txt') $player.ExitCode
if($player.ExitCode -ne 0){throw "Underground review exited $($player.ExitCode)"}
if(!(Test-Path (Join-Path $output 'result.txt'))){throw 'Underground review did not complete'}
Get-Content (Join-Path $output 'result.txt')
