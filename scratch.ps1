
$src = Get-Content 'disasm_test.cs' -Raw
Add-Type -TypeDefinition $src
[ProgramFind]::Main()
