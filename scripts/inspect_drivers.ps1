$cecil = [System.Reflection.Assembly]::LoadFrom((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Mono.Cecil.dll'))
$drivers = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.Drivers.dll'))
foreach ($t in $drivers.Types) {
    if ($t.FullName -match 'Ata|Storage|Ide|Disk|Block') {
        Write-Output "Drivers: $($t.FullName)"
    }
}
$sys = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.System.dll'))
foreach ($t in $sys.Types) {
    if ($t.FullName -match 'BlockDevice|Disk|Storage') {
        Write-Output "System: $($t.FullName)"
    }
}
