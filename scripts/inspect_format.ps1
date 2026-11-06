$cecil = [System.Reflection.Assembly]::LoadFrom((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Mono.Cecil.dll'))
$sys = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.System.dll'))
$vm = $sys.GetType('Cosmos.Kernel.System.FileSystem.VfsManager')
foreach ($m in $vm.Methods) {
    if ($m.Name -eq 'TryFormat') {
        $paramList = @()
        foreach ($p in $m.Parameters) {
            $paramList += ($p.ParameterType.FullName + " " + $p.Name)
        }
        Write-Output "  TryFormat ($([string]::Join(', ', $paramList))) : $($m.ReturnType.FullName)"
    }
}
$ffo = $sys.GetType('Cosmos.Kernel.System.FileSystem.Fat.FatFormatOptions')
Write-Output "FatFormatOptions ctors & props:"
foreach ($c in $ffo.Methods) {
    if ($c.Name -eq '.ctor') {
        $paramList = @()
        foreach ($p in $c.Parameters) {
            $paramList += ($p.ParameterType.FullName + " " + $p.Name)
        }
        Write-Output "  .ctor ($([string]::Join(', ', $paramList)))"
    }
}
foreach ($p in $ffo.Properties) {
    Write-Output "  Prop: $($p.Name) : $($p.PropertyType.FullName)"
}
