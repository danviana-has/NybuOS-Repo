$bytes = [System.IO.File]::ReadAllBytes("C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll")

# Procura a string "DefaultFont.psf"
$pattern = [System.Text.Encoding]::ASCII.GetBytes("DefaultFont.psf")
$idx = -1
for ($i = 0; $i -lt $bytes.Length - $pattern.Length; $i++) {
    $match = $true
    for ($j = 0; $j -lt $pattern.Length; $j++) {
        if ($bytes[$i + $j] -ne $pattern[$j]) { $match = $false; break }
    }
    if ($match) { $idx = $i; break }
}
Write-Host "DefaultFont.psf string found at offset $idx"

# Procura assinaturas PSF válidas: 0x36 0x04 0x00 0x10 (PSF1 16px) ou 0x72 0xB5 0x4A 0x86
for ($i = 0; $i -lt $bytes.Length - 4; $i++) {
    if ($bytes[$i] -eq 0x36 -and $bytes[$i+1] -eq 0x04 -and $bytes[$i+2] -eq 0x00 -and ($bytes[$i+3] -eq 16 -or $bytes[$i+3] -eq 14 -or $bytes[$i+3] -eq 8)) {
        Write-Host "Found standard PSF1 font at offset $i with height $($bytes[$i+3])"
    }
    if ($bytes[$i] -eq 0x72 -and $bytes[$i+1] -eq 0xB5 -and $bytes[$i+2] -eq 0x4A -and $bytes[$i+3] -eq 0x86) {
        Write-Host "Found PSF2 font at offset $i"
    }
}
