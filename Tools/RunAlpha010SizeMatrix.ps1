param([Parameter(Mandatory=$true)][string]$BuildDirectory,[Parameter(Mandatory=$true)][string]$OutputRoot,[switch]$Resume)
$ErrorActionPreference='Stop'
$evidenceRoot=[IO.Path]::GetFullPath($OutputRoot)
if((Test-Path -LiteralPath $evidenceRoot) -and !$Resume){throw 'Choose a fresh all-size evidence directory or explicitly resume completed results'}
New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
$expectedWidths=@{Miniature=128000;Small=256000;Medium=512000;Large=1024000;Colossal=2048000}
$summaries=@()
foreach($size in @('Miniature','Small','Medium','Large','Colossal')){
    $evidence=Join-Path $evidenceRoot $size
    "Running $size" | Set-Content -LiteralPath (Join-Path $evidenceRoot 'status.txt')
    if(!(Test-Path -LiteralPath (Join-Path $evidence 'result.json'))){
        & (Join-Path $PSScriptRoot 'RunAlpha010FoundationAudit.ps1') -BuildDirectory $BuildDirectory -OutputDirectory $evidence -WorldSize $size -Months 12 | Out-Null
    }
    $launch=Get-Content -LiteralPath (Join-Path $evidence 'launch.json') -Raw | ConvertFrom-Json
    $deadline=[DateTime]::UtcNow.AddMinutes(25)
    while($true){
        $ownedPlayer=Get-CimInstance Win32_Process -Filter ('ProcessId='+$launch.playerId)
        # A completed player's PID can be reused by another Windows process.
        if(!$ownedPlayer -or $ownedPlayer.ExecutablePath -ne (Join-Path $BuildDirectory 'Aevoria.exe') -or !$ownedPlayer.CommandLine.Contains($evidence)){break}
        if([DateTime]::UtcNow -gt $deadline){throw "$size audit exceeded deadline; inspect its owned player and evidence"}
        Start-Sleep -Seconds 2
    }
    $resultFile=Join-Path $evidence 'result.json'
    if(!(Test-Path -LiteralPath $resultFile)){throw "$size native audit exited without a result; inspect player.log"}
    $result=Get-Content -LiteralPath $resultFile -Raw | ConvertFrom-Json
    # Huge is the retained enum alias for Large. Mono may return either name.
    $canonicalSize=if($result.worldSize -eq 'Huge'){'Large'}else{$result.worldSize}
    if($canonicalSize -ne $size -or !$result.persistence -or !$result.cleanup -or $result.errors -ne 0 -or $result.simulatedMonths -lt 12){throw "$size native acceptance failed"}
    if($launch.presentationSha256 -ne (Get-FileHash -LiteralPath (Join-Path $BuildDirectory 'Aevoria_Data\Managed\Aevoria.Presentation.dll')).Hash -or $launch.worldSha256 -ne (Get-FileHash -LiteralPath (Join-Path $BuildDirectory 'Aevoria_Data\Managed\Aevoria.World.dll')).Hash){throw "$size result belongs to another binary"}
    $metadataFile=@(Get-ChildItem -LiteralPath $launch.isolatedSaves -Filter metadata.xml -Recurse)
    if($metadataFile.Count -ne 1){throw "$size must have one actual saved world"}
    [xml]$metadata=Get-Content -LiteralPath $metadataFile[0].FullName
    if([double]$metadata.WorldMetadata.Width -ne $expectedWidths[$size] -or [double]$metadata.WorldMetadata.Depth -ne $expectedWidths[$size]){throw "$size saved geography dimensions do not match the requested preset"}
    $result | Add-Member -NotePropertyName canonicalWorldSize -NotePropertyValue $canonicalSize -Force
    $result | Add-Member -NotePropertyName savedWidthMeters -NotePropertyValue ([double]$metadata.WorldMetadata.Width) -Force
    $summaries+=$result
    $summaries | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'summary.json') -Encoding UTF8
    Write-Output "$size passed: $($result.initialCivilizations) civilizations; $($result.renders) renders"
}
'PASS: all five actual sizes completed native creation, rendered observation, a live year, save/load and cleanup.' | Set-Content -LiteralPath (Join-Path $evidenceRoot 'status.txt')
