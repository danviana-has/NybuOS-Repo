$cecil = [System.Reflection.Assembly]::LoadFrom((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Mono.Cecil.dll'))
$sys = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.System.dll'))
$vm = $sys.GetType('Cosmos.Kernel.System.FileSystem.VfsManager')
Write-Output "VfsManager methods:"
foreach ($m in $vm.Methods) {
    $paramList = @()
    foreach ($p in $m.Parameters) {
        $paramList += ($p.ParameterType.Name + " " + $p.Name)
    }
    $ps = [string]::Join(", ", $paramList)
    Write-Output "  $($m.Name) ($ps) : $($m.ReturnType.Name)"
}
