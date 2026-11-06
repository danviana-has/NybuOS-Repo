$txt = Get-Content 'obj\Release\net10.0\linux-x64\cosmos\limine\limine-bios-hdd.h' -Raw
$m = [regex]::Matches($txt, '0x[0-9a-fA-F]{2}')
Write-Output "Total bytes in limine_hdd_bin: $($m.Count)"
