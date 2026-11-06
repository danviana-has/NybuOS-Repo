$bytes = [System.IO.File]::ReadAllBytes('wallpaper.jpg')
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
$sb.AppendLine("    public static byte[] GetWallpaperJpgBytes() {") | Out-Null
$sb.AppendLine("        if (_cachedBytes != null) return _cachedBytes;") | Out-Null
$sb.AppendLine("        int totalLen = 0;") | Out-Null
$sb.AppendLine("        for (int i = 0; i < Chunks.Length; i++) totalLen += Chunks[i].Length;") | Out-Null
$sb.AppendLine("        var sb = new System.Text.StringBuilder(totalLen);") | Out-Null
$sb.AppendLine("        for (int i = 0; i < Chunks.Length; i++) sb.Append(Chunks[i]);") | Out-Null
$sb.AppendLine("        _cachedBytes = Convert.FromBase64String(sb.ToString());") | Out-Null
$sb.AppendLine("        return _cachedBytes;") | Out-Null
$sb.AppendLine("    }") | Out-Null
$sb.AppendLine("    public static byte[] GetWallpaperBmpBytes() => GetWallpaperJpgBytes();") | Out-Null
$sb.AppendLine("}") | Out-Null
[System.IO.File]::WriteAllText('GUI\EmbeddedWallpaperData.cs.tmp', $sb.ToString(), [System.Text.Encoding]::UTF8)
Move-Item -Path 'GUI\EmbeddedWallpaperData.cs.tmp' -Destination 'GUI\EmbeddedWallpaperData.cs' -Force
Write-Host "Generated EmbeddedWallpaperData.cs successfully with $($chunks.Count) chunks!"
