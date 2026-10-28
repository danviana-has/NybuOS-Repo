$bytes = [System.IO.File]::ReadAllBytes("wallpaper_800x564.bmp")
$base64 = [Convert]::ToBase64String($bytes)

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("using System;")
[void]$sb.AppendLine("namespace NybuOS.GUI;")
[void]$sb.AppendLine("public static class EmbeddedWallpaperData {")
[void]$sb.AppendLine("    private static readonly string[] Chunks = new string[] {")

$chunkSize = 4000
for ($i = 0; $i -lt $base64.Length; $i += $chunkSize) {
    $len = [Math]::Min($chunkSize, $base64.Length - $i)
    $part = $base64.Substring($i, $len)
    [void]$sb.AppendLine("        @`"$part`",")
}

[void]$sb.AppendLine("    };")
[void]$sb.AppendLine("    public static byte[] GetWallpaperBmpBytes() {")
[void]$sb.AppendLine("        return Convert.FromBase64String(string.Concat(Chunks));")
[void]$sb.AppendLine("    }")
[void]$sb.AppendLine("}")

[System.IO.File]::WriteAllText("GUI\EmbeddedWallpaperData.cs", $sb.ToString())
Write-Host "Done generating GUI\EmbeddedWallpaperData.cs ($($bytes.Length) bytes)"
