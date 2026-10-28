$bytes = [System.IO.File]::ReadAllBytes('C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll')
$str = [System.Text.Encoding]::UTF8.GetString($bytes)
$regex = [regex]'DrawString[A-Za-z0-9_.]*'
$matches = $regex.Matches($str)
$matches | ForEach-Object { $_.Value } | Select-Object -Unique | Out-File -FilePath 'drawstring_matches.txt' -Encoding utf8
Write-Host "Done finding DrawString matches"
