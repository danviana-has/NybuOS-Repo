$packages = (Get-ChildItem -Recurse 'C:\Users\dvian\.nuget\packages' -Filter 'Cosmos.Kernel.System.dll' | Where-Object { $_.FullName -like '*net10.0*' } | Select-Object -First 1).FullName
$bytes = [System.IO.File]::ReadAllBytes($packages)
$str = [System.Text.Encoding]::ASCII.GetString($bytes)
$start = [Math]::Max(0, 212754 - 100)
$clean = $str.Substring($start, 400) -replace '[^a-zA-Z0-9_\-\.\:\/ ]', '.'
Write-Host $clean
