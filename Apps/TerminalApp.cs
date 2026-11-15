using System;
using System.Collections.Generic;
using System.IO;
using Cosmos.Kernel.System;
using NybuOS.Core.FileSystem;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Apps;

public class TerminalApp : AppBase
{
    public override string Id => "terminal";
    public override string Title => "Terminal NybuOS";
    public override FastBitmap? Icon16 => AssetManager.IconTerminal16;
    public override FastBitmap? Icon24 => AssetManager.IconTerminal24;
    public override FastBitmap? Icon48 => AssetManager.IconTerminal48;

    private readonly List<string> _lines = new();
    private string _currentCommand = "";
    private string _currentDirectory = FileService.RootPath;
    private readonly List<string> _history = new();
    private int _historyIndex = -1;
    private int _scrollOffset = 0;

    public override void Initialize(Core.WindowManager.Window window)
    {
        base.Initialize(window);
        _lines.Clear();
        _lines.Add("NybuOS [Versão V Lagoons] CLI Shell (x64)");
        _lines.Add("Cosmos OS Gen 3 Kernel | .NET 10 NativeAOT");
        _lines.Add("Digite 'help' para listar os comandos disponíveis.");
        _lines.Add("");
    }

    public override void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH)
    {
        // Dark terminal background (#121212)
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, clientH, 0xFF121212);

        int lineH = 18;
        int maxLines = (clientH - 30) / lineH;

        int startLine = Math.Max(0, _lines.Count - maxLines - _scrollOffset);
        int curY = clientY + 8;

        for (int i = startLine; i < _lines.Count; i++)
        {
            if (curY + lineH > clientY + clientH - 24) break;
            FontRenderer.DrawText(buffer, screenW, screenH, _lines[i], clientX + 10, curY, ColorScheme.TextWhite, bold: false, size: 12);
            curY += lineH;
        }

        // Active prompt line
        string prompt = $"nybu@lagoons:{_currentDirectory}> {_currentCommand}_";
        FontRenderer.DrawText(buffer, screenW, screenH, prompt, clientX + 10, curY, 0xFF00FF66, bold: true, size: 12);
    }

    public override void HandleInput(int clientX, int clientY, int clientW, int clientH)
    {
        while (InputState.TryReadKey(out var keyEvent))
        {
            if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.Enter)
            {
                ExecuteCommand(_currentCommand);
                _currentCommand = "";
                _historyIndex = -1;
            }
            else if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.Backspace)
            {
                if (_currentCommand.Length > 0)
                {
                    _currentCommand = _currentCommand.Substring(0, _currentCommand.Length - 1);
                }
            }
            else if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.UpArrow)
            {
                if (_history.Count > 0)
                {
                    if (_historyIndex == -1) _historyIndex = _history.Count - 1;
                    else if (_historyIndex > 0) _historyIndex--;
                    _currentCommand = _history[_historyIndex];
                }
            }
            else if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.DownArrow)
            {
                if (_historyIndex >= 0)
                {
                    if (_historyIndex < _history.Count - 1)
                    {
                        _historyIndex++;
                        _currentCommand = _history[_historyIndex];
                    }
                    else
                    {
                        _historyIndex = -1;
                        _currentCommand = "";
                    }
                }
            }
            else if (keyEvent.KeyChar >= ' ' && keyEvent.KeyChar <= '~')
            {
                _currentCommand += keyEvent.KeyChar;
            }
        }
    }

    private void ExecuteCommand(string input)
    {
        string cmdLine = input.Trim();
        _lines.Add($"nybu@lagoons:{_currentDirectory}> {cmdLine}");
        if (string.IsNullOrEmpty(cmdLine)) return;

        _history.Add(cmdLine);

        string[] parts = cmdLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0].ToLowerInvariant();
        string arg = parts.Length > 1 ? cmdLine.Substring(parts[0].Length).Trim() : "";

        switch (cmd)
        {
            case "help":
                _lines.Add("Comandos Disponíveis:");
                _lines.Add("  dir / ls      - Lista arquivos e diretórios");
                _lines.Add("  cd <caminho>  - Altera o diretório atual");
                _lines.Add("  mkdir <nome>  - Cria um novo diretório");
                _lines.Add("  cat <arquivo> - Exibe o conteúdo de um arquivo");
                _lines.Add("  rm <caminho>  - Exclui arquivo ou pasta");
                _lines.Add("  echo <texto>  - Exibe texto");
                _lines.Add("  sysinfo       - Informações do sistema operacional");
                _lines.Add("  mem           - Estatísticas de memória RAM e GC");
                _lines.Add("  theme <cor>   - Altera cor de destaque (azul, laranja, roxo...)");
                _lines.Add("  kill <appId>  - Encerra uma aplicação ativa");
                _lines.Add("  clear / cls   - Limpa o terminal");
                _lines.Add("  reboot        - Reinicia o sistema");
                _lines.Add("  shutdown      - Desliga o sistema");
                break;

            case "dir":
            case "ls":
                var dirs = FileService.GetDirectories(_currentDirectory);
                var files = FileService.GetFiles(_currentDirectory);
                _lines.Add($" Diretório de {_currentDirectory}:");
                foreach (var d in dirs) _lines.Add($"  [PASTA] {Path.GetFileName(d)}");
                foreach (var f in files) _lines.Add($"  [ARQ]   {Path.GetFileName(f)}");
                _lines.Add($"  Total: {dirs.Length} pasta(s), {files.Length} arquivo(s)");
                break;

            case "cd":
                if (string.IsNullOrEmpty(arg) || arg == "~")
                {
                    _currentDirectory = FileService.RootPath;
                }
                else if (arg == "..")
                {
                    string? parent = Path.GetDirectoryName(_currentDirectory.TrimEnd('\\'));
                    _currentDirectory = string.IsNullOrEmpty(parent) ? FileService.RootPath : parent;
                }
                else
                {
                    string target = Path.IsPathRooted(arg) ? arg : Path.Combine(_currentDirectory, arg);
                    if (FileService.DirectoryExists(target)) _currentDirectory = target;
                    else _lines.Add($"Erro: Diretório '{arg}' não encontrado.");
                }
                break;

            case "mkdir":
                if (!string.IsNullOrEmpty(arg))
                {
                    string path = Path.IsPathRooted(arg) ? arg : Path.Combine(_currentDirectory, arg);
                    FileService.CreateDirectory(path);
                    _lines.Add($"Diretório '{arg}' criado com sucesso.");
                }
                else _lines.Add("Uso: mkdir <nome_da_pasta>");
                break;

            case "cat":
                if (!string.IsNullOrEmpty(arg))
                {
                    string path = Path.IsPathRooted(arg) ? arg : Path.Combine(_currentDirectory, arg);
                    if (FileService.FileExists(path))
                    {
                        string content = FileService.ReadAllText(path);
                        var lines = content.Split('\n');
                        for (int i = 0; i < Math.Min(30, lines.Length); i++) _lines.Add(lines[i].TrimEnd('\r'));
                    }
                    else _lines.Add($"Erro: Arquivo '{arg}' não encontrado.");
                }
                else _lines.Add("Uso: cat <arquivo>");
                break;

            case "rm":
                if (!string.IsNullOrEmpty(arg))
                {
                    string path = Path.IsPathRooted(arg) ? arg : Path.Combine(_currentDirectory, arg);
                    if (FileService.FileExists(path))
                    {
                        FileService.DeleteFile(path);
                        _lines.Add($"Arquivo '{arg}' excluído.");
                    }
                    else if (FileService.DirectoryExists(path))
                    {
                        FileService.DeleteDirectory(path);
                        _lines.Add($"Diretório '{arg}' excluído.");
                    }
                    else _lines.Add($"Erro: '{arg}' não encontrado.");
                }
                else _lines.Add("Uso: rm <caminho>");
                break;

            case "echo":
                _lines.Add(arg);
                break;

            case "sysinfo":
                _lines.Add("NybuOS — Versão V Lagoons [Bare-Metal x64]");
                _lines.Add("Kernel: Cosmos OS Gen 3 | Compilador: .NET 10 NativeAOT");
                _lines.Add($"VFS: {FileService.RootPath} (FAT32: {(FileService.IsFatMounted ? "Ativo" : "Simulado")})");
                _lines.Add($"Tema Atual: {ColorScheme.AccentName} | Brilho: {ColorScheme.BrightnessPercent}% | Volume: {ColorScheme.VolumePercent}%");
                break;

            case "mem":
                long mem = GC.GetTotalMemory(false);
                _lines.Add($"Memória Heap do GC: {mem / 1024} KB ({mem} bytes)");
                _lines.Add($"GC Geração 0 Coletas: {GC.CollectionCount(0)}");
                break;

            case "theme":
                if (!string.IsNullOrEmpty(arg))
                {
                    ColorScheme.SetAccent(arg);
                    _lines.Add($"Cor de destaque alterada para: {ColorScheme.AccentName}");
                }
                else _lines.Add("Uso: theme <azul|laranja|roxo|verde|amarelo|vermelho>");
                break;

            case "kill":
                if (!string.IsNullOrEmpty(arg))
                {
                    AppRegistry.CloseApp(arg);
                    _lines.Add($"Aplicação '{arg}' finalizada.");
                }
                else _lines.Add("Uso: kill <appId>");
                break;

            case "clear":
            case "cls":
                _lines.Clear();
                break;

            case "reboot":
                _lines.Add("Reiniciando sistema...");
                Power.Reboot();
                break;

            case "shutdown":
                _lines.Add("Desligando sistema...");
                Power.Shutdown();
                break;

            default:
                _lines.Add($"Comando desconhecido: '{cmd}'. Digite 'help' para ajuda.");
                break;
        }

        _lines.Add("");
    }
}
