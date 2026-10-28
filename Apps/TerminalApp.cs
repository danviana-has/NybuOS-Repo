using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;
using NybuOS.FileSystem;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// Terminal interativo em janela gráfica com prompt e execução de comandos do sistema.
/// </summary>
public class TerminalApp : Window
{
    private readonly VirtualFileSystem _vfs;
    private readonly List<string> _history = new List<string>();
    private readonly StringBuilder _currentInput = new StringBuilder();
    private string _currentDir = @"C:\";
    private int _maxLines = 15;
    private int _blinkCounter = 0;
    private bool _cursorVisible = true;

    public TerminalApp(int id, int x, int y, VirtualFileSystem vfs)
        : base(id, "Prompt de Comando - NybuOS Shell", x, y, 420, 260)
    {
        Icon = IconResources.Terminal16;
        _vfs = vfs;

        _history.Add("NybuOS [Versao 1.0.0 - 64-bit]");
        _history.Add("(C) Copyright NybuOS Corporation. Todos os direitos reservados.");
        _history.Add("");
        _history.Add("Digite 'help' para listar os comandos disponiveis.");
        _history.Add("");
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Fundo preto clássico do DOS
        display.DrawFilledRectangle(cx, cy, cw, ch, Color.Black);

        // Borda chanfrada afundada para emular terminal
        display.Draw3DBevel(cx, cy, cw, ch, true);

        int charW = CosmosNativeFont.Width;
        int charH = CosmosNativeFont.Height;
        int lineHeight = charH + 2;
        int maxCharsPerLine = Math.Max(10, (cw - 24) / charW);

        // Monta lista de linhas formatadas com quebra de linha visual para não ultrapassar a janela
        var formattedLines = new List<string>();
        for (int i = 0; i < _history.Count; i++)
        {
            string line = _history[i];
            if (string.IsNullOrEmpty(line))
            {
                formattedLines.Add("");
                continue;
            }

            while (line.Length > maxCharsPerLine)
            {
                formattedLines.Add(line.Substring(0, maxCharsPerLine));
                line = line.Substring(maxCharsPerLine);
            }
            formattedLines.Add(line);
        }

        int availableHeight = ch - 26;
        _maxLines = Math.Max(1, availableHeight / lineHeight);

        int startLine = Math.Max(0, formattedLines.Count - _maxLines + 1);
        int ty = cy + 6;

        for (int i = startLine; i < formattedLines.Count; i++)
        {
            if (ty + lineHeight > cy + ch - lineHeight - 4) break;
            display.DrawString(cx + 8, ty, formattedLines[i], Color.FromArgb(200, 200, 200));
            ty += lineHeight;
        }

        // Linha do prompt atual: C:\> comando
        string prompt = $"{_currentDir}> " + _currentInput.ToString();
        if (prompt.Length > maxCharsPerLine)
        {
            prompt = prompt.Substring(prompt.Length - maxCharsPerLine);
        }
        display.DrawString(cx + 8, ty, prompt, Color.Lime);

        // Cursor piscante
        _blinkCounter++;
        if (_blinkCounter > 15)
        {
            _cursorVisible = !_cursorVisible;
            _blinkCounter = 0;
        }

        if (IsActive && _cursorVisible && ty + charH < cy + ch - 2)
        {
            int cursorX = cx + 8 + (prompt.Length * charW);
            display.DrawLine(cursorX, ty, cursorX, ty + charH - 1, Color.Lime);
            display.DrawLine(cursorX + 1, ty, cursorX + 1, ty + charH - 1, Color.Lime);
        }
    }

    public override void OnKeyDown(KeyEvent key)
    {
        if (key.Key == Key.Enter)
        {
            string cmd = _currentInput.ToString().Trim();
            _history.Add($"{_currentDir}> {cmd}");
            _currentInput.Clear();

            if (!string.IsNullOrEmpty(cmd))
            {
                ExecuteCommand(cmd);
            }
        }
        else if (key.Key == Key.Backspace)
        {
            if (_currentInput.Length > 0)
            {
                _currentInput.Remove(_currentInput.Length - 1, 1);
            }
        }
        else if (key.KeyChar != '\0' && !char.IsControl(key.KeyChar))
        {
            if (_currentInput.Length < 45)
            {
                _currentInput.Append(key.KeyChar);
            }
        }
    }

    private void ExecuteCommand(string input)
    {
        string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string command = parts[0].ToLower();

        switch (command)
        {
            case "help":
                _history.Add("Comandos disponiveis:");
                _history.Add("  help         - Exibe esta mensagem de ajuda");
                _history.Add("  dir / ls     - Lista arquivos e diretorios da pasta atual");
                _history.Add("  cd <pasta>   - Navega para um diretorio");
                _history.Add("  type <arq>   - Exibe o conteudo de um arquivo de texto");
                _history.Add("  mkdir <nome> - Cria um novo diretorio");
                _history.Add("  rmdir <nome> - Remove um diretorio existente");
                _history.Add("  del <arq>    - Remove um arquivo");
                _history.Add("  ver          - Exibe a versao do sistema");
                _history.Add("  cls / clear  - Limpa a janela do terminal");
                _history.Add("  time         - Exibe a hora do relogio interno");
                _history.Add("  shutdown     - Desliga o computador com seguranca");
                break;

            case "cls":
            case "clear":
                _history.Clear();
                break;

            case "ver":
                _history.Add("NybuOS versao 1.0.0 [Cosmos C# / Native AOT]");
                break;

            case "time":
                _history.Add("Hora do sistema: " + DateTime.Now.ToString("HH:mm:ss"));
                break;

            case "dir":
            case "ls":
                var dirs = _vfs.ListDirectories(_currentDir);
                var files = _vfs.ListFiles(_currentDir);

                _history.Add($" Diretorio de {_currentDir}");
                _history.Add("");

                foreach (var d in dirs)
                {
                    string name = d.Substring(d.LastIndexOf('\\') + 1);
                    _history.Add($"  <DIR>          {name}");
                }

                foreach (var f in files)
                {
                    string name = f.Substring(f.LastIndexOf('\\') + 1);
                    _history.Add($"                 {name}");
                }

                _history.Add($"       {files.Count} arquivo(s), {dirs.Count} pasta(s)");
                break;

            case "cd":
                if (parts.Length < 2)
                {
                    _history.Add(_currentDir);
                }
                else
                {
                    string target = parts[1];
                    if (target == "..")
                    {
                        int lastSlash = _currentDir.TrimEnd('\\').LastIndexOf('\\');
                        if (lastSlash >= 2)
                        {
                            _currentDir = _currentDir.Substring(0, lastSlash);
                            if (_currentDir.Length == 2 && _currentDir[1] == ':') _currentDir += "\\";
                        }
                    }
                    else if (target == "\\")
                    {
                        _currentDir = @"C:\";
                    }
                    else
                    {
                        string newPath = _currentDir.TrimEnd('\\') + "\\" + target;
                        if (_vfs.DirectoryExists(newPath))
                        {
                            _currentDir = _vfs.NormalizePath(newPath);
                        }
                        else
                        {
                            _history.Add("Diretorio nao encontrado: " + target);
                        }
                    }
                }
                break;

            case "type":
            case "cat":
                if (parts.Length < 2)
                {
                    _history.Add("Sintaxe: type <nome_do_arquivo>");
                }
                else
                {
                    string fileTarget = _currentDir.TrimEnd('\\') + "\\" + parts[1];
                    if (_vfs.FileExists(fileTarget))
                    {
                        string text = _vfs.ReadFile(fileTarget);
                        string[] lines = text.Split('\n');
                        foreach (var l in lines)
                        {
                            _history.Add(l.Trim('\r'));
                        }
                    }
                    else
                    {
                        _history.Add("Arquivo nao encontrado.");
                    }
                }
                break;

            case "mkdir":
                if (parts.Length < 2)
                {
                    _history.Add("Sintaxe: mkdir <nome_da_pasta>");
                }
                else
                {
                    string newDir = _currentDir.TrimEnd('\\') + "\\" + parts[1];
                    _vfs.CreateDirectory(newDir);
                    _history.Add("Diretorio criado com sucesso.");
                }
                break;

            case "rmdir":
            case "rd":
                if (parts.Length < 2)
                {
                    _history.Add("Sintaxe: rmdir <pasta>");
                }
                else
                {
                    string toDelDir = _currentDir.TrimEnd('\\') + "\\" + parts[1];
                    if (_vfs.DeleteDirectory(toDelDir))
                    {
                        _history.Add("Diretorio removido com sucesso.");
                    }
                    else
                    {
                        _history.Add("Erro: diretorio nao encontrado ou nao pode ser removido.");
                    }
                }
                break;

            case "shutdown":
                _history.Add("Desligando o NybuOS...");
                PowerDriver.Shutdown();
                break;

            case "reboot":
                _history.Add("Reiniciando o NybuOS...");
                PowerDriver.Reboot();
                break;

            case "del":
            case "rm":
                if (parts.Length < 2)
                {
                    _history.Add("Sintaxe: del <arquivo>");
                }
                else
                {
                    string toDel = _currentDir.TrimEnd('\\') + "\\" + parts[1];
                    if (_vfs.DeleteFile(toDel))
                    {
                        _history.Add("Arquivo removido.");
                    }
                    else
                    {
                        _history.Add("Arquivo nao encontrado.");
                    }
                }
                break;

            default:
                _history.Add($"'{parts[0]}' nao e reconhecido como um comando interno.");
                break;
        }

        _history.Add("");
    }
}
