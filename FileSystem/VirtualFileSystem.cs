using System;
using System.Collections.Generic;
using System.IO;
using Cosmos.Kernel.System.Storage;
using Cosmos.Kernel.System.FileSystem;
using Cosmos.Kernel.System.FileSystem.Fat;

namespace NybuOS.FileSystem;

/// <summary>
/// Subsistema de arquivos do NybuOS integrado com partições reais FAT32 e VFS.
/// </summary>
public class VirtualFileSystem
{
    private bool _isRealVfsMounted = false;
    private string _rootMount = @"0:\";

    // Estrutura em memória persistente (garante funcionalidade integral se executado apenas em ISO CD-ROM sem HDD formatado)
    private readonly Dictionary<string, string> _memoryFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, byte[]> _memoryBinaryFiles = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _memoryDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool IsMounted => _isRealVfsMounted;
    public string RootPath => _rootMount;

    public void Initialize()
    {
        InitializeDefaultStructure();
        _rootMount = @"0:\";
        _isRealVfsMounted = false;

        try
        {
            if (HardDiskInstaller.IsSystemInstalledOnHardDisk())
            {
                _isRealVfsMounted = true;
                WriteFile(@"C:\installed.tag", "INSTALLED=1\nBOOT=HDD\n");
                ScanHardDiskFat32();
            }
        }
        catch { }

        SyncFactoryFiles();
    }

    private void InitializeDefaultStructure()
    {
        _memoryDirs.Add(@"C:\");
        _memoryDirs.Add(@"C:\boot");
        _memoryDirs.Add(@"C:\Downloads");
        _memoryDirs.Add(@"C:\System");
        _memoryDirs.Add(@"C:\Documents");
        _memoryDirs.Add(@"C:\Desktop");
        _memoryDirs.Add(@"C:\Programs");
        _memoryDirs.Add(@"C:\limine");
        _memoryDirs.Add(@"C:\EFI");
        _memoryDirs.Add(@"C:\NybuOS");
    }

    private void ScanHardDiskFat32()
    {
        try
        {
            var hdd = HardDiskInstaller.FindTargetHardDisk();
            if (hdd == null) return;

            var fat32 = new DirectFat32(hdd, HardDiskInstaller.PartitionStartLba);

            // 1. Lê raiz (Cluster 2)
            fat32.ReadDirectoryEntries(2, out var rootFiles, out var rootDirs);
            foreach (var d in rootDirs)
            {
                string path = @"C:\" + d;
                _memoryDirs.Add(path);

                // Lê subdiretório
                uint cluster = fat32.FindDirectoryCluster(2, d);
                if (cluster > 0)
                {
                    fat32.ReadDirectoryEntries(cluster, out var subFiles, out var subDirs);
                    foreach (var sd in subDirs)
                    {
                        _memoryDirs.Add(path + "\\" + sd);
                    }
                    foreach (var sf in subFiles)
                    {
                        string filePath = path + "\\" + sf;
                        if (!_memoryFiles.ContainsKey(filePath))
                        {
                            _memoryFiles[filePath] = "[Arquivo FAT32 em disco]";
                        }
                    }
                }
            }

            foreach (var f in rootFiles)
            {
                string filePath = @"C:\" + f;
                if (!_memoryFiles.ContainsKey(filePath))
                {
                    _memoryFiles[filePath] = "[Arquivo FAT32 em disco]";
                }
            }
        }
        catch { }
    }

    private void SyncFactoryFiles()
    {
        // Arquivos de boot e kernel do sistema operacional
        WriteFile(@"C:\boot\NybuOS.elf", "NybuOS 64-bit ELF Kernel Binary Executable");
        WriteFile(@"C:\boot\limine.conf", "timeout: 3\n/NybuOS Purple\n    protocol: limine\n    kernel_path: boot():/boot/NybuOS.elf\n");
        WriteFile(@"C:\boot\limine-bios.sys", "Limine BIOS Stage 3 Bootloader System");
        WriteFile(@"C:\NybuOS.elf", "NybuOS 64-bit ELF Kernel Binary Executable (Root Image)");
        WriteFile(@"C:\limine.conf", "timeout: 3\n/NybuOS Purple\n    protocol: limine\n    kernel_path: boot():/boot/NybuOS.elf\n");
        WriteFile(@"C:\Downloads\info.txt", "Pasta oficial de downloads do NybuOS.\nTodos os arquivos baixados pelo aplicativo Download Central sao gravados nesta pasta.\n");

        WriteFile(@"C:\Documents\readme.txt", 
            "Bem-vindo ao NybuOS Purple!\n\n" +
            "Sistema Operacional grafico em C# para arquitetura x86_64.\n" +
            "Recursos integrados:\n" +
            "- Window Manager com Z-Order e Tema macOS Frosted Glass\n" +
            "- Subsistema VFS com persistencia em FAT32\n" +
            "- Download Central com suporte HTTP/HTTPS e assistente de atualizacao\n" +
            "- Gerenciador de Arquivos, Editor de Texto, Planilha, Calculadora e Terminal\n");

        WriteFile(@"C:\Documents\planilha.csv", 
            "Item,Qtd,Unit,Total\n" +
            "Placa-Mae,1,450,450\n" +
            "Processador,1,850,850\n" +
            "Memoria RAM,2,220,440\n" +
            "Disco SSD,1,300,300\n");

        WriteFile(@"C:\System\config.sys", 
            "OS=NybuOS\n" +
            "VERSION=Purple-1.2.0\n" +
            "ARCH=x86_64\n" +
            "GUI=True\n" +
            "THEME=MacOS-FrostedGlass\n" +
            "SHELL=NybuShell\n");

        WriteFile(@"C:\Desktop\notas.txt", 
            "Novidades do NybuOS Purple:\n" +
            "1. Interface minimalista com vidro translucido (60%-75% opacidade)\n" +
            "2. Novo wallpaper integrado em alta definicao\n" +
            "3. Aplicativo Download Central para baixar qualquer arquivo via link HTTP/HTTPS\n" +
            "4. Acesso direto a pasta C:\\boot com NybuOS.elf e C:\\Downloads\n");
    }

    public string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return @"C:\";
        string clean = path.Replace('/', '\\').Trim();
        if (clean.StartsWith("0:", StringComparison.OrdinalIgnoreCase))
        {
            clean = "C:" + clean.Substring(2);
        }
        else if (!clean.StartsWith("C:", StringComparison.OrdinalIgnoreCase))
        {
            if (clean.StartsWith("\\"))
                clean = "C:" + clean;
            else
                clean = @"C:\" + clean;
        }
        return clean;
    }

    public bool FileExists(string path)
    {
        string p = NormalizePath(path);

        if (_memoryFiles.ContainsKey(p)) return true;

        if (_isRealVfsMounted)
        {
            try
            {
                return File.Exists(p);
            }
            catch { }
        }

        return false;
    }

    public bool DirectoryExists(string path)
    {
        string p = NormalizePath(path);

        if (_memoryDirs.Contains(p)) return true;

        if (_isRealVfsMounted)
        {
            try
            {
                return Directory.Exists(p);
            }
            catch { }
        }

        return false;
    }

    public bool CreateDirectory(string path)
    {
        string p = NormalizePath(path);
        _memoryDirs.Add(p);

        if (_isRealVfsMounted)
        {
            try
            {
                Directory.CreateDirectory(p);
                return true;
            }
            catch { }
        }

        return true;
    }

    public bool WriteFile(string path, string content)
    {
        string p = NormalizePath(path);
        _memoryFiles[p] = content;

        // Assegura que o diretório pai existe no registro
        int lastSlash = p.LastIndexOf('\\');
        if (lastSlash > 2)
        {
            string dir = p.Substring(0, lastSlash);
            _memoryDirs.Add(dir);
        }

        if (_isRealVfsMounted)
        {
            try
            {
                File.WriteAllText(p, content);
            }
            catch { }
        }

        try
        {
            if (HardDiskInstaller.IsSystemInstalledOnHardDisk())
            {
                var hdd = HardDiskInstaller.FindTargetHardDisk();
                if (hdd != null)
                {
                    var fat32 = new DirectFat32(hdd, HardDiskInstaller.PartitionStartLba);
                    string[] parts = p.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
                    if (parts.Length == 2)
                    {
                        fat32.UpdateFile(2, parts[1], parts[1], data);
                    }
                    else if (parts.Length >= 3)
                    {
                        string dirName = parts[1];
                        string fileName = parts[2];
                        uint dirCluster = fat32.FindDirectoryCluster(2, dirName);
                        if (dirCluster == 0)
                        {
                            dirCluster = fat32.CreateDirectory(2, dirName.ToUpper());
                        }
                        if (dirCluster > 0)
                        {
                            fat32.UpdateFile(dirCluster, fileName, fileName, data);
                        }
                    }
                }
            }
        }
        catch { }

        return true;
    }

    public bool WriteFile(string path, byte[] bytes)
    {
        if (bytes == null) return false;
        string p = NormalizePath(path);
        _memoryBinaryFiles[p] = bytes;

        int lastSlash = p.LastIndexOf('\\');
        if (lastSlash > 2)
        {
            string dir = p.Substring(0, lastSlash);
            _memoryDirs.Add(dir);
        }

        if (_isRealVfsMounted)
        {
            try
            {
                File.WriteAllBytes(p, bytes);
            }
            catch { }
        }

        try
        {
            if (HardDiskInstaller.IsSystemInstalledOnHardDisk())
            {
                var hdd = HardDiskInstaller.FindTargetHardDisk();
                if (hdd != null)
                {
                    var fat32 = new DirectFat32(hdd, HardDiskInstaller.PartitionStartLba);
                    string[] parts = p.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        fat32.UpdateFile(2, parts[1], parts[1], bytes);
                    }
                    else if (parts.Length >= 3)
                    {
                        string dirName = parts[1];
                        string fileName = parts[2];
                        uint dirCluster = fat32.FindDirectoryCluster(2, dirName);
                        if (dirCluster == 0)
                        {
                            dirCluster = fat32.CreateDirectory(2, dirName.ToUpper());
                        }
                        if (dirCluster > 0)
                        {
                            fat32.UpdateFile(dirCluster, fileName, fileName, bytes);
                        }
                    }
                }
            }
        }
        catch { }

        return true;
    }

    public byte[] ReadFileBytes(string path)
    {
        string p = NormalizePath(path);

        if (_isRealVfsMounted)
        {
            try
            {
                if (File.Exists(p))
                {
                    return File.ReadAllBytes(p);
                }
            }
            catch { }
        }

        if (_memoryBinaryFiles.TryGetValue(p, out byte[] bytes))
        {
            return bytes;
        }

        return Array.Empty<byte>();
    }

    public string ReadFile(string path)
    {
        string p = NormalizePath(path);

        if (_isRealVfsMounted)
        {
            try
            {
                if (File.Exists(p))
                {
                    return File.ReadAllText(p);
                }
            }
            catch { }
        }

        if (_memoryFiles.TryGetValue(p, out string content))
        {
            return content;
        }

        return string.Empty;
    }

    public bool DeleteFile(string path)
    {
        string p = NormalizePath(path);
        bool removed = _memoryFiles.Remove(p);
        _memoryBinaryFiles.Remove(p);

        if (_isRealVfsMounted)
        {
            try
            {
                if (File.Exists(p))
                {
                    File.Delete(p);
                    return true;
                }
            }
            catch { }
        }

        return removed;
    }

    public bool DeleteDirectory(string path)
    {
        string p = NormalizePath(path);
        if (p.Equals(@"C:\", StringComparison.OrdinalIgnoreCase) || p.Equals(@"0:\", StringComparison.OrdinalIgnoreCase))
        {
            return false; // Não permite excluir raiz
        }

        bool removed = _memoryDirs.Remove(p);

        // Remove arquivos contidos no diretório em memória
        string prefix = p.EndsWith("\\") ? p : p + "\\";
        var filesToRemove = new List<string>();
        foreach (var file in _memoryFiles.Keys)
        {
            if (file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                filesToRemove.Add(file);
        }
        foreach (var f in filesToRemove)
        {
            _memoryFiles.Remove(f);
            _memoryBinaryFiles.Remove(f);
        }

        if (_isRealVfsMounted)
        {
            try
            {
                if (Directory.Exists(p))
                {
                    Directory.Delete(p, true);
                    return true;
                }
            }
            catch { }
        }

        return removed;
    }

    public List<string> ListDirectories(string directoryPath)
    {
        string target = NormalizePath(directoryPath);
        if (!target.EndsWith("\\")) target += "\\";

        var result = new List<string>();

        if (_isRealVfsMounted)
        {
            try
            {
                if (Directory.Exists(target))
                {
                    string[] dirs = Directory.GetDirectories(target);
                    foreach (var d in dirs)
                    {
                        if (!result.Contains(d)) result.Add(d);
                    }
                }
            }
            catch { }
        }

        foreach (var dir in _memoryDirs)
        {
            if (dir.Equals(target.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) continue;

            if (dir.StartsWith(target, StringComparison.OrdinalIgnoreCase))
            {
                string relative = dir.Substring(target.Length);
                if (!relative.Contains('\\') && !string.IsNullOrEmpty(relative))
                {
                    if (!result.Contains(dir)) result.Add(dir);
                }
            }
        }

        return result;
    }

    public List<string> ListFiles(string directoryPath)
    {
        string target = NormalizePath(directoryPath);
        if (!target.EndsWith("\\")) target += "\\";

        var result = new List<string>();

        if (_isRealVfsMounted)
        {
            try
            {
                if (Directory.Exists(target))
                {
                    string[] files = Directory.GetFiles(target);
                    foreach (var f in files)
                    {
                        if (!result.Contains(f)) result.Add(f);
                    }
                }
            }
            catch { }
        }

        foreach (var f in _memoryFiles.Keys)
        {
            if (f.StartsWith(target, StringComparison.OrdinalIgnoreCase))
            {
                string relative = f.Substring(target.Length);
                if (!relative.Contains('\\') && !string.IsNullOrEmpty(relative))
                {
                    if (!result.Contains(f)) result.Add(f);
                }
            }
        }

        foreach (var f in _memoryBinaryFiles.Keys)
        {
            if (f.StartsWith(target, StringComparison.OrdinalIgnoreCase))
            {
                string relative = f.Substring(target.Length);
                if (!relative.Contains('\\') && !string.IsNullOrEmpty(relative))
                {
                    if (!result.Contains(f)) result.Add(f);
                }
            }
        }

        return result;
    }
}
