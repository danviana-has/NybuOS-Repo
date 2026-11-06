$cecil = [System.Reflection.Assembly]::LoadFrom((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Mono.Cecil.dll'))
$drivers = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.Drivers.dll'))
$ahci = $drivers.GetType('Cosmos.Kernel.Drivers.Pci.Storage.Ahci.AhciDriver')
Write-Output "AhciDriver methods:"
foreach ($m in $ahci.Methods) {
    Write-Output "  $($m.Name)"
}
$port = $drivers.GetType('Cosmos.Kernel.Drivers.Pci.Storage.Ahci.AhciPort')
Write-Output "AhciPort methods:"
foreach ($m in $port.Methods) {
    Write-Output "  $($m.Name)"
}
