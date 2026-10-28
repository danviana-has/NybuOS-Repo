using System;
using System.IO;

namespace NybuOS.GUI;

/// <summary>
/// Repositório e gerenciador do papel de parede moderno (wallpaper.jpg escalado em BMP HD).
/// O BMP já está nas dimensões nativas exatas (800x564), eliminando qualquer pixelização ou perda de performance.
/// </summary>
public static class WallpaperResources
{
    private static IconImage? _cachedWallpaper = null;

    /// <summary>
    /// Retorna o papel de parede HD oficial decodificado nas dimensões exatas da área útil.
    /// </summary>
    public static IconImage? GetScaledWallpaper(int targetWidth, int targetHeight)
    {
        if (_cachedWallpaper != null)
        {
            return _cachedWallpaper;
        }

        byte[]? bmpBytes = null;
        try
        {
            bmpBytes = EmbeddedWallpaperData.GetWallpaperBmpBytes();
        }
        catch
        {
            bmpBytes = null;
        }

        if (bmpBytes == null || bmpBytes.Length < 54)
        {
            _cachedWallpaper = GenerateCosmicBackdrop(targetWidth, targetHeight);
            return _cachedWallpaper;
        }

        // Decodificação direta de BMP de 24bpp sem reescalonamento em runtime
        try
        {
            int dataOffset = BitConverter.ToInt32(bmpBytes, 10);
            int width = BitConverter.ToInt32(bmpBytes, 18);
            int height = BitConverter.ToInt32(bmpBytes, 22);
            short bpp = BitConverter.ToInt16(bmpBytes, 28);

            bool topDown = height < 0;
            int actualHeight = Math.Abs(height);

            var img = new IconImage(width, actualHeight);
            int rowSize = ((width * bpp + 31) / 32) * 4;

            for (int y = 0; y < actualHeight; y++)
            {
                int bmpY = topDown ? y : (actualHeight - 1 - y);
                int rowOffset = dataOffset + (bmpY * rowSize);

                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = rowOffset + (x * (bpp / 8));
                    if (pixelOffset + 2 < bmpBytes.Length)
                    {
                        byte b = bmpBytes[pixelOffset];
                        byte g = bmpBytes[pixelOffset + 1];
                        byte r = bmpBytes[pixelOffset + 2];
                        uint argb = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
                        img.SetPixel(x, y, argb);
                    }
                }
            }

            _cachedWallpaper = img;
            return _cachedWallpaper;
        }
        catch
        {
            _cachedWallpaper = GenerateCosmicBackdrop(targetWidth, targetHeight);
            return _cachedWallpaper;
        }
    }

    private static IconImage GenerateCosmicBackdrop(int width, int height)
    {
        var img = new IconImage(width, height);
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / height;
            byte r = (byte)(18 + (int)(30 * t));
            byte g = (byte)(15 + (int)(20 * t));
            byte b = (byte)(28 + (int)(50 * t));
            uint bg = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b;

            for (int x = 0; x < width; x++)
            {
                img.SetPixel(x, y, bg);
            }
        }
        return img;
    }
}
