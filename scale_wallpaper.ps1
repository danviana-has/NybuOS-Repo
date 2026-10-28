Add-Type -AssemblyName System.Drawing

$jpgPath = Resolve-Path ".\wallpaper.jpg"
$b = New-Object System.Drawing.Bitmap($jpgPath.Path)
$targetW = 800
$targetH = 560
$res = New-Object System.Drawing.Bitmap($targetW, $targetH)
$g = [System.Drawing.Graphics]::FromImage($res)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($b, 0, 0, $targetW, $targetH)

$scaledBmpPath = Join-Path $PSScriptRoot "wallpaper_scaled.bmp"
$res.Save($scaledBmpPath, [System.Drawing.Imaging.ImageFormat]::Bmp)

$rawBytes = [System.IO.File]::ReadAllBytes($scaledBmpPath)
Write-Host "Scaled BMP created: $($rawBytes.Length) bytes ($targetW x $targetH)"

$b.Dispose()
$res.Dispose()
$g.Dispose()
