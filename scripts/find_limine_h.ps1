$res = Invoke-RestMethod -Uri 'https://api.github.com/repos/limine-bootloader/limine/git/trees/v10.8.5?recursive=1'
$res.tree | Where-Object { $_.path -like '*protocol*' -or $_.path -like '*limine*.h' } | Select-Object -ExpandProperty path
