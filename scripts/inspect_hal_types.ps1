$dllPath = (Get-ChildItem -Recurse 'C:\Users\dvian\.nuget\packages' -Filter 'Cosmos.Kernel.HAL.dll' | Where-Object { $_.FullName -like '*net10.0*' } | Select-Object -First 1).FullName
$bytes = [System.IO.File]::ReadAllBytes($dllPath)
$str = [System.Text.Encoding]::ASCII.GetString($bytes)
$matches = [System.Text.RegularExpressions.Regex]::Matches($str, 'Cosmos\.Kernel\.HAL\.Devices\.Storage\.[A-Za-z0-9_]+')
$matches | ForEach-Object { $_.Value } | Select-Object -Unique
