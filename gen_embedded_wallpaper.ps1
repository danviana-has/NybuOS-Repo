$jpgPath = Resolve-Path ".\wallpaper.jpg"
$bytes = [System.IO.File]::ReadAllBytes($jpgPath.Path)

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("using System;")
[void]$sb.AppendLine("namespace NybuOS.GUI;")
[void]$sb.AppendLine("public static class EmbeddedWallpaperData {")
[void]$sb.AppendLine("    public static byte[] GetWallpaperJpgBytes() {")
[void]$sb.AppendLine("        return new byte[] {")

for ($i = 0; $i -lt $bytes.Length; $i += 32) {
    $chunkLen = [Math]::Min(32, $bytes.Length - $i)
    $slice = [byte[]]::new($chunkLen)
    [Array]::Copy($bytes, $i, $slice, 0, $chunkLen)
    $hex = ($slice | ForEach-Object { "0x{0:X2}" -f $_ }) -join ", "
    if ($i + 32 -lt $bytes.Length) { $hex += "," }
    [void]$sb.AppendLine("            " + $hex)
}

[void]$sb.AppendLine("        };")
[void]$sb.AppendLine("    }")
[void]$sb.AppendLine("}")

[System.IO.File]::WriteAllText("GUI\EmbeddedWallpaperData.cs", $sb.ToString())
Write-Host "Generated GUI\EmbeddedWallpaperData.cs ($($bytes.Length) bytes)"
