using System;

namespace NybuOS.Core.FontEngine;

public sealed class CachedGlyph
{
    public char Character;
    public int Width;
    public int Height;
    public int XOfs;
    public int YOfs;
    public int XAdvance;
    public byte[] Alpha = Array.Empty<byte>();
}
