$dll = (Get-ChildItem -Recurse 'C:\Users\dvian\.nuget\packages' -Filter 'Cosmos.Kernel.Boot.Limine.dll' | Where-Object { $_.FullName -like '*net10.0*' } | Select-Object -First 1).FullName
$fs = [System.IO.File]::OpenRead($dll)
$pe = New-Object System.Reflection.PortableExecutable.PEReader($fs)
$mr = $pe.GetMetadataReader()
foreach ($th in $mr.TypeDefinitions) {
    $td = $mr.GetTypeDefinition($th)
    $ns = $mr.GetString($td.Namespace)
    $name = $mr.GetString($td.Name)
    Write-Host "$ns.$name"
}
$fs.Close()
