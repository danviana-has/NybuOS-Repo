$isoBytes = [System.IO.File]::ReadAllBytes('output-x64/NybuOS.iso')
$target = [System.Text.Encoding]::ASCII.GetBytes('BOOTX64.EFI')
for ($i = 0; $i -lt $isoBytes.Length - $target.Length; $i += 2048) {
    # Check sector for BOOTX64.EFI
    for ($j = 0; $j -lt 2048 - $target.Length; $j++) {
        $found = $true
        for ($k = 0; $k -lt $target.Length; $k++) {
            if ($isoBytes[$i + $j + $k] -ne $target[$k]) {
                $found = $false
                break
            }
        }
        if ($found) {
            $recStart = $i + $j - 33
            $extentLba = [System.BitConverter]::ToUInt32($isoBytes, $recStart + 2)
            $extentLen = [System.BitConverter]::ToUInt32($isoBytes, $recStart + 10)
            Write-Host "Found BOOTX64.EFI reference at sector $($i / 2048)! Extent LBA: $extentLba, Length: $extentLen"
        }
    }
}
