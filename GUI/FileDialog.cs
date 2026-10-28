using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;
using NybuOS.FileSystem;

namespace NybuOS.GUI;

public enum FileDialogMode
{
    Open,
    Save
}

/// <summary>
/// Caixa de diálogo modal para Abrir ou Salvar arquivos com navegação no FAT32,
/// seleção com duplo clique e campo de texto interativo para escolha do nome.
/// </summary>
public class FileDialog : Window
{
    private readonly VirtualFileSystem _vfs;
    public FileDialogMode Mode { get; }
    public string ExtensionFilter { get; }
    private readonly Action<string> _onConfirmed;
    private readonly Action? _onCancelled;

    private string _currentDirectory = @"C:\Documents";
    private readonly StringBuilder _fileNameInput = new StringBuilder();
    private readonly List<string> _files = new List<string>();
    private int _selectedIndex = -1;
    private int _lastClickIndex = -1;
    private int _lastClickTick = 0;
    private int _tickCount = 0;

    public FileDialog(int id, string title, FileDialogMode mode, string defaultFileName, string extensionFilter, VirtualFileSystem vfs, Action<string> onConfirmed, Action? onCancelled = null)
        : base(id, title, 200, 150, 380, 260)
    {
        Icon = IconResources.FileManagerIcon ?? IconResources.Doc16;
        Mode = mode;
        ExtensionFilter = extensionFilter.ToLower();
        _vfs = vfs;
        _onConfirmed = onConfirmed;
        _onCancelled = onCancelled;

        if (!string.IsNullOrEmpty(defaultFileName))
        {
            _fileNameInput.Append(defaultFileName);
        }

        RefreshFileList();
    }

    public void RefreshFileList()
    {
        _files.Clear();
        try
        {
            var allFiles = _vfs.ListFiles(_currentDirectory);
            foreach (var f in allFiles)
            {
                if (string.IsNullOrEmpty(ExtensionFilter) || f.ToLower().EndsWith(ExtensionFilter))
                {
                    _files.Add(f);
                }
            }
        }
        catch { }

        _selectedIndex = -1;
    }

    public override void DrawContent(DisplayDriver display)
    {
        _tickCount++;
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Fundo estilo Dark Glass / Modern Surface
        display.DrawFilledRectangle(cx, cy, cw, ch, Theme.Surface);

        // Cabeçalho da pasta atual
        display.DrawString(cx + 8, cy + 6, $"Pasta: {_currentDirectory}", Theme.TextSecondary);

        // Lista de Arquivos (Área branca / Dark Input box)
        int listX = cx + 8;
        int listY = cy + 24;
        int listW = cw - 16;
        int listH = ch - 86;

        display.DrawFilledRectangle(listX, listY, listW, listH, Theme.InputBg);
        display.DrawRectangle(listX, listY, listW, listH, Theme.LightBorder);

        int maxVisible = listH / 18;
        for (int i = 0; i < Math.Min(_files.Count, maxVisible); i++)
        {
            int itemY = listY + 2 + (i * 18);
            bool isSel = (i == _selectedIndex);
            string fName = Path.GetFileName(_files[i]);

            if (isSel)
            {
                display.DrawFilledRectangle(listX + 2, itemY, listW - 4, 16, Theme.AccentColor);
                display.DrawString(listX + 6, itemY + 1, fName, Color.White);
            }
            else
            {
                display.DrawString(listX + 6, itemY + 1, fName, Theme.TextPrimary);
            }
        }

        if (_files.Count == 0)
        {
            display.DrawString(listX + 10, listY + 10, "(Nenhum arquivo encontrado)", Theme.TextSecondary);
        }

        // Campo de entrada de texto: "Nome do Arquivo:"
        int inputLabelY = listY + listH + 8;
        display.DrawString(cx + 8, inputLabelY + 2, "Nome:", Theme.TextPrimary);

        int inputX = cx + 55;
        int inputW = cw - 70;
        int inputH = 20;
        display.DrawModernInputBox(inputX, inputLabelY, inputW, inputH, _fileNameInput.ToString(), true);

        // Botões de Ação na parte inferior
        int btnY = cy + ch - 26;
        string confirmBtnText = (Mode == FileDialogMode.Save) ? "Salvar" : "Abrir";

        display.DrawModernButton(cw - 170 + cx, btnY, 75, 22, confirmBtnText, true);
        display.DrawModernButton(cw - 85 + cx, btnY, 75, 22, "Cancelar", false);
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        int listX = cx + 8;
        int listY = cy + 24;
        int listW = cw - 16;
        int listH = ch - 86;

        // Clique na lista de arquivos
        if (mouseX >= listX && mouseX <= listX + listW && mouseY >= listY && mouseY <= listY + listH)
        {
            int clickedIndex = (mouseY - listY) / 18;
            if (clickedIndex >= 0 && clickedIndex < _files.Count)
            {
                _selectedIndex = clickedIndex;
                string chosen = Path.GetFileName(_files[clickedIndex]);
                _fileNameInput.Clear();
                _fileNameInput.Append(chosen);

                // Detecta duplo clique (dois cliques rápidos no mesmo item)
                if (_lastClickIndex == clickedIndex && (_tickCount - _lastClickTick) < 15)
                {
                    ConfirmChoice();
                    return;
                }

                _lastClickIndex = clickedIndex;
                _lastClickTick = _tickCount;
            }
            return;
        }

        // Botões na parte inferior
        int btnY = cy + ch - 26;
        if (mouseY >= btnY && mouseY <= btnY + 24)
        {
            // Botão Confirmar (Salvar / Abrir)
            if (mouseX >= cw - 170 + cx && mouseX <= cw - 95 + cx)
            {
                ConfirmChoice();
                return;
            }

            // Botão Cancelar
            if (mouseX >= cw - 85 + cx && mouseX <= cw - 10 + cx)
            {
                Cancel();
                return;
            }
        }
    }

    public override void OnKeyDown(KeyEvent key)
    {
        if (key.Key == Key.Enter)
        {
            ConfirmChoice();
        }
        else if (key.Key == Key.Escape)
        {
            Cancel();
        }
        else if (key.Key == Key.Backspace)
        {
            if (_fileNameInput.Length > 0)
            {
                _fileNameInput.Remove(_fileNameInput.Length - 1, 1);
            }
        }
        else if (key.KeyChar != '\0' && !char.IsControl(key.KeyChar))
        {
            if (_fileNameInput.Length < 35)
            {
                _fileNameInput.Append(key.KeyChar);
            }
        }
    }

    private void ConfirmChoice()
    {
        string name = _fileNameInput.ToString().Trim();
        if (string.IsNullOrEmpty(name)) return;

        if (!string.IsNullOrEmpty(ExtensionFilter) && !name.ToLower().EndsWith(ExtensionFilter))
        {
            name += ExtensionFilter;
        }

        string fullPath = Path.Combine(_currentDirectory, name);
        IsClosed = true;
        _onConfirmed?.Invoke(fullPath);
    }

    private void Cancel()
    {
        IsClosed = true;
        _onCancelled?.Invoke();
    }
}
