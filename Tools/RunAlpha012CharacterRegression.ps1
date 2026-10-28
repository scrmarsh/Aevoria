param([Parameter(Mandatory=$true)][string]$BuildDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$build=[IO.Path]::GetFullPath($BuildDirectory);$output=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $output){throw 'Choose fresh regression evidence'}
New-Item -ItemType Directory -Path $output | Out-Null
$save=Join-Path D:/AevoriaValidation ('CharacterRegression012-'+[guid]::NewGuid().ToString('N'))
# Reuse the actual established UI/mining/save-load audit, without modifying its
# product assertions. The historical flag names the audit, not the build version.
$arguments=@('-aevoriaAudit010Characters','-aevoriaTestRoot',$save,'-aevoriaOutput',$output,'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',(Join-Path $output 'player.log'))
[ordered]@{build=$build;isolatedSaves=$save;arguments=$arguments;presentationSha256=(Get-FileHash (Join-Path $build Aevoria_Data/Managed/Aevoria.Presentation.dll)).Hash} | ConvertTo-Json | Set-Content (Join-Path $output 'launch.json') -Encoding UTF8
$process=Start-Process -FilePath (Join-Path $build Aevoria.exe) -ArgumentList ($arguments | ForEach-Object {'"'+$_+'"'}) -PassThru
$process.WaitForExit();Set-Content (Join-Path $output 'exit-code.txt') $process.ExitCode
if($process.ExitCode -ne 0){throw "Character gameplay regression exited $($process.ExitCode)"}
$result=Get-Content (Join-Path $output 'result.json') -Raw | ConvertFrom-Json
if(!$result.passed -or $result.runtimeErrors -ne 0 -or !$result.boulderMiningReloaded){throw 'Native UI/mining regression failed'}
Get-Content (Join-Path $output 'interface-result.txt');Get-Content (Join-Path $output 'boulder-result.txt')
