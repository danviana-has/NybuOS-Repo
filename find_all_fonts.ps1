$p = 'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll'
$bytes = [System.IO.File]::ReadAllBytes($p)
$str = [System.Text.Encoding]::UTF8.GetString($bytes)
$regex = [regex]'[A-Za-z0-9_.]*Font[A-Za-z0-9_.]*'
$regex.Matches($str) | ForEach-Object { $_.Value } | Select-Object -Unique | Out-File -FilePath 'all_fonts.txt' -Encoding utf8
Write-Host "Done"
