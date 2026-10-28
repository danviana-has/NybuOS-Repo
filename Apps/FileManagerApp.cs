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

    private string _currentPath = @"C:\";
    private List<string> _directories = new List<string>();
    private List<string> _files = new List<string>();
    private int _selectedIndex = -1;

    public FileManagerApp(int id, int x, int y, VirtualFileSystem vfs, Action<string> openTextEditorCallback)
        : base(id, "Gerenciador de Arquivos", x, y, 380, 280)
    {
        Icon = IconResources.Computer16;
        _vfs = vfs;
        _openTextEditorCallback = openTextEditorCallback;
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

        // Fundo da área do cliente
        display.DrawFilledRectangle(cx, cy, cw, ch, Theme.Surface);

        // Barra de Ferramentas com botões
        display.DrawButton(cx + 4, cy + 4, 55, 20, "Voltar", false);
        display.DrawButton(cx + 63, cy + 4, 75, 20, "Novo Arq", false);
        display.DrawButton(cx + 142, cy + 4, 65, 20, "Atualizar", false);

        // Caixa de texto do caminho atual (Sunken)
        int pathBoxY = cy + 28;
        display.DrawFilledRectangle(cx + 4, pathBoxY, cw - 8, 20, Color.White);
        display.Draw3DBevel(cx + 4, pathBoxY, cw - 8, 20, true);

        if (IconResources.Drive16 != null)
        {
            display.DrawIcon(cx + 7, pathBoxY + 2, IconResources.Drive16);
            display.DrawString(cx + 26, pathBoxY + 4, _currentPath, Color.Black);
        }
        else
        {
            display.DrawString(cx + 8, pathBoxY + 4, _currentPath, Color.Black);
        }

        // Área da Lista de Arquivos (Sunken branca)
        int listY = cy + 52;
        int listH = ch - 56;
        display.DrawFilledRectangle(cx + 4, listY, cw - 8, listH, Color.White);
        display.Draw3DBevel(cx + 4, listY, cw - 8, listH, true);

        int itemY = listY + 4;
        int index = 0;

        // Desenha subdiretórios com ícone clássico de pasta do Windows 95
        for (int i = 0; i < _directories.Count; i++)
        {
            if (itemY + 16 > listY + listH) break;

            string fullDir = _directories[i];
            string dirName = fullDir.Substring(fullDir.LastIndexOf('\\') + 1);

            bool isSelected = (index == _selectedIndex);
            if (isSelected)
            {
                display.DrawFilledRectangle(cx + 6, itemY - 1, cw - 12, 17, Theme.SelectionBg);
            }

            // Ícone de pasta do Windows 95
            var folderIcon = isSelected ? (IconResources.FolderOpen16 ?? IconResources.FolderClosed16) : IconResources.FolderClosed16;
            if (folderIcon != null)
            {
                display.DrawIcon(cx + 8, itemY, folderIcon);
            }

            Color textColor = isSelected ? Theme.TextSelected : Color.FromArgb(0, 0, 128);
            display.DrawString(cx + 28, itemY + 2, dirName, textColor);

            itemY += 18;
            index++;
        }

        // Desenha arquivos com seus ícones respectivos do Windows 95
        for (int i = 0; i < _files.Count; i++)
        {
            if (itemY + 16 > listY + listH) break;

            string fullFile = _files[i];
            string fileName = fullFile.Substring(fullFile.LastIndexOf('\\') + 1);

            bool isSelected = (index == _selectedIndex);
            if (isSelected)
            {
                display.DrawFilledRectangle(cx + 6, itemY - 1, cw - 12, 17, Theme.SelectionBg);
            }

            // Determina o ícone do arquivo baseado na extensão
            IconImage fileIcon = IconResources.Doc16;
            string lowerName = fileName.ToLower();
            if (lowerName.EndsWith(".bmp") || lowerName.EndsWith(".ico"))
            {
                fileIcon = IconResources.PaintPad16 ?? IconResources.Doc16;
            }
            else if (lowerName.EndsWith(".wav") || lowerName.EndsWith(".mp3"))
            {
                fileIcon = IconResources.Media16 ?? IconResources.Doc16;
            }
            else if (lowerName.EndsWith(".exe") || lowerName.EndsWith(".bat"))
            {
                fileIcon = IconResources.Terminal16 ?? IconResources.Doc16;
            }

            if (fileIcon != null)
            {
                display.DrawIcon(cx + 8, itemY, fileIcon);
            }

            Color textColor = isSelected ? Theme.TextSelected : Color.Black;
            display.DrawString(cx + 28, itemY + 2, fileName, textColor);

            itemY += 18;
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

        // Botão Voltar
        if (mouseX >= cx + 4 && mouseX <= cx + 59 && mouseY >= cy + 4 && mouseY <= cy + 24)
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

        // Botão Novo Arq
        if (mouseX >= cx + 63 && mouseX <= cx + 138 && mouseY >= cy + 4 && mouseY <= cy + 24)
        {
            string newFilePath = _currentPath.TrimEnd('\\') + $"\\novo_{DateTime.Now.Ticks % 1000}.txt";
            _vfs.WriteFile(newFilePath, "Novo arquivo de texto criado via Explorer.\n");
            RefreshDirectory();
            return;
        }

        // Botão Atualizar
        if (mouseX >= cx + 142 && mouseX <= cx + 207 && mouseY >= cy + 4 && mouseY <= cy + 24)
        {
            RefreshDirectory();
            return;
        }

        // Clique na Lista de Itens
        int listY = cy + 52;
        int listH = ClientHeight - 56;
        if (mouseX >= cx + 4 && mouseX <= cx + ClientWidth - 4 && mouseY >= listY + 4 && mouseY <= listY + listH)
        {
            int clickedRow = (mouseY - (listY + 4)) / 18;
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
                        _openTextEditorCallback?.Invoke(targetFile);
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
