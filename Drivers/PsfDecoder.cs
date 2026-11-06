using System;
using System.IO;

namespace NybuOS.Drivers;

/// <summary>
/// Decodificador de fontes em formato PC Screen Font (PSF1 e PSF2) aceito pelo Cosmos OS.
/// Carrega e renderiza fontes bitmap como a fonte Arial (8x16) convertida pelo usuário.
/// </summary>
public class PsfFont
{
    public int Width { get; }
    public int Height { get; }
    public int GlyphCount { get; }
    public int BytesPerGlyph { get; }
    public byte[] GlyphData { get; }

    public PsfFont(int width, int height, int glyphCount, int bytesPerGlyph, byte[] glyphData)
    {
        Width = width;
        Height = height;
        GlyphCount = glyphCount;
        BytesPerGlyph = bytesPerGlyph;
        GlyphData = glyphData;
    }

    /// <summary>
    /// Testa se o pixel na coordenada (x, y) do caractere c está aceso.
    /// </summary>
    public bool GetPixel(char c, int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;

        int glyphIndex = (int)c;
        if (glyphIndex < 0 || glyphIndex >= GlyphCount) glyphIndex = 63; // '?'

        int bytesPerRow = (Width + 7) / 8;
        int byteOffset = (glyphIndex * BytesPerGlyph) + (y * bytesPerRow) + (x / 8);

        if (byteOffset >= GlyphData.Length) return false;

        byte b = GlyphData[byteOffset];
        int bit = 7 - (x % 8);
        return ((b >> bit) & 1) != 0;
    }
}

public static class PsfDecoder
{
    private static PsfFont? _defaultFont;

    public static PsfFont DefaultFont
    {
        get
        {
            if (_defaultFont == null)
            {
                _defaultFont = Decode(EmbeddedFontData.FontePsf) ?? CreateFallbackFont();
            }
            return _defaultFont;
        }
    }

    /// <summary>
    /// Decodifica um arquivo PSF a partir de seus bytes (suporta PSF1 e PSF2).
    /// </summary>
    public static PsfFont? Decode(byte[]? data)
    {
        if (data == null || data.Length < 4) return null;

        // PSF1: Magic 0x36, 0x04
        if (data[0] == 0x36 && data[1] == 0x04)
        {
            byte mode = data[2];
            byte charSize = data[3];
            int glyphCount = (mode & 0x01) != 0 ? 512 : 256;
            int width = 8;
            int height = charSize;
            int headerSize = 4;

            int totalGlyphBytes = glyphCount * charSize;
            if (headerSize + totalGlyphBytes > data.Length) return null;

            byte[] glyphBytes = new byte[totalGlyphBytes];
            Buffer.BlockCopy(data, headerSize, glyphBytes, 0, totalGlyphBytes);

            return new PsfFont(width, height, glyphCount, charSize, glyphBytes);
        }

        // PSF2: Magic 0x72, 0xB5, 0x4A, 0x86
        if (data.Length >= 32 && data[0] == 0x72 && data[1] == 0xB5 && data[2] == 0x4A && data[3] == 0x86)
        {
            int headerSize = BitConverter.ToInt32(data, 8);
            int glyphCount = BitConverter.ToInt32(data, 16);
            int bytesPerGlyph = BitConverter.ToInt32(data, 20);
            int height = BitConverter.ToInt32(data, 24);
            int width = BitConverter.ToInt32(data, 28);

            if (headerSize < 32 || glyphCount <= 0 || bytesPerGlyph <= 0 || width <= 0 || height <= 0)
                return null;

            int totalGlyphBytes = glyphCount * bytesPerGlyph;
            if (headerSize + totalGlyphBytes > data.Length) return null;

            byte[] glyphBytes = new byte[totalGlyphBytes];
            Buffer.BlockCopy(data, headerSize, glyphBytes, 0, totalGlyphBytes);

            return new PsfFont(width, height, glyphCount, bytesPerGlyph, glyphBytes);
        }

        return null;
    }

    private static PsfFont CreateFallbackFont()
    {
        // Fallback básico 8x16
        return new PsfFont(8, 16, 256, 16, new byte[256 * 16]);
    }
}
