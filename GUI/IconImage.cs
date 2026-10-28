using System;
using System.Drawing;

namespace NybuOS.GUI;

/// <summary>
/// Representa uma imagem ou ícone em memória decodificado de um arquivo .ICO ou .BMP.
/// Armazena largura, altura e um buffer de pixels ARGB de 32 bits (com canal Alpha de transparência).
/// </summary>
public class IconImage
{
    public int Width { get; }
    public int Height { get; }
    public uint[] Pixels { get; }

    public IconImage(int width, int height)
    {
        if (width <= 0) width = 16;
        if (height <= 0) height = 16;
        Width = width;
        Height = height;
        Pixels = new uint[width * height];
    }

    public IconImage(int width, int height, uint[] pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels ?? new uint[width * height];
    }

    public void SetPixel(int x, int y, uint argb)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            Pixels[y * Width + x] = argb;
        }
    }

    public uint GetPixel(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            return Pixels[y * Width + x];
        }
        return 0; // Transparente
    }

    public bool IsTransparent(int x, int y)
    {
        uint pixel = GetPixel(x, y);
        return (pixel >> 24) == 0;
    }
}
