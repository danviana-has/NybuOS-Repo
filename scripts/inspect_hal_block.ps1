$cecil = [System.Reflection.Assembly]::LoadFrom((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Mono.Cecil.dll'))
$hal = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.HAL.dll'))
foreach ($t in $hal.Types) {
    if ($t.FullName -match 'Block|Storage|Ata|Ide|Disk') {
        Write-Output "HAL: $($t.FullName)"
        foreach ($m in $t.Methods) {
            if ($m.IsPublic) {
                Write-Output "   $($m.Name)"
            }
        }
    }
}
