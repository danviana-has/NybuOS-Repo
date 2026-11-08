using System;
using System.Drawing;
using Cosmos.Kernel.System.Graphics;
using LunarLabs.Fonts;
using NybuOS.GUI;

namespace NybuOS.Drivers;

/// <summary>
/// Estrutura de glifo em cache para alta performance na renderização de texto.
/// </summary>
public struct CachedGlyph
{
    public int Width;
    public int Height;
    public int XOfs;
    public int YOfs;
    public int XAdvance;
    public byte[]? Pixels;
}

/// <summary>
/// Renderizador de fontes TrueType (TTF) baseado na biblioteca LunarFonts.
/// Aplica Alpha Blending de 0 a 255 com o fundo do canvas/wallpaper,
/// proporcionando antialiasing avançado e contornos perfeitamente suaves.
/// </summary>
public static class TrueTypeRenderer
{
    public const float DefaultFontSize = 21.0f;
    private static Font? _font;
    private static CachedGlyph[]? _asciiCache;
    private static int _baselineOffset = 18;
    private static int _lineHeight = 22;
    private static bool _isInitialized = false;

    public static int DefaultHeight => _lineHeight;
    public static bool IsLoaded => _isInitialized && _font != null;

    public static void Initialize()
    {
        if (_isInitialized) return;

        try
        {
            byte[] fontBytes = EmbeddedArialFontData.GetFontBytes();
            if (fontBytes != null && fontBytes.Length > 0)
            {
                _font = new Font(fontBytes);
                float scale = _font.ScaleInPixels(DefaultFontSize);

                _font.GetFontVMetrics(out int ascent, out int descent, out int lineGap);
                _baselineOffset = (int)(ascent * scale + 0.5f);
                _lineHeight = (int)((ascent - descent + lineGap) * scale + 0.5f);
                if (_lineHeight <= 0) _lineHeight = 22;
                if (_baselineOffset <= 0) _baselineOffset = 18;

                _asciiCache = new CachedGlyph[256];

                // Pré-renderiza todos os caracteres comuns (ASCII 32 a 255)
                for (int i = 32; i < 256; i++)
                {
                    char c = (char)i;
                    try
                    {
                        var glyph = _font.RenderGlyph(c, scale);
                        if (glyph != null && glyph.Image != null)
                        {
                            _asciiCache[i] = new CachedGlyph
                            {
                                Width = glyph.Image.Width,
                                Height = glyph.Image.Height,
                                XOfs = glyph.xOfs,
                                YOfs = glyph.yOfs,
                                XAdvance = glyph.xAdvance,
                                Pixels = glyph.Image.Pixels
                            };
                        }
                    }
                    catch
                    {
                    }
                }

                _isInitialized = true;
            }
        }
        catch
        {
            _isInitialized = false;
        }
    }

    public static CachedGlyph GetGlyph(char c)
    {
        if (_asciiCache != null && c < 256)
        {
            var g = _asciiCache[c];
            if (g.Pixels != null) return g;
        }

        if (_font != null)
        {
            try
            {
                float scale = _font.ScaleInPixels(DefaultFontSize);
                var glyph = _font.RenderGlyph(c, scale);
                if (glyph != null && glyph.Image != null)
                {
                    var cached = new CachedGlyph
                    {
                        Width = glyph.Image.Width,
                        Height = glyph.Image.Height,
                        XOfs = glyph.xOfs,
                        YOfs = glyph.yOfs,
                        XAdvance = glyph.xAdvance,
                        Pixels = glyph.Image.Pixels
                    };
                    if (c < 256 && _asciiCache != null)
                    {
                        _asciiCache[c] = cached;
                    }
                    return cached;
                }
            }
            catch
            {
            }
        }

        return default;
    }

    public static int MeasureString(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        if (!_isInitialized) Initialize();

        int totalW = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\n' || c == '\r') continue;

            var g = GetGlyph(c);
            if (g.Pixels != null)
            {
                totalW += g.XAdvance;
            }
            else
            {
                totalW += 8;
            }
        }
        return totalW;
    }

    /// <summary>
    /// Renderiza o texto TrueType com Alpha Blending pixel-a-pixel.
    /// Utiliza a cobertura de 0 a 255 de cada pixel para suavizar as bordas das letras
    /// mesclando suavemente a cor da fonte com o fundo.
    /// </summary>
    public static void DrawString(Canvas canvas, int x, int y, string text, Color color, Color? bg = null, IconImage? activeWallpaper = null)
    {
        if (canvas == null || string.IsNullOrEmpty(text)) return;
        if (!_isInitialized) Initialize();

        int curX = x;
        int curY = y;

        int cR = color.R;
        int cG = color.G;
        int cB = color.B;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\n')
            {
                curX = x;
                curY += _lineHeight + 2;
                continue;
            }
            if (c == '\r') continue;

            var g = GetGlyph(c);
            if (g.Pixels == null || g.Width <= 0 || g.Height <= 0)
            {
                curX += (g.XAdvance > 0 ? g.XAdvance : 7);
                continue;
            }

            int drawX = curX + g.XOfs;
            int drawY = curY + _baselineOffset + g.YOfs;

            int gw = g.Width;
            int gh = g.Height;
            byte[] pixels = g.Pixels;

            for (int row = 0; row < gh; row++)
            {
                int py = drawY + row;
                if (py < 0 || py >= canvas.Height) continue;
                int rowOff = row * gw;

                for (int col = 0; col < gw; col++)
                {
                    byte cov = pixels[rowOff + col];
                    if (cov == 0) continue;

                    int px = drawX + col;
                    if (px < 0 || px >= canvas.Width) continue;

                    if (cov == 255)
                    {
                        canvas.DrawPoint(color, px, py);
                    }
                    else
                    {
                        // Mistura Alpha Blending (0 a 255) com o fundo
                        Color bgCol;
                        if (bg.HasValue)
                        {
                            bgCol = bg.Value;
                        }
                        else
                        {
                            bgCol = canvas.GetPointColor(px, py);
                            if (bgCol.R == 0 && bgCol.G == 0 && bgCol.B == 0 && activeWallpaper != null && px < activeWallpaper.Width && py < activeWallpaper.Height)
                            {
                                uint wallPx = activeWallpaper.Pixels[py * activeWallpaper.Width + px];
                                bgCol = Color.FromArgb((int)((wallPx >> 16) & 0xFF), (int)((wallPx >> 8) & 0xFF), (int)(wallPx & 0xFF));
                            }
                        }

                        int invCov = 255 - cov;
                        int r = (cR * cov + bgCol.R * invCov) / 255;
                        int gVal = (cG * cov + bgCol.G * invCov) / 255;
                        int bVal = (cB * cov + bgCol.B * invCov) / 255;

                        canvas.DrawPoint(Color.FromArgb(r, gVal, bVal), px, py);
                    }
                }
            }

            curX += g.XAdvance;
        }
    }
}
