$dll = 'output-x64/Cosmos.Kernel.Boot.Limine.dll'
$bytes = [System.IO.File]::ReadAllBytes($dll)
$str = [System.Text.Encoding]::ASCII.GetString($bytes)
$matches = [System.Text.RegularExpressions.Regex]::Matches($str, 'Limine[A-Za-z0-9_]+')
$matches | ForEach-Object { $_.Value } | Select-Object -Unique
