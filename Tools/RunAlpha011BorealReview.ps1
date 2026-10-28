param([Parameter(Mandatory=$true)][string]$BuildDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$ForceD3D11)
$ErrorActionPreference='Stop'
$build=[IO.Path]::GetFullPath($BuildDirectory);$output=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $output){throw 'Choose a fresh boreal evidence directory'}
New-Item -ItemType Directory -Path $output | Out-Null
$save=Join-Path D:/AevoriaValidation ('Boreal011-'+[guid]::NewGuid().ToString('N'))
$arguments=@('-aevoriaAudit009Environment','-aevoria011BorealOnly','-aevoriaTestRoot',$save,'-aevoriaOutput',$output,'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',(Join-Path $output 'player.log'))
if($ForceD3D11){$arguments+='-force-d3d11'}
[ordered]@{isolatedSaves=$save;build=$build;arguments=$arguments;presentationSha256=(Get-FileHash (Join-Path $build 'Aevoria_Data/Managed/Aevoria.Presentation.dll')).Hash;worldSha256=(Get-FileHash (Join-Path $build 'Aevoria_Data/Managed/Aevoria.World.dll')).Hash} | ConvertTo-Json | Set-Content (Join-Path $output 'launch.json') -Encoding UTF8
$player=Start-Process -FilePath (Join-Path $build 'Aevoria.exe') -ArgumentList ($arguments | ForEach-Object {'"'+$_+'"'}) -PassThru
$player.WaitForExit()
Set-Content (Join-Path $output 'exit-code.txt') $player.ExitCode
if($player.ExitCode -ne 0){throw "Boreal review exited $($player.ExitCode)"}
if(!(Test-Path (Join-Path $output 'boreal-result.txt'))){throw 'Boreal review did not complete'}
Get-Content (Join-Path $output 'boreal-result.txt')
