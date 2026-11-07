$dll = (Get-ChildItem -Recurse 'C:\Users\dvian\.nuget\packages' -Filter 'Cosmos.Kernel.Boot.Limine.dll' | Where-Object { $_.FullName -like '*net10.0*' } | Select-Object -First 1).FullName
$asm = [System.Reflection.Assembly]::LoadFrom($dll)
$asm.GetTypes() | ForEach-Object {
    Write-Host "Type: $($_.FullName)"
    $_.GetMembers() | ForEach-Object { Write-Host "   $($_.ToString())" }
}
