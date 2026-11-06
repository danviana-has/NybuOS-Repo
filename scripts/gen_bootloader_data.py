with open(r'obj\Release\net10.0\linux-x64\cosmos\limine\limine-bios-hdd.h', 'r') as f:
    text = f.read()

import re
vals = re.findall(r'0x[0-9a-fA-F]{2}', text)
print(f"Limine HDD bin values: {len(vals)}")

with open(r'obj\Release\net10.0\linux-x64\cosmos\limine\limine-bios.sys', 'rb') as f:
    bios_sys = f.read()
print(f"Limine BIOS sys length: {len(bios_sys)}")

with open(r'obj\Release\net10.0\linux-x64\cosmos\iso_root\boot\limine\limine.conf', 'r') as f:
    limine_conf = f.read()

with open(r'FileSystem\EmbeddedBootloaderData.cs', 'w', encoding='utf-8') as out:
    out.write("""namespace NybuOS.FileSystem;

/// <summary>
/// Dados embarcados do bootloader Limine para instalacao nativa em MBR e FAT32.
/// </summary>
public static class EmbeddedBootloaderData
{
    public const string LimineConf = @"timeout: 0

/NybuOS
    protocol: limine
    path: boot():/boot/NybuOS.elf
";

    /// <summary>
    /// Limine Stage 1 (MBR) e Stage 2 (Post-MBR gap) - 21.851 bytes.
    /// </summary>
    public static readonly byte[] LimineHddBin = new byte[]
    {
""")
    for i in range(0, len(vals), 16):
        chunk = vals[i:i+16]
        out.write("        " + ", ".join(chunk) + ",\n")
    out.write("""    };

    /// <summary>
    /// Limine BIOS SYS runtime - 252.052 bytes.
    /// </summary>
    public static readonly byte[] LimineBiosSys = new byte[]
    {
""")
    for i in range(0, len(bios_sys), 16):
        chunk = [f"0x{b:02x}" for b in bios_sys[i:i+16]]
        out.write("        " + ", ".join(chunk) + ",\n")
    out.write("""    };
}
""")

print("EmbeddedBootloaderData.cs written successfully.")
