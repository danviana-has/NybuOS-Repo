$bytes = [System.IO.File]::ReadAllBytes("C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll")
$offset = 405892
$headerSize = [BitConverter]::ToInt32($bytes, $offset + 8)
$numGlyphs = [BitConverter]::ToInt32($bytes, $offset + 16)
$charSize = [BitConverter]::ToInt32($bytes, $offset + 20)
$height = [BitConverter]::ToInt32($bytes, $offset + 24)
$width = [BitConverter]::ToInt32($bytes, $offset + 28)

Write-Host "Offset: $offset, Glyphs: $numGlyphs, CharSize: $charSize, Width: $width, Height: $height"

# Extrai o arquivo PSF2 completo
$totalBytes = $headerSize + ($numGlyphs * $charSize)
$fontData = [byte[]]::new($totalBytes)
[Array]::Copy($bytes, $offset, $fontData, 0, $totalBytes)
[System.IO.File]::WriteAllBytes("CosmosOfficialFont.psf", $fontData)

# Gera a classe C# com renderização procedural exata e nítida
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("using System;")
[void]$sb.AppendLine("namespace NybuOS.Drivers;")
[void]$sb.AppendLine("public static class CosmosNativeFont {")
[void]$sb.AppendLine("    public const int Width = $width;")
[void]$sb.AppendLine("    public const int Height = $height;")
[void]$sb.AppendLine("    public const int BytesPerGlyph = $charSize;")
[void]$sb.AppendLine("    public static readonly byte[] GlyphData = new byte[] {")

$glyphStart = $headerSize
$glyphTotal = $numGlyphs * $charSize
for ($i = $glyphStart; $i -lt $glyphStart + $glyphTotal; $i += 16) {
    $sliceLen = [Math]::Min(16, $glyphStart + $glyphTotal - $i)
    $slice = [byte[]]::new($sliceLen)
    [Array]::Copy($fontData, $i, $slice, 0, $sliceLen)
    $hex = ($slice | ForEach-Object { "0x{0:X2}" -f $_ }) -join ", "
    if ($i + 16 -lt $glyphStart + $glyphTotal) { $hex += "," }
    [void]$sb.AppendLine("        " + $hex)
}

[void]$sb.AppendLine("    };")
[void]$sb.AppendLine("    public static bool GetPixel(char c, int x, int y) {")
[void]$sb.AppendLine("        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;")
[void]$sb.AppendLine("        int glyphIndex = (int)c;")
[void]$sb.AppendLine("        if (glyphIndex < 0 || glyphIndex >= $numGlyphs) glyphIndex = 63;")
[void]$sb.AppendLine("        int bytesPerRow = (Width + 7) / 8;")
[void]$sb.AppendLine("        int byteOffset = (glyphIndex * BytesPerGlyph) + (y * bytesPerRow) + (x / 8);")
[void]$sb.AppendLine("        if (byteOffset >= GlyphData.Length) return false;")
[void]$sb.AppendLine("        byte b = GlyphData[byteOffset];")
[void]$sb.AppendLine("        int bit = 7 - (x % 8);")
[void]$sb.AppendLine("        return ((b >> bit) & 1) != 0;")
[void]$sb.AppendLine("    }")
[void]$sb.AppendLine("}")

[System.IO.File]::WriteAllText("Drivers\CosmosNativeFont.cs", $sb.ToString())
Write-Host "Created Drivers\CosmosNativeFont.cs ($width x $height)"
