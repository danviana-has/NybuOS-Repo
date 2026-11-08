using System;
using System.Collections.Generic;
using System.Drawing;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;
using NybuOS.FileSystem;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// Aplicativo Gerenciador de Arquivos com navegação real de diretórios no VFS
/// e renderização de ícones oficiais do Windows 95 para pastas e arquivos.
/// </summary>
public class FileManagerApp : Window
{
    private readonly VirtualFileSystem _vfs;
    private readonly Action<string> _openTextEditorCallback;
    private readonly Action<string>? _openMediaCallback;

    private string _currentPath = @"C:\";
    private List<string> _directories = new List<string>();
    private List<string> _files = new List<string>();
    private int _selectedIndex = -1;

    public FileManagerApp(int id, int x, int y, VirtualFileSystem vfs, Action<string> openTextEditorCallback, Action<string>? openMediaCallback = null)
        : base(id, "Gerenciador de Arquivos - NybuOS Purple", x, y, 760, 500)
    {
        Icon = IconResources.Computer16;
        _vfs = vfs;
        _openTextEditorCallback = openTextEditorCallback;
        _openMediaCallback = openMediaCallback;
        RefreshDirectory();
    }

    public void NavigateTo(string path)
    {
        _currentPath = _vfs.NormalizePath(path);
        RefreshDirectory();
    }

    private void RefreshDirectory()
    {
        _directories = _vfs.ListDirectories(_currentPath);
        _files = _vfs.ListFiles(_currentPath);
        _selectedIndex = -1;
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Fundo translúcido (65% opacidade / 35% transparência estilo macOS)
        display.DrawFrostedGlass(cx, cy, cw, ch, Color.FromArgb(246, 248, 252), 165);

        // Barra de Ferramentas com navegação rápida integrada
        display.DrawModernButton(cx + 6, cy + 6, 70, 30, "< Voltar", false);
        display.DrawModernButton(cx + 82, cy + 6, 75, 30, "C:\\ Raiz", false);
        display.DrawModernButton(cx + 163, cy + 6, 90, 30, "Downloads", false);
        display.DrawModernButton(cx + 259, cy + 6, 105, 30, "Boot/Kernel", false);
        display.DrawModernButton(cx + 370, cy + 6, 85, 30, "Novo Arq", false);
        display.DrawModernButton(cx + 461, cy + 6, 85, 30, "Atualizar", false);

        // Caixa de texto do caminho atual (Sunken)
        int pathBoxY = cy + 42;
        display.DrawFilledRectangle(cx + 6, pathBoxY, cw - 12, 24, Color.White);
        display.Draw3DBevel(cx + 6, pathBoxY, cw - 12, 24, true);

        if (IconResources.Drive16 != null)
        {
            display.DrawIcon(cx + 10, pathBoxY + 4, IconResources.Drive16);
            display.DrawString(cx + 32, pathBoxY + 5, _currentPath, Color.Black);
        }
        else
        {
            display.DrawString(cx + 10, pathBoxY + 5, _currentPath, Color.Black);
        }

        // Área da Lista de Arquivos (Vidro fosco 72% opacidade para máxima legibilidade)
        int listY = cy + 72;
        int listH = ch - 78;
        display.DrawFrostedGlass(cx + 6, listY, cw - 12, listH, Color.White, 185);
        display.Draw3DBevel(cx + 6, listY, cw - 12, listH, true);

        int itemY = listY + 4;
        int index = 0;

        // Desenha subdiretórios com ícone clássico de pasta do Windows 95
        for (int i = 0; i < _directories.Count; i++)
        {
            if (itemY + 20 > listY + listH) break;

            string fullDir = _directories[i];
            string dirName = fullDir.Substring(fullDir.LastIndexOf('\\') + 1);

            bool isSelected = (index == _selectedIndex);
            if (isSelected)
            {
                display.DrawFilledRectangle(cx + 6, itemY - 1, cw - 12, 23, Theme.SelectionBg);
            }

            // Ícone de pasta
            var folderIcon = isSelected ? (IconResources.FolderOpen16 ?? IconResources.FolderClosed16) : IconResources.FolderClosed16;
            if (folderIcon != null)
            {
                display.DrawIcon(cx + 8, itemY + 3, folderIcon);
            }

            Color textColor = isSelected ? Theme.TextSelected : Color.FromArgb(0, 0, 128);
            display.DrawString(cx + 28, itemY + 1, dirName, textColor);

            itemY += 24;
            index++;
        }

        // Desenha arquivos com seus ícones respectivos
        for (int i = 0; i < _files.Count; i++)
        {
            if (itemY + 20 > listY + listH) break;

            string fullFile = _files[i];
            string fileName = fullFile.Substring(fullFile.LastIndexOf('\\') + 1);

            bool isSelected = (index == _selectedIndex);
            if (isSelected)
            {
                display.DrawFilledRectangle(cx + 6, itemY - 1, cw - 12, 23, Theme.SelectionBg);
            }

            // Determina o ícone do arquivo baseado na extensão
            IconImage fileIcon = IconResources.Doc16;
            string lowerName = fileName.ToLower();
            if (lowerName.EndsWith(".bmp") || lowerName.EndsWith(".ico") || lowerName.EndsWith(".png") || lowerName.EndsWith(".jpg"))
            {
                fileIcon = IconResources.PaintPad16 ?? IconResources.Doc16;
            }
            else if (lowerName.EndsWith(".wav") || lowerName.EndsWith(".mp3"))
            {
                fileIcon = IconResources.Media16 ?? IconResources.Doc16;
            }
            else if (lowerName.EndsWith(".elf") || lowerName.EndsWith(".bin") || lowerName.EndsWith(".exe"))
            {
                fileIcon = IconResources.Terminal16 ?? IconResources.Doc16;
            }

            if (fileIcon != null)
            {
                display.DrawIcon(cx + 8, itemY + 3, fileIcon);
            }

            Color textColor = isSelected ? Theme.TextSelected : (lowerName.EndsWith(".elf") ? Color.FromArgb(142, 68, 237) : Color.Black);
            display.DrawString(cx + 28, itemY + 1, fileName, textColor);

            itemY += 24;
            index++;
        }

        if (_directories.Count == 0 && _files.Count == 0)
        {
            display.DrawString(cx + 12, listY + 12, "(Diretório Vazio)", Color.Gray);
        }
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;

        // Botão < Voltar
        if (mouseX >= cx + 6 && mouseX <= cx + 76 && mouseY >= cy + 6 && mouseY <= cy + 36)
        {
            int lastSlash = _currentPath.TrimEnd('\\').LastIndexOf('\\');
            if (lastSlash >= 2)
            {
                _currentPath = _currentPath.Substring(0, lastSlash);
                if (_currentPath.Length == 2 && _currentPath[1] == ':') _currentPath += "\\";
                RefreshDirectory();
            }
            return;
        }

        // Botão C:\ Raiz
        if (mouseX >= cx + 82 && mouseX <= cx + 157 && mouseY >= cy + 6 && mouseY <= cy + 36)
        {
            _currentPath = @"C:\";
            RefreshDirectory();
            return;
        }

        // Botão Downloads
        if (mouseX >= cx + 163 && mouseX <= cx + 253 && mouseY >= cy + 6 && mouseY <= cy + 36)
        {
            _currentPath = @"C:\Downloads";
            RefreshDirectory();
            return;
        }

        // Botão Boot/Kernel
        if (mouseX >= cx + 259 && mouseX <= cx + 364 && mouseY >= cy + 6 && mouseY <= cy + 36)
        {
            _currentPath = @"C:\boot";
            RefreshDirectory();
            return;
        }

        // Botão Novo Arq
        if (mouseX >= cx + 370 && mouseX <= cx + 455 && mouseY >= cy + 6 && mouseY <= cy + 36)
        {
            string newFilePath = _currentPath.TrimEnd('\\') + $"\\novo_{DateTime.Now.Ticks % 1000}.txt";
            _vfs.WriteFile(newFilePath, "Novo arquivo de texto criado via Explorer.\n");
            RefreshDirectory();
            return;
        }

        // Botão Atualizar
        if (mouseX >= cx + 461 && mouseX <= cx + 546 && mouseY >= cy + 6 && mouseY <= cy + 36)
        {
            RefreshDirectory();
            return;
        }

        // Clique na Lista de Itens
        int listY = cy + 72;
        int listH = ClientHeight - 78;
        if (mouseX >= cx + 6 && mouseX <= cx + ClientWidth - 6 && mouseY >= listY + 4 && mouseY <= listY + listH)
        {
            int clickedRow = (mouseY - (listY + 4)) / 24;
            int totalItems = _directories.Count + _files.Count;

            if (clickedRow >= 0 && clickedRow < totalItems)
            {
                if (_selectedIndex == clickedRow)
                {
                    // Duplo clique / ativação: Abre diretório ou arquivo
                    if (clickedRow < _directories.Count)
                    {
                        _currentPath = _directories[clickedRow];
                        RefreshDirectory();
                    }
                    else
                    {
                        int fileIdx = clickedRow - _directories.Count;
                        string targetFile = _files[fileIdx];
                        string lower = targetFile.ToLower();
                        if ((lower.EndsWith(".bmp") || lower.EndsWith(".png") || lower.EndsWith(".jpg")) && _openMediaCallback != null)
                        {
                            _openMediaCallback(targetFile);
                        }
                        else
                        {
                            _openTextEditorCallback?.Invoke(targetFile);
                        }
                    }
                }
                else
                {
                    _selectedIndex = clickedRow;
                }
            }
        }
    }
}
