using System;

namespace NybuOS.Drivers;

/// <summary>
/// Fonte oficial do NybuOS RV baseada na fonte Arial convertida para PSF2 (fonte.psf) aceita pelo Cosmos OS.
/// Oferece caracteres nítidos em resolução 8x16 com proporções perfeitas para desktops Full HD.
/// </summary>
public static class CosmosNativeFont
{
    public const int Width = 8;
    public const int Height = 16;

    public static bool GetPixel(char c, int x, int y)
    {
        return PsfDecoder.DefaultFont.GetPixel(c, x, y);
    }
}
