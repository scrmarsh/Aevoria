param([Parameter(Mandatory=$true)][string]$BuildDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory,[string]$CompatibilityWorldSource='',[switch]$DoctorOnly)
$ErrorActionPreference='Stop'
$build=[IO.Path]::GetFullPath($BuildDirectory);$output=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $output){throw 'Choose a fresh character evidence directory'}
New-Item -ItemType Directory -Path $output | Out-Null
$save=Join-Path D:/AevoriaValidation ('Character012-'+[guid]::NewGuid().ToString('N'))
$arguments=@('-aevoriaAudit012Characters','-aevoriaTestRoot',$save,'-aevoriaOutput',$output,'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',(Join-Path $output 'player.log'))
if($DoctorOnly){$arguments+='-aevoriaDoctorReview'}
if($CompatibilityWorldSource){New-Item -ItemType Directory -Path $save | Out-Null;Copy-Item -LiteralPath (Join-Path $CompatibilityWorldSource 'Worlds') -Destination $save -Recurse;$worlds=@(Get-ChildItem (Join-Path $save 'Worlds') -Directory);if($worlds.Count -ne 1){throw 'Compatibility fixture must contain exactly one world'};$arguments+=@('-aevoriaCompatibilityWorld',$worlds[0].Name)}
[ordered]@{build=$build;isolatedSaves=$save;arguments=$arguments;worldSha256=(Get-FileHash (Join-Path $build Aevoria_Data/Managed/Aevoria.World.dll)).Hash;presentationSha256=(Get-FileHash (Join-Path $build Aevoria_Data/Managed/Aevoria.Presentation.dll)).Hash} | ConvertTo-Json | Set-Content (Join-Path $output 'launch.json') -Encoding UTF8
$player=Start-Process -FilePath (Join-Path $build Aevoria.exe) -ArgumentList ($arguments | ForEach-Object {'"'+$_+'"'}) -PassThru
$player.WaitForExit();Set-Content (Join-Path $output 'exit-code.txt') $player.ExitCode
if($player.ExitCode -ne 0){throw "Character review exited $($player.ExitCode)"}
if(!(Test-Path (Join-Path $output result.txt))){throw 'Character review did not finish'}
Get-Content (Join-Path $output result.txt)
