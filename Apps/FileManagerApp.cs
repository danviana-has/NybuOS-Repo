using System;
using System.IO;
using NybuOS.Core.FileSystem;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Apps;

public class FileManagerApp : AppBase
{
    public override string Id => "files";
    public override string Title => "Gerenciador de Arquivos";
    public override FastBitmap? Icon16 => AssetManager.IconFiles16;
    public override FastBitmap? Icon24 => AssetManager.IconFiles24;
    public override FastBitmap? Icon48 => AssetManager.IconFiles48;

    private string _currentPath = FileService.RootPath;
    private string[] _directories = Array.Empty<string>();
    private string[] _files = Array.Empty<string>();
    private int _selectedIndex = -1;
    private int _scrollOffset = 0;

    public override void Initialize(Core.WindowManager.Window window)
    {
        base.Initialize(window);
        Refresh();
    }

    private void Refresh()
    {
        _directories = FileService.GetDirectories(_currentPath);
        _files = FileService.GetFiles(_currentPath);
        _selectedIndex = -1;
    }

    public override void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH)
    {
        // 1. Toolbar Background
        int tbH = 38;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, tbH, ColorScheme.BackgroundLight);
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, clientX, clientY + tbH, clientW, ColorScheme.WindowBorder);

        // Buttons: Voltar, Nova Pasta, Excluir, Atualizar
        int btnX = clientX + 10;
        int btnY = clientY + 6;
        int btnH = 26;

        DrawButton(buffer, screenW, screenH, btnX, btnY, 32, btnH, "▲");
        btnX += 40;
        DrawButton(buffer, screenW, screenH, btnX, btnY, 95, btnH, "+ Nova Pasta");
        btnX += 105;
        DrawButton(buffer, screenW, screenH, btnX, btnY, 70, btnH, "Excluir");
        btnX += 80;
        DrawButton(buffer, screenW, screenH, btnX, btnY, 75, btnH, "Atualizar");

        // Path Display Bar
        int pathX = btnX + 85;
        int pathW = Math.Max(120, clientW - (pathX - clientX) - 12);
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, pathX, btnY, pathW, btnH, ColorScheme.WindowBody);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, pathX, btnY, pathW, btnH, ColorScheme.WindowBorder);
        FontRenderer.DrawText(buffer, screenW, screenH, _currentPath, pathX + 8, btnY + 6, ColorScheme.TextPrimary, bold: false, size: 12);

        // 2. Table Column Headers
        int headY = clientY + tbH + 1;
        int headH = 26;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, headY, clientW, headH, ColorScheme.ControlBackground);
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, clientX, headY + headH, clientW, ColorScheme.WindowBorder);

        FontRenderer.DrawText(buffer, screenW, screenH, "Nome", clientX + 32, headY + 6, ColorScheme.TextSecondary, bold: true, size: 12);
        FontRenderer.DrawText(buffer, screenW, screenH, "Tipo", clientX + (clientW / 2), headY + 6, ColorScheme.TextSecondary, bold: true, size: 12);
        FontRenderer.DrawText(buffer, screenW, screenH, "Tamanho", clientX + clientW - 100, headY + 6, ColorScheme.TextSecondary, bold: true, size: 12);

        // 3. File / Directory List
        int rowY = headY + headH + 2;
        int rowH = 28;
        int totalItems = _directories.Length + _files.Length;

        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        for (int i = _scrollOffset; i < totalItems; i++)
        {
            if (rowY + rowH > clientY + clientH - 24) break;

            bool isDir = i < _directories.Length;
            string fullPath = isDir ? _directories[i] : _files[i - _directories.Length];
            string name = Path.GetFileName(fullPath);
            if (string.IsNullOrEmpty(name)) name = fullPath;

            bool isSelected = (i == _selectedIndex);
            bool isHover = mx >= clientX && mx < clientX + clientW && my >= rowY && my < rowY + rowH;

            if (isSelected)
            {
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX + 2, rowY, clientW - 4, rowH, 0x400078D7);
                FastRenderer.DrawRectangle(buffer, screenW, screenH, clientX + 2, rowY, clientW - 4, rowH, ColorScheme.Accent);
            }
            else if (isHover)
            {
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX + 2, rowY, clientW - 4, rowH, ColorScheme.ControlBackground);
            }

            // Icon indicator
            string iconStr = isDir ? "📁" : "📄";
            FontRenderer.DrawText(buffer, screenW, screenH, iconStr, clientX + 8, rowY + 6, ColorScheme.TextPrimary, bold: false, size: 12);

            // Name
            FontRenderer.DrawText(buffer, screenW, screenH, name, clientX + 32, rowY + 6, ColorScheme.TextPrimary, bold: isDir, size: 12);

            // Type
            string typeStr = isDir ? "Pasta de Arquivos" : Path.GetExtension(name).ToUpperInvariant() + " Arquivo";
            FontRenderer.DrawText(buffer, screenW, screenH, typeStr, clientX + (clientW / 2), rowY + 6, ColorScheme.TextSecondary, bold: false, size: 12);

            // Size
            string sizeStr = isDir ? "--" : "1 KB";
            FontRenderer.DrawText(buffer, screenW, screenH, sizeStr, clientX + clientW - 100, rowY + 6, ColorScheme.TextSecondary, bold: false, size: 12);

            rowY += rowH;
        }

        // 4. Status Bar at Bottom
        int sbH = 22;
        int sbY = clientY + clientH - sbH;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, sbY, clientW, sbH, ColorScheme.BackgroundLight);
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, clientX, sbY, clientW, ColorScheme.WindowBorder);
        string statusText = $"{totalItems} item(ns) | FAT32 Disco: {FileService.RootPath}";
        FontRenderer.DrawText(buffer, screenW, screenH, statusText, clientX + 10, sbY + 4, ColorScheme.TextMuted, bold: false, size: 12);
    }

    private void DrawButton(uint[] buffer, int screenW, int screenH, int x, int y, int w, int h, string text)
    {
        int mx = InputState.MouseX;
        int my = InputState.MouseY;
        bool hover = mx >= x && mx < x + w && my >= y && my < y + h;
        uint bg = hover ? ColorScheme.ControlHover : ColorScheme.ControlBackground;

        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, x, y, w, h, bg);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, x, y, w, h, ColorScheme.WindowBorder);
        int tw = FontRenderer.MeasureText(text, bold: false, size: 12);
        FontRenderer.DrawText(buffer, screenW, screenH, text, x + (w - tw) / 2, y + 6, ColorScheme.TextPrimary, bold: false, size: 12);
    }

    public override void HandleInput(int clientX, int clientY, int clientW, int clientH)
    {
        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        if (InputState.LeftClicked)
        {
            int btnX = clientX + 10;
            int btnY = clientY + 6;
            int btnH = 26;

            // Voltar
            if (mx >= btnX && mx < btnX + 32 && my >= btnY && my < btnY + btnH)
            {
                string? parent = Path.GetDirectoryName(_currentPath.TrimEnd('\\'));
                if (!string.IsNullOrEmpty(parent))
                {
                    _currentPath = parent;
                    Refresh();
                }
                return;
            }
            btnX += 40;

            // Nova Pasta
            if (mx >= btnX && mx < btnX + 95 && my >= btnY && my < btnY + btnH)
            {
                string newDir = Path.Combine(_currentPath, "NovaPasta_" + (_directories.Length + 1));
                FileService.CreateDirectory(newDir);
                Refresh();
                return;
            }
            btnX += 105;

            // Excluir
            if (mx >= btnX && mx < btnX + 70 && my >= btnY && my < btnY + btnH)
            {
                if (_selectedIndex >= 0)
                {
                    if (_selectedIndex < _directories.Length)
                    {
                        FileService.DeleteDirectory(_directories[_selectedIndex]);
                    }
                    else
                    {
                        FileService.DeleteFile(_files[_selectedIndex - _directories.Length]);
                    }
                    Refresh();
                }
                return;
            }
            btnX += 80;

            // Atualizar
            if (mx >= btnX && mx < btnX + 75 && my >= btnY && my < btnY + btnH)
            {
                Refresh();
                return;
            }

            // Click on list item
            int headY = clientY + 38 + 1;
            int rowY = headY + 26 + 2;
            int rowH = 28;
            int totalItems = _directories.Length + _files.Length;

            for (int i = _scrollOffset; i < totalItems; i++)
            {
                if (my >= rowY && my < rowY + rowH)
                {
                    if (_selectedIndex == i)
                    {
                        // Double click action: enter directory or open file in Notepad
                        if (i < _directories.Length)
                        {
                            _currentPath = _directories[i];
                            Refresh();
                        }
                        else
                        {
                            string file = _files[i - _directories.Length];
                            AppRegistry.OpenInNotepad(file);
                        }
                    }
                    else
                    {
                        _selectedIndex = i;
                    }
                    return;
                }
                rowY += rowH;
            }
        }
    }
}
