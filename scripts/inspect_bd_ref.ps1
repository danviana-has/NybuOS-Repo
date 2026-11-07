$bytes = [System.IO.File]::ReadAllBytes('obj/Release/net10.0/linux-x64/cosmos/ref/Cosmos.Kernel.HAL.dll')
$str = [System.Text.Encoding]::ASCII.GetString($bytes)
$matches = [System.Text.RegularExpressions.Regex]::Matches($str, '[A-Za-z0-9_]{3,}BlockDevice[A-Za-z0-9_]*')
$matches | ForEach-Object { $_.Value } | Select-Object -Unique
