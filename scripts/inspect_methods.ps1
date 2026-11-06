$cecil = [System.Reflection.Assembly]::LoadFrom((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Mono.Cecil.dll'))
$mod = [Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.System.dll'))
$targetTypes = @(
    'Cosmos.Kernel.System.Storage.StorageManager',
    'Cosmos.Kernel.System.Storage.PartitionManager',
    'Cosmos.Kernel.System.FileSystem.VfsManager',
    'Cosmos.Kernel.System.FileSystem.Fat.FatFormatter',
    'Cosmos.Kernel.System.FileSystem.Fat.FatFileSystemType'
)

foreach ($t in $mod.Types) {
    if ($targetTypes -contains $t.FullName) {
        Write-Output "=== $($t.FullName) ==="
        foreach ($m in $t.Methods) {
            Write-Output "  $($m.Name) ($($m.Parameters.Count) params)"
        }
    }
}
