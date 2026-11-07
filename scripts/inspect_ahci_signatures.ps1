$packages = (Get-ChildItem -Recurse 'C:\Users\dvian\.nuget\packages' -Filter 'Cosmos.Kernel.HAL.dll' | Where-Object { $_.FullName -like '*net10.0*' } | Select-Object -First 1).FullName
$bytes = [System.IO.File]::ReadAllBytes($packages)
$str = [System.Text.Encoding]::ASCII.GetString($bytes)

# Search for strings relating to AHCI port signature or device type
$matches = [System.Text.RegularExpressions.Regex]::Matches($str, '(?:0xEB140101|0x96690101|EB14|9669|ATAPI|Atapi|CDROM|Cdrom)')
$matches | ForEach-Object { $_.Value } | Select-Object -Unique
