$cecil = [System.Reflection.Assembly]::LoadFrom((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Mono.Cecil.dll'))
$sys = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.System.dll'))
$pm = $sys.GetType('Cosmos.Kernel.System.Storage.PartitionManager')
Write-Output "PartitionManager.Create methods:"
foreach ($m in $pm.Methods) {
    if ($m.Name -eq 'Create') {
        $paramList = @()
        foreach ($p in $m.Parameters) {
            $paramList += ($p.ParameterType.FullName + " " + $p.Name)
        }
        Write-Output "  Create ($([string]::Join(', ', $paramList))) : $($m.ReturnType.FullName)"
    }
}
