$dll = 'output-x64/Cosmos.Kernel.Boot.Limine.dll'
if (Test-Path $dll) {
    $bytes = [System.IO.File]::ReadAllBytes($dll)
    $str = [System.Text.Encoding]::ASCII.GetString($bytes)
    $matches = [System.Text.RegularExpressions.Regex]::Matches($str, '[A-Za-z0-9_]{3,}(?:Module|Request|Response|File|Kernel|Limine)[A-Za-z0-9_]*')
    $matches | ForEach-Object { $_.Value } | Select-Object -Unique
}
