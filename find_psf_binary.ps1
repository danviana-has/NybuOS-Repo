$bytes = [System.IO.File]::ReadAllBytes("C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll")

# Procura a assinatura do PSF1 (0x36 0x04) ou PSF2 (0x72 0xb5 0x4a 0x86)
$found = -1
for ($i = 0; $i -lt $bytes.Length - 4; $i++) {
    if ($bytes[$i] -eq 0x72 -and $bytes[$i+1] -eq 0xb5 -and $bytes[$i+2] -eq 0x4a -and $bytes[$i+3] -eq 0x86) {
        Write-Host "Found PSF2 header at offset $i"
        $found = $i
        break
    }
    if ($bytes[$i] -eq 0x36 -and $bytes[$i+1] -eq 0x04) {
        Write-Host "Found PSF1 header at offset $i"
        $found = $i
        break
    }
}

if ($found -ge 0) {
    # Salva 8192 bytes a partir do header PSF
    $len = [Math]::Min(16384, $bytes.Length - $found)
    $fontBytes = [byte[]]::new($len)
    [Array]::Copy($bytes, $found, $fontBytes, 0, $len)
    [System.IO.File]::WriteAllBytes("extracted_cosmos_font.psf", $fontBytes)
    Write-Host "Extracted PSF font file ($len bytes)"
}
