$cecil = [System.Reflection.Assembly]::LoadFrom((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Mono.Cecil.dll'))
$sys = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.System.dll'))
$hal = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.HAL.dll'))
$drivers = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.Drivers.dll'))

foreach ($mod in @($sys, $hal, $drivers)) {
    foreach ($t in $mod.Types) {
        if ($t.FullName -match 'Atapi|Cdrom|Iso9660|Iso') {
            Write-Output "$($mod.Assembly.Name.Name): $($t.FullName)"
        }
    }
}
