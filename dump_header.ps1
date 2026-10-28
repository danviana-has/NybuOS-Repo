$bytes = [System.IO.File]::ReadAllBytes("extracted_cosmos_font.psf")
$hex = ($bytes[0..30] | ForEach-Object { "0x{0:X2}" -f $_ }) -join " "
Write-Host "Header: $hex"
