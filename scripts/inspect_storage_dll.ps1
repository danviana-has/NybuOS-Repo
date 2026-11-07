$dll = 'bin\Release\net10.0\linux-x64\Cosmos.Kernel.System.dll'
if (Test-Path $dll) {
    $asm = [System.Reflection.Assembly]::LoadFrom((Resolve-Path $dll))
    $types = $asm.GetTypes() | Where-Object { $_.FullName -like '*Storage*' -or $_.FullName -like '*Device*' -or $_.FullName -like '*VFS*' }
    foreach ($t in $types) {
        Write-Host "Type: $($t.FullName)"
        $t.GetMethods([System.Reflection.BindingFlags]'Public,Static,Instance') | ForEach-Object {
            Write-Host "   Method: $($_.Name)"
        }
    }
}
