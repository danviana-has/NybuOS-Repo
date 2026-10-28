Add-Type -AssemblyName System.Drawing

$jpgPath = Resolve-Path ".\wallpaper.jpg"
$b = [System.Drawing.Image]::FromFile($jpgPath.Path)

$w = 800
$h = 564
$bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($b, 0, 0, $w, $h)

$scaledBmpPath = Join-Path $PSScriptRoot "wallpaper_800x564.bmp"
$bmp.Save($scaledBmpPath, [System.Drawing.Imaging.ImageFormat]::Bmp)

$bytes = [System.IO.File]::ReadAllBytes($scaledBmpPath)
Write-Host "Created wallpaper_800x564.bmp: $($bytes.Length) bytes ($w x $h)"

$b.Dispose()
$bmp.Dispose()
$g.Dispose()
