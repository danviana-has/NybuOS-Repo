using System;

namespace NybuOS.Core.Graphics;

/// <summary>
/// Ultra-fast bare-metal BMP decoder supporting 24-bit and 32-bit (with Alpha).
/// Zero floating-point math, 100% pixel fidelity, no external dependencies.
/// </summary>
public static class BmpDecoder
{
    public static FastBitmap? Decode(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 54) return null;

        // Magic number 'BM'
        if (bytes[0] != 0x42 || bytes[1] != 0x4D) return null;

        int dataOffset = BitConverter.ToInt32(bytes, 10);
        int width = BitConverter.ToInt32(bytes, 18);
        int height = BitConverter.ToInt32(bytes, 22);
        short bpp = BitConverter.ToInt16(bytes, 28);
        int compression = BitConverter.ToInt32(bytes, 30);

        if (width <= 0 || height == 0) return null;
        if (bpp != 24 && bpp != 32) return null;
        if (compression != 0 && compression != 3) return null; // BI_RGB or BI_BITFIELDS

        bool topDown = false;
        if (height < 0)
        {
            height = -height;
            topDown = true;
        }

        var result = new FastBitmap(width, height);
        uint[] output = result.Pixels;

        if (bpp == 32)
        {
            int rowBytes = width * 4;
            for (int y = 0; y < height; y++)
            {
                int srcY = topDown ? y : (height - 1 - y);
                int srcRowOff = dataOffset + srcY * rowBytes;
                int dstRowOff = y * width;

                if (srcRowOff + rowBytes > bytes.Length) break;

                for (int x = 0; x < width; x++)
                {
                    int p = srcRowOff + x * 4;
                    byte b = bytes[p];
                    byte g = bytes[p + 1];
                    byte r = bytes[p + 2];
                    byte a = bytes[p + 3];

                    output[dstRowOff + x] = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
                }
            }
        }
        else if (bpp == 24)
        {
            int rowBytes = ((width * 3 + 3) / 4) * 4; // 4-byte aligned stride
            for (int y = 0; y < height; y++)
            {
                int srcY = topDown ? y : (height - 1 - y);
                int srcRowOff = dataOffset + srcY * rowBytes;
                int dstRowOff = y * width;

                if (srcRowOff + width * 3 > bytes.Length) break;

                for (int x = 0; x < width; x++)
                {
                    int p = srcRowOff + x * 3;
                    byte b = bytes[p];
                    byte g = bytes[p + 1];
                    byte r = bytes[p + 2];

                    output[dstRowOff + x] = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;
                }
            }
        }

        return result;
    }
}
