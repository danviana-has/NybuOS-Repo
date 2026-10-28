$header = Get-Content "Drivers\CosmosNativeFontHeader.cs" -Raw
$orig = Get-Content "Drivers\CosmosNativeFont.cs" -Raw

$startIdx = $orig.IndexOf("0x00, 0x00, 0x00, 0x00")
if ($startIdx -lt 0) {
    $startIdx = $orig.IndexOf("0x")
}
# Pega a parte do array de bytes
$arrayBody = $orig.Substring($startIdx)
$endIdx = $arrayBody.IndexOf("};")
$arrayData = $arrayBody.Substring(0, $endIdx)

$finalCode = $header + "`r`n        " + $arrayData + "`r`n    };`r`n}"
[System.IO.File]::WriteAllText("Drivers\CosmosNativeFont.cs", $finalCode)
Remove-Item "Drivers\CosmosNativeFontHeader.cs" -Force
Write-Host "Re-assembled Drivers\CosmosNativeFont.cs (8x14)!"
