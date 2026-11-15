using System;
using System.Collections.Generic;
using System.IO;
using NybuOS.Core.FileSystem;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Apps;

public class NotepadApp : AppBase
{
    public override string Id => "notepad";
    public override string Title => string.IsNullOrEmpty(_filePath) ? "Sem Título - Bloco de Notas" : $"{Path.GetFileName(_filePath)} - Bloco de Notas";
    public override FastBitmap? Icon16 => AssetManager.IconNotepad16;
    public override FastBitmap? Icon24 => AssetManager.IconNotepad24;
    public override FastBitmap? Icon48 => AssetManager.IconNotepad48;

    private readonly List<string> _lines = new();
    private int _cursorLine = 0;
    private int _cursorCol = 0;
    private int _scrollLine = 0;
    private string _filePath = "";
    private bool _isModified = false;

    public override void Initialize(Core.WindowManager.Window window)
    {
        base.Initialize(window);
        if (_lines.Count == 0)
        {
            _lines.Add("");
        }
    }

    public void LoadFile(string path)
    {
        _filePath = path;
        _lines.Clear();
        if (FileService.FileExists(path))
        {
            string content = FileService.ReadAllText(path);
            var raw = content.Replace("\r", "").Split('\n');
            for (int i = 0; i < raw.Length; i++) _lines.Add(raw[i]);
        }
        if (_lines.Count == 0) _lines.Add("");
        _cursorLine = 0;
        _cursorCol = 0;
        _isModified = false;
    }

    public void SaveFile()
    {
        if (string.IsNullOrEmpty(_filePath))
        {
            _filePath = Path.Combine(FileService.RootPath, "documentos", "NovoDocumento.txt");
        }
        string content = string.Join('\n', _lines);
        FileService.WriteAllText(_filePath, content);
        _isModified = false;
    }

    public override void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH)
    {
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, clientH, ColorScheme.WindowBody);

        // 1. Menu Bar
        int menuH = 26;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, menuH, ColorScheme.BackgroundLight);
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, clientX, clientY + menuH, clientW, ColorScheme.WindowBorder);

        FontRenderer.DrawText(buffer, screenW, screenH, "Novo", clientX + 12, clientY + 5, ColorScheme.TextPrimary, bold: false, size: 12);
        FontRenderer.DrawText(buffer, screenW, screenH, "Salvar", clientX + 54, clientY + 5, ColorScheme.TextPrimary, bold: false, size: 12);
        FontRenderer.DrawText(buffer, screenW, screenH, "Abrir Padrão", clientX + 104, clientY + 5, ColorScheme.TextPrimary, bold: false, size: 12);

        // 2. Editor Surface
        int textY = clientY + menuH + 4;
        int lineH = 18;
        int statusH = 22;
        int maxVisible = (clientH - menuH - statusH - 8) / lineH;

        for (int i = 0; i < maxVisible; i++)
        {
            int lineIdx = _scrollLine + i;
            if (lineIdx >= _lines.Count) break;

            int py = textY + (i * lineH);
            string lineText = _lines[lineIdx];
            FontRenderer.DrawText(buffer, screenW, screenH, lineText, clientX + 10, py, ColorScheme.TextPrimary, bold: false, size: 14);

            // Blinking cursor
            if (lineIdx == _cursorLine && (Environment.TickCount / 500) % 2 == 0)
            {
                string sub = lineText.Substring(0, Math.Min(_cursorCol, lineText.Length));
                int cursorX = clientX + 10 + FontRenderer.MeasureText(sub, bold: false, size: 14);
                FastRenderer.DrawVerticalLine(buffer, screenW, screenH, cursorX, py, lineH - 2, ColorScheme.Black);
            }
        }

        // 3. Status Bar
        int sbY = clientY + clientH - statusH;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, sbY, clientW, statusH, ColorScheme.BackgroundLight);
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, clientX, sbY, clientW, ColorScheme.WindowBorder);

        int totalChars = 0;
        for (int i = 0; i < _lines.Count; i++) totalChars += _lines[i].Length;

        string statusText = $"Lin {_cursorLine + 1}, Col {_cursorCol + 1} | {totalChars} caracteres | UTF-8 | {(_isModified ? "*" : "")}{_filePath}";
        FontRenderer.DrawText(buffer, screenW, screenH, statusText, clientX + 10, sbY + 4, ColorScheme.TextMuted, bold: false, size: 12);
    }

    public override void HandleInput(int clientX, int clientY, int clientW, int clientH)
    {
        // Menu item clicks
        if (InputState.LeftClicked)
        {
            int mx = InputState.MouseX;
            int my = InputState.MouseY;

            if (my >= clientY && my < clientY + 26)
            {
                // Novo
                if (mx >= clientX + 8 && mx < clientX + 48)
                {
                    _lines.Clear();
                    _lines.Add("");
                    _cursorLine = 0;
                    _cursorCol = 0;
                    _filePath = "";
                    _isModified = false;
                    return;
                }
                // Salvar
                if (mx >= clientX + 50 && mx < clientX + 98)
                {
                    SaveFile();
                    return;
                }
                // Abrir Padrão
                if (mx >= clientX + 100 && mx < clientX + 180)
                {
                    LoadFile(Path.Combine(FileService.RootPath, "documentos", "BoasVindas.txt"));
                    return;
                }
            }
        }

        // Keyboard text input
        while (InputState.TryReadKey(out var keyEvent))
        {
            _isModified = true;
            string curLine = _lines[_cursorLine];

            if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.Enter)
            {
                string left = curLine.Substring(0, Math.Min(_cursorCol, curLine.Length));
                string right = curLine.Substring(Math.Min(_cursorCol, curLine.Length));
                _lines[_cursorLine] = left;
                _lines.Insert(_cursorLine + 1, right);
                _cursorLine++;
                _cursorCol = 0;
            }
            else if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.Backspace)
            {
                if (_cursorCol > 0)
                {
                    string left = curLine.Substring(0, _cursorCol - 1);
                    string right = curLine.Substring(_cursorCol);
                    _lines[_cursorLine] = left + right;
                    _cursorCol--;
                }
                else if (_cursorLine > 0)
                {
                    string prevLine = _lines[_cursorLine - 1];
                    _cursorCol = prevLine.Length;
                    _lines[_cursorLine - 1] = prevLine + curLine;
                    _lines.RemoveAt(_cursorLine);
                    _cursorLine--;
                }
            }
            else if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.UpArrow)
            {
                if (_cursorLine > 0)
                {
                    _cursorLine--;
                    _cursorCol = Math.Min(_cursorCol, _lines[_cursorLine].Length);
                }
            }
            else if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.DownArrow)
            {
                if (_cursorLine < _lines.Count - 1)
                {
                    _cursorLine++;
                    _cursorCol = Math.Min(_cursorCol, _lines[_cursorLine].Length);
                }
            }
            else if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.LeftArrow)
            {
                if (_cursorCol > 0) _cursorCol--;
            }
            else if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.RightArrow)
            {
                if (_cursorCol < curLine.Length) _cursorCol++;
            }
            else if (keyEvent.KeyChar >= ' ' && keyEvent.KeyChar <= '~')
            {
                string left = curLine.Substring(0, Math.Min(_cursorCol, curLine.Length));
                string right = curLine.Substring(Math.Min(_cursorCol, curLine.Length));
                _lines[_cursorLine] = left + keyEvent.KeyChar + right;
                _cursorCol++;
            }
        }
    }
}
