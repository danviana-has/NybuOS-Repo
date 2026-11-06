$path = "$env:UserProfile\.nuget\packages\cosmos.kernel.system\3.0.95\runtimes\linux-x64\lib\net10.0\Cosmos.Kernel.System.dll"
$bytes = [System.IO.File]::ReadAllBytes($path)
$asm = [System.Reflection.Assembly]::Load($bytes)
$types = $asm.GetExportedTypes() | Where-Object { $_.Namespace -like "*Storage*" -or $_.Namespace -like "*FileSystem*" }
foreach ($t in $types) {
    Write-Host "TYPE: " $t.FullName
    foreach ($m in $t.GetMethods([System.Reflection.BindingFlags]'Public,Static,Instance,DeclaredOnly')) {
        Write-Host "   METHOD: " $m.ToString()
    }
    foreach ($p in $t.GetProperties([System.Reflection.BindingFlags]'Public,Static,Instance,DeclaredOnly')) {
        Write-Host "   PROP: " $p.ToString()
    }
}
