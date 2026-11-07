$packages = (Get-ChildItem -Recurse 'C:\Users\dvian\.nuget\packages' -Filter 'Cosmos.Kernel.HAL.dll' | Where-Object { $_.FullName -like '*net10.0*' } | Select-Object -First 1).FullName
$bytes = [System.IO.File]::ReadAllBytes($packages)
$str = [System.Text.Encoding]::ASCII.GetString($bytes)

$idx = 0
while (($idx = $str.IndexOf("Atapi", $idx, [System.StringComparison]::OrdinalIgnoreCase)) -ge 0) {
    $start = [Math]::Max(0, $idx - 80)
    $len = [Math]::Min($str.Length - $start, 160)
    $clean = $str.Substring($start, $len) -replace '[^a-zA-Z0-9_\-\.\:\/ ]', '.'
    Write-Host "Match at $idx : $clean"
    $idx += 5
}
