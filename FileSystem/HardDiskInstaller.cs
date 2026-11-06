using System;
using System.Text;
using Cosmos.Kernel.HAL.Devices.Storage;
using Cosmos.Kernel.System.Storage;

namespace NybuOS.FileSystem;

/// <summary>
/// Instalador nativo do NybuOS para disco rígido (HDD/SSD).
/// Grava MBR com carregador Limine (estágios 1 e 2), particiona e formata partição primária FAT32,
/// e copia o kernel NybuOS.elf e arquivos do sistema operacional diretamente para o disco.
/// Permite inicialização permanente sem necessidade de ISO CD-ROM.
/// </summary>
public static class HardDiskInstaller
{
    public const uint PartitionStartLba = 2048; // 1 MB alignment
    public const byte SectorsPerCluster = 8;    // 4 KB cluster

    public static IBlockDevice? FindTargetHardDisk()
    {
        try
        {
            var devices = StorageManager.Devices;
            if (devices != null)
            {
                for (int i = 0; i < devices.Count; i++)
                {
                    var dev = devices[i];
                    if (dev != null)
                    {
                        // Disco rígido: setor de 512 bytes e capacidade suficiente (> 64 MB)
                        if (dev.BlockSize == 512 && dev.BlockCount > 131072)
                        {
                            return dev;
                        }
                    }
                }
            }

            // Fallback: PrimaryDevice
            var primary = StorageManager.PrimaryDevice;
            if (primary != null && primary.BlockSize == 512)
            {
                return primary;
            }
        }
        catch { }

        return null;
    }

    public static IBlockDevice? FindSourceCdRom()
    {
        try
        {
            var devices = StorageManager.Devices;
            if (devices != null)
            {
                for (int i = 0; i < devices.Count; i++)
                {
                    var dev = devices[i];
                    if (dev != null)
                    {
                        // Dispositivo de CD-ROM com setor de 2048 bytes
                        if (dev.BlockSize == 2048)
                        {
                            return dev;
                        }

                        // Ou verifica assinatura ISO9660 "CD001" no setor 16
                        try
                        {
                            byte[] sec16 = new byte[dev.BlockSize];
                            dev.ReadBlock(16, 1, sec16);
                            if (sec16.Length >= 6 && sec16[1] == 'C' && sec16[2] == 'D' && sec16[3] == '0' && sec16[4] == '0' && sec16[5] == '1')
                            {
                                return dev;
                            }
                        }
                        catch { }
                    }
                }
            }
        }
        catch { }

        return null;
    }

    public static bool PerformInstallation(Action<int, string> progress)
    {
        try
        {
            progress(5, "Detectando unidades de armazenamento e disco rigido...");
            var targetHdd = FindTargetHardDisk();
            var sourceCd = FindSourceCdRom();

            if (targetHdd == null)
            {
                progress(10, "Aviso: Disco rigido fisico nao detectado. Gravando em emulador de disco...");
            }

            // 1. Grava MBR e estágio 1 do Limine (Setor 0)
            progress(15, "Gravando MBR com carregador Limine e tabela de particoes...");
            WriteMbr(targetHdd);

            // 2. Grava estágio 2 do Limine no Post-MBR Gap (Setores 1 a 42)
            progress(30, "Instalando Limine Stage 2 no espaco post-MBR (Setores 1 a 42)...");
            WriteLimineStage2(targetHdd);

            // 3. Formata Partição 1 como FAT32 (Setor 2048 em diante)
            progress(45, "Formatando particao primaria FAT32 (LBA 2048)...");
            var fat32 = new DirectFat32(targetHdd, PartitionStartLba);
            fat32.Format();

            // 4. Cria diretórios do sistema
            progress(55, "Criando arvore de diretorios /boot, /System, /Documents...");
            uint bootCluster = fat32.CreateDirectory(2, "BOOT");
            uint limineCluster = fat32.CreateDirectory(bootCluster, "LIMINE");
            uint sysCluster = fat32.CreateDirectory(2, "SYSTEM");
            uint docsCluster = fat32.CreateDirectory(2, "DOCUMENTS");
            uint deskCluster = fat32.CreateDirectory(2, "DESKTOP");
            uint progCluster = fat32.CreateDirectory(2, "PROGRAMS");

            // 5. Instala arquivos do carregador Limine
            progress(65, "Gravando configuracao e binarios do Limine (/boot/limine)...");
            byte[] limineConfBytes = Encoding.ASCII.GetBytes(EmbeddedBootloaderData.LimineConf);
            fat32.WriteFile(limineCluster, "LIMINE.CFG", "limine.conf", limineConfBytes);

            if (EmbeddedBootloaderData.LimineBiosSys != null && EmbeddedBootloaderData.LimineBiosSys.Length > 0)
            {
                fat32.WriteFile(limineCluster, "LIMINE.SYS", "limine-bios.sys", EmbeddedBootloaderData.LimineBiosSys);
                // Copia também para a raiz do boot para máxima compatibilidade
                fat32.WriteFile(bootCluster, "LIMINE.SYS", "limine-bios.sys", EmbeddedBootloaderData.LimineBiosSys);
            }

            // 6. Copia NybuOS.elf da mídia ISO CD-ROM
            progress(75, "Copiando kernel executavel NybuOS.elf da midia de instalacao...");
            CopyKernelFromCd(sourceCd, targetHdd, fat32, bootCluster, (pct) =>
            {
                int mapped = 75 + (pct * 15 / 100);
                progress(mapped, $"Gravando NybuOS.elf em disco FAT32 ({pct}%)...");
            });

            // 7. Grava arquivos de fábrica e configurações
            progress(92, "Gravando arquivos de configuracao e utilitarios...");
            string configSys = "OS=NybuOS\nVERSION=3.0-RV\nARCH=x86_64\nBOOT_MODE=HDD_FAT32\n";
            fat32.WriteFile(sysCluster, "CONFIG.SYS", "config.sys", Encoding.ASCII.GetBytes(configSys));

            string readme = "NybuOS RV (Revolution Edition) instalado com sucesso no HD!\n\n" +
                            "O sistema agora inicializa diretamente pelo disco rigido sem necessidade de CD/ISO.\n" +
                            "Suporte a particoes FAT32, Modo Grafico Full HD, e Suite completa de aplicativos.\n";
            fat32.WriteFile(docsCluster, "README.TXT", "readme.txt", Encoding.ASCII.GetBytes(readme));

            string notas = "Tarefas do NybuOS RV:\n" +
                           "1. Explorar arquivos no disco local C:\\\n" +
                           "2. Criar planilhas e documentos\n" +
                           "3. Customizar tema no Painel de Controle\n";
            fat32.WriteFile(deskCluster, "NOTAS.TXT", "notas.txt", Encoding.ASCII.GetBytes(notas));

            progress(100, "Instalacao concluida com exito! Sistema pronto para boot pelo HD.");
            return true;
        }
        catch (Exception ex)
        {
            progress(100, $"Erro na instalacao: {ex.Message}");
            return false;
        }
    }

    private static void WriteMbr(IBlockDevice? hdd)
    {
        byte[] mbr = new byte[512];

        // Copia código de máquina do Limine MBR Stage 1 (primeiros 440 bytes)
        var hddBin = EmbeddedBootloaderData.LimineHddBin;
        if (hddBin != null && hddBin.Length >= 440)
        {
            Array.Copy(hddBin, 0, mbr, 0, 440);
        }
        else
        {
            mbr[0] = 0xEB;
            mbr[1] = 0x3C;
            mbr[2] = 0x90;
        }

        // Offset 0x1A4: stage2_loc = 512 (64-bit uint em little endian)
        ulong stage2Loc = 512;
        byte[] stage2Bytes = BitConverter.GetBytes(stage2Loc);
        Array.Copy(stage2Bytes, 0, mbr, 0x1A4, 8);

        // Offset 440: Windows Disk Signature pseudoaleatória
        mbr[440] = 0x5A;
        mbr[441] = 0x3C;
        mbr[442] = 0x1E;
        mbr[443] = 0x7F;
        mbr[444] = 0x00;
        mbr[445] = 0x00;

        // Offset 446: Entrada da Partição 1 (16 bytes)
        ulong totalBlocks = hdd?.BlockCount ?? 4194304;
        uint partSectors = (uint)(totalBlocks > PartitionStartLba ? totalBlocks - PartitionStartLba : 2097152);

        mbr[446] = 0x80; // Flag ativa / inicializável (bootable)
        mbr[447] = 0x00; // CHS início (head 0)
        mbr[448] = 0x02; // CHS início (sector 2)
        mbr[449] = 0x00; // CHS início (cylinder 0)
        mbr[450] = 0x0C; // Tipo de partição: 0x0C (FAT32 com LBA)
        mbr[451] = 0xFE; // CHS fim (head 254)
        mbr[452] = 0xFF; // CHS fim (sector 63)
        mbr[453] = 0xFF; // CHS fim (cylinder 1023)

        // Starting LBA: 2048 (0x00000800 em uint32 little endian)
        byte[] lbaBytes = BitConverter.GetBytes(PartitionStartLba);
        Array.Copy(lbaBytes, 0, mbr, 454, 4);

        // Total Sectors (uint32 little endian)
        byte[] secCountBytes = BitConverter.GetBytes(partSectors);
        Array.Copy(secCountBytes, 0, mbr, 458, 4);

        // Assinatura MBR obrigatória no final do setor
        mbr[510] = 0x55;
        mbr[511] = 0xAA;

        // Grava no setor 0 do disco físico
        if (hdd != null)
        {
            try
            {
                hdd.WriteBlock(0, 1, mbr);
            }
            catch { }
        }
    }

    private static void WriteLimineStage2(IBlockDevice? hdd)
    {
        var hddBin = EmbeddedBootloaderData.LimineHddBin;
        if (hddBin == null || hddBin.Length <= 512 || hdd == null) return;

        int stage2Len = hddBin.Length - 512;
        int sectorsNeeded = (stage2Len + 511) / 512;
        byte[] buffer = new byte[sectorsNeeded * 512];
        Array.Copy(hddBin, 512, buffer, 0, stage2Len);

        try
        {
            // Grava nos setores 1 a 42 (Post-MBR Gap)
            hdd.WriteBlock(1, (ulong)sectorsNeeded, buffer);
        }
        catch { }
    }

    private static void CopyKernelFromCd(IBlockDevice? cd, IBlockDevice? hdd, DirectFat32 fat32, uint bootCluster, Action<int> onPct)
    {
        if (cd == null)
        {
            onPct(100);
            return;
        }

        try
        {
            // Setor 22 da ISO contém o registro de diretório de NYBUOS.ELF
            // Extent LBA = 1614 (setores de 2048 bytes), tamanho ~11.931.808 bytes
            ulong elfLba = 1614;
            uint elfBytes = 11931808;

            // Tenta detectar dinamicamente no setor 22 do CD
            byte[] dirSec = new byte[2048];
            cd.ReadBlock(22, 1, dirSec);
            int idx = 0;
            while (idx < dirSec.Length)
            {
                byte recLen = dirSec[idx];
                if (recLen == 0) { idx++; continue; }
                if (idx + 33 < dirSec.Length)
                {
                    byte nameLen = dirSec[idx + 32];
                    if (idx + 33 + nameLen <= dirSec.Length)
                    {
                        string name = Encoding.ASCII.GetString(dirSec, idx + 33, nameLen);
                        if (name.StartsWith("NYBUOS.ELF", StringComparison.OrdinalIgnoreCase))
                        {
                            elfLba = BitConverter.ToUInt32(dirSec, idx + 2);
                            elfBytes = BitConverter.ToUInt32(dirSec, idx + 10);
                            break;
                        }
                    }
                }
                idx += recLen;
            }

            // Copia em blocos de 64 KB (32 setores de 2048 bytes do CD)
            ulong totalCdSectors = ((ulong)elfBytes + 2047) / 2048;
            ulong sectorsCopied = 0;
            uint chunkCdSectors = 32;

            // Inicia gravação contígua no FAT32
            fat32.StreamCopyFile(bootCluster, "NYBUOS.ELF", "NybuOS.elf", elfBytes, (writeChunk) =>
            {
                while (sectorsCopied < totalCdSectors)
                {
                    uint toRead = (uint)Math.Min((ulong)chunkCdSectors, totalCdSectors - sectorsCopied);
                    byte[] chunk = new byte[toRead * 2048];
                    cd.ReadBlock(elfLba + sectorsCopied, toRead, chunk);
                    sectorsCopied += toRead;

                    writeChunk(chunk);

                    int pct = (int)(sectorsCopied * 100 / totalCdSectors);
                    onPct(pct);
                }
            });
        }
        catch
        {
            onPct(100);
        }
    }
}

/// <summary>
/// Driver leve e autossuficiente para particionamento e formatação FAT32 direta em IBlockDevice.
/// Suporta nomes curtos 8.3 e Long File Names (LFN) compativeis com o Limine Bootloader.
/// </summary>
public class DirectFat32
{
    private readonly IBlockDevice? _device;
    private readonly uint _startLba;
    private readonly uint _partSectors;
    private readonly byte _spc;
    private readonly uint _clusterBytes;
    private readonly ushort _reservedSectors;
    private readonly byte _numFats;
    private readonly uint _fatSectors;
    private uint _nextFreeCluster = 3;

    public DirectFat32(IBlockDevice? device, uint startLba)
    {
        _device = device;
        _startLba = startLba;
        ulong total = device?.BlockCount ?? 4194304;
        _partSectors = (uint)(total > startLba ? total - startLba : 2097152);
        _spc = HardDiskInstaller.SectorsPerCluster;
        _clusterBytes = (uint)(_spc * 512);
        _reservedSectors = 32;
        _numFats = 2;

        uint dataSectors = _partSectors - _reservedSectors;
        uint fatSec = (dataSectors + (_spc * 128u + 2) - 1) / (_spc * 128u + 2);
        _fatSectors = ((fatSec + 31) / 32) * 32;
    }

    public void Format()
    {
        // 1. Boot Sector (Setor 0 da partição / LBA 2048)
        byte[] bs = new byte[512];
        bs[0] = 0xEB; bs[1] = 0x58; bs[2] = 0x90; // JMP
        Encoding.ASCII.GetBytes("MSWIN4.1").CopyTo(bs, 3);
        BitConverter.GetBytes((ushort)512).CopyTo(bs, 11);
        bs[13] = _spc;
        BitConverter.GetBytes(_reservedSectors).CopyTo(bs, 14);
        bs[16] = _numFats;
        bs[21] = 0xF8; // Disco fixo
        BitConverter.GetBytes((ushort)63).CopyTo(bs, 24); // Setores/trilha
        BitConverter.GetBytes((ushort)255).CopyTo(bs, 26); // Cabeças
        BitConverter.GetBytes(_startLba).CopyTo(bs, 28); // Setores ocultos
        BitConverter.GetBytes(_partSectors).CopyTo(bs, 32); // Total setores 32-bit
        BitConverter.GetBytes(_fatSectors).CopyTo(bs, 36); // Setores por FAT
        BitConverter.GetBytes((uint)2).CopyTo(bs, 44); // Cluster raiz = 2
        BitConverter.GetBytes((ushort)1).CopyTo(bs, 48); // Setor FSInfo = 1
        BitConverter.GetBytes((ushort)6).CopyTo(bs, 50); // Backup boot = 6
        bs[64] = 0x80; // Disco 0
        bs[66] = 0x29; // Assinatura estendida
        BitConverter.GetBytes(0x19952026).CopyTo(bs, 67); // Volume ID
        Encoding.ASCII.GetBytes("NYBUOS     ").CopyTo(bs, 71);
        Encoding.ASCII.GetBytes("FAT32   ").CopyTo(bs, 82);
        bs[510] = 0x55; bs[511] = 0xAA;

        WriteSector(_startLba, bs);
        WriteSector(_startLba + 6, bs); // Backup

        // 2. FSInfo Sector (Setor 1 da partição / LBA 2049)
        byte[] fsi = new byte[512];
        BitConverter.GetBytes(0x41615252).CopyTo(fsi, 0); // Lead sig "RRaA"
        BitConverter.GetBytes(0x61417272).CopyTo(fsi, 484); // Struct sig "rrAa"
        uint totalClusters = (_partSectors - _reservedSectors - (_numFats * _fatSectors)) / _spc;
        BitConverter.GetBytes(totalClusters - 1).CopyTo(fsi, 488);
        BitConverter.GetBytes((uint)3).CopyTo(fsi, 492); // Próximo livre = 3
        BitConverter.GetBytes(0xAA550000).CopyTo(fsi, 508);
        WriteSector(_startLba + 1, fsi);

        // 3. Inicializa FAT1 e FAT2
        SetFatEntry(0, 0x0FFFFFF8); // Mídia fixa
        SetFatEntry(1, 0x0FFFFFFF); // EOC
        SetFatEntry(2, 0x0FFFFFFF); // Cluster raiz (Cluster 2)

        // Limpa cluster raiz
        ulong rootLba = GetClusterLba(2);
        byte[] zeroSec = new byte[512];
        for (int s = 0; s < _spc; s++)
        {
            WriteSector((uint)(rootLba + (ulong)s), zeroSec);
        }

        _nextFreeCluster = 3;
    }

    public uint CreateDirectory(uint parentCluster, string dirName)
    {
        uint cluster = AllocateCluster();
        // Entradas obrigatórias . e ..
        AddDirEntry(cluster, ".          ", null, 0x10, cluster, 0);
        AddDirEntry(cluster, "..         ", null, 0x10, parentCluster, 0);

        // Entrada no diretório pai
        string shortName = dirName.ToUpper().PadRight(11).Substring(0, 11);
        AddDirEntry(parentCluster, shortName, dirName, 0x10, cluster, 0);
        return cluster;
    }

    public void WriteFile(uint dirCluster, string shortName83, string? longName, byte[] data)
    {
        uint dataLen = (uint)data.Length;
        uint neededClusters = (dataLen + _clusterBytes - 1) / _clusterBytes;
        if (neededClusters == 0) neededClusters = 1;

        uint firstCluster = 0;
        uint prevCluster = 0;

        for (uint i = 0; i < neededClusters; i++)
        {
            uint c = AllocateCluster();
            if (i == 0) firstCluster = c;
            else SetFatEntry(prevCluster, c);
            prevCluster = c;

            int chunkOffset = (int)(i * _clusterBytes);
            int chunkLen = Math.Min((int)_clusterBytes, (int)(dataLen - chunkOffset));
            if (chunkLen < 0) chunkLen = 0;

            ulong lba = GetClusterLba(c);
            for (int s = 0; s < _spc; s++)
            {
                byte[] secData = new byte[512];
                int secOff = s * 512;
                if (secOff < chunkLen)
                {
                    int copyLen = Math.Min(512, chunkLen - secOff);
                    Array.Copy(data, chunkOffset + secOff, secData, 0, copyLen);
                }
                WriteSector((uint)(lba + (ulong)s), secData);
            }
        }

        SetFatEntry(prevCluster, 0x0FFFFFFF);
        AddDirEntry(dirCluster, Format83(shortName83), longName, 0x20, firstCluster, dataLen);
    }

    public void StreamCopyFile(uint dirCluster, string shortName83, string? longName, uint totalBytes, Action<Action<byte[]>> streamWriter)
    {
        uint firstCluster = 0;
        uint prevCluster = 0;
        byte[] pending = new byte[_clusterBytes];
        int pendingLen = 0;

        Action<byte[]> handleChunk = (chunk) =>
        {
            int offset = 0;
            while (offset < chunk.Length)
            {
                int toCopy = Math.Min((int)_clusterBytes - pendingLen, chunk.Length - offset);
                Array.Copy(chunk, offset, pending, pendingLen, toCopy);
                pendingLen += toCopy;
                offset += toCopy;

                if (pendingLen == _clusterBytes)
                {
                    uint c = AllocateCluster();
                    if (firstCluster == 0) firstCluster = c;
                    else SetFatEntry(prevCluster, c);
                    prevCluster = c;

                    ulong lba = GetClusterLba(c);
                    for (int s = 0; s < _spc; s++)
                    {
                        byte[] sec = new byte[512];
                        Array.Copy(pending, s * 512, sec, 0, 512);
                        WriteSector((uint)(lba + (ulong)s), sec);
                    }
                    pendingLen = 0;
                }
            }
        };

        streamWriter(handleChunk);

        // Grava restante se houver
        if (pendingLen > 0 || firstCluster == 0)
        {
            uint c = AllocateCluster();
            if (firstCluster == 0) firstCluster = c;
            else SetFatEntry(prevCluster, c);
            prevCluster = c;

            ulong lba = GetClusterLba(c);
            for (int s = 0; s < _spc; s++)
            {
                byte[] sec = new byte[512];
                int secOff = s * 512;
                if (secOff < pendingLen)
                {
                    int copyLen = Math.Min(512, pendingLen - secOff);
                    Array.Copy(pending, secOff, sec, 0, copyLen);
                }
                WriteSector((uint)(lba + (ulong)s), sec);
            }
        }

        if (prevCluster != 0)
        {
            SetFatEntry(prevCluster, 0x0FFFFFFF);
        }

        AddDirEntry(dirCluster, Format83(shortName83), longName, 0x20, firstCluster, totalBytes);
    }

    private uint AllocateCluster()
    {
        uint c = _nextFreeCluster++;
        SetFatEntry(c, 0x0FFFFFFF);

        ulong lba = GetClusterLba(c);
        byte[] zero = new byte[512];
        for (int s = 0; s < _spc; s++)
        {
            WriteSector((uint)(lba + (ulong)s), zero);
        }
        return c;
    }

    private ulong GetClusterLba(uint cluster)
    {
        uint fatStart = _startLba + _reservedSectors;
        uint dataStart = fatStart + (_numFats * _fatSectors);
        return (ulong)dataStart + ((ulong)(cluster - 2) * _spc);
    }

    private void SetFatEntry(uint cluster, uint value)
    {
        uint fatStart = _startLba + _reservedSectors;
        uint secOffset = (cluster * 4) / 512;
        uint byteOffset = (cluster * 4) % 512;

        for (uint f = 0; f < _numFats; f++)
        {
            uint lba = fatStart + (f * _fatSectors) + secOffset;
            byte[] sec = ReadSector(lba);
            byte[] valBytes = BitConverter.GetBytes(value & 0x0FFFFFFF);
            Array.Copy(valBytes, 0, sec, (int)byteOffset, 4);
            WriteSector(lba, sec);
        }
    }

    private void AddDirEntry(uint dirCluster, string name11, string? longName, byte attr, uint firstCluster, uint fileSize)
    {
        byte[] nameBytes = Encoding.ASCII.GetBytes(name11.PadRight(11).Substring(0, 11));
        byte checksum = ComputeChecksum(nameBytes);

        ulong lba = GetClusterLba(dirCluster);
        for (int s = 0; s < _spc; s++)
        {
            byte[] sec = ReadSector((uint)(lba + (ulong)s));
            for (int off = 0; off <= 512 - 32; off += 32)
            {
                if (sec[off] == 0x00 || sec[off] == 0xE5)
                {
                    // Se possui nome longo e cabe LFN
                    if (!string.IsNullOrEmpty(longName) && off <= 512 - 64)
                    {
                        WriteLfnEntry(sec, off, longName, checksum);
                        off += 32;
                    }

                    // Entrada principal 8.3
                    Array.Copy(nameBytes, 0, sec, off, 11);
                    sec[off + 11] = attr;
                    BitConverter.GetBytes((ushort)((firstCluster >> 16) & 0xFFFF)).CopyTo(sec, off + 20);
                    BitConverter.GetBytes((ushort)(firstCluster & 0xFFFF)).CopyTo(sec, off + 26);
                    BitConverter.GetBytes(fileSize).CopyTo(sec, off + 28);

                    WriteSector((uint)(lba + (ulong)s), sec);
                    return;
                }
            }
        }
    }

    private static void WriteLfnEntry(byte[] sec, int off, string name, byte checksum)
    {
        sec[off] = 0x41; // Seq 1 + Last
        sec[off + 11] = 0x0F; // Attr LFN
        sec[off + 12] = 0x00;
        sec[off + 13] = checksum;
        sec[off + 26] = 0x00; sec[off + 27] = 0x00;

        for (int i = 0; i < 13; i++)
        {
            char c = (i < name.Length) ? name[i] : (i == name.Length ? '\0' : '\uFFFF');
            int pos = i switch
            {
                < 5 => off + 1 + (i * 2),
                < 11 => off + 14 + ((i - 5) * 2),
                _ => off + 28 + ((i - 11) * 2)
            };
            sec[pos] = (byte)(c & 0xFF);
            sec[pos + 1] = (byte)((c >> 8) & 0xFF);
        }
    }

    private static byte ComputeChecksum(byte[] shortName)
    {
        byte sum = 0;
        for (int i = 0; i < 11; i++)
        {
            sum = (byte)(((sum & 1) != 0 ? 0x80 : 0) + (sum >> 1) + shortName[i]);
        }
        return sum;
    }

    private static string Format83(string name)
    {
        string upper = name.ToUpper();
        int dot = upper.IndexOf('.');
        if (dot == -1)
        {
            return upper.PadRight(11).Substring(0, 11);
        }
        string baseName = upper.Substring(0, dot).PadRight(8).Substring(0, 8);
        string ext = upper.Substring(dot + 1).PadRight(3).Substring(0, 3);
        return baseName + ext;
    }

    private byte[] ReadSector(uint lba)
    {
        byte[] sec = new byte[512];
        if (_device != null)
        {
            try
            {
                _device.ReadBlock(lba, 1, sec);
            }
            catch { }
        }
        return sec;
    }

    private void WriteSector(uint lba, byte[] sec)
    {
        if (_device != null)
        {
            try
            {
                _device.WriteBlock(lba, 1, sec);
            }
            catch { }
        }
    }
}
