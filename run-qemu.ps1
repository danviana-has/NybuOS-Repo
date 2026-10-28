param(
    [string]$RAM = "1024M"
)

$qemu = "$env:LOCALAPPDATA\Cosmos\Tools\qemu\bin\qemu-system-x86_64.exe"
$share = "$env:LOCALAPPDATA\Cosmos\Tools\qemu\share\qemu"
$iso = "$PSScriptRoot\output-x64\NybuOS.iso"

if (-not (Test-Path $iso)) {
    $iso = "$PSScriptRoot\bin\Release\net10.0\linux-x64\cosmos\NybuOS.iso"
}

$isoForward = $iso.Replace('\', '/')
$shareForward = $share.Replace('\', '/')

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Iniciando NybuOS no QEMU com $RAM de RAM (TCG Limine Compat)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# cosmos oficial usa: -M q35 -cpu max -m RAM -device ide-cd... -vga std -serial stdio
# NUNCA use WHPX com o kernel do Cosmos/Limine porque o hypervisor da Microsoft quebra MSI interrupts (exit code 4).
$argsList = @(
    "-L", $shareForward,
    "-M", "q35",
    "-cpu", "max",
    "-m", $RAM,
    "-drive", "file=$isoForward,if=none,id=cosmoscd,format=raw,readonly=on",
    "-device", "ide-cd,drive=cosmoscd,bootindex=0",
    "-boot", "d",
    "-no-reboot",
    "-no-shutdown",
    "-vga", "std",
    "-serial", "stdio"
)

& $qemu @argsList
