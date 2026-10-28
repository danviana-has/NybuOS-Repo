$pkg = "C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\cosmos.kernel.system.3.0.95.nupkg"
$zip = [System.IO.Compression.ZipFile]::OpenRead($pkg)
$entry = $zip.Entries | Where-Object { $_.Name -like "*.psf" } | Select-Object -First 1

if ($entry) {
    $s = $entry.Open()
    $ms = New-Object System.IO.MemoryStream
    $s.CopyTo($ms)
    $bytes = $ms.ToArray()
    [System.IO.File]::WriteAllBytes("CosmosDefaultFont.psf", $bytes)
    Write-Host "Extracted PSF font: $($entry.FullName), size: $($bytes.Length) bytes"
} else {
    Write-Host "No .psf file found in root, looking in subdirs..."
}
$zip.Dispose()
