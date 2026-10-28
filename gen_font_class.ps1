$bytes = [System.IO.File]::ReadAllBytes("extracted_cosmos_font.psf")
# Header PSF1:
# byte 0: 0x36
# byte 1: 0x04
# byte 2: font mode
# byte 3: character size (bytes per glyph)
$charSize = $bytes[3]
Write-Host "PSF1 char size (glyph height): $charSize"

# Cada glifo ocupa $charSize bytes. No PSF1, a largura é sempre 8 bits (1 byte por linha).
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("using System;")
[void]$sb.AppendLine("namespace NybuOS.Drivers;")
[void]$sb.AppendLine("public static class CosmosNativeFont {")
[void]$sb.AppendLine("    public const int Width = 8;")
[void]$sb.AppendLine("    public const int Height = $charSize;")
[void]$sb.AppendLine("    // 256 caracteres extraídos diretamente da fonte PSF original do Cosmos OS")
[void]$sb.AppendLine("    public static readonly byte[] GlyphData = new byte[] {")

$glyphBytesTotal = 256 * $charSize
for ($i = 4; $i -lt 4 + $glyphBytesTotal; $i += 16) {
    $sliceLen = [Math]::Min(16, 4 + $glyphBytesTotal - $i)
    $slice = [byte[]]::new($sliceLen)
    [Array]::Copy($bytes, $i, $slice, 0, $sliceLen)
    $hex = ($slice | ForEach-Object { "0x{0:X2}" -f $_ }) -join ", "
    if ($i + 16 -lt 4 + $glyphBytesTotal) { $hex += "," }
    [void]$sb.AppendLine("        " + $hex)
}

[void]$sb.AppendLine("    };")
[void]$sb.AppendLine("    public static bool GetPixel(char c, int x, int y) {")
[void]$sb.AppendLine("        if (x < 0 || x >= 8 || y < 0 || y >= Height) return false;")
[void]$sb.AppendLine("        int glyphIndex = (int)c;")
[void]$sb.AppendLine("        if (glyphIndex < 0 || glyphIndex >= 256) glyphIndex = 63; // '?'")
[void]$sb.AppendLine("        int byteOffset = (glyphIndex * Height) + y;")
[void]$sb.AppendLine("        if (byteOffset >= GlyphData.Length) return false;")
[void]$sb.AppendLine("        byte rowByte = GlyphData[byteOffset];")
[void]$sb.AppendLine("        return ((rowByte >> (7 - x)) & 1) != 0;")
[void]$sb.AppendLine("    }")
[void]$sb.AppendLine("}")

[System.IO.File]::WriteAllText("Drivers\CosmosNativeFont.cs", $sb.ToString())
Write-Host "Generated Drivers\CosmosNativeFont.cs successfully!"
