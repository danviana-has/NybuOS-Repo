Add-Type -AssemblyName System.Drawing
$srcPath = "wallpaper.jpg"
if (![System.IO.File]::Exists($srcPath)) {
    Write-Error "wallpaper.jpg not found!"
    exit 1
}

$src = [System.Drawing.Bitmap]::FromFile($srcPath)
Write-Host "Source image loaded: $($src.Width)x$($src.Height)"

# Resolução 1280x720 HD 16:9 de altíssima fidelidade
$targetW = 1280
$targetH = 720
$bmp = New-Object System.Drawing.Bitmap($targetW, $targetH, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.DrawImage($src, 0, 0, $targetW, $targetH)
$g.Dispose()
$src.Dispose()

$bmpPath = "wallpaper.bmp"
$bmp.Save($bmpPath, [System.Drawing.Imaging.ImageFormat]::Bmp)
$bmp.Dispose()
Write-Host "Saved wallpaper.bmp ($((Get-Item $bmpPath).Length) bytes)"

# Atualiza EmbeddedWallpaperData.cs com o BMP
$bytes = [System.IO.File]::ReadAllBytes($bmpPath)
$b64 = [Convert]::ToBase64String($bytes)
$chunkSize = 32000
$chunks = New-Object System.Collections.Generic.List[string]
for ($i = 0; $i -lt $b64.Length; $i += $chunkSize) {
    $len = [Math]::Min($chunkSize, $b64.Length - $i)
    $chunks.Add('        @"' + $b64.Substring($i, $len) + '"')
}

$sb = New-Object System.Text.StringBuilder
$sb.AppendLine("using System;") | Out-Null
$sb.AppendLine("using System.IO;") | Out-Null
$sb.AppendLine("namespace NybuOS.GUI;") | Out-Null
$sb.AppendLine("public static class EmbeddedWallpaperData {") | Out-Null
$sb.AppendLine("    private static readonly string[] Chunks = new string[] {") | Out-Null
$sb.AppendLine(($chunks -join ",`r`n")) | Out-Null
$sb.AppendLine("    };") | Out-Null
$sb.AppendLine("    private static byte[]? _cachedBytes = null;") | Out-Null
$sb.AppendLine("    public static byte[] GetWallpaperBmpBytes() {") | Out-Null
$sb.AppendLine("        if (_cachedBytes != null) return _cachedBytes;") | Out-Null
$sb.AppendLine("        int totalLen = 0;") | Out-Null
$sb.AppendLine("        for (int i = 0; i < Chunks.Length; i++) totalLen += Chunks[i].Length;") | Out-Null
$sb.AppendLine("        var sb = new System.Text.StringBuilder(totalLen);") | Out-Null
$sb.AppendLine("        for (int i = 0; i < Chunks.Length; i++) sb.Append(Chunks[i]);") | Out-Null
$sb.AppendLine("        _cachedBytes = Convert.FromBase64String(sb.ToString());") | Out-Null
$sb.AppendLine("        return _cachedBytes;") | Out-Null
$sb.AppendLine("    }") | Out-Null
$sb.AppendLine("    public static byte[] GetWallpaperJpgBytes() => GetWallpaperBmpBytes();") | Out-Null
$sb.AppendLine("}") | Out-Null

[System.IO.File]::WriteAllText('GUI\EmbeddedWallpaperData.cs.tmp', $sb.ToString(), [System.Text.Encoding]::UTF8)
Move-Item -Path 'GUI\EmbeddedWallpaperData.cs.tmp' -Destination 'GUI\EmbeddedWallpaperData.cs' -Force
Write-Host "Generated EmbeddedWallpaperData.cs with $($chunks.Count) chunks for wallpaper.bmp!"
