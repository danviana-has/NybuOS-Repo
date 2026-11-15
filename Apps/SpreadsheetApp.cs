using System;
using System.IO;
using NybuOS.Core.FileSystem;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Apps;

public class SpreadsheetApp : AppBase
{
    public override string Id => "sheets";
    public override string Title => "Planilha NybuOS";
    public override FastBitmap? Icon16 => AssetManager.IconSheets16;
    public override FastBitmap? Icon24 => AssetManager.IconSheets24;
    public override FastBitmap? Icon48 => AssetManager.IconSheets48;

    private const int MaxCols = 26; // A to Z
    private const int MaxRows = 50; // 1 to 50
    private const int CellWidth = 65;
    private const int CellHeight = 22;
    private const int RowHeaderWidth = 35;

    private readonly string[,] _rawCells = new string[MaxCols, MaxRows];
    private readonly string[,] _displayCells = new string[MaxCols, MaxRows];

    private int _selectedCol = 0;
    private int _selectedRow = 0;

    private int _scrollCol = 0;
    private int _scrollRow = 0;

    private bool _isEditingFormula = false;
    private string _editBuffer = "";

    public override void Initialize(Core.WindowManager.Window window)
    {
        base.Initialize(window);
        ClearGrid();

        // Seed default sample table
        _rawCells[0, 0] = "Item";
        _rawCells[1, 0] = "Qtd";
        _rawCells[2, 0] = "Preço";
        _rawCells[3, 0] = "Total";

        _rawCells[0, 1] = "RAM DDR5";
        _rawCells[1, 1] = "2";
        _rawCells[2, 1] = "450";
        _rawCells[3, 1] = "=B2*C2";

        _rawCells[0, 2] = "NVMe SSD";
        _rawCells[1, 2] = "1";
        _rawCells[2, 2] = "600";
        _rawCells[3, 2] = "=B3*C3";

        _rawCells[0, 3] = "Total Geral";
        _rawCells[3, 3] = "=SOMA(D2:D3)";

        RecalculateAll();
    }

    private void ClearGrid()
    {
        for (int c = 0; c < MaxCols; c++)
        {
            for (int r = 0; r < MaxRows; r++)
            {
                _rawCells[c, r] = "";
                _displayCells[c, r] = "";
            }
        }
    }

    public override void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH)
    {
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, clientH, ColorScheme.WindowBody);

        // 1. Actions Bar (Salvar, Carregar, Limpar)
        int actH = 30;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, actH, ColorScheme.BackgroundLight);
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, clientX, clientY + actH, clientW, ColorScheme.WindowBorder);

        DrawSheetBtn(buffer, screenW, screenH, clientX + 8, clientY + 4, 60, 22, "Salvar");
        DrawSheetBtn(buffer, screenW, screenH, clientX + 74, clientY + 4, 70, 22, "Carregar");
        DrawSheetBtn(buffer, screenW, screenH, clientX + 150, clientY + 4, 60, 22, "Limpar");

        // 2. Formula Bar (Top 32px)
        int fbY = clientY + actH;
        int fbH = 32;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, fbY, clientW, fbH, ColorScheme.BackgroundLight);
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, clientX, fbY + fbH, clientW, ColorScheme.WindowBorder);

        // Cell coordinate box
        string coord = $"{(char)('A' + _selectedCol)}{_selectedRow + 1}";
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX + 8, fbY + 4, 46, 24, ColorScheme.WindowBody);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, clientX + 8, fbY + 4, 46, 24, ColorScheme.WindowBorder);
        FontRenderer.DrawText(buffer, screenW, screenH, coord, clientX + 14, fbY + 8, ColorScheme.Accent, bold: true, size: 12);

        // fx label
        FontRenderer.DrawText(buffer, screenW, screenH, "fx", clientX + 62, fbY + 8, ColorScheme.TextSecondary, bold: true, size: 12);

        // Formula text box
        int boxX = clientX + 84;
        int boxW = clientW - 96;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, boxX, fbY + 4, boxW, 24, ColorScheme.WindowBody);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, boxX, fbY + 4, boxW, 24, _isEditingFormula ? ColorScheme.Accent : ColorScheme.WindowBorder);

        string fText = _isEditingFormula ? _editBuffer : (_rawCells[_selectedCol, _selectedRow] ?? "");
        FontRenderer.DrawText(buffer, screenW, screenH, fText, boxX + 6, fbY + 8, ColorScheme.TextPrimary, bold: false, size: 12);

        // 3. Grid Header Row (Column Letters A-Z)
        int gridY = fbY + fbH;
        int headerH = 24;

        // Top-left corner box
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, gridY, RowHeaderWidth, headerH, ColorScheme.ControlBackground);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, clientX, gridY, RowHeaderWidth, headerH, ColorScheme.WindowBorder);

        int visibleCols = (clientW - RowHeaderWidth) / CellWidth;
        int visibleRows = (clientH - (gridY - clientY) - headerH) / CellHeight;

        for (int c = 0; c < visibleCols; c++)
        {
            int colIdx = _scrollCol + c;
            if (colIdx >= MaxCols) break;

            int cx = clientX + RowHeaderWidth + (c * CellWidth);
            char colLetter = (char)('A' + colIdx);

            FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, cx, gridY, CellWidth, headerH, ColorScheme.ControlBackground);
            FastRenderer.DrawRectangle(buffer, screenW, screenH, cx, gridY, CellWidth, headerH, ColorScheme.WindowBorder);
            FontRenderer.DrawText(buffer, screenW, screenH, colLetter.ToString(), cx + (CellWidth / 2) - 4, gridY + 5, ColorScheme.TextSecondary, bold: true, size: 12);
        }

        // 4. Grid Cells & Row Headers
        int rowStartY = gridY + headerH;

        for (int r = 0; r < visibleRows; r++)
        {
            int rowIdx = _scrollRow + r;
            if (rowIdx >= MaxRows) break;

            int cy = rowStartY + (r * CellHeight);

            // Row Header (1, 2, 3...)
            FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, cy, RowHeaderWidth, CellHeight, ColorScheme.ControlBackground);
            FastRenderer.DrawRectangle(buffer, screenW, screenH, clientX, cy, RowHeaderWidth, CellHeight, ColorScheme.WindowBorder);
            string rowNumStr = (rowIdx + 1).ToString();
            int rw = FontRenderer.MeasureText(rowNumStr, bold: true, size: 12);
            FontRenderer.DrawText(buffer, screenW, screenH, rowNumStr, clientX + (RowHeaderWidth - rw) / 2, cy + 4, ColorScheme.TextSecondary, bold: true, size: 12);

            // Cells in this row
            for (int c = 0; c < visibleCols; c++)
            {
                int colIdx = _scrollCol + c;
                if (colIdx >= MaxCols) break;

                int cx = clientX + RowHeaderWidth + (c * CellWidth);

                bool isSel = (colIdx == _selectedCol && rowIdx == _selectedRow);

                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, cx, cy, CellWidth, CellHeight, ColorScheme.WindowBody);
                FastRenderer.DrawRectangle(buffer, screenW, screenH, cx, cy, CellWidth, CellHeight, ColorScheme.ControlBackground);

                if (isSel)
                {
                    // Active Cell Selection Border
                    FastRenderer.DrawRectangle(buffer, screenW, screenH, cx, cy, CellWidth, CellHeight, ColorScheme.Accent);
                    FastRenderer.DrawRectangle(buffer, screenW, screenH, cx + 1, cy + 1, CellWidth - 2, CellHeight - 2, ColorScheme.Accent);
                }

                // Display Value
                string val = _displayCells[colIdx, rowIdx];
                if (!string.IsNullOrEmpty(val))
                {
                    if (val.Length > 8) val = val.Substring(0, 7) + "..";
                    FontRenderer.DrawText(buffer, screenW, screenH, val, cx + 5, cy + 4, ColorScheme.TextPrimary, bold: false, size: 12);
                }
            }
        }
    }

    private void DrawSheetBtn(uint[] buffer, int screenW, int screenH, int x, int y, int w, int h, string text)
    {
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, x, y, w, h, ColorScheme.ControlBackground);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, x, y, w, h, ColorScheme.WindowBorder);
        int tw = FontRenderer.MeasureText(text, bold: false, size: 12);
        FontRenderer.DrawText(buffer, screenW, screenH, text, x + (w - tw) / 2, y + 4, ColorScheme.TextPrimary, bold: false, size: 12);
    }

    public override void HandleInput(int clientX, int clientY, int clientW, int clientH)
    {
        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        if (InputState.LeftClicked)
        {
            // Actions (Salvar, Carregar, Limpar)
            if (my >= clientY + 4 && my < clientY + 26)
            {
                if (mx >= clientX + 8 && mx < clientX + 68) { SaveToCsv(); return; }
                if (mx >= clientX + 74 && mx < clientX + 144) { LoadFromCsv(); return; }
                if (mx >= clientX + 150 && mx < clientX + 210) { ClearGrid(); RecalculateAll(); return; }
            }

            // Formula box click
            int fbY = clientY + 30;
            int boxX = clientX + 84;
            int boxW = clientW - 96;
            if (mx >= boxX && mx < boxX + boxW && my >= fbY + 4 && my < fbY + 28)
            {
                _isEditingFormula = true;
                _editBuffer = _rawCells[_selectedCol, _selectedRow] ?? "";
                return;
            }

            // Cell click
            int gridY = fbY + 32;
            int rowStartY = gridY + 24;
            if (mx >= clientX + RowHeaderWidth && my >= rowStartY)
            {
                int c = _scrollCol + (mx - (clientX + RowHeaderWidth)) / CellWidth;
                int r = _scrollRow + (my - rowStartY) / CellHeight;

                if (c >= 0 && c < MaxCols && r >= 0 && r < MaxRows)
                {
                    _selectedCol = c;
                    _selectedRow = r;
                    _isEditingFormula = false;
                    return;
                }
            }

            _isEditingFormula = false;
        }

        // Keyboard input for editing cells
        while (InputState.TryReadKey(out var keyEvent))
        {
            if (_isEditingFormula)
            {
                if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.Enter)
                {
                    _rawCells[_selectedCol, _selectedRow] = _editBuffer;
                    _isEditingFormula = false;
                    RecalculateAll();
                }
                else if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.Backspace)
                {
                    if (_editBuffer.Length > 0)
                    {
                        _editBuffer = _editBuffer.Substring(0, _editBuffer.Length - 1);
                    }
                }
                else if (keyEvent.KeyChar >= ' ' && keyEvent.KeyChar <= '~')
                {
                    if (_editBuffer.Length < 30) _editBuffer += keyEvent.KeyChar;
                }
            }
            else
            {
                // Navigation
                if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.UpArrow && _selectedRow > 0) _selectedRow--;
                else if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.DownArrow && _selectedRow < MaxRows - 1) _selectedRow++;
                else if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.LeftArrow && _selectedCol > 0) _selectedCol--;
                else if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.RightArrow && _selectedCol < MaxCols - 1) _selectedCol++;
                else if (keyEvent.KeyChar >= ' ' && keyEvent.KeyChar <= '~')
                {
                    _isEditingFormula = true;
                    _editBuffer = keyEvent.KeyChar.ToString();
                }
            }
        }
    }

    private void RecalculateAll()
    {
        for (int r = 0; r < MaxRows; r++)
        {
            for (int c = 0; c < MaxCols; c++)
            {
                string raw = _rawCells[c, r]?.Trim() ?? "";
                if (raw.StartsWith('='))
                {
                    _displayCells[c, r] = EvaluateFormula(raw.Substring(1));
                }
                else
                {
                    _displayCells[c, r] = raw;
                }
            }
        }
    }

    private string EvaluateFormula(string expr)
    {
        try
        {
            expr = expr.ToUpperInvariant().Trim();

            // SOMA or SUM
            if (expr.StartsWith("SOMA(") || expr.StartsWith("SUM("))
            {
                int start = expr.IndexOf('(') + 1;
                int end = expr.IndexOf(')');
                if (start > 0 && end > start)
                {
                    string range = expr.Substring(start, end - start);
                    return EvaluateRangeSum(range).ToString();
                }
            }

            // MEDIA or AVERAGE
            if (expr.StartsWith("MEDIA(") || expr.StartsWith("AVERAGE("))
            {
                int start = expr.IndexOf('(') + 1;
                int end = expr.IndexOf(')');
                if (start > 0 && end > start)
                {
                    string range = expr.Substring(start, end - start);
                    return EvaluateRangeAverage(range).ToString();
                }
            }

            // Basic cell operations: A1+B1, A1*2, etc.
            char[] ops = { '+', '-', '*', '/' };
            int opIdx = -1;
            char foundOp = ' ';
            for (int i = 0; i < ops.Length; i++)
            {
                opIdx = expr.IndexOf(ops[i]);
                if (opIdx > 0) { foundOp = ops[i]; break; }
            }

            if (opIdx > 0)
            {
                string leftStr = expr.Substring(0, opIdx).Trim();
                string rightStr = expr.Substring(opIdx + 1).Trim();

                double v1 = GetOperandValue(leftStr);
                double v2 = GetOperandValue(rightStr);

                double res = foundOp switch
                {
                    '+' => v1 + v2,
                    '-' => v1 - v2,
                    '*' => v1 * v2,
                    '/' => v2 != 0 ? v1 / v2 : 0,
                    _ => 0
                };
                return res.ToString();
            }

            return GetOperandValue(expr).ToString();
        }
        catch
        {
            return "#ERRO";
        }
    }

    private double GetOperandValue(string token)
    {
        if (double.TryParse(token, out var val)) return val;
        // Parse cell coordinate like A1, B3
        if (token.Length >= 2 && char.IsLetter(token[0]) && int.TryParse(token.Substring(1), out var row))
        {
            int c = char.ToUpperInvariant(token[0]) - 'A';
            int r = row - 1;
            if (c >= 0 && c < MaxCols && r >= 0 && r < MaxRows)
            {
                if (double.TryParse(_displayCells[c, r], out var cVal)) return cVal;
            }
        }
        return 0;
    }

    private double EvaluateRangeSum(string range)
    {
        var parts = range.Split(':');
        if (parts.Length != 2) return 0;

        int c1 = parts[0][0] - 'A';
        int r1 = int.Parse(parts[0].Substring(1)) - 1;
        int c2 = parts[1][0] - 'A';
        int r2 = int.Parse(parts[1].Substring(1)) - 1;

        double sum = 0;
        for (int c = Math.Min(c1, c2); c <= Math.Max(c1, c2); c++)
        {
            for (int r = Math.Min(r1, r2); r <= Math.Max(r1, r2); r++)
            {
                if (double.TryParse(_displayCells[c, r], out var v)) sum += v;
            }
        }
        return sum;
    }

    private double EvaluateRangeAverage(string range)
    {
        var parts = range.Split(':');
        if (parts.Length != 2) return 0;

        int c1 = parts[0][0] - 'A';
        int r1 = int.Parse(parts[0].Substring(1)) - 1;
        int c2 = parts[1][0] - 'A';
        int r2 = int.Parse(parts[1].Substring(1)) - 1;

        double sum = 0;
        int count = 0;
        for (int c = Math.Min(c1, c2); c <= Math.Max(c1, c2); c++)
        {
            for (int r = Math.Min(r1, r2); r <= Math.Max(r1, r2); r++)
            {
                if (double.TryParse(_displayCells[c, r], out var v))
                {
                    sum += v;
                    count++;
                }
            }
        }
        return count > 0 ? sum / count : 0;
    }

    private void SaveToCsv()
    {
        var sb = new System.Text.StringBuilder();
        for (int r = 0; r < 20; r++)
        {
            var rowVals = new string[10];
            for (int c = 0; c < 10; c++) rowVals[c] = _rawCells[c, r] ?? "";
            sb.AppendLine(string.Join(';', rowVals));
        }
        string path = Path.Combine(FileService.RootPath, "planilhas", "tabela.csv");
        FileService.WriteAllText(path, sb.ToString());
    }

    private void LoadFromCsv()
    {
        string path = Path.Combine(FileService.RootPath, "planilhas", "tabela.csv");
        if (FileService.FileExists(path))
        {
            ClearGrid();
            string content = FileService.ReadAllText(path);
            var lines = content.Split('\n');
            for (int r = 0; r < Math.Min(lines.Length, MaxRows); r++)
            {
                var cols = lines[r].TrimEnd('\r').Split(';');
                for (int c = 0; c < Math.Min(cols.Length, MaxCols); c++)
                {
                    _rawCells[c, r] = cols[c];
                }
            }
            RecalculateAll();
        }
    }
}
