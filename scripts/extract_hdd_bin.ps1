$bytes = [System.IO.File]::ReadAllBytes('obj/Release/net10.0/linux-x64/cosmos/limine/limine-bios-hdd.h')
$str = [System.Text.Encoding]::ASCII.GetString($bytes)

# Extract byte array from C header
$match = [System.Text.RegularExpressions.Regex]::Match($str, '\{([^}]+)\}')
if ($match.Success) {
    $hexValues = $match.Groups[1].Value.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
    $raw = [byte[]]($hexValues | ForEach-Object { [Convert]::ToByte($_, 16) })
    [System.IO.File]::WriteAllBytes('limine_hdd_raw.bin', $raw)
    Write-Host "Extracted raw hdd bin: $($raw.Length) bytes"
} else {
    Write-Host "Failed to extract"
}
