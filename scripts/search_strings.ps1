$bytes = [System.IO.File]::ReadAllBytes('limine_hdd_raw.bin')
$ascii = [System.Text.Encoding]::ASCII.GetString($bytes)
$matches = [System.Text.RegularExpressions.Regex]::Matches($ascii, '[\x20-\x7E]{4,}')
foreach ($m in $matches) {
    if ($m.Value -match 'boot|drive|Could|panic|stage|disk|volume') {
        Write-Host "$($m.Index.ToString('X4')): $($m.Value)"
    }
}
