Add-Type -Path 'obj\Release\net10.0\linux-x64\cosmos\ref\Cosmos.Kernel.System.dll'
$asm = [AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq 'Cosmos.Kernel.System' }
$types = $asm.GetTypes() | Where-Object { $_.Namespace -like '*Storage*' -or $_.Namespace -like '*FileSystem*' -or $_.Name -like '*Disk*' -or $_.Name -like '*VFS*' }
foreach ($t in $types) {
    Write-Output $t.FullName
}
