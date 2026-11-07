$isoBytes = [System.IO.File]::ReadAllBytes('output-x64/NybuOS.iso')
$target = [System.Text.Encoding]::ASCII.GetBytes('NYBUOS.ELF')
for ($i = 0; $i -lt 100 * 2048; $i += 2048) {
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
            Write-Host "Found NYBUOS.ELF reference at sector $($i / 2048)! Extent LBA: $extentLba, Length: $extentLen"
        }
    }
}
