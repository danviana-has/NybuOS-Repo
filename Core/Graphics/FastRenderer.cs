using System;

namespace NybuOS.Core.Graphics;

public static class FastRenderer
{
    public static void Clear(uint[] buffer, uint color)
    {
        if (buffer == null) return;
        Array.Fill(buffer, color);
    }

    public static void CopyBuffer(uint[] src, uint[] dest)
    {
        if (src == null || dest == null) return;
        Array.Copy(src, dest, Math.Min(src.Length, dest.Length));
    }

    public static void DrawFilledRectangle(uint[] buffer, int screenW, int screenH, int x, int y, int w, int h, uint color)
    {
        if (buffer == null || w <= 0 || h <= 0) return;

        int x1 = Math.Max(0, x);
        int y1 = Math.Max(0, y);
        int x2 = Math.Min(screenW, x + w);
        int y2 = Math.Min(screenH, y + h);

        if (x1 >= x2 || y1 >= y2) return;

        uint a = (color >> 24) & 0xFF;
        if (a == 255)
        {
            int fillCount = x2 - x1;
            for (int cy = y1; cy < y2; cy++)
            {
                int rowOff = cy * screenW + x1;
                Array.Fill(buffer, color, rowOff, fillCount);
            }
        }
        else if (a > 0)
        {
            uint r = (color >> 16) & 0xFF;
            uint g = (color >> 8) & 0xFF;
            uint b = color & 0xFF;
            uint invA = 255 - a;

            for (int cy = y1; cy < y2; cy++)
            {
                int rowOff = cy * screenW;
                for (int cx = x1; cx < x2; cx++)
                {
                    int idx = rowOff + cx;
                    uint bg = buffer[idx];
                    uint bgR = (bg >> 16) & 0xFF;
                    uint bgG = (bg >> 8) & 0xFF;
                    uint bgB = bg & 0xFF;

                    uint outR = (r * a + bgR * invA) / 255;
                    uint outG = (g * a + bgG * invA) / 255;
                    uint outB = (b * a + bgB * invA) / 255;

                    buffer[idx] = 0xFF000000 | (outR << 16) | (outG << 8) | outB;
                }
            }
        }
    }

    public static void DrawRectangle(uint[] buffer, int screenW, int screenH, int x, int y, int w, int h, uint color)
    {
        if (w <= 0 || h <= 0) return;
        DrawHorizontalLine(buffer, screenW, screenH, x, y, w, color);
        DrawHorizontalLine(buffer, screenW, screenH, x, y + h - 1, w, color);
        DrawVerticalLine(buffer, screenW, screenH, x, y, h, color);
        DrawVerticalLine(buffer, screenW, screenH, x + w - 1, y, h, color);
    }

    public static void DrawHorizontalLine(uint[] buffer, int screenW, int screenH, int x, int y, int length, uint color)
    {
        if (buffer == null || y < 0 || y >= screenH || length <= 0) return;
        int x1 = Math.Max(0, x);
        int x2 = Math.Min(screenW, x + length);
        if (x1 >= x2) return;

        int rowOff = y * screenW;
        for (int cx = x1; cx < x2; cx++)
        {
            buffer[rowOff + cx] = color;
        }
    }

    public static void DrawVerticalLine(uint[] buffer, int screenW, int screenH, int x, int y, int length, uint color)
    {
        if (buffer == null || x < 0 || x >= screenW || length <= 0) return;
        int y1 = Math.Max(0, y);
        int y2 = Math.Min(screenH, y + length);
        if (y1 >= y2) return;

        for (int cy = y1; cy < y2; cy++)
        {
            buffer[cy * screenW + x] = color;
        }
    }

    public static void DrawLine(uint[] buffer, int screenW, int screenH, int x0, int y0, int x1, int y1, uint color)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < screenW && y0 >= 0 && y0 < screenH)
            {
                buffer[y0 * screenW + x0] = color;
            }

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

    public static void DrawBitmap(uint[] buffer, int screenW, int screenH, FastBitmap? bmp, int destX, int destY)
    {
        if (bmp == null || buffer == null) return;

        int bw = bmp.Width;
        int bh = bmp.Height;
        var srcPixels = bmp.Pixels;

        int x1 = Math.Max(0, destX);
        int y1 = Math.Max(0, destY);
        int x2 = Math.Min(screenW, destX + bw);
        int y2 = Math.Min(screenH, destY + bh);

        if (x1 >= x2 || y1 >= y2) return;

        for (int cy = y1; cy < y2; cy++)
        {
            int destRowOff = cy * screenW;
            int srcRowOff = (cy - destY) * bw;

            for (int cx = x1; cx < x2; cx++)
            {
                uint src = srcPixels[srcRowOff + (cx - destX)];
                uint a = (src >> 24) & 0xFF;
                if (a == 255)
                {
                    buffer[destRowOff + cx] = src;
                }
                else if (a > 0)
                {
                    int destIdx = destRowOff + cx;
                    uint bg = buffer[destIdx];

                    uint sR = (src >> 16) & 0xFF;
                    uint sG = (src >> 8) & 0xFF;
                    uint sB = src & 0xFF;

                    uint bgR = (bg >> 16) & 0xFF;
                    uint bgG = (bg >> 8) & 0xFF;
                    uint bgB = bg & 0xFF;

                    uint invA = 255 - a;
                    uint outR = (sR * a + bgR * invA) / 255;
                    uint outG = (sG * a + bgG * invA) / 255;
                    uint outB = (sB * a + bgB * invA) / 255;

                    buffer[destIdx] = 0xFF000000 | (outR << 16) | (outG << 8) | outB;
                }
            }
        }
    }

    /// <summary>
    /// Soft shadow around windows (Windows 10 style).
    /// </summary>
    public static void DrawShadow(uint[] buffer, int screenW, int screenH, int x, int y, int w, int h, int radius = 6)
    {
        if (buffer == null || radius <= 0) return;

        // Draw multiple expanding translucent bounding borders
        for (int r = 1; r <= radius; r++)
        {
            uint alpha = (uint)(24 * (radius - r + 1) / radius);
            uint shadowColor = (alpha << 24); // Black with soft alpha

            int sx = x - r;
            int sy = y - r;
            int sw = w + (r * 2);
            int sh = h + (r * 2);

            DrawFilledRectangle(buffer, screenW, screenH, sx, sy, sw, 1, shadowColor); // Top
            DrawFilledRectangle(buffer, screenW, screenH, sx, sy + sh - 1, sw, 1, shadowColor); // Bottom
            DrawFilledRectangle(buffer, screenW, screenH, sx, sy, 1, sh, shadowColor); // Left
            DrawFilledRectangle(buffer, screenW, screenH, sx + sw - 1, sy, 1, sh, shadowColor); // Right
        }
    }

    /// <summary>
    /// Smooth brightness overlay adjustment.
    /// </summary>
    public static void ApplyBrightness(uint[] buffer, int brightnessPercent)
    {
        if (buffer == null || brightnessPercent >= 100) return;
        if (brightnessPercent < 15) brightnessPercent = 15;

        // Multiply RGB channels by brightness percent
        int factor = (brightnessPercent * 256) / 100;
        for (int i = 0; i < buffer.Length; i++)
        {
            uint c = buffer[i];
            uint r = (((c >> 16) & 0xFF) * (uint)factor) >> 8;
            uint g = (((c >> 8) & 0xFF) * (uint)factor) >> 8;
            uint b = ((c & 0xFF) * (uint)factor) >> 8;
            buffer[i] = 0xFF000000 | (r << 16) | (g << 8) | b;
        }
    }

    /// <summary>
    /// High precision crisp Windows 10 mouse pointer.
    /// </summary>
    public static void DrawMouseCursor(uint[] buffer, int screenW, int screenH, int mx, int my)
    {
        if (buffer == null) return;

        // 12x19 standard mouse arrow
        int[] cursor = {
            1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 2, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0,
            1, 2, 2, 2, 2, 1, 0, 0, 0, 0, 0, 0,
            1, 2, 2, 2, 2, 2, 1, 0, 0, 0, 0, 0,
            1, 2, 2, 2, 2, 2, 2, 1, 0, 0, 0, 0,
            1, 2, 2, 2, 2, 2, 2, 2, 1, 0, 0, 0,
            1, 2, 2, 2, 2, 2, 2, 2, 2, 1, 0, 0,
            1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 0,
            1, 2, 2, 1, 2, 2, 1, 0, 0, 0, 0, 0,
            1, 2, 1, 0, 1, 2, 2, 1, 0, 0, 0, 0,
            1, 1, 0, 0, 1, 2, 2, 1, 0, 0, 0, 0,
            1, 0, 0, 0, 0, 1, 2, 2, 1, 0, 0, 0,
            0, 0, 0, 0, 0, 1, 2, 2, 1, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 1, 2, 2, 1, 0, 0,
            0, 0, 0, 0, 0, 0, 1, 2, 1, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0
        };

        int cw = 12;
        int ch = 19;

        for (int y = 0; y < ch; y++)
        {
            int py = my + y;
            if (py < 0 || py >= screenH) continue;
            int rowOff = py * screenW;
            int cRowOff = y * cw;

            for (int x = 0; x < cw; x++)
            {
                int px = mx + x;
                if (px < 0 || px >= screenW) continue;

                int code = cursor[cRowOff + x];
                if (code == 1)
                {
                    buffer[rowOff + px] = ColorScheme.Black; // Outline
                }
                else if (code == 2)
                {
                    buffer[rowOff + px] = ColorScheme.White; // Fill
                }
            }
        }
    }
}
