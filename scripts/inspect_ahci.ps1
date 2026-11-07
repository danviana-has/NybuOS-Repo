$dllPath = (Get-ChildItem -Recurse 'C:\Users\dvian\.nuget\packages' -Filter 'Cosmos.Kernel.HAL.dll' | Where-Object { $_.FullName -like '*net10.0*' } | Select-Object -First 1).FullName
$bytes = [System.IO.File]::ReadAllBytes($dllPath)
$str = [System.Text.Encoding]::ASCII.GetString($bytes)
$matches = [System.Text.RegularExpressions.Regex]::Matches($str, '[A-Za-z0-9_]{3,}(?:Ahci|Ata|Atapi|Cdrom|BlockDevice|Sata)[A-Za-z0-9_]*')
$matches | ForEach-Object { $_.Value } | Select-Object -Unique
