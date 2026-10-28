using System;
using System.Drawing;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;
using NybuOS.FileSystem;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// PaintPad: Editor gráfico completo estilo MS Paint do Windows 95.
/// Permite desenho livre com mouse, seleção de cores da paleta, borracha, espessura
/// e exportação direta para arquivos de imagem Bitmap (.BMP) no disco FAT32.
/// </summary>
public class PaintPadApp : Window
{
    private readonly VirtualFileSystem _vfs;
    public const int CanvasW = 260;
    public const int CanvasH = 160;

    public enum DrawTool
    {
        Brush,
        Line,
        Eraser
    }

    private readonly Color[,] _canvas = new Color[CanvasW, CanvasH];
    private Color _selectedColor = Color.Black;
    private DrawTool _currentTool = DrawTool.Brush;
    private int _brushSize = 2; // 1, 2, 4
    private bool _isDrawing = false;
    private int _lastLocalX = -1;
    private int _lastLocalY = -1;
    private int _lineStartX = -1;
    private int _lineStartY = -1;
    private int _linePreviewX = -1;
    private int _linePreviewY = -1;
    private string _status = "Pronto";
    private int _saveCounter = 1;

    // Cores da paleta estilo Windows 95 Paint
    private readonly Color[] _palette = new Color[]
    {
        Color.Black,
        Color.White,
        Color.FromArgb(128, 128, 128), // Cinza escuro
        Color.FromArgb(192, 192, 192), // Cinza claro
        Color.FromArgb(128, 0, 0),     // Vinho
        Color.Red,
        Color.FromArgb(255, 128, 0),   // Laranja
        Color.Yellow,
        Color.FromArgb(0, 128, 0),     // Verde floresta
        Color.FromArgb(0, 255, 0),     // Verde claro
        Color.FromArgb(0, 255, 255),   // Ciano
        Color.Blue,
        Color.FromArgb(0, 0, 128),     // Azul marinho
        Color.FromArgb(128, 0, 128),   // Roxo
        Color.FromArgb(255, 128, 192), // Rosa
        Color.FromArgb(128, 64, 0)     // Marrom
    };

    public PaintPadApp(int id, int x, int y, VirtualFileSystem vfs)
        : base(id, "PaintPad - NybuOS", x, y, 370, 280)
    {
        Icon = IconResources.PaintPadIcon ?? IconResources.PaintPad16;
        _vfs = vfs;
        ClearCanvas();
    }

    private void ClearCanvas()
    {
        for (int py = 0; py < CanvasH; py++)
        {
            for (int px = 0; px < CanvasW; px++)
            {
                _canvas[px, py] = Color.White;
            }
        }
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Fundo da área de cliente
        display.DrawFilledRectangle(cx, cy, cw, ch, Theme.Surface);

        // Barra de Ferramentas Superior com suporte a Reta
        display.DrawButton(cx + 4, cy + 3, 44, 20, "Pincel", _currentTool == DrawTool.Brush);
        display.DrawButton(cx + 50, cy + 3, 42, 20, "Reta", _currentTool == DrawTool.Line);
        display.DrawButton(cx + 94, cy + 3, 50, 20, "Borracha", _currentTool == DrawTool.Eraser);
        display.DrawButton(cx + 146, cy + 3, 34, 20, "1px", _brushSize == 1);
        display.DrawButton(cx + 182, cy + 3, 34, 20, "2px", _brushSize == 2);
        display.DrawButton(cx + 218, cy + 3, 34, 20, "4px", _brushSize == 4);
        display.DrawButton(cx + 254, cy + 3, 48, 20, "Limpar", false);
        display.DrawButton(cx + 304, cy + 3, 52, 20, "Salvar", false);

        // Tela de Pintura (Canvas com chanfrado 3D Sunken)
        int canvasX = cx + 4;
        int canvasY = cy + 26;

        // Borda chanfrada afundada ao redor do papel
        display.Draw3DBevel(canvasX - 2, canvasY - 2, CanvasW + 4, CanvasH + 4, true);

        // Renderiza os pixels da pintura
        for (int py = 0; py < CanvasH; py++)
        {
            for (int px = 0; px < CanvasW; px++)
            {
                Color pixelColor = _canvas[px, py];
                display.DrawPixel(canvasX + px, canvasY + py, pixelColor);
            }
        }

        // Preview de linha reta enquanto arrasta
        if (_isDrawing && _currentTool == DrawTool.Line && _lineStartX >= 0 && _linePreviewX >= 0)
        {
            Color pColor = _selectedColor;
            DrawPreviewLine(display, canvasX + _lineStartX, canvasY + _lineStartY, canvasX + _linePreviewX, canvasY + _linePreviewY, pColor);
        }

        // Paleta de Cores Inferior
        int paletteY = canvasY + CanvasH + 6;
        int boxW = 16;
        int boxH = 16;

        // Caixa da cor selecionada atual
        display.DrawFilledRectangle(cx + 4, paletteY, 28, 28, Theme.Surface);
        display.Draw3DBevel(cx + 4, paletteY, 28, 28, false);
        display.DrawFilledRectangle(cx + 8, paletteY + 4, 20, 20, _currentTool == DrawTool.Eraser ? Color.White : _selectedColor);
        display.Draw3DBevel(cx + 8, paletteY + 4, 20, 20, true);

        // Amostras de cores em duas linhas de 8 cores
        for (int i = 0; i < _palette.Length; i++)
        {
            int col = i % 8;
            int row = i / 8;
            int bx = cx + 38 + (col * (boxW + 2));
            int by = paletteY + (row * (boxH + 2));

            display.DrawFilledRectangle(bx, by, boxW, boxH, _palette[i]);
            display.Draw3DBevel(bx, by, boxW, boxH, true);
        }

        // Barra de Status Inferior (Sunken)
        int statusY = ch - 18 + cy;
        display.DrawFilledRectangle(cx + 2, statusY, cw - 4, 16, Theme.Surface);
        display.Draw3DBevel(cx + 2, statusY, cw - 4, 16, true);
        display.DrawString(cx + 6, statusY + 2, _status, Theme.TextPrimary);
    }

    private void DrawPreviewLine(DisplayDriver display, int x0, int y0, int x1, int y1, Color c)
    {
        display.DrawLine(x0, y0, x1, y1, c);
    }

    private void PutBrush(int x, int y, Color color)
    {
        int half = _brushSize / 2;
        for (int dy = -half; dy <= half; dy++)
        {
            for (int dx = -half; dx <= half; dx++)
            {
                int tx = x + dx;
                int ty = y + dy;
                if (tx >= 0 && tx < CanvasW && ty >= 0 && ty < CanvasH)
                {
                    _canvas[tx, ty] = color;
                }
            }
        }
    }

    /// <summary>
    /// Algoritmo de Bresenham contínuo para evitar quadradinhos e desenhar linhas sólidas sem falhas.
    /// </summary>
    private void DrawLineBresenham(int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            PutBrush(x0, y0, color);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;

        // Botões da Barra de Ferramentas
        if (mouseY >= cy + 3 && mouseY <= cy + 23)
        {
            if (mouseX >= cx + 4 && mouseX <= cx + 48) { _currentTool = DrawTool.Brush; _status = "Ferramenta: Pincel"; return; }
            if (mouseX >= cx + 50 && mouseX <= cx + 92) { _currentTool = DrawTool.Line; _status = "Ferramenta: Reta (Clique e arraste)"; return; }
            if (mouseX >= cx + 94 && mouseX <= cx + 144) { _currentTool = DrawTool.Eraser; _status = "Ferramenta: Borracha"; return; }
            if (mouseX >= cx + 146 && mouseX <= cx + 180) { _brushSize = 1; _status = "Espessura: 1px"; return; }
            if (mouseX >= cx + 182 && mouseX <= cx + 216) { _brushSize = 2; _status = "Espessura: 2px"; return; }
            if (mouseX >= cx + 218 && mouseX <= cx + 252) { _brushSize = 4; _status = "Espessura: 4px"; return; }
            if (mouseX >= cx + 254 && mouseX <= cx + 302) { ClearCanvas(); _status = "Tela limpa"; return; }
            if (mouseX >= cx + 304 && mouseX <= cx + 356) { SaveBitmap(); return; }
        }

        // Clique na Tela de Desenho
        int canvasX = cx + 4;
        int canvasY = cy + 26;
        if (mouseX >= canvasX && mouseX < canvasX + CanvasW && mouseY >= canvasY && mouseY < canvasY + CanvasH)
        {
            _isDrawing = true;
            int localX = mouseX - canvasX;
            int localY = mouseY - canvasY;

            if (_currentTool == DrawTool.Line)
            {
                _lineStartX = localX;
                _lineStartY = localY;
                _linePreviewX = localX;
                _linePreviewY = localY;
            }
            else
            {
                Color c = _currentTool == DrawTool.Eraser ? Color.White : _selectedColor;
                PutBrush(localX, localY, c);
                _lastLocalX = localX;
                _lastLocalY = localY;
            }
            return;
        }

        // Clique na Paleta de Cores
        int paletteY = canvasY + CanvasH + 6;
        int boxW = 16;
        int boxH = 16;
        for (int i = 0; i < _palette.Length; i++)
        {
            int col = i % 8;
            int row = i / 8;
            int bx = cx + 38 + (col * (boxW + 2));
            int by = paletteY + (row * (boxH + 2));

            if (mouseX >= bx && mouseX <= bx + boxW && mouseY >= by && mouseY <= by + boxH)
            {
                _selectedColor = _palette[i];
                if (_currentTool == DrawTool.Eraser) _currentTool = DrawTool.Brush;
                _status = "Cor selecionada";
                return;
            }
        }
    }

    public override void OnMouseMove(int mouseX, int mouseY)
    {
        if (!_isDrawing) return;

        int canvasX = ClientX + 4;
        int canvasY = ClientY + 26;

        int localX = Math.Clamp(mouseX - canvasX, 0, CanvasW - 1);
        int localY = Math.Clamp(mouseY - canvasY, 0, CanvasH - 1);

        if (_currentTool == DrawTool.Line)
        {
            _linePreviewX = localX;
            _linePreviewY = localY;
        }
        else
        {
            Color c = _currentTool == DrawTool.Eraser ? Color.White : _selectedColor;
            if (_lastLocalX >= 0 && _lastLocalY >= 0)
            {
                DrawLineBresenham(_lastLocalX, _lastLocalY, localX, localY, c);
            }
            else
            {
                PutBrush(localX, localY, c);
            }
            _lastLocalX = localX;
            _lastLocalY = localY;
        }
    }

    public override void OnMouseUp(int mouseX, int mouseY)
    {
        if (_isDrawing && _currentTool == DrawTool.Line && _lineStartX >= 0)
        {
            int canvasX = ClientX + 4;
            int canvasY = ClientY + 26;
            int localX = Math.Clamp(mouseX - canvasX, 0, CanvasW - 1);
            int localY = Math.Clamp(mouseY - canvasY, 0, CanvasH - 1);

            Color c = _selectedColor;
            DrawLineBresenham(_lineStartX, _lineStartY, localX, localY, c);
            _status = "Reta desenhada";
        }

        _isDrawing = false;
        _lastLocalX = -1;
        _lastLocalY = -1;
        _lineStartX = -1;
        _lineStartY = -1;
        _linePreviewX = -1;
        _linePreviewY = -1;
    }

    private void SaveBitmap()
    {
        try
        {
            byte[] bmpBytes = BmpEncoder.Encode24bpp(_canvas, CanvasW, CanvasH);
            string fileName = $@"C:\Documents\desenho_{_saveCounter++}.bmp";
            _vfs.WriteFile(fileName, bmpBytes);
            _status = $"Salvo: {fileName}";
        }
        catch (Exception ex)
        {
            _status = $"Erro ao salvar: {ex.Message}";
        }
    }
}
