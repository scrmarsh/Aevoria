param([Parameter(Mandatory=$true)][string]$BuildDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory,[string]$Seed='shaping-continents',[ValidateRange(0,16000)][int]$Relief=3600,[ValidateSet('Miniature','Small','Medium','Large','Colossal')][string]$WorldSize='Miniature',[switch]$Motion,[switch]$Contacts,[switch]$Zoom,[switch]$RegionalCoverage,[switch]$Atlas,[ValidateRange(0,1)][int]$AtlasRevision=0,[ValidateSet('Prototype','Water','Coast','Full','Climate','Materials','Interface')][string]$Review='Prototype')
$ErrorActionPreference='Stop'
if($AtlasRevision -gt 0 -and !$Atlas){throw 'An Atlas terrain revision requires -Atlas'}
$build=[IO.Path]::GetFullPath($BuildDirectory);$evidence=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $evidence){throw 'Choose a fresh prototype evidence directory'}
New-Item -ItemType Directory -Path $evidence | Out-Null
$saveRoot=Join-Path D:\AevoriaValidation ('Prototype011-'+[guid]::NewGuid().ToString('N'))
$baselineWorldDirectory=$env:AEVORIA_GENESIS_BASELINE_WORLD
$baselineId='';$baselineArchiveHash=''
if($baselineWorldDirectory){
    $baselineWorldDirectory=[IO.Path]::GetFullPath($baselineWorldDirectory);$baselineId=Split-Path $baselineWorldDirectory -Leaf
    if($baselineId -notmatch '^[0-9a-fA-F]{32}$' -or !(Test-Path -LiteralPath (Join-Path $baselineWorldDirectory 'world.xml'))){throw 'Explicit baseline fixture must be a complete saved-world directory with a GUID name'}
    $baselineArchiveHash=(Get-FileHash -LiteralPath (Join-Path $baselineWorldDirectory 'world.xml')).Hash
    $fixtureParent=Join-Path $saveRoot 'Worlds';New-Item -ItemType Directory -Path $fixtureParent -Force | Out-Null
    Copy-Item -LiteralPath $baselineWorldDirectory -Destination (Join-Path $fixtureParent $baselineId) -Recurse
    [ordered]@{source=$baselineWorldDirectory;sha256=$baselineArchiveHash;isolatedCopy=(Join-Path $fixtureParent $baselineId)} | ConvertTo-Json | Set-Content (Join-Path $evidence 'baseline-save-fixture.json') -Encoding UTF8
}
$arguments=@('-aevoriaAudit009Terrain','-aevoria009Size',$WorldSize,'-aevoria009Seed',$Seed,'-aevoria011Relief',[string]$Relief,'-aevoriaTestRoot',$saveRoot,'-aevoriaOutput',$evidence,'-screen-width','2560','-screen-height','1440','-screen-fullscreen','0','-logFile',(Join-Path $evidence 'player.log'))
if($Review -eq 'Prototype'){$arguments+='-aevoria011MountainPrototype'}elseif($Review -eq 'Water'){$arguments+='-aevoria009WaterOnly'}elseif($Review -eq 'Coast'){$arguments+='-aevoria009CoastOnly'}
if($Atlas){$arguments+=@('-aevoria014Atlas','-aevoria014AtlasRevision',[string]$AtlasRevision)}
if($Review -eq 'Materials'){$arguments+='-aevoria014MaterialsOnly'}
if($Review -eq 'Interface'){$arguments+='-aevoria014InterfaceOnly'}
if($baselineId){$arguments+=@('-aevoria014LoadBaseline',$baselineId)}
if($Review -eq 'Climate'){$arguments+='-aevoria013ClimateOnly'}
if($Motion){$arguments+='-aevoria011Motion'}
if($Contacts){$arguments+='-aevoria011Contacts'}
if($Zoom){$arguments+='-aevoria011WaterZoom'}
if($RegionalCoverage){$arguments+='-aevoria011RegionalCoverage'}
# The native rendering review needs a real game window for ScreenCapture.
# Only the telemetry helper below is a hidden background process.
$player=Start-Process -FilePath (Join-Path $build 'Aevoria.exe') -ArgumentList ($arguments | ForEach-Object {'"'+$_+'"'}) -PassThru
$sampleArgs=@('-NoProfile','-File',(Join-Path $PSScriptRoot 'SampleAevoriaProcess.ps1'),'-PlayerId',[string]$player.Id,'-OutputFile',(Join-Path $evidence 'process-memory.csv'))
$sampler=Start-Process powershell -ArgumentList ($sampleArgs | ForEach-Object {'"'+$_+'"'}) -WindowStyle Hidden -PassThru
[ordered]@{playerId=$player.Id;samplerId=$sampler.Id;seed=$Seed;requestedSize=$WorldSize;mountainRelief=$Relief;review=$Review;motion=[bool]$Motion;contacts=[bool]$Contacts;zoom=[bool]$Zoom;regionalCoverage=[bool]$RegionalCoverage;arguments=$arguments;savedWorlds=$saveRoot;build=$build;worldSha256=(Get-FileHash (Join-Path $build 'Aevoria_Data/Managed/Aevoria.World.dll')).Hash;presentationSha256=(Get-FileHash (Join-Path $build 'Aevoria_Data/Managed/Aevoria.Presentation.dll')).Hash;scope='Native canonical terrain/water review. Captures require visual review; successful automation is not acceptance.'} | ConvertTo-Json | Set-Content (Join-Path $evidence 'launch.json') -Encoding UTF8
Get-Content (Join-Path $evidence 'launch.json')
