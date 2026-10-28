using System;

namespace NybuOS.Drivers;

/// <summary>
/// Driver de gerenciamento de energia (Power & ACPI) do NybuOS.
/// Executa o desligamento e reinicialização de hardware real e máquinas virtuais
/// (QEMU, VMware, VirtualBox) através do subsistema ACPI/Power do Cosmos.
/// </summary>
public static class PowerDriver
{
    public static void Shutdown()
    {
        try
        {
            Cosmos.Kernel.System.Power.Shutdown();
        }
        catch { }

        try
        {
            Cosmos.Kernel.System.Power.Halt();
        }
        catch { }
    }

    public static void Reboot()
    {
        try
        {
            Cosmos.Kernel.System.Power.Reboot();
        }
        catch { }

        try
        {
            Cosmos.Kernel.System.Power.Halt();
        }
        catch { }
    }
}
