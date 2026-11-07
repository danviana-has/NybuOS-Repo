$b = [System.IO.File]::ReadAllBytes('obj/Release/net10.0/linux-x64/cosmos/iso_root/boot/NybuOS.elf')
$ms = New-Object System.IO.MemoryStream
$gz = New-Object System.IO.Compression.GZipStream($ms, [System.IO.Compression.CompressionLevel]::Optimal)
$gz.Write($b, 0, $b.Length)
$gz.Close()
$compressed = $ms.ToArray()
Write-Host "NybuOS.elf original: $($b.Length) bytes, GZip compressed: $($compressed.Length) bytes"
