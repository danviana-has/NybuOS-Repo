$packages = (Get-ChildItem -Recurse 'C:\Users\dvian\.nuget\packages' -Filter 'Cosmos.Kernel.HAL.dll' | Where-Object { $_.FullName -like '*net10.0*' } | Select-Object -First 1).FullName
$asm = [System.Reflection.Assembly]::LoadFrom($packages)
$t = $asm.GetTypes() | Where-Object { $_.Name -like '*Ahci*' -or $_.Name -like '*Sata*' }
foreach ($type in $t) {
    Write-Host "Type: $($type.FullName)"
    $type.GetMethods([System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static') | ForEach-Object {
        Write-Host "   Method: $($_.Name)"
    }
}
