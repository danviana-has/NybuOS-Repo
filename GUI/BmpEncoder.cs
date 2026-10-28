using System;
using System.Drawing;
using System.IO;

namespace NybuOS.GUI;

/// <summary>
/// Codificador e decodificador de arquivos de imagem Bitmap (.BMP) padrão Windows.
/// Utilizado pelo PaintPad para salvar obras de arte no FAT32 e pelo Media Reproduction para visualização.
/// </summary>
public static class BmpEncoder
{
    /// <summary>
    /// Codifica uma matriz de cores em um arquivo .BMP de 24 bits por pixel.
    /// </summary>
    public static byte[] Encode24bpp(Color[,] canvas, int width, int height)
    {
        int rowStride = ((width * 3 + 3) / 4) * 4; // Alinhamento DWORD de 4 bytes
        int imageSize = rowStride * height;
        int fileSize = 54 + imageSize; // 14 bytes BITMAPFILEHEADER + 40 bytes BITMAPINFOHEADER

        byte[] bmp = new byte[fileSize];

        // 1. BITMAPFILEHEADER (14 bytes)
        bmp[0] = (byte)'B';
        bmp[1] = (byte)'M';
        WriteUInt32(bmp, 2, (uint)fileSize);
        WriteUInt16(bmp, 6, 0); // Reserved 1
        WriteUInt16(bmp, 8, 0); // Reserved 2
        WriteUInt32(bmp, 10, 54); // Offset dos dados de pixels

        // 2. BITMAPINFOHEADER (40 bytes)
        WriteUInt32(bmp, 14, 40); // Tamanho do cabeçalho
        WriteInt32(bmp, 18, width); // Largura
        WriteInt32(bmp, 22, height); // Altura (positivo = bottom-up)
        WriteUInt16(bmp, 26, 1); // Planes
        WriteUInt16(bmp, 28, 24); // Bits por pixel (24 bpp BGR)
        WriteUInt32(bmp, 30, 0); // Compressão BI_RGB (0 = sem compressão)
        WriteUInt32(bmp, 34, (uint)imageSize); // Tamanho da imagem
        WriteInt32(bmp, 38, 2835); // Pixels por metro X (72 DPI)
        WriteInt32(bmp, 42, 2835); // Pixels por metro Y (72 DPI)
        WriteUInt32(bmp, 46, 0); // Cores usadas
        WriteUInt32(bmp, 50, 0); // Cores importantes

        // 3. Dados dos Pixels (Bottom-Up)
        int dataOffset = 54;
        for (int y = height - 1; y >= 0; y--)
        {
            int rowStart = dataOffset;
            for (int x = 0; x < width; x++)
            {
                Color c = canvas[x, y];
                bmp[dataOffset++] = c.B;
                bmp[dataOffset++] = c.G;
                bmp[dataOffset++] = c.R;
            }

            // Padding até múltiplo de 4 bytes
            int bytesWritten = dataOffset - rowStart;
            int padding = rowStride - bytesWritten;
            for (int p = 0; p < padding; p++)
            {
                bmp[dataOffset++] = 0;
            }
        }

        return bmp;
    }

    /// <summary>
    /// Decodifica um arquivo .BMP simples (24bpp ou 32bpp uncompressed) para um IconImage.
    /// </summary>
    public static IconImage Decode(byte[] data)
    {
        if (data == null || data.Length < 54)
            return null;

        if (data[0] != 'B' || data[1] != 'M')
            return null;

        uint pixelOffset = BitConverter.ToUInt32(data, 10);
        int width = BitConverter.ToInt32(data, 18);
        int height = BitConverter.ToInt32(data, 22);
        ushort bpp = BitConverter.ToUInt16(data, 28);
        uint compression = BitConverter.ToUInt32(data, 30);

        if (width <= 0 || height == 0 || (bpp != 24 && bpp != 32) || compression != 0)
            return null;

        bool topDown = height < 0;
        int absHeight = Math.Abs(height);

        var result = new IconImage(width, absHeight);
        int rowStride = ((width * (bpp / 8) + 3) / 4) * 4;

        for (int y = 0; y < absHeight; y++)
        {
            int targetY = topDown ? y : (absHeight - 1 - y);
            int rowOffset = (int)pixelOffset + y * rowStride;

            for (int x = 0; x < width; x++)
            {
                if (bpp == 24)
                {
                    int pxOffset = rowOffset + x * 3;
                    if (pxOffset + 2 < data.Length)
                    {
                        byte b = data[pxOffset];
                        byte g = data[pxOffset + 1];
                        byte r = data[pxOffset + 2];
                        result.SetPixel(x, targetY, 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b);
                    }
                }
                else if (bpp == 32)
                {
                    int pxOffset = rowOffset + x * 4;
                    if (pxOffset + 3 < data.Length)
                    {
                        byte b = data[pxOffset];
                        byte g = data[pxOffset + 1];
                        byte r = data[pxOffset + 2];
                        byte a = data[pxOffset + 3];
                        if (a == 0) a = 255;
                        result.SetPixel(x, targetY, ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b);
                    }
                }
            }
        }

        return result;
    }

    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
    }

    private static void WriteInt32(byte[] buffer, int offset, int value)
    {
        WriteUInt32(buffer, offset, (uint)value);
    }

    private static void WriteUInt32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
    }
}
