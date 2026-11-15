Add-Type -AssemblyName System.Drawing

# 1. Convert wallpaper to high quality BMP
$srcWp = [System.Drawing.Image]::FromFile((Resolve-Path "assets/wallpaper.jpg"))
Write-Host "Wallpaper original: $($srcWp.Width) x $($srcWp.Height)"

# Create high-res 1024x768 or 1200x673 24bpp BMP
$bmpWp = New-Object System.Drawing.Bitmap(1024, 768, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$gWp = [System.Drawing.Graphics]::FromImage($bmpWp)
$gWp.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gWp.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$gWp.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$gWp.DrawImage($srcWp, 0, 0, 1024, 768)
$gWp.Dispose()
$srcWp.Dispose()

$wpPath = "assets/wallpaper.bmp"
$bmpWp.Save($wpPath, [System.Drawing.Imaging.ImageFormat]::Bmp)
$bmpWp.Dispose()
Write-Host "Saved $wpPath, size: $((Get-Item $wpPath).Length) bytes"

# 2. Convert each icon into 16x16, 24x24, 48x48 32-bit BMPs with alpha transparency!
$iconsDir = "assets/icons"
$bmpIconsDir = "assets/icons_bmp"
if (-not (Test-Path $bmpIconsDir)) { New-Item -ItemType Directory -Path $bmpIconsDir | Out-Null }

foreach ($pngFile in Get-ChildItem "$iconsDir/*.png") {
    $img = [System.Drawing.Bitmap]::FromFile($pngFile.FullName)
    $name = [System.IO.Path]::GetFileNameWithoutExtension($pngFile.Name)
    
    if ($pngFile.Name -eq "OS Icon.png") {
        # OS Icon: 32x18 for taskbar
        $targetBmp = New-Object System.Drawing.Bitmap(32, 18, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($targetBmp)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.DrawImage($img, 0, 0, 32, 18)
        $g.Dispose()
        $targetBmp.Save("$bmpIconsDir/os_logo_32.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
        $targetBmp.Dispose()
    }
    
    foreach ($sz in @(16, 24, 48)) {
        $targetBmp = New-Object System.Drawing.Bitmap($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($targetBmp)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.DrawImage($img, 0, 0, $sz, $sz)
        $g.Dispose()
        $targetBmp.Save("$bmpIconsDir/${name}_${sz}.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
        $targetBmp.Dispose()
    }
    $img.Dispose()
    Write-Host "Processed icon $name"
}
