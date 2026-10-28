$path = 'C:\Users\dvian\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll'
$bytes = [System.IO.File]::ReadAllBytes($path)
$asm = [System.Reflection.Assembly]::Load($bytes)
Write-Host "Assembly loaded successfully."

$types = $asm.GetExportedTypes()
foreach ($t in $types) {
    if ($t.FullName -like "*Font*" -or $t.FullName -like "*Draw*" -or $t.FullName -like "*String*") {
        Write-Host "Type: $($t.FullName)"
        $t.GetMethods() | ForEach-Object { Write-Host "   Method: $($_.ToString())" }
    }
}

$canvasType = $asm.GetType("Cosmos.Kernel.System.Graphics.Canvas")
if ($canvasType) {
    Write-Host "Canvas methods:"
    $canvasType.GetMethods() | ForEach-Object { Write-Host "   Canvas: $($_.ToString())" }
}
