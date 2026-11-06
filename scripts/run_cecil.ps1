$cecil = [System.Reflection.Assembly]::LoadFrom((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Mono.Cecil.dll'))
$mod = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.System.dll'))
foreach ($t in $mod.Types) {
    if ($t.FullName -match 'Storage|FileSystem|Disk|VFS') {
        Write-Output $t.FullName
    }
}
