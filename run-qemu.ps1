param(
    [string]$RAM = "2048M"
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
Write-Host " Iniciando NybuOS RV no QEMU Full HD (1920x1080) com $RAM" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Suporte Full HD 1920x1080 com 64MB de memória de vídeo VGA
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
    "-global", "VGA.vgamem_mb=64",
    "-serial", "stdio"
)

& $qemu @argsList
