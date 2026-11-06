$cecil = [System.Reflection.Assembly]::LoadFrom((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Mono.Cecil.dll'))
$sys = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.System.dll'))
$sm = $sys.GetType('Cosmos.Kernel.System.Storage.StorageManager')
Write-Output "StorageManager properties & methods:"
foreach ($p in $sm.Properties) {
    Write-Output "  Property: $($p.Name) : $($p.PropertyType.FullName)"
}
foreach ($m in $sm.Methods) {
    Write-Output "  Method: $($m.Name) ($([string]::Join(', ', ($m.Parameters | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name })))) : $($m.ReturnType.FullName)"
}
