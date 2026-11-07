$packages = (Get-ChildItem -Recurse 'C:\Users\dvian\.nuget\packages' -Filter 'Cosmos.Kernel.HAL.dll' | Where-Object { $_.FullName -like '*net10.0*' } | Select-Object -First 1).FullName
$asm = [System.Reflection.Assembly]::LoadFrom($packages)
try {
    $t = $asm.GetTypes()
} catch [System.Reflection.ReflectionTypeLoadException] {
    $_.Exception.LoaderExceptions | ForEach-Object { Write-Host $_.Message }
}
