param([string]$InputCsv='Docs/Validation/Alpha-v0.0.8/local-height-probe.csv',[string]$OutputPng='M:\Aevoria\Docs\Validation\Alpha-v0.0.8\local-height-probe-after.png')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$rows=Import-Csv -LiteralPath $InputCsv
if($rows.Count -ne 257*257){throw 'Expected the 257-square local field probe.'}
$raw=New-Object double[] $rows.Count;$carved=New-Object double[] $rows.Count
for($i=0;$i -lt $rows.Count;$i++){$raw[$i]=[double]$rows[$i].raw;$carved[$i]=[double]$rows[$i].carved}
$bmp=New-Object Drawing.Bitmap 771,282
$gfx=[Drawing.Graphics]::FromImage($bmp);$gfx.Clear([Drawing.Color]::White)
$labels=@('Base slope x5','Carved slope x5','Carving depth / 500 m')
$font=New-Object Drawing.Font 'Arial',10
for($k=0;$k -lt 3;$k++){$gfx.DrawString($labels[$k],$font,[Drawing.Brushes]::Black,($k*257),261)}
for($z=1;$z -lt 256;$z++){for($x=1;$x -lt 256;$x++){
    $i=$z*257+$x
    for($k=0;$k -lt 2;$k++){
        if($k -eq 0){$a=$raw}else{$a=$carved}
        $sx=($a[$i-1]-$a[$i+1])/32*5;$sz=($a[$i-257]-$a[$i+257])/32*5
        $length=[Math]::Sqrt($sx*$sx+$sz*$sz+1)
        $r=[int](127+127*$sx/$length);$g=[int](255/$length);$b=[int](127+127*$sz/$length)
        $bmp.SetPixel(($x+$k*257),(256-$z),[Drawing.Color]::FromArgb($r,$g,$b))
    }
    $v=[int](255*[Math]::Max([double]0,[Math]::Min([double]1,($raw[$i]-$carved[$i])/500)))
    $bmp.SetPixel(($x+514),(256-$z),[Drawing.Color]::FromArgb($v,$v,$v))
}}
$bmp.Save([IO.Path]::GetFullPath($OutputPng));$font.Dispose();$gfx.Dispose();$bmp.Dispose()
