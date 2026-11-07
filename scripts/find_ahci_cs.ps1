$nuget = 'C:\Users\dvian\.nuget\packages'
Get-ChildItem -Recurse $nuget -Filter "*Ahci*.cs" | ForEach-Object { $_.FullName }
