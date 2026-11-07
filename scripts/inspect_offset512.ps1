$bytes = [System.IO.File]::ReadAllBytes('limine_hdd_raw.bin')
Write-Host "Length: $($bytes.Length)"
$first32 = ($bytes[512..543] | ForEach-Object { $_.ToString('X2') }) -join ' '
Write-Host "Offset 512: $first32"
