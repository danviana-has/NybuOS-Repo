namespace NybuOS.Core.Graphics;

public class FastBitmap
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public uint[] Pixels { get; private set; }

    public FastBitmap(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new uint[width * height];
    }

    public FastBitmap(int width, int height, uint[] pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public FastBitmap Resize(int newWidth, int newHeight)
    {
        if (newWidth <= 0 || newHeight <= 0) return this;
        if (newWidth == Width && newHeight == Height) return this;

        var result = new FastBitmap(newWidth, newHeight);
        var targetPixels = result.Pixels;
        var srcPixels = Pixels;

        int xRatio = ((Width << 16) / newWidth) + 1;
        int yRatio = ((Height << 16) / newHeight) + 1;

        for (int y = 0; y < newHeight; y++)
        {
            int srcY = (y * yRatio) >> 16;
            if (srcY >= Height) srcY = Height - 1;
            int srcRowOffset = srcY * Width;
            int targetRowOffset = y * newWidth;

            for (int x = 0; x < newWidth; x++)
            {
                int srcX = (x * xRatio) >> 16;
                if (srcX >= Width) srcX = Width - 1;
                targetPixels[targetRowOffset + x] = srcPixels[srcRowOffset + srcX];
            }
        }

        return result;
    }
}
