param([Parameter(Mandatory=$true)][string]$BuildDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$ForceD3D11)
$ErrorActionPreference='Stop'
$build=[IO.Path]::GetFullPath($BuildDirectory);$output=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $output){throw 'Choose a fresh input evidence directory'}
New-Item -ItemType Directory -Path $output | Out-Null
$save=Join-Path D:/AevoriaValidation ('Inputs011-'+[guid]::NewGuid().ToString('N'))
[ordered]@{isolatedSaves=$save;build=$build;forceD3D11=[bool]$ForceD3D11;presentationSha256=(Get-FileHash (Join-Path $build 'Aevoria_Data/Managed/Aevoria.Presentation.dll')).Hash;worldSha256=(Get-FileHash (Join-Path $build 'Aevoria_Data/Managed/Aevoria.World.dll')).Hash} | ConvertTo-Json | Set-Content (Join-Path $output 'launch.json') -Encoding UTF8
foreach($stage in 1,2){
    $arguments=@('-aevoriaAudit011Inputs','-aevoriaTestRoot',$save,'-aevoriaOutput',$output,'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',(Join-Path $output "player-stage$stage.log"))
    if($stage -eq 2){$arguments+='-aevoria011InputReload'}
    if($ForceD3D11){$arguments+='-force-d3d11'}
    $player=Start-Process -FilePath (Join-Path $build 'Aevoria.exe') -ArgumentList ($arguments | ForEach-Object {'"'+$_+'"'}) -PassThru
    $player.WaitForExit()
    if($player.ExitCode -ne 0){throw "Input review stage $stage exited $($player.ExitCode)"}
    Add-Content (Join-Path $output 'exit-codes.txt') "stage=$stage exitCode=$($player.ExitCode)"
    $result=if($stage -eq 1){'input-stage-one.txt'}else{'input-restart-result.txt'}
    if(!(Test-Path (Join-Path $output $result))){throw "Input review stage $stage did not complete"}
    Get-Content (Join-Path $output $result)
}
