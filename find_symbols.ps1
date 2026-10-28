$str = [System.IO.File]::ReadAllBytes('C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll')
$text = [System.Text.Encoding]::ASCII.GetString($str)
$matches = [regex]::Matches($text, '[A-Za-z0-9_.]*(?:Font|DrawString|DrawArray)[A-Za-z0-9_.]*')
$matches | ForEach-Object { $_.Value } | Select-Object -Unique | Out-File -FilePath 'symbols.txt'
Write-Host "Done writing symbols.txt"
