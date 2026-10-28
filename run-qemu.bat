@echo off
set QEMU=%LOCALAPPDATA%\Cosmos\Tools\qemu\bin\qemu-system-x86_64.exe
set SHARE=%LOCALAPPDATA%\Cosmos\Tools\qemu\share\qemu
set ISO=%~dp0output-x64\NybuOS.iso

if "%1"=="" (
    set RAM=1024M
) else (
    set RAM=%1
)

echo ========================================================
echo  Iniciando NybuOS no QEMU com %RAM% de RAM
echo ========================================================

"%QEMU%" -L "%SHARE%" -M q35 -cpu max -m %RAM% -drive file="%ISO%",if=none,id=cosmoscd,format=raw,readonly=on -device ide-cd,drive=cosmoscd,bootindex=0 -boot d -no-reboot -no-shutdown -vga std -serial stdio
