param([Parameter(Mandatory=$true)][string]$EvidenceDirectory)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($EvidenceDirectory);$output=Join-Path $root Replays
New-Item -ItemType Directory -Path $output -Force | Out-Null
$ffmpeg=(Get-Command ffmpeg -ErrorAction Stop).Source
foreach($prefix in @('doctor-walk','doctor-run','doctor-crouch','doctor-uphill-walk','doctor-downhill-walk','doctor-Pickaxe-strike','roman-Pickaxe-strike')){
    $frames=@(Get-ChildItem -LiteralPath $root -Filter ($prefix+'-*.png') | Where-Object {$_.BaseName -match ('^'+[regex]::Escape($prefix)+'-\d+$')} | Sort-Object Name)
    if($frames.Count -lt 2){throw ('Missing moving samples: '+$prefix)}
    $fps=if($prefix.EndsWith('strike')){30}else{8}
    $lines=[Collections.Generic.List[string]]::new();$lines.Add('ffconcat version 1.0')
    foreach($frame in $frames){if($frame.FullName.Contains("'")){throw 'Evidence path must not contain an apostrophe'};$lines.Add("file '"+$frame.FullName.Replace('\','/')+"'");$lines.Add('duration '+(1.0/$fps).ToString('R',[Globalization.CultureInfo]::InvariantCulture))}
    $list=Join-Path $output ($prefix+'.ffconcat');[IO.File]::WriteAllLines($list,$lines,[Text.UTF8Encoding]::new($false))
    $video=Join-Path $output ($prefix+'.mp4')
    & $ffmpeg -hide_banner -loglevel error -y -f concat -safe 0 -i $list -vf 'scale=1280:-2' -r $fps -c:v libx264 -crf 22 -pix_fmt yuv420p $video
    if($LASTEXITCODE -ne 0){throw 'Replay encode failed'}
    if($prefix.EndsWith('strike')){& $ffmpeg -hide_banner -loglevel error -y -stream_loop 7 -i $video -c copy (Join-Path $output ($prefix+'-loop.mp4'));if($LASTEXITCODE -ne 0){throw 'Replay repetition failed'}}
    if($prefix -eq 'doctor-walk' -or $prefix -eq 'doctor-crouch'){& $ffmpeg -hide_banner -loglevel error -y -f concat -safe 0 -i $list -vf 'crop=700:970:610:100,scale=280:388,tile=4x2' -frames:v 1 (Join-Path $output ($prefix+'-sequence.png'));if($LASTEXITCODE -ne 0){throw 'Sequence strip failed'}}
}
Write-Output 'Encoded native sampled replays. Locomotion is an 8fps sampled replay; strike samples were captured every second simulated frame at 60fps and replay at30fps.'
