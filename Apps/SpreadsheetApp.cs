using System;
using System.Drawing;
using System.Text;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;
using NybuOS.FileSystem;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// Aplicativo de Planilha com grid de células alfanuméricas e persistência CSV em disco.
/// </summary>
public class SpreadsheetApp : Window
{
    private const int Rows = 8;
    private const int Cols = 4;
    private readonly string[,] _cells = new string[Rows, Cols];

    private int _selectedRow = 0;
    private int _selectedCol = 0;
    private readonly VirtualFileSystem _vfs;
    private readonly WindowManager? _windowManager;
    private string _filePath = @"C:\Documents\planilha.csv";
    private string _status = "Planilha Pronta";

    public SpreadsheetApp(int id, int x, int y, VirtualFileSystem vfs, WindowManager? wm = null)
        : base(id, "Planilha NybuCalc - NybuOS", x, y, 380, 260)
    {
        Icon = IconResources.SpreadsheetIcon ?? IconResources.Spreadsheet16;
        _vfs = vfs;
        _windowManager = wm;
        InitializeDefaultData();
    }

    private void InitializeDefaultData()
    {
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                _cells[r, c] = "";

        // Cabeçalhos padrão
        _cells[0, 0] = "Item";
        _cells[0, 1] = "Qtd";
        _cells[0, 2] = "Preco";
        _cells[0, 3] = "Total";

        _cells[1, 0] = "Teclado";
        _cells[1, 1] = "2";
        _cells[1, 2] = "150";
        _cells[1, 3] = "300";

        _cells[2, 0] = "Mouse";
        _cells[2, 1] = "3";
        _cells[2, 2] = "80";
        _cells[2, 3] = "240";

        _cells[3, 0] = "Monitor";
        _cells[3, 1] = "1";
        _cells[3, 2] = "600";
        _cells[3, 3] = "600";
    }

    public void SaveToDisk(string? path = null)
    {
        if (!string.IsNullOrEmpty(path)) _filePath = path;

        var sb = new StringBuilder();
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                sb.Append(_cells[r, c]);
                if (c < Cols - 1) sb.Append(",");
            }
            sb.AppendLine();
        }

        _vfs.WriteFile(_filePath, sb.ToString());
        _status = "Planilha salva em " + System.IO.Path.GetFileName(_filePath);
    }

    public void LoadFromDisk(string? path = null)
    {
        if (!string.IsNullOrEmpty(path)) _filePath = path;

        string content = _vfs.ReadFile(_filePath);
        if (string.IsNullOrEmpty(content)) return;

        string[] lines = content.Split('\n');
        for (int r = 0; r < Math.Min(lines.Length, Rows); r++)
        {
            string line = lines[r].Trim('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split(',');
            for (int c = 0; c < Math.Min(parts.Length, Cols); c++)
            {
                _cells[r, c] = parts[c];
            }
        }
        _status = "Carregada: " + System.IO.Path.GetFileName(_filePath);
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Fundo cinza Win95 / Modern Surface
        display.DrawFilledRectangle(cx, cy, cw, ch, Theme.Surface);

        // Botões de comando: Novo, Salvar, Carregar, Limpar
        display.DrawButton(cx + 4, cy + 3, 46, 20, "Novo", false);
        display.DrawButton(cx + 54, cy + 3, 52, 20, "Salvar", false);
        display.DrawButton(cx + 110, cy + 3, 64, 20, "Carregar", false);
        display.DrawButton(cx + 178, cy + 3, 54, 20, "Limpar", false);

        display.DrawString(cx + 240, cy + 7, System.IO.Path.GetFileName(_filePath), Theme.AccentColor);

        // Barra de Fórmula/Edição (Sunken)
        char colLetter = (char)('A' + _selectedCol);
        string cellRef = $"[{colLetter}{_selectedRow + 1}] ";
        int formulaY = cy + 27;

        display.DrawFilledRectangle(cx + 4, formulaY, cw - 8, 18, Color.White);
        display.Draw3DBevel(cx + 4, formulaY, cw - 8, 18, true);

        string currentVal = _cells[_selectedRow, _selectedCol] ?? "";
        display.DrawString(cx + 8, formulaY + 3, cellRef + currentVal, Color.Black);

        // Tabela / Grid
        int gridX = cx + 4;
        int gridY = formulaY + 22;
        int cellW = (cw - 32) / Cols;
        int cellH = 18;

        // Cabeçalho das Colunas (A, B, C, D)
        display.DrawFilledRectangle(gridX, gridY, 24, cellH, Theme.Surface);
        display.Draw3DBevel(gridX, gridY, 24, cellH, false);

        for (int c = 0; c < Cols; c++)
        {
            int colX = gridX + 24 + (c * cellW);
            display.DrawFilledRectangle(colX, gridY, cellW, cellH, Theme.Surface);
            display.Draw3DBevel(colX, gridY, cellW, cellH, false);
            char letter = (char)('A' + c);
            display.DrawString(colX + (cellW / 2) - 4, gridY + 3, letter.ToString(), Color.Black);
        }

        // Linhas de Células
        for (int r = 0; r < Rows; r++)
        {
            int rowY = gridY + cellH + (r * cellH);

            // Cabeçalho da Linha (1, 2, 3...)
            display.DrawFilledRectangle(gridX, rowY, 24, cellH, Theme.Surface);
            display.Draw3DBevel(gridX, rowY, 24, cellH, false);
            display.DrawString(gridX + 8, rowY + 3, (r + 1).ToString(), Color.Black);

            for (int c = 0; c < Cols; c++)
            {
                int colX = gridX + 24 + (c * cellW);
                bool isSelected = (r == _selectedRow && c == _selectedCol);

                Color bg = isSelected ? Theme.SelectionBg : Color.White;
                Color fg = isSelected ? Theme.TextSelected : Color.Black;

                display.DrawFilledRectangle(colX, rowY, cellW, cellH, bg);
                display.DrawRectangle(colX, rowY, cellW, cellH, Theme.ShadowBorder);

                string val = _cells[r, c] ?? "";
                if (val.Length > (cellW - 8) / Font8x12.Width && val.Length > 2)
                {
                    val = val.Substring(0, (cellW - 8) / Font8x12.Width);
                }

                display.DrawString(colX + 4, rowY + 3, val, fg);
            }
        }

        // Status na parte inferior
        int statusY = cy + ch - 16;
        display.DrawString(cx + 8, statusY, _status, Color.DarkSlateGray);
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;

        // Botão Novo
        if (mouseX >= cx + 4 && mouseX <= cx + 50 && mouseY >= cy + 3 && mouseY <= cy + 23)
        {
            InitializeDefaultData();
            _filePath = @"C:\Documents\nova_planilha.csv";
            _status = "Nova planilha criada";
            return;
        }

        // Botão Salvar (Abre Diálogo Salvar com Escolha de Nome)
        if (mouseX >= cx + 54 && mouseX <= cx + 106 && mouseY >= cy + 3 && mouseY <= cy + 23)
        {
            if (_windowManager != null)
            {
                var dlg = new FileDialog(9903, "Salvar Planilha", FileDialogMode.Save, System.IO.Path.GetFileName(_filePath), ".csv", _vfs, (chosen) =>
                {
                    SaveToDisk(chosen);
                });
                _windowManager.AddWindow(dlg);
            }
            else
            {
                SaveToDisk();
            }
            return;
        }

        // Botão Carregar (Abre Diálogo Carregar com Duplo Clique)
        if (mouseX >= cx + 110 && mouseX <= cx + 174 && mouseY >= cy + 3 && mouseY <= cy + 23)
        {
            if (_windowManager != null)
            {
                var dlg = new FileDialog(9904, "Carregar Planilha", FileDialogMode.Open, "", ".csv", _vfs, (chosen) =>
                {
                    LoadFromDisk(chosen);
                });
                _windowManager.AddWindow(dlg);
            }
            else
            {
                LoadFromDisk();
            }
            return;
        }

        // Botão Limpar
        if (mouseX >= cx + 178 && mouseX <= cx + 232 && mouseY >= cy + 3 && mouseY <= cy + 23)
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    _cells[r, c] = "";
            _status = "Planilha limpa";
            return;
        }

        // Clique no Grid
        int gridX = cx + 4;
        int formulaY = cy + 27;
        int gridY = formulaY + 22;
        int cellW = (ClientWidth - 32) / Cols;
        int cellH = 18;

        if (mouseX >= gridX + 24 && mouseX <= gridX + 24 + (Cols * cellW) &&
            mouseY >= gridY + cellH && mouseY <= gridY + cellH + (Rows * cellH))
        {
            int c = (mouseX - (gridX + 24)) / cellW;
            int r = (mouseY - (gridY + cellH)) / cellH;

            if (r >= 0 && r < Rows && c >= 0 && c < Cols)
            {
                _selectedRow = r;
                _selectedCol = c;
            }
        }
    }

    public override void OnKeyDown(KeyEvent key)
    {
        string current = _cells[_selectedRow, _selectedCol] ?? "";

        if (key.Key == Key.Backspace)
        {
            if (current.Length > 0)
            {
                _cells[_selectedRow, _selectedCol] = current.Substring(0, current.Length - 1);
            }
        }
        else if (key.Key == Key.UpArrow)
        {
            if (_selectedRow > 0) _selectedRow--;
        }
        else if (key.Key == Key.DownArrow || key.Key == Key.Enter)
        {
            if (_selectedRow < Rows - 1) _selectedRow++;
        }
        else if (key.Key == Key.LeftArrow)
        {
            if (_selectedCol > 0) _selectedCol--;
        }
        else if (key.Key == Key.RightArrow)
        {
            if (_selectedCol < Cols - 1) _selectedCol++;
        }
        else if (key.KeyChar != '\0' && !char.IsControl(key.KeyChar))
        {
            _cells[_selectedRow, _selectedCol] = current + key.KeyChar;
        }
    }
}
