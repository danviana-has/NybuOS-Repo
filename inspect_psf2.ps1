$bytes = [System.IO.File]::ReadAllBytes("C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll")

function Inspect-PSF2($offset) {
    Write-Host "=== Inspecting PSF2 at $offset ==="
    # PSF2 header:
    # 0..3: magic (0x72 0xb5 0x4a 0x86)
    # 4..7: version
    # 8..11: headersize
    # 12..15: flags
    # 16..19: length (number of glyphs)
    # 20..23: charsize (bytes per glyph)
    # 24..27: height
    # 28..31: width
    $headerSize = [BitConverter]::ToInt32($bytes, $offset + 8)
    $numGlyphs = [BitConverter]::ToInt32($bytes, $offset + 16)
    $charSize = [BitConverter]::ToInt32($bytes, $offset + 20)
    $height = [BitConverter]::ToInt32($bytes, $offset + 24)
    $width = [BitConverter]::ToInt32($bytes, $offset + 28)

    Write-Host "HeaderSize: $headerSize, NumGlyphs: $numGlyphs, CharSize: $charSize, Width: $width, Height: $height"

    return @{ Offset = $offset; HeaderSize = $headerSize; NumGlyphs = $numGlyphs; CharSize = $charSize; Width = $width; Height = $height }
}

$f1 = Inspect-PSF2 114874
$f2 = Inspect-PSF2 405892
