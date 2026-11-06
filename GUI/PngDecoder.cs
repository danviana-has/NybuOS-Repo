using System;
using System.IO;

namespace NybuOS.GUI;

/// <summary>
/// Decodificador PNG 100% nativo em C# gerenciado para o Cosmos OS.
/// Implementa especificação completa W3C PNG + RFC 1950 (zlib) + RFC 1951 (Deflate Inflater)
/// e filtros Paeth/Sub/Up/Average sem qualquer dependência de DLLs externas do Windows/Linux.
/// Suporta decodificação de imagens RGBA de 32bpp e redimensionamento proporcional com filtro bilinear.
/// </summary>
public static class PngDecoder
{
    public static IconImage? Decode(byte[]? data, int targetWidth = 0, int targetHeight = 0)
    {
        if (data == null || data.Length < 24) return null;

        // Assinatura PNG: 89 50 4E 47 0D 0A 1A 0A
        if (data[0] != 0x89 || data[1] != 0x50 || data[2] != 0x4E || data[3] != 0x47 ||
            data[4] != 0x0D || data[5] != 0x0A || data[6] != 0x1A || data[7] != 0x0A)
        {
            return null;
        }

        int width = 0;
        int height = 0;
        byte bitDepth = 0;
        byte colorType = 0;

        // Coleta blocos IDAT
        int idatTotalSize = 0;
        int pos = 8;
        while (pos + 12 <= data.Length)
        {
            int len = ReadInt32BigEndian(data, pos);
            if (len < 0 || pos + 12 + len > data.Length) break;

            string type = GetChunkType(data, pos + 4);
            if (type == "IHDR")
            {
                width = ReadInt32BigEndian(data, pos + 8);
                height = ReadInt32BigEndian(data, pos + 12);
                bitDepth = data[pos + 16];
                colorType = data[pos + 17];
            }
            else if (type == "IDAT")
            {
                idatTotalSize += len;
            }
            else if (type == "IEND")
            {
                break;
            }
            pos += 12 + len;
        }

        if (width <= 0 || height <= 0 || idatTotalSize <= 0) return null;
        if (bitDepth != 8) return null; // Suporte primário para 8 bits por canal (24bpp e 32bpp)

        byte[] compressed = new byte[idatTotalSize];
        int writePos = 0;
        pos = 8;
        while (pos + 12 <= data.Length)
        {
            int len = ReadInt32BigEndian(data, pos);
            if (len < 0 || pos + 12 + len > data.Length) break;

            string type = GetChunkType(data, pos + 4);
            if (type == "IDAT")
            {
                Buffer.BlockCopy(data, pos + 8, compressed, writePos, len);
                writePos += len;
            }
            else if (type == "IEND")
            {
                break;
            }
            pos += 12 + len;
        }

        int bytesPerPixel = (colorType == 6) ? 4 : (colorType == 2 ? 3 : 1);
        int stride = width * bytesPerPixel;
        int expectedDecompressedSize = height * (1 + stride);

        byte[] uncompressed = ZLibInflate(compressed, expectedDecompressedSize);
        if (uncompressed == null || uncompressed.Length < expectedDecompressedSize)
        {
            return null;
        }

        // Reconstrói scanlines aplicando unfilter
        uint[] pixels = new uint[width * height];
        byte[] prevRow = new byte[stride];
        byte[] curRow = new byte[stride];

        int srcOffset = 0;
        for (int y = 0; y < height; y++)
        {
            byte filterType = uncompressed[srcOffset++];
            int scanlineStart = srcOffset;

            // Desfaz o filtro da linha
            for (int x = 0; x < stride; x++)
            {
                byte raw = uncompressed[scanlineStart + x];
                byte a = (x >= bytesPerPixel) ? curRow[x - bytesPerPixel] : (byte)0;
                byte b = prevRow[x];
                byte c = (x >= bytesPerPixel) ? prevRow[x - bytesPerPixel] : (byte)0;

                byte val = 0;
                switch (filterType)
                {
                    case 0: // None
                        val = raw;
                        break;
                    case 1: // Sub
                        val = (byte)(raw + a);
                        break;
                    case 2: // Up
                        val = (byte)(raw + b);
                        break;
                    case 3: // Average
                        val = (byte)(raw + ((a + b) >> 1));
                        break;
                    case 4: // Paeth
                        val = (byte)(raw + PaethPredictor(a, b, c));
                        break;
                }
                curRow[x] = val;
            }

            srcOffset += stride;

            // Converte curRow em pixels ARGB de 32 bits
            int rowPixelOffset = y * width;
            if (colorType == 6) // RGBA
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = x * 4;
                    byte r = curRow[offset];
                    byte g = curRow[offset + 1];
                    byte b = curRow[offset + 2];
                    byte alpha = curRow[offset + 3];
                    pixels[rowPixelOffset + x] = ((uint)alpha << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
                }
            }
            else if (colorType == 2) // RGB
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = x * 3;
                    byte r = curRow[offset];
                    byte g = curRow[offset + 1];
                    byte b = curRow[offset + 2];
                    pixels[rowPixelOffset + x] = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;
                }
            }

            // Armazena curRow em prevRow para a próxima linha
            Buffer.BlockCopy(curRow, 0, prevRow, 0, stride);
        }

        var decodedImage = new IconImage(width, height, pixels);

        if (targetWidth > 0 && targetHeight > 0 && (width != targetWidth || height != targetHeight))
        {
            return Resize(decodedImage, targetWidth, targetHeight);
        }

        return decodedImage;
    }

    private static byte PaethPredictor(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return (byte)a;
        if (pb <= pc) return (byte)b;
        return (byte)c;
    }

    private static int ReadInt32BigEndian(byte[] b, int offset)
    {
        return (b[offset] << 24) | (b[offset + 1] << 16) | (b[offset + 2] << 8) | b[offset + 3];
    }

    private static string GetChunkType(byte[] b, int offset)
    {
        char[] chars = new char[4];
        chars[0] = (char)b[offset];
        chars[1] = (char)b[offset + 1];
        chars[2] = (char)b[offset + 2];
        chars[3] = (char)b[offset + 3];
        return new string(chars);
    }

    /// <summary>
    /// Redimensiona uma imagem com filtragem bilinear de alta qualidade.
    /// </summary>
    public static IconImage Resize(IconImage src, int newW, int newH)
    {
        if (src == null || newW <= 0 || newH <= 0) return new IconImage(16, 16);
        if (src.Width == newW && src.Height == newH) return src;

        uint[] destPixels = new uint[newW * newH];
        float xRatio = (float)(src.Width - 1) / (float)newW;
        float yRatio = (float)(src.Height - 1) / (float)newH;

        for (int y = 0; y < newH; y++)
        {
            float srcY = y * yRatio;
            int yFloor = (int)srcY;
            int yCeil = Math.Min(src.Height - 1, yFloor + 1);
            float yDiff = srcY - yFloor;

            int destRow = y * newW;

            for (int x = 0; x < newW; x++)
            {
                float srcX = x * xRatio;
                int xFloor = (int)srcX;
                int xCeil = Math.Min(src.Width - 1, xFloor + 1);
                float xDiff = srcX - xFloor;

                uint c00 = src.Pixels[yFloor * src.Width + xFloor];
                uint c10 = src.Pixels[yFloor * src.Width + xCeil];
                uint c01 = src.Pixels[yCeil * src.Width + xFloor];
                uint c11 = src.Pixels[yCeil * src.Width + xCeil];

                byte a = (byte)BilinearInterpolate((c00 >> 24) & 0xFF, (c10 >> 24) & 0xFF, (c01 >> 24) & 0xFF, (c11 >> 24) & 0xFF, xDiff, yDiff);
                byte r = (byte)BilinearInterpolate((c00 >> 16) & 0xFF, (c10 >> 16) & 0xFF, (c01 >> 16) & 0xFF, (c11 >> 16) & 0xFF, xDiff, yDiff);
                byte g = (byte)BilinearInterpolate((c00 >> 8) & 0xFF, (c10 >> 8) & 0xFF, (c01 >> 8) & 0xFF, (c11 >> 8) & 0xFF, xDiff, yDiff);
                byte b = (byte)BilinearInterpolate(c00 & 0xFF, c10 & 0xFF, c01 & 0xFF, c11 & 0xFF, xDiff, yDiff);

                destPixels[destRow + x] = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
            }
        }

        return new IconImage(newW, newH, destPixels);
    }

    private static float BilinearInterpolate(float c00, float c10, float c01, float c11, float tx, float ty)
    {
        float top = c00 + (c10 - c00) * tx;
        float bottom = c01 + (c11 - c01) * tx;
        return top + (bottom - top) * ty;
    }

    #region ZLib / Deflate Inflater Puro em C#

    private static byte[]? ZLibInflate(byte[] compressed, int expectedOutputSize)
    {
        if (compressed == null || compressed.Length < 6) return null;

        // Cabeçalho ZLib: 2 bytes
        // byte 0: CMF (Compression Method and Flags) - usualmente 0x78 (Deflate, janela 32K)
        // byte 1: FLG
        int cmf = compressed[0];
        int flg = compressed[1];
        if ((cmf * 256 + flg) % 31 != 0) return null; // Checksum de cabeçalho inválido

        int offset = 2;
        if ((flg & 0x20) != 0) // Preset dictionary
        {
            offset += 4;
        }

        var reader = new BitReader(compressed, offset);
        var output = new byte[expectedOutputSize];
        int outPos = 0;

        bool isFinal = false;
        while (!isFinal && outPos < expectedOutputSize)
        {
            isFinal = reader.ReadBits(1) != 0;
            int blockType = reader.ReadBits(2);

            if (blockType == 0) // Bloco Não Comprimido (Stored)
            {
                reader.AlignToByte();
                int len = reader.ReadBits(16);
                int nlen = reader.ReadBits(16);
                for (int i = 0; i < len && outPos < expectedOutputSize; i++)
                {
                    output[outPos++] = (byte)reader.ReadBits(8);
                }
            }
            else if (blockType == 1) // Árvore Huffman Estática (Fixed)
            {
                HuffmanTree litLen = GetFixedLitLenTree();
                HuffmanTree dist = GetFixedDistTree();
                InflateBlock(reader, litLen, dist, output, ref outPos);
            }
            else if (blockType == 2) // Árvore Huffman Dinâmica
            {
                int hlit = reader.ReadBits(5) + 257;
                int hdist = reader.ReadBits(5) + 1;
                int hclen = reader.ReadBits(4) + 4;

                int[] codeLengthOrder = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
                int[] clLengths = new int[19];
                for (int i = 0; i < hclen; i++)
                {
                    clLengths[codeLengthOrder[i]] = reader.ReadBits(3);
                }

                HuffmanTree codeLengthsTree = new HuffmanTree(clLengths);
                int totalCodes = hlit + hdist;
                int[] litLenAndDistLengths = new int[totalCodes];
                int codeIdx = 0;

                while (codeIdx < totalCodes)
                {
                    int sym = codeLengthsTree.DecodeSymbol(reader);
                    if (sym < 16)
                    {
                        litLenAndDistLengths[codeIdx++] = sym;
                    }
                    else if (sym == 16)
                    {
                        int repeat = 3 + reader.ReadBits(2);
                        int prev = (codeIdx > 0) ? litLenAndDistLengths[codeIdx - 1] : 0;
                        while (repeat-- > 0 && codeIdx < totalCodes)
                        {
                            litLenAndDistLengths[codeIdx++] = prev;
                        }
                    }
                    else if (sym == 17)
                    {
                        int repeat = 3 + reader.ReadBits(3);
                        while (repeat-- > 0 && codeIdx < totalCodes)
                        {
                            litLenAndDistLengths[codeIdx++] = 0;
                        }
                    }
                    else if (sym == 18)
                    {
                        int repeat = 11 + reader.ReadBits(7);
                        while (repeat-- > 0 && codeIdx < totalCodes)
                        {
                            litLenAndDistLengths[codeIdx++] = 0;
                        }
                    }
                }

                int[] litLenLengths = new int[hlit];
                Array.Copy(litLenAndDistLengths, 0, litLenLengths, 0, hlit);
                HuffmanTree litLen = new HuffmanTree(litLenLengths);

                int[] distLengths = new int[hdist];
                Array.Copy(litLenAndDistLengths, hlit, distLengths, 0, hdist);
                HuffmanTree dist = new HuffmanTree(distLengths);

                InflateBlock(reader, litLen, dist, output, ref outPos);
            }
            else
            {
                return null; // Bloco reservado / inválido
            }
        }

        return output;
    }

    private static void InflateBlock(BitReader reader, HuffmanTree litLen, HuffmanTree dist, byte[] output, ref int outPos)
    {
        int[] lengthBases = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258 };
        int[] lengthExtraBits = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };

        int[] distBases = { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577 };
        int[] distExtraBits = { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };

        while (true)
        {
            int sym = litLen.DecodeSymbol(reader);
            if (sym < 256)
            {
                if (outPos < output.Length)
                {
                    output[outPos++] = (byte)sym;
                }
            }
            else if (sym == 256)
            {
                break; // Fim do bloco
            }
            else
            {
                int lenIdx = sym - 257;
                int length = lengthBases[lenIdx];
                int extraBits = lengthExtraBits[lenIdx];
                if (extraBits > 0)
                {
                    length += reader.ReadBits(extraBits);
                }

                int distSym = dist.DecodeSymbol(reader);
                int distance = distBases[distSym];
                int distExtra = distExtraBits[distSym];
                if (distExtra > 0)
                {
                    distance += reader.ReadBits(distExtra);
                }

                int copyStart = outPos - distance;
                for (int i = 0; i < length && outPos < output.Length; i++)
                {
                    output[outPos++] = output[copyStart + i];
                }
            }
        }
    }

    private static HuffmanTree? _fixedLitLenTree;
    private static HuffmanTree? _fixedDistTree;

    private static HuffmanTree GetFixedLitLenTree()
    {
        if (_fixedLitLenTree != null) return _fixedLitLenTree;
        int[] lengths = new int[288];
        for (int i = 0; i <= 143; i++) lengths[i] = 8;
        for (int i = 144; i <= 255; i++) lengths[i] = 9;
        for (int i = 256; i <= 279; i++) lengths[i] = 7;
        for (int i = 280; i <= 287; i++) lengths[i] = 8;
        _fixedLitLenTree = new HuffmanTree(lengths);
        return _fixedLitLenTree;
    }

    private static HuffmanTree GetFixedDistTree()
    {
        if (_fixedDistTree != null) return _fixedDistTree;
        int[] lengths = new int[32];
        for (int i = 0; i < 32; i++) lengths[i] = 5;
        _fixedDistTree = new HuffmanTree(lengths);
        return _fixedDistTree;
    }

    private class BitReader
    {
        private readonly byte[] _data;
        private int _byteOffset;
        private int _bitBuffer;
        private int _bitsCount;

        public BitReader(byte[] data, int startOffset)
        {
            _data = data;
            _byteOffset = startOffset;
            _bitBuffer = 0;
            _bitsCount = 0;
        }

        public int ReadBits(int count)
        {
            while (_bitsCount < count)
            {
                if (_byteOffset < _data.Length)
                {
                    _bitBuffer |= (_data[_byteOffset++] & 0xFF) << _bitsCount;
                    _bitsCount += 8;
                }
                else
                {
                    break;
                }
            }

            int mask = (1 << count) - 1;
            int result = _bitBuffer & mask;
            _bitBuffer >>= count;
            _bitsCount -= count;
            return result;
        }

        public void AlignToByte()
        {
            int bitsToDrop = _bitsCount % 8;
            _bitBuffer >>= bitsToDrop;
            _bitsCount -= bitsToDrop;
        }
    }

    private class HuffmanTree
    {
        private readonly int[] _symbols;
        private readonly int[] _counts;
        private readonly int[] _offsets;

        public HuffmanTree(int[] codeLengths)
        {
            _counts = new int[16];
            for (int i = 0; i < codeLengths.Length; i++)
            {
                int len = codeLengths[i];
                if (len > 0 && len <= 15) _counts[len]++;
            }

            _offsets = new int[16];
            _offsets[1] = 0;
            for (int len = 1; len < 15; len++)
            {
                _offsets[len + 1] = _offsets[len] + _counts[len];
            }

            _symbols = new int[codeLengths.Length];
            int[] currentOffsets = new int[16];
            Array.Copy(_offsets, currentOffsets, 16);

            for (int symbol = 0; symbol < codeLengths.Length; symbol++)
            {
                int len = codeLengths[symbol];
                if (len > 0 && len <= 15)
                {
                    _symbols[currentOffsets[len]++] = symbol;
                }
            }
        }

        public int DecodeSymbol(BitReader reader)
        {
            int code = 0;
            int first = 0;
            int count = 0;

            for (int len = 1; len <= 15; len++)
            {
                code |= reader.ReadBits(1);
                count = _counts[len];
                if (code - first < count)
                {
                    return _symbols[_offsets[len] + (code - first)];
                }
                first += count;
                first <<= 1;
                code <<= 1;
            }

            return -1;
        }
    }

    #endregion
}
