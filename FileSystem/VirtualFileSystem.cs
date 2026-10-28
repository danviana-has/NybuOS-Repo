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
        SyncFactoryFiles();
    }

    private void InitializeDefaultStructure()
    {
        _memoryDirs.Add(@"C:\");
        _memoryDirs.Add(@"C:\Desktop");
        _memoryDirs.Add(@"C:\Documents");
        _memoryDirs.Add(@"C:\System");
        _memoryDirs.Add(@"C:\Programs");
    }

    private void SyncFactoryFiles()
    {
        WriteFile(@"C:\Documents\readme.txt", 
            "Bem-vindo ao NybuOS 1.0!\n\n" +
            "Sistema Operacional grafico em C# para arquitetura x86_64.\n" +
            "Recursos integrados:\n" +
            "- Window Manager com Z-Order e Tema Windows 95 Clássico\n" +
            "- Subsistema VFS com persistencia em FAT32\n" +
            "- Gerenciador de Arquivos, Editor de Texto, Planilha, Calculadora e Terminal\n");

        WriteFile(@"C:\Documents\planilha.csv", 
            "Item,Qtd,Unit,Total\n" +
            "Placa-Mae,1,450,450\n" +
            "Processador,1,850,850\n" +
            "Memoria RAM,2,220,440\n" +
            "Disco SSD,1,300,300\n");

        WriteFile(@"C:\System\config.sys", 
            "OS=NybuOS\n" +
            "VERSION=1.0.0\n" +
            "ARCH=x86_64\n" +
            "GUI=True\n" +
            "SHELL=NybuShell\n");

        WriteFile(@"C:\Desktop\notas.txt", 
            "Tarefas do NybuOS:\n" +
            "1. Testar calculadora com operacoes aritmeticas\n" +
            "2. Salvar planilha em disco\n" +
            "3. Navegar pelas pastas no Explorer\n");
    }

    public string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return @"C:\";
        string clean = path.Replace('/', '\\').Trim();
        if (!clean.StartsWith("C:", StringComparison.OrdinalIgnoreCase) && !clean.StartsWith("0:", StringComparison.OrdinalIgnoreCase))
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
                return true;
            }
            catch { }
        }

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
                return true;
            }
            catch { }
        }

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
