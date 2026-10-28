using System;
using System.IO;

namespace NybuOS.GUI;

/// <summary>
/// Decodificador de arquivos de ícone (.ICO) padrão do Windows 95 / 9x.
/// Suporta decodificação de ícones de 16 cores (4bpp), 256 cores (8bpp), 24bpp e 32bpp,
/// com máscara AND de transparência de 1-bit e seleção de resolução (16x16 e 32x32).
/// </summary>
public static class IcoDecoder
{
    /// <summary>
    /// Decodifica a melhor imagem de um arquivo .ICO a partir de um array de bytes.
    /// Se targetSize for especificado (ex: 16 ou 32), busca a imagem correspondente.
    /// </summary>
    public static IconImage Decode(byte[] data, int targetSize = 32)
    {
        if (data == null || data.Length < 22)
            return CreateFallbackIcon(targetSize);

        try
        {
            ushort reserved = BitConverter.ToUInt16(data, 0);
            ushort type = BitConverter.ToUInt16(data, 2);
            ushort count = BitConverter.ToUInt16(data, 4);

            if (type != 1 || count == 0)
                return CreateFallbackIcon(targetSize);

            // Procura a entrada mais próxima do tamanho desejado
            int bestIndex = 0;
            int bestDiff = int.MaxValue;

            for (int i = 0; i < count; i++)
            {
                int entryOffset = 6 + i * 16;
                if (entryOffset + 16 > data.Length) break;

                int w = data[entryOffset];
                if (w == 0) w = 256;

                int diff = Math.Abs(w - targetSize);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestIndex = i;
                }
            }

            return DecodeEntry(data, bestIndex);
        }
        catch
        {
            return CreateFallbackIcon(targetSize);
        }
    }

    /// <summary>
    /// Decodifica um arquivo .ICO a partir de um caminho no sistema de arquivos FAT32 / VFS.
    /// </summary>
    public static IconImage DecodeFromFile(string filePath, int targetSize = 32)
    {
        try
        {
            if (File.Exists(filePath))
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                return Decode(bytes, targetSize);
            }
        }
        catch
        {
            // Fallback silencioso
        }

        return CreateFallbackIcon(targetSize);
    }

    private static IconImage DecodeEntry(byte[] data, int entryIndex)
    {
        int entryOffset = 6 + entryIndex * 16;
        int rawW = data[entryOffset];
        int rawH = data[entryOffset + 1];
        int width = rawW == 0 ? 256 : rawW;
        int height = rawH == 0 ? 256 : rawH;

        uint imageSize = BitConverter.ToUInt32(data, entryOffset + 8);
        uint imageOffset = BitConverter.ToUInt32(data, entryOffset + 12);

        if (imageOffset >= data.Length || imageOffset + 40 > data.Length)
            return CreateFallbackIcon(width);

        int offset = (int)imageOffset;
        uint headerSize = BitConverter.ToUInt32(data, offset);
        int biWidth = BitConverter.ToInt32(data, offset + 4);
        int biHeight = BitConverter.ToInt32(data, offset + 8);
        ushort biPlanes = BitConverter.ToUInt16(data, offset + 12);
        ushort biBitCount = BitConverter.ToUInt16(data, offset + 14);
        uint biCompression = BitConverter.ToUInt32(data, offset + 16);
        uint biClrUsed = BitConverter.ToUInt32(data, offset + 32);

        // No ICO, biHeight normalmente é 2 * altura real (inclui a máscara AND)
        int realHeight = biHeight / 2;
        if (realHeight <= 0) realHeight = height;
        if (biWidth <= 0) biWidth = width;

        offset += (int)headerSize;

        // Leitura da paleta de cores (se biBitCount <= 8)
        uint[] palette = null;
        if (biBitCount <= 8)
        {
            int numColors = (int)biClrUsed;
            if (numColors == 0)
            {
                numColors = 1 << biBitCount;
            }

            palette = new uint[numColors];
            for (int c = 0; c < numColors; c++)
            {
                if (offset + 4 <= data.Length)
                {
                    byte b = data[offset++];
                    byte g = data[offset++];
                    byte r = data[offset++];
                    byte reservedByte = data[offset++];
                    palette[c] = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;
                }
            }
        }

        var result = new IconImage(biWidth, realHeight);

        // Decodificação dos pixels XOR (imagem colorida)
        int rowStride = ((biWidth * biBitCount + 31) / 32) * 4;
        int xorDataStart = offset;
        int xorDataSize = rowStride * realHeight;

        // Máscara AND (1 bit por pixel, DWORD-aligned)
        int maskStride = ((biWidth + 31) / 32) * 4;
        int maskDataStart = xorDataStart + xorDataSize;

        for (int y = 0; y < realHeight; y++)
        {
            // As linhas em BMP/ICO são bottom-up (de baixo para cima)
            int targetY = realHeight - 1 - y;
            int xorRowOffset = xorDataStart + y * rowStride;
            int maskRowOffset = maskDataStart + y * maskStride;

            for (int x = 0; x < biWidth; x++)
            {
                // Determina se a máscara AND define transparência para este pixel
                bool isTransparent = false;
                if (maskRowOffset < data.Length)
                {
                    int maskByteIndex = maskRowOffset + (x / 8);
                    if (maskByteIndex < data.Length)
                    {
                        byte maskByte = data[maskByteIndex];
                        int bit = (maskByte >> (7 - (x % 8))) & 1;
                        if (bit == 1)
                        {
                            isTransparent = true;
                        }
                    }
                }

                if (isTransparent)
                {
                    result.SetPixel(x, targetY, 0x00000000); // Totalmente transparente
                    continue;
                }

                uint pixelColor = 0xFF000000;

                if (biBitCount == 4 && palette != null)
                {
                    int byteIdx = xorRowOffset + (x / 2);
                    if (byteIdx < data.Length)
                    {
                        byte b = data[byteIdx];
                        int paletteIdx = (x % 2 == 0) ? ((b >> 4) & 0x0F) : (b & 0x0F);
                        if (paletteIdx < palette.Length)
                        {
                            pixelColor = palette[paletteIdx];
                        }
                    }
                }
                else if (biBitCount == 8 && palette != null)
                {
                    int byteIdx = xorRowOffset + x;
                    if (byteIdx < data.Length)
                    {
                        byte paletteIdx = data[byteIdx];
                        if (paletteIdx < palette.Length)
                        {
                            pixelColor = palette[paletteIdx];
                        }
                    }
                }
                else if (biBitCount == 24)
                {
                    int byteIdx = xorRowOffset + x * 3;
                    if (byteIdx + 3 <= data.Length)
                    {
                        byte b = data[byteIdx];
                        byte g = data[byteIdx + 1];
                        byte r = data[byteIdx + 2];
                        pixelColor = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;
                    }
                }
                else if (biBitCount == 32)
                {
                    int byteIdx = xorRowOffset + x * 4;
                    if (byteIdx + 4 <= data.Length)
                    {
                        byte b = data[byteIdx];
                        byte g = data[byteIdx + 1];
                        byte r = data[byteIdx + 2];
                        byte a = data[byteIdx + 3];
                        pixelColor = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
                    }
                }

                result.SetPixel(x, targetY, pixelColor);
            }
        }

        return result;
    }

    /// <summary>
    /// Ícone fallback caso o arquivo esteja corrompido ou inacessível.
    /// </summary>
    public static IconImage CreateFallbackIcon(int size)
    {
        var icon = new IconImage(size, size);
        uint border = 0xFF000000;
        uint fill = 0xFFC0C0C0;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                    icon.SetPixel(x, y, border);
                else
                    icon.SetPixel(x, y, fill);
            }
        }

        return icon;
    }
}
