using System;
using System.Drawing;
using System.Text;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;
using NybuOS.FileSystem;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// Editor de Texto com digitação contínua, persistência real de arquivos e tema clássico.
/// </summary>
public class TextEditorApp : Window
{
    private readonly VirtualFileSystem _vfs;
    private readonly WindowManager? _windowManager;
    private readonly StringBuilder _textBuffer = new StringBuilder();
    private string _filePath = @"C:\Documents\readme.txt";
    private string _statusMessage = "Pronto";
    private int _cursorIndex = 0;
    private int _blinkCounter = 0;
    private bool _cursorVisible = true;

    public TextEditorApp(int id, int x, int y, VirtualFileSystem vfs, WindowManager? wm = null, string? initialFilePath = null)
        : base(id, "Bloco de Notas - NybuOS", x, y, 380, 280)
    {
        Icon = IconResources.TextEditorIcon ?? IconResources.Doc16;
        _vfs = vfs;
        _windowManager = wm;
        if (!string.IsNullOrEmpty(initialFilePath))
        {
            _filePath = initialFilePath;
            OpenFile(_filePath);
        }
        else
        {
            OpenFile(_filePath);
        }
    }

    public void OpenFile(string path)
    {
        _filePath = path;
        string content = _vfs.ReadFile(_filePath);
        _textBuffer.Clear();
        _textBuffer.Append(content);
        _cursorIndex = _textBuffer.Length;
        _statusMessage = $"Carregado: {System.IO.Path.GetFileName(_filePath)}";
    }

    public void SaveFile()
    {
        _vfs.WriteFile(_filePath, _textBuffer.ToString());
        _statusMessage = $"Salvo: {System.IO.Path.GetFileName(_filePath)} ({_textBuffer.Length} bytes)";
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Fundo cinza Win95
        display.DrawFilledRectangle(cx, cy, cw, ch, Theme.Surface);

        // Barra de Ações / Botões
        display.DrawButton(cx + 4, cy + 3, 50, 20, "Novo", false);
        display.DrawButton(cx + 58, cy + 3, 55, 20, "Salvar", false);
        display.DrawButton(cx + 117, cy + 3, 55, 20, "Abrir", false);

        // Caminho do arquivo atual
        display.DrawString(cx + 180, cy + 7, System.IO.Path.GetFileName(_filePath), Color.DarkBlue);

        // Área de edição branca chanfrada afundada (Sunken)
        int editY = cy + 27;
        int editH = ch - 48;
        display.DrawFilledRectangle(cx + 4, editY, cw - 8, editH, Color.White);
        display.Draw3DBevel(cx + 4, editY, cw - 8, editH, true);

        // Renderiza o texto com quebra de linha visual
        string text = _textBuffer.ToString();
        int charW = CosmosNativeFont.Width;
        int charH = CosmosNativeFont.Height;
        int maxCharsPerLine = (cw - 20) / charW;
        int tx = cx + 8;
        int ty = editY + 6;

        int curLineCol = 0;
        int cursorPixelX = tx;
        int cursorPixelY = ty;

        for (int i = 0; i <= text.Length; i++)
        {
            if (i == _cursorIndex)
            {
                cursorPixelX = tx;
                cursorPixelY = ty;
            }

            if (i == text.Length) break;

            char c = text[i];
            if (c == '\n')
            {
                tx = cx + 8;
                ty += charH + 2;
                curLineCol = 0;
                continue;
            }
            if (c == '\r') continue;

            if (curLineCol >= maxCharsPerLine)
            {
                tx = cx + 8;
                ty += charH + 2;
                curLineCol = 0;
            }

            if (ty + charH < editY + editH - 2)
            {
                display.DrawChar(tx, ty, c, Color.Black);
            }

            tx += charW;
            curLineCol++;
        }

        // Renderização do Cursor Piscante
        _blinkCounter++;
        if (_blinkCounter > 15)
        {
            _cursorVisible = !_cursorVisible;
            _blinkCounter = 0;
        }

        if (IsActive && _cursorVisible && cursorPixelY + charH < editY + editH)
        {
            display.DrawLine(cursorPixelX, cursorPixelY, cursorPixelX, cursorPixelY + charH - 1, Color.Black);
            display.DrawLine(cursorPixelX + 1, cursorPixelY, cursorPixelX + 1, cursorPixelY + charH - 1, Color.Black);
        }

        // Barra de Status na parte inferior
        int statusY = cy + ch - 18;
        display.Draw3DBevel(cx + 4, statusY, cw - 8, 16, true);
        display.DrawString(cx + 8, statusY + 2, _statusMessage, Color.Black);
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;

        // Botão Novo
        if (mouseX >= cx + 4 && mouseX <= cx + 54 && mouseY >= cy + 3 && mouseY <= cy + 23)
        {
            _textBuffer.Clear();
            _cursorIndex = 0;
            _filePath = @"C:\Documents\novo.txt";
            _statusMessage = "Novo documento";
            return;
        }

        // Botão Salvar (Abre Diálogo Salvar com Escolha de Nome)
        if (mouseX >= cx + 58 && mouseX <= cx + 113 && mouseY >= cy + 3 && mouseY <= cy + 23)
        {
            if (_windowManager != null)
            {
                var dlg = new FileDialog(9901, "Salvar Documento", FileDialogMode.Save, System.IO.Path.GetFileName(_filePath), ".txt", _vfs, (chosen) =>
                {
                    _filePath = chosen;
                    SaveFile();
                });
                _windowManager.AddWindow(dlg);
            }
            else
            {
                SaveFile();
            }
            return;
        }

        // Botão Abrir (Abre Diálogo Abrir com Duplo Clique)
        if (mouseX >= cx + 117 && mouseX <= cx + 172 && mouseY >= cy + 3 && mouseY <= cy + 23)
        {
            if (_windowManager != null)
            {
                var dlg = new FileDialog(9902, "Abrir Documento", FileDialogMode.Open, "", ".txt", _vfs, (chosen) =>
                {
                    OpenFile(chosen);
                });
                _windowManager.AddWindow(dlg);
            }
            else
            {
                OpenFile(_filePath);
            }
            return;
        }

        // Clique no corpo do texto para posicionar o cursor no final
        int editY = cy + 27;
        int editH = ClientHeight - 48;
        if (mouseX >= cx + 4 && mouseX <= cx + ClientWidth - 4 && mouseY >= editY && mouseY <= editY + editH)
        {
            _cursorIndex = _textBuffer.Length;
        }
    }

    public override void OnKeyDown(KeyEvent key)
    {
        if (key.Key == Key.Backspace)
        {
            if (_cursorIndex > 0 && _textBuffer.Length > 0)
            {
                _textBuffer.Remove(_cursorIndex - 1, 1);
                _cursorIndex--;
                _statusMessage = "Modificado";
            }
        }
        else if (key.Key == Key.Delete)
        {
            if (_cursorIndex < _textBuffer.Length)
            {
                _textBuffer.Remove(_cursorIndex, 1);
                _statusMessage = "Modificado";
            }
        }
        else if (key.Key == Key.Enter)
        {
            _textBuffer.Insert(_cursorIndex, '\n');
            _cursorIndex++;
            _statusMessage = "Modificado";
        }
        else if (key.Key == Key.LeftArrow)
        {
            if (_cursorIndex > 0) _cursorIndex--;
        }
        else if (key.Key == Key.RightArrow)
        {
            if (_cursorIndex < _textBuffer.Length) _cursorIndex++;
        }
        else if (key.KeyChar != '\0' && !char.IsControl(key.KeyChar))
        {
            _textBuffer.Insert(_cursorIndex, key.KeyChar);
            _cursorIndex++;
            _statusMessage = "Modificado";
        }
    }
}
