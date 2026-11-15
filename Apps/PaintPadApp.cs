using System;
using System.IO;
using NybuOS.Core.FileSystem;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Apps;

public class PaintPadApp : AppBase
{
    public override string Id => "paint";
    public override string Title => "PaintPad";
    public override FastBitmap? Icon16 => AssetManager.IconPaint16;
    public override FastBitmap? Icon24 => AssetManager.IconPaint24;
    public override FastBitmap? Icon48 => AssetManager.IconPaint48;

    private int _canvasWidth = 480;
    private int _canvasHeight = 320;
    private uint[] _canvasPixels = Array.Empty<uint>();

    private enum ToolMode { Brush, Eraser, Line, Rectangle, Fill }
    private ToolMode _currentTool = ToolMode.Brush;

    private int _brushSize = 2;
    private uint _currentColor = ColorScheme.Black;

    private int _lastDrawX = -1;
    private int _lastDrawY = -1;

    private readonly uint[] _palette = {
        0xFF000000, 0xFF7F7F7F, 0xFF880015, 0xFFED1C24,
        0xFFFF7F27, 0xFFFFF200, 0xFF22B14C, 0xFF00A2E8,
        0xFF3F48CC, 0xFFA349A4, 0xFFFFFFFF, 0xFFC3C3C3
    };

    public override void Initialize(Core.WindowManager.Window window)
    {
        base.Initialize(window);
        InitCanvas();
    }

    private void InitCanvas()
    {
        _canvasPixels = new uint[_canvasWidth * _canvasHeight];
        Array.Fill(_canvasPixels, ColorScheme.White);
    }

    public override void OnResize(int newClientW, int newClientH)
    {
        int newW = Math.Max(320, newClientW - 20);
        int newH = Math.Max(200, newClientH - 60);

        if (newW != _canvasWidth || newH != _canvasHeight)
        {
            var oldPixels = _canvasPixels;
            int oldW = _canvasWidth;
            int oldH = _canvasHeight;

            _canvasWidth = newW;
            _canvasHeight = newH;
            _canvasPixels = new uint[newW * newH];
            Array.Fill(_canvasPixels, ColorScheme.White);

            // Copy old contents
            int copyW = Math.Min(oldW, newW);
            int copyH = Math.Min(oldH, newH);
            for (int y = 0; y < copyH; y++)
            {
                Array.Copy(oldPixels, y * oldW, _canvasPixels, y * newW, copyW);
            }
        }
    }

    public override void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH)
    {
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, clientH, ColorScheme.BackgroundLight);

        // 1. Toolbar (Top 46px)
        int tbH = 46;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, tbH, ColorScheme.WindowBody);
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, clientX, clientY + tbH, clientW, ColorScheme.WindowBorder);

        // Tool Buttons: Pincel, Borracha, Linha, Retângulo, Limpar, Salvar
        int curX = clientX + 10;
        int btnY = clientY + 8;
        int btnH = 30;

        DrawToolBtn(buffer, screenW, screenH, curX, btnY, 56, btnH, "Pincel", _currentTool == ToolMode.Brush);
        curX += 62;
        DrawToolBtn(buffer, screenW, screenH, curX, btnY, 62, btnH, "Borracha", _currentTool == ToolMode.Eraser);
        curX += 68;
        DrawToolBtn(buffer, screenW, screenH, curX, btnY, 50, btnH, "Linha", _currentTool == ToolMode.Line);
        curX += 56;
        DrawToolBtn(buffer, screenW, screenH, curX, btnY, 70, btnH, "Retângulo", _currentTool == ToolMode.Rectangle);
        curX += 76;
        DrawToolBtn(buffer, screenW, screenH, curX, btnY, 56, btnH, "Limpar", false);
        curX += 62;
        DrawToolBtn(buffer, screenW, screenH, curX, btnY, 82, btnH, "💾 Salvar", false);

        // Palette Swatches
        int palX = curX + 96;
        int palY = clientY + 8;
        for (int i = 0; i < _palette.Length; i++)
        {
            int px = palX + ((i % 6) * 20);
            int py = palY + ((i / 6) * 15);
            FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, px, py, 16, 12, _palette[i]);
            FastRenderer.DrawRectangle(buffer, screenW, screenH, px, py, 16, 12, ColorScheme.WindowBorder);
            if (_currentColor == _palette[i])
            {
                FastRenderer.DrawRectangle(buffer, screenW, screenH, px - 1, py - 1, 18, 14, ColorScheme.Black);
            }
        }

        // Active Color preview box
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, palX + 130, palY, 30, 30, _currentColor);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, palX + 130, palY, 30, 30, ColorScheme.Black);

        // 2. Canvas Area
        int canvasX = clientX + 10;
        int canvasY = clientY + tbH + 8;

        // Shadow & Border around canvas
        FastRenderer.DrawRectangle(buffer, screenW, screenH, canvasX - 1, canvasY - 1, _canvasWidth + 2, _canvasHeight + 2, ColorScheme.WindowBorder);

        // Blit Canvas Pixels
        for (int cy = 0; cy < _canvasHeight; cy++)
        {
            int destY = canvasY + cy;
            if (destY >= screenH) break;
            int destRowOff = destY * screenW;
            int srcRowOff = cy * _canvasWidth;

            for (int cx = 0; cx < _canvasWidth; cx++)
            {
                int destX = canvasX + cx;
                if (destX >= screenW) break;
                buffer[destRowOff + destX] = _canvasPixels[srcRowOff + cx];
            }
        }
    }

    private void DrawToolBtn(uint[] buffer, int screenW, int screenH, int x, int y, int w, int h, string text, bool selected)
    {
        uint bg = selected ? ColorScheme.ControlPressed : ColorScheme.ControlBackground;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, x, y, w, h, bg);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, x, y, w, h, selected ? ColorScheme.Accent : ColorScheme.WindowBorder);
        int tw = FontRenderer.MeasureText(text, bold: selected, size: 12);
        FontRenderer.DrawText(buffer, screenW, screenH, text, x + (w - tw) / 2, y + 8, ColorScheme.TextPrimary, bold: selected, size: 12);
    }

    public override void HandleInput(int clientX, int clientY, int clientW, int clientH)
    {
        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        // 1. Toolbar clicks
        if (InputState.LeftClicked && my >= clientY && my < clientY + 46)
        {
            int curX = clientX + 10;

            if (mx >= curX && mx < curX + 56) { _currentTool = ToolMode.Brush; return; }
            curX += 62;
            if (mx >= curX && mx < curX + 62) { _currentTool = ToolMode.Eraser; return; }
            curX += 68;
            if (mx >= curX && mx < curX + 50) { _currentTool = ToolMode.Line; return; }
            curX += 56;
            if (mx >= curX && mx < curX + 70) { _currentTool = ToolMode.Rectangle; return; }
            curX += 76;
            if (mx >= curX && mx < curX + 56) { InitCanvas(); return; } // Limpar
            curX += 62;
            if (mx >= curX && mx < curX + 82) { SaveToBmp(); return; } // Salvar

            // Palette click
            int palX = curX + 96;
            int palY = clientY + 8;
            for (int i = 0; i < _palette.Length; i++)
            {
                int px = palX + ((i % 6) * 20);
                int py = palY + ((i / 6) * 15);
                if (mx >= px && mx < px + 16 && my >= py && my < py + 12)
                {
                    _currentColor = _palette[i];
                    return;
                }
            }
        }

        // 2. Canvas drawing
        int canvasX = clientX + 10;
        int canvasY = clientY + 46 + 8;

        if (InputState.LeftDown)
        {
            if (mx >= canvasX && mx < canvasX + _canvasWidth && my >= canvasY && my < canvasY + _canvasHeight)
            {
                int px = mx - canvasX;
                int py = my - canvasY;

                uint drawCol = _currentTool == ToolMode.Eraser ? ColorScheme.White : _currentColor;

                if (_lastDrawX >= 0 && _lastDrawY >= 0)
                {
                    DrawCanvasLine(_lastDrawX, _lastDrawY, px, py, drawCol);
                }
                else
                {
                    DrawCanvasPoint(px, py, drawCol);
                }

                _lastDrawX = px;
                _lastDrawY = py;
            }
        }
        else
        {
            _lastDrawX = -1;
            _lastDrawY = -1;
        }
    }

    private void DrawCanvasPoint(int cx, int cy, uint col)
    {
        for (int dy = -_brushSize; dy <= _brushSize; dy++)
        {
            for (int dx = -_brushSize; dx <= _brushSize; dx++)
            {
                int x = cx + dx;
                int y = cy + dy;
                if (x >= 0 && x < _canvasWidth && y >= 0 && y < _canvasHeight)
                {
                    _canvasPixels[y * _canvasWidth + x] = col;
                }
            }
        }
    }

    private void DrawCanvasLine(int x0, int y0, int x1, int y1, uint col)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            DrawCanvasPoint(x0, y0, col);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    private void SaveToBmp()
    {
        // 24-bit uncompressed BMP encoder
        int rowStride = (_canvasWidth * 3 + 3) & ~3;
        int imageSize = rowStride * _canvasHeight;
        int fileSize = 54 + imageSize;

        byte[] bmp = new byte[fileSize];

        // BMP Header
        bmp[0] = 0x42; // 'B'
        bmp[1] = 0x4D; // 'M'
        BitConverter.GetBytes(fileSize).CopyTo(bmp, 2);
        BitConverter.GetBytes(54).CopyTo(bmp, 10); // Offset to pixel data

        // DIB Header
        BitConverter.GetBytes(40).CopyTo(bmp, 14); // Header size
        BitConverter.GetBytes(_canvasWidth).CopyTo(bmp, 18);
        BitConverter.GetBytes(_canvasHeight).CopyTo(bmp, 22);
        BitConverter.GetBytes((short)1).CopyTo(bmp, 26); // Planes
        BitConverter.GetBytes((short)24).CopyTo(bmp, 28); // 24-bit RGB
        BitConverter.GetBytes(imageSize).CopyTo(bmp, 34);

        // Pixels (BMP stores bottom-to-top BGR)
        for (int y = 0; y < _canvasHeight; y++)
        {
            int srcY = _canvasHeight - 1 - y;
            int rowOffset = 54 + (y * rowStride);

            for (int x = 0; x < _canvasWidth; x++)
            {
                uint col = _canvasPixels[srcY * _canvasWidth + x];
                int pOff = rowOffset + (x * 3);
                bmp[pOff] = (byte)(col & 0xFF);         // Blue
                bmp[pOff + 1] = (byte)((col >> 8) & 0xFF);  // Green
                bmp[pOff + 2] = (byte)((col >> 16) & 0xFF); // Red
            }
        }

        string savePath = Path.Combine(FileService.RootPath, "desenhos", "desenho.bmp");
        FileService.WriteAllBytes(savePath, bmp);
    }
}
