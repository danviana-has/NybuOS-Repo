# Use ildasm or javap equivalent in dotnet: dotnet ildasm or read with cecil or inspect source
$packages = (Get-ChildItem -Recurse 'C:\Users\dvian\.nuget\packages' -Filter 'Cosmos.Kernel.HAL.*.nupkg' | Select-Object -First 1).FullName
Write-Host "Found nupkg: $packages"
