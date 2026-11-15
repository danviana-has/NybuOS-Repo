using System;
using LunarLabs.Fonts;
using NybuOS.Core.Graphics;

namespace NybuOS.Core.FontEngine;

public static class FontRenderer
{
    private static Font? _regularFont;
    private static Font? _boldFont;

    // Cache indexed by [isBold ? 1 : 0, sizeIndex, charCode]
    // Supported sizes: 12, 14, 18, 22
    private static readonly int[] SupportedSizes = { 12, 14, 18, 22 };
    private static readonly CachedGlyph?[][][] Cache = new CachedGlyph?[2][][];

    public static void Initialize()
    {
        for (int b = 0; b < 2; b++)
        {
            Cache[b] = new CachedGlyph?[SupportedSizes.Length][];
            for (int s = 0; s < SupportedSizes.Length; s++)
            {
                Cache[b][s] = new CachedGlyph?[512];
            }
        }

        try
        {
            if (AssetManager.RegularFontData != null && AssetManager.RegularFontData.Length > 0)
            {
                _regularFont = new Font(AssetManager.RegularFontData);
            }
            if (AssetManager.BoldFontData != null && AssetManager.BoldFontData.Length > 0)
            {
                _boldFont = new Font(AssetManager.BoldFontData);
            }
        }
        catch
        {
            _regularFont = null;
            _boldFont = null;
        }

        // Pre-render common ASCII and Latin characters
        for (int s = 0; s < SupportedSizes.Length; s++)
        {
            int size = SupportedSizes[s];
            for (int c = 32; c <= 126; c++)
            {
                GetOrCreateGlyph((char)c, false, size);
                GetOrCreateGlyph((char)c, true, size);
            }
            // Common accented characters
            char[] accents = "áéíóúàèìòùãõâêîôûçÁÉÍÓÚÀÈÌÒÙÃÕÂÊÎÔÛÇ".ToCharArray();
            for (int i = 0; i < accents.Length; i++)
            {
                GetOrCreateGlyph(accents[i], false, size);
                GetOrCreateGlyph(accents[i], true, size);
            }
        }
    }

    private static int GetSizeIndex(int size)
    {
        for (int i = 0; i < SupportedSizes.Length; i++)
        {
            if (SupportedSizes[i] == size) return i;
        }
        // closest size
        if (size <= 13) return 0; // 12
        if (size <= 16) return 1; // 14
        if (size <= 20) return 2; // 18
        return 3; // 22
    }

    private static CachedGlyph? GetOrCreateGlyph(char c, bool bold, int size)
    {
        int bIdx = bold ? 1 : 0;
        int sIdx = GetSizeIndex(size);
        int cIdx = (int)c;

        if (cIdx < 512 && Cache[bIdx][sIdx][cIdx] != null)
        {
            return Cache[bIdx][sIdx][cIdx];
        }

        Font? font = bold ? (_boldFont ?? _regularFont) : (_regularFont ?? _boldFont);
        if (font == null)
        {
            return CreateFallbackGlyph(c, size);
        }

        try
        {
            float scale = font.ScaleInPixels(size);
            var glyph = font.RenderGlyph(c, scale);
            if (glyph == null || glyph.Image == null)
            {
                return CreateFallbackGlyph(c, size);
            }

            var cached = new CachedGlyph
            {
                Character = c,
                Width = glyph.Image.Width,
                Height = glyph.Image.Height,
                XOfs = glyph.xOfs,
                YOfs = glyph.yOfs,
                XAdvance = glyph.xAdvance > 0 ? glyph.xAdvance : (glyph.Image.Width + 2),
                Alpha = glyph.Image.Pixels ?? Array.Empty<byte>()
            };

            if (cIdx < 512)
            {
                Cache[bIdx][sIdx][cIdx] = cached;
            }
            return cached;
        }
        catch
        {
            return CreateFallbackGlyph(c, size);
        }
    }

    private static CachedGlyph CreateFallbackGlyph(char c, int size)
    {
        int w = size * 3 / 5;
        int h = size;
        return new CachedGlyph
        {
            Character = c,
            Width = w,
            Height = h,
            XOfs = 0,
            YOfs = -h,
            XAdvance = w + 2,
            Alpha = new byte[w * h]
        };
    }

    public static int MeasureText(string? text, bool bold = false, int size = 14)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        int width = 0;
        for (int i = 0; i < text.Length; i++)
        {
            var glyph = GetOrCreateGlyph(text[i], bold, size);
            if (glyph != null)
            {
                width += glyph.XAdvance;
            }
            else
            {
                width += size * 3 / 5;
            }
        }
        return width;
    }

    public static void DrawText(uint[] buffer, int screenW, int screenH, string? text, int startX, int startY, uint color, bool bold = false, int size = 14)
    {
        if (string.IsNullOrEmpty(text) || buffer == null) return;

        int cursorX = startX;
        int baselineY = startY + size; // Font baseline

        uint alphaMask = (color >> 24) & 0xFF;
        if (alphaMask == 0) alphaMask = 255;
        uint rCol = (color >> 16) & 0xFF;
        uint gCol = (color >> 8) & 0xFF;
        uint bCol = color & 0xFF;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == ' ')
            {
                cursorX += size / 3;
                continue;
            }
            if (c == '\t')
            {
                cursorX += (size / 3) * 4;
                continue;
            }

            var glyph = GetOrCreateGlyph(c, bold, size);
            if (glyph == null)
            {
                cursorX += size / 2;
                continue;
            }

            int gW = glyph.Width;
            int gH = glyph.Height;
            byte[] alphaData = glyph.Alpha;

            if (gW > 0 && gH > 0 && alphaData.Length >= gW * gH)
            {
                int drawX = cursorX + glyph.XOfs;
                int drawY = baselineY + glyph.YOfs;

                for (int gy = 0; gy < gH; gy++)
                {
                    int py = drawY + gy;
                    if (py < 0 || py >= screenH) continue;
                    int rowOffset = py * screenW;
                    int gRowOffset = gy * gW;

                    for (int gx = 0; gx < gW; gx++)
                    {
                        int px = drawX + gx;
                        if (px < 0 || px >= screenW) continue;

                        byte a = alphaData[gRowOffset + gx];
                        if (a == 0) continue;

                        int finalA = (int)((a * alphaMask) / 255);
                        if (finalA <= 0) continue;

                        int bufIdx = rowOffset + px;
                        if (finalA >= 250)
                        {
                            buffer[bufIdx] = 0xFF000000 | (rCol << 16) | (gCol << 8) | bCol;
                        }
                        else
                        {
                            uint bg = buffer[bufIdx];
                            uint bgR = (bg >> 16) & 0xFF;
                            uint bgG = (bg >> 8) & 0xFF;
                            uint bgB = bg & 0xFF;

                            uint outR = (uint)(((rCol * finalA) + (bgR * (255 - finalA))) / 255);
                            uint outG = (uint)(((gCol * finalA) + (bgG * (255 - finalA))) / 255);
                            uint outB = (uint)(((bCol * finalA) + (bgB * (255 - finalA))) / 255);

                            buffer[bufIdx] = 0xFF000000 | (outR << 16) | (outG << 8) | outB;
                        }
                    }
                }
            }

            cursorX += glyph.XAdvance;
        }
    }
}
