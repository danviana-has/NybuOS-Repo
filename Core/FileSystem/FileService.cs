using System;
using System.Collections.Generic;
using System.IO;
using Cosmos.Kernel.System.FileSystem;
using Cosmos.Kernel.System.FileSystem.Fat;
using Cosmos.Kernel.System.Storage;

namespace NybuOS.Core.FileSystem;

public static class FileService
{
    private static bool _vfsInitialized = false;
    public static bool IsFatMounted { get; private set; } = false;
    public static string RootPath { get; private set; } = @"0:\";

    // Memory fallback storage in case bare-metal environment has no partitioned disk
    private static readonly Dictionary<string, byte[]> MemoryFiles = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> MemoryDirs = new(StringComparer.OrdinalIgnoreCase);

    public static void Initialize()
    {
        if (_vfsInitialized) return;
        _vfsInitialized = true;

        try
        {
            VfsManager.RegisterFileSystem("fat", new FatFileSystemType());

            // Try to find partition and mount 0:\
            var partitions = StorageManager.Partitions;
            if (partitions != null && partitions.Count > 0)
            {
                for (int i = 0; i < partitions.Count; i++)
                {
                    if (VfsManager.TryMount("fat", partitions[i], MountFlags.None, RootPath, out _))
                    {
                        IsFatMounted = true;
                        break;
                    }
                }
            }
        }
        catch
        {
            IsFatMounted = false;
        }

        // Initialize standard directories
        CreateDirectory(Path.Combine(RootPath, "documentos"));
        CreateDirectory(Path.Combine(RootPath, "planilhas"));
        CreateDirectory(Path.Combine(RootPath, "desenhos"));
        CreateDirectory(Path.Combine(RootPath, "sistema"));

        // Seed welcome file
        string welcomeFile = Path.Combine(RootPath, "documentos", "BoasVindas.txt");
        if (!FileExists(welcomeFile))
        {
            WriteAllText(welcomeFile, 
                "Bem-vindo ao NybuOS — Versão V Lagoons!\n" +
                "Arquitetura: x64 | Kernel: Cosmos Gen 3 (.NET 10)\n" +
                "Interface no tema claro inspirada no Windows 10.\n" +
                "Todos os seus arquivos sao salvos com seguranca no FAT32.");
        }
    }

    public static bool DirectoryExists(string path)
    {
        if (IsFatMounted)
        {
            try { return Directory.Exists(path); } catch { }
        }
        return MemoryDirs.Contains(Normalize(path));
    }

    public static void CreateDirectory(string path)
    {
        if (IsFatMounted)
        {
            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
            catch { }
        }
        MemoryDirs.Add(Normalize(path));
    }

    public static bool FileExists(string path)
    {
        if (IsFatMounted)
        {
            try { return File.Exists(path); } catch { }
        }
        return MemoryFiles.ContainsKey(Normalize(path));
    }

    public static void WriteAllText(string path, string content)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content ?? string.Empty);
        WriteAllBytes(path, bytes);
    }

    public static string ReadAllText(string path)
    {
        byte[] bytes = ReadAllBytes(path);
        if (bytes == null || bytes.Length == 0) return string.Empty;
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    public static void WriteAllBytes(string path, byte[] data)
    {
        if (IsFatMounted)
        {
            try
            {
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllBytes(path, data);
            }
            catch { }
        }
        MemoryFiles[Normalize(path)] = data;
    }

    public static byte[] ReadAllBytes(string path)
    {
        if (IsFatMounted)
        {
            try
            {
                if (File.Exists(path))
                {
                    return File.ReadAllBytes(path);
                }
            }
            catch { }
        }

        if (MemoryFiles.TryGetValue(Normalize(path), out var memData))
        {
            return memData;
        }
        return Array.Empty<byte>();
    }

    public static bool DeleteFile(string path)
    {
        bool success = false;
        if (IsFatMounted)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    success = true;
                }
            }
            catch { }
        }
        if (MemoryFiles.Remove(Normalize(path)))
        {
            success = true;
        }
        return success;
    }

    public static bool DeleteDirectory(string path)
    {
        bool success = false;
        if (IsFatMounted)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    success = true;
                }
            }
            catch { }
        }

        string norm = Normalize(path);
        MemoryDirs.Remove(norm);
        // remove nested files
        var keysToRemove = new List<string>();
        foreach (var k in MemoryFiles.Keys)
        {
            if (k.StartsWith(norm)) keysToRemove.Add(k);
        }
        foreach (var k in keysToRemove) MemoryFiles.Remove(k);

        return success;
    }

    public static string[] GetFiles(string dirPath)
    {
        var list = new List<string>();
        if (IsFatMounted)
        {
            try
            {
                if (Directory.Exists(dirPath))
                {
                    list.AddRange(Directory.GetFiles(dirPath));
                }
            }
            catch { }
        }

        string normDir = Normalize(dirPath).TrimEnd('\\') + "\\";
        foreach (var k in MemoryFiles.Keys)
        {
            if (k.StartsWith(normDir, StringComparison.OrdinalIgnoreCase))
            {
                string sub = k.Substring(normDir.Length);
                if (!sub.Contains('\\'))
                {
                    if (!list.Contains(k)) list.Add(k);
                }
            }
        }

        return list.ToArray();
    }

    public static string[] GetDirectories(string dirPath)
    {
        var list = new List<string>();
        if (IsFatMounted)
        {
            try
            {
                if (Directory.Exists(dirPath))
                {
                    list.AddRange(Directory.GetDirectories(dirPath));
                }
            }
            catch { }
        }

        string normDir = Normalize(dirPath).TrimEnd('\\') + "\\";
        foreach (var d in MemoryDirs)
        {
            if (d.StartsWith(normDir, StringComparison.OrdinalIgnoreCase))
            {
                string sub = d.Substring(normDir.Length);
                if (!string.IsNullOrEmpty(sub) && !sub.Contains('\\'))
                {
                    if (!list.Contains(d)) list.Add(d);
                }
            }
        }

        return list.ToArray();
    }

    private static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path)) return RootPath;
        return path.Replace('/', '\\');
    }
}
