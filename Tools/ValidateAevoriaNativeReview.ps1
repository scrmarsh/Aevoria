param([Parameter(Mandatory=$true)][string]$EvidenceDirectory,[string]$SuccessToken='AEVORIA011_MOUNTAIN_PROTOTYPE_COMPLETE')
$ErrorActionPreference='Stop'
$evidence=[IO.Path]::GetFullPath($EvidenceDirectory)
$log=[IO.File]::ReadAllText((Join-Path $evidence 'player.log'))
$problems=[Collections.Generic.List[string]]::new()
$exitFile=Join-Path $evidence 'process-exit.json'
if(!(Test-Path -LiteralPath $exitFile)){$problems.Add('Verified native process exit is absent')}else{$processExit=Get-Content -LiteralPath $exitFile -Raw | ConvertFrom-Json;if($null -eq $processExit.exitCode -or $processExit.exitCode -ne 0){$problems.Add('Native process did not exit cleanly')}}
if(-not $log.Contains($SuccessToken)){$problems.Add('Native audit did not report completion')}
if($log -match '(?im)Shader error in|shader is not supported|NullReferenceException:|MissingReferenceException:|Failed to capture screen shot|^ERROR:'){$problems.Add('Player log contains a rendering/runtime failure')}
$failures=Join-Path $evidence 'failures.txt'
if((Test-Path -LiteralPath $failures) -and (Get-Item -LiteralPath $failures).Length -gt 0){$problems.Add('Native audit failure ledger is not empty')}
$viewsFile=Join-Path $evidence 'views.json';$views=$null
if(-not(Test-Path -LiteralPath $viewsFile)){$problems.Add('View manifest is absent')}else{
    $views=Get-Content -LiteralPath $viewsFile -Raw | ConvertFrom-Json
    if(@($views.views).Count -lt 5){$problems.Add('Expected five native views')}
    foreach($view in $views.views){$imagePath=Join-Path $evidence ($view.size+'-'+$view.name+'.png');if(-not(Test-Path -LiteralPath $imagePath)){$problems.Add('Missing native capture: '+$view.name)}elseif((Get-Item -LiteralPath $imagePath).Length -lt 10000){$problems.Add('Empty/truncated capture: '+$view.name)}}
}
[ordered]@{automatedChecksPassed=($problems.Count -eq 0);visualAcceptance='Requires separate inspection of native views and moving samples';problems=@($problems);checkedUtc=[DateTime]::UtcNow.ToString('O')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'native-review-checks.json') -Encoding UTF8
if($problems.Count -gt 0){throw ($problems -join '; ')}
Write-Output 'PASS: native audit completion, shader/runtime log and capture manifest checks; visual acceptance remains separate'
