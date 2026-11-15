using System;

namespace NybuOS.Core.Graphics;

/// <summary>
/// High performance pure C# baseline JPEG decoder for bare-metal OS.
/// Decodes JFIF/EXIF baseline DCT images into 32-bit ARGB bitmaps.
/// </summary>
public static class JpegDecoder
{
    private static readonly int[] ZAG = {
         0,  1,  8, 16,  9,  2,  3, 10,
        17, 24, 32, 25, 18, 11,  4,  5,
        12, 19, 26, 33, 40, 48, 41, 34,
        27, 20, 13,  6,  7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36,
        29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46,
        53, 60, 61, 54, 47, 55, 62, 63
    };

    private sealed class Component
    {
        public int Id;
        public int HFactor;
        public int VFactor;
        public int QTableId;
        public int DcTableId;
        public int AcTableId;
        public int DcPred;
    }

    private sealed class HuffmanTable
    {
        public readonly int[] LookAhead = new int[256];
        public readonly int[] MinCode = new int[17];
        public readonly int[] MaxCode = new int[17];
        public readonly int[] ValPtr = new int[17];
        public byte[] Values = Array.Empty<byte>();
    }

    private sealed class BitReader
    {
        private readonly byte[] _data;
        private int _pos;
        private uint _buf;
        private int _bits;

        public BitReader(byte[] data, int pos)
        {
            _data = data;
            _pos = pos;
            _buf = 0;
            _bits = 0;
        }

        public int ReadBit()
        {
            EnsureBits(1);
            int bit = (int)((_buf >> (_bits - 1)) & 1);
            _bits--;
            return bit;
        }

        public int ReadBits(int n)
        {
            if (n == 0) return 0;
            EnsureBits(n);
            int val = (int)((_buf >> (_bits - n)) & ((1u << n) - 1));
            _bits -= n;
            return val;
        }

        private void EnsureBits(int n)
        {
            while (_bits < n)
            {
                byte b = 0;
                if (_pos < _data.Length)
                {
                    b = _data[_pos++];
                    if (b == 0xFF && _pos < _data.Length && _data[_pos] == 0x00) _pos++; // Byte stuffing
                }
                _buf = (_buf << 8) | b;
                _bits += 8;
            }
        }

        public int PeekBits(int n)
        {
            EnsureBits(n);
            return (int)((_buf >> (_bits - n)) & ((1u << n) - 1));
        }

        public void SkipBits(int n) => _bits -= n;
    }

    public static FastBitmap? Decode(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4) return null;

        int pos = 0;
        if (bytes[pos++] != 0xFF || bytes[pos++] != 0xD8) return null; // SOI check

        int[][] qTables = new int[4][];
        HuffmanTable[] dcTables = new HuffmanTable[4];
        HuffmanTable[] acTables = new HuffmanTable[4];

        int width = 0;
        int height = 0;
        int numComponents = 0;
        Component[]? comp = null;
        int restartInterval = 0;

        while (pos < bytes.Length)
        {
            if (bytes[pos++] != 0xFF) continue;
            while (pos < bytes.Length && bytes[pos] == 0xFF) pos++;
            if (pos >= bytes.Length) break;
            byte marker = bytes[pos++];
            if (marker == 0xD9) break; // EOI
            if (marker == 0xD8) continue; // SOI

            int len = (bytes[pos] << 8) | bytes[pos + 1];
            int nextMarkerPos = pos + len;
            pos += 2;

            if (marker == 0xDB) // DQT
            {
                int end = pos + len - 2;
                while (pos < end)
                {
                    int info = bytes[pos++];
                    int id = info & 0x0F;
                    int prec = info >> 4;
                    qTables[id] = new int[64];
                    for (int i = 0; i < 64; i++)
                    {
                        if (prec == 0) qTables[id][ZAG[i]] = bytes[pos++];
                        else { qTables[id][ZAG[i]] = (bytes[pos] << 8) | bytes[pos + 1]; pos += 2; }
                    }
                }
            }
            else if (marker == 0xC0) // SOF0
            {
                pos++; // precision
                height = (bytes[pos] << 8) | bytes[pos + 1]; pos += 2;
                width = (bytes[pos] << 8) | bytes[pos + 1]; pos += 2;
                numComponents = bytes[pos++];
                comp = new Component[numComponents];
                for (int i = 0; i < numComponents; i++)
                {
                    comp[i] = new Component();
                    comp[i].Id = bytes[pos++];
                    int factor = bytes[pos++];
                    comp[i].HFactor = factor >> 4;
                    comp[i].VFactor = factor & 0x0F;
                    comp[i].QTableId = bytes[pos++];
                }
            }
            else if (marker == 0xC4) // DHT
            {
                int end = pos + len - 2;
                while (pos < end)
                {
                    int info = bytes[pos++];
                    int id = info & 0x0F;
                    int cls = info >> 4;
                    byte[] counts = new byte[16];
                    for (int i = 0; i < 16; i++) counts[i] = bytes[pos++];
                    int totalCodes = 0;
                    for (int i = 0; i < 16; i++) totalCodes += counts[i];
                    byte[] symbols = new byte[totalCodes];
                    for (int i = 0; i < totalCodes; i++) symbols[i] = bytes[pos++];
                    var ht = BuildHuffman(counts, symbols);
                    if (cls == 0) dcTables[id] = ht;
                    else acTables[id] = ht;
                }
            }
            else if (marker == 0xDD) // DRI
            {
                restartInterval = (bytes[pos] << 8) | bytes[pos + 1];
                pos += 2;
            }
            else if (marker == 0xDA) // SOS
            {
                if (comp == null) return null;
                int nComp = bytes[pos++];
                for (int i = 0; i < nComp; i++)
                {
                    int cId = bytes[pos++];
                    int tableId = bytes[pos++];
                    for (int c = 0; c < numComponents; c++)
                    {
                        if (comp[c].Id == cId)
                        {
                            comp[c].DcTableId = tableId >> 4;
                            comp[c].AcTableId = tableId & 0x0F;
                            break;
                        }
                    }
                }
                pos += 3;
                return DecodeScan(bytes, pos, width, height, comp, qTables, dcTables, acTables, restartInterval);
            }
            pos = nextMarkerPos;
        }

        return null;
    }

    private static HuffmanTable BuildHuffman(byte[] counts, byte[] symbols)
    {
        var ht = new HuffmanTable();
        ht.Values = symbols;
        for (int i = 0; i < 256; i++) ht.LookAhead[i] = -1;

        int code = 0;
        int k = 0;
        for (int l = 1; l <= 16; l++)
        {
            if (counts[l - 1] == 0)
            {
                ht.MinCode[l] = -1;
                ht.MaxCode[l] = -1;
                ht.ValPtr[l] = -1;
            }
            else
            {
                ht.ValPtr[l] = k;
                ht.MinCode[l] = code;
                for (int i = 0; i < counts[l - 1]; i++)
                {
                    if (l <= 8)
                    {
                        int shift = 8 - l;
                        int first = code << shift;
                        int last = first + (1 << shift);
                        for (int idx = first; idx < last; idx++)
                        {
                            ht.LookAhead[idx] = (l << 8) | symbols[k];
                        }
                    }
                    code++;
                    k++;
                }
                ht.MaxCode[l] = code - 1;
            }
            code <<= 1;
        }
        return ht;
    }

    private static int DecodeHuffman(BitReader reader, HuffmanTable table)
    {
        int peek = reader.PeekBits(8);
        int entry = table.LookAhead[peek];
        if (entry != -1)
        {
            reader.SkipBits(entry >> 8);
            return entry & 0xFF;
        }
        reader.SkipBits(8);
        int code = peek;
        for (int l = 9; l <= 16; l++)
        {
            code = (code << 1) | reader.ReadBit();
            if (table.MaxCode[l] != -1 && code <= table.MaxCode[l])
            {
                return table.Values[table.ValPtr[l] + (code - table.MinCode[l])];
            }
        }
        return 0;
    }

    private static int Extend(int val, int n)
    {
        int vt = 1 << (n - 1);
        if (val < vt) return val + ((-1 << n) + 1);
        return val;
    }

    private static FastBitmap DecodeScan(byte[] data, int startPos, int width, int height, Component[] comp, int[][] qTables, HuffmanTable[] dcTables, HuffmanTable[] acTables, int restartInterval)
    {
        var reader = new BitReader(data, startPos);
        int maxH = 1, maxV = 1;
        for (int i = 0; i < comp.Length; i++)
        {
            if (comp[i].HFactor > maxH) maxH = comp[i].HFactor;
            if (comp[i].VFactor > maxV) maxV = comp[i].VFactor;
        }

        int mcuWidth = maxH * 8;
        int mcuHeight = maxV * 8;
        int mcusX = (width + mcuWidth - 1) / mcuWidth;
        int mcusY = (height + mcuHeight - 1) / mcuHeight;

        var result = new FastBitmap(width, height);
        uint[] output = result.Pixels;

        int[][] blockY = new int[maxH * maxV][];
        int[][] blockCb = new int[comp.Length > 1 ? comp[1].HFactor * comp[1].VFactor : 1][];
        int[][] blockCr = new int[comp.Length > 2 ? comp[2].HFactor * comp[2].VFactor : 1][];

        for (int i = 0; i < blockY.Length; i++) blockY[i] = new int[64];
        for (int i = 0; i < blockCb.Length; i++) blockCb[i] = new int[64];
        for (int i = 0; i < blockCr.Length; i++) blockCr[i] = new int[64];

        for (int my = 0; my < mcusY; my++)
        {
            for (int mx = 0; mx < mcusX; mx++)
            {
                for (int b = 0; b < blockY.Length; b++)
                {
                    DecodeBlock(reader, comp[0], qTables[comp[0].QTableId], dcTables[comp[0].DcTableId], acTables[comp[0].AcTableId], blockY[b]);
                    Idct(blockY[b]);
                }
                if (comp.Length > 1)
                {
                    for (int b = 0; b < blockCb.Length; b++)
                    {
                        DecodeBlock(reader, comp[1], qTables[comp[1].QTableId], dcTables[comp[1].DcTableId], acTables[comp[1].AcTableId], blockCb[b]);
                        Idct(blockCb[b]);
                    }
                }
                if (comp.Length > 2)
                {
                    for (int b = 0; b < blockCr.Length; b++)
                    {
                        DecodeBlock(reader, comp[2], qTables[comp[2].QTableId], dcTables[comp[2].DcTableId], acTables[comp[2].AcTableId], blockCr[b]);
                        Idct(blockCr[b]);
                    }
                }

                int startX = mx * mcuWidth;
                int startY = my * mcuHeight;

                for (int py = 0; py < mcuHeight; py++)
                {
                    int yPixel = startY + py;
                    if (yPixel >= height) break;
                    int yBlockIdx = (py / 8) * maxH;
                    int yInBlock = py % 8;

                    for (int px = 0; px < mcuWidth; px++)
                    {
                        int xPixel = startX + px;
                        if (xPixel >= width) break;

                        int bx = px / 8;
                        int xInBlock = px % 8;
                        int ySample = blockY[yBlockIdx + bx][yInBlock * 8 + xInBlock];

                        int cbSample = 0, crSample = 0;
                        if (comp.Length > 1)
                        {
                            int cbX = px * comp[1].HFactor / maxH;
                            int cbY = py * comp[1].VFactor / maxV;
                            int cbBlock = (cbY / 8) * comp[1].HFactor + (cbX / 8);
                            cbSample = blockCb[cbBlock][(cbY % 8) * 8 + (cbX % 8)];
                        }
                        if (comp.Length > 2)
                        {
                            int crX = px * comp[2].HFactor / maxH;
                            int crY = py * comp[2].VFactor / maxV;
                            int crBlock = (crY / 8) * comp[2].HFactor + (crX / 8);
                            crSample = blockCr[crBlock][(crY % 8) * 8 + (crX % 8)];
                        }

                        int r = Clamp((int)(ySample + 1.402f * crSample + 128));
                        int g = Clamp((int)(ySample - 0.344136f * cbSample - 0.714136f * crSample + 128));
                        int b = Clamp((int)(ySample + 1.772f * cbSample + 128));

                        output[yPixel * width + xPixel] = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
                    }
                }
            }
        }
        return result;
    }

    private static void DecodeBlock(BitReader reader, Component comp, int[] qTable, HuffmanTable dcTable, HuffmanTable acTable, int[] block)
    {
        for (int i = 0; i < 64; i++) block[i] = 0;

        int s = DecodeHuffman(reader, dcTable);
        int diff = 0;
        if (s > 0)
        {
            int bits = reader.ReadBits(s);
            diff = Extend(bits, s);
        }
        comp.DcPred += diff;
        block[0] = comp.DcPred * qTable[0];

        int k = 1;
        while (k < 64)
        {
            int rs = DecodeHuffman(reader, acTable);
            int r = rs >> 4;
            s = rs & 0x0F;
            if (s == 0)
            {
                if (r != 15) break;
                k += 16;
                continue;
            }
            k += r;
            if (k >= 64) break;
            int bits = reader.ReadBits(s);
            int val = Extend(bits, s);
            block[ZAG[k]] = val * qTable[ZAG[k]];
            k++;
        }
    }

    private static void Idct(int[] block)
    {
        float[] temp = new float[64];
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                float sum = 0;
                for (int k = 0; k < 8; k++)
                {
                    float c = (k == 0) ? 0.70710678f : 1.0f;
                    sum += c * block[k * 8 + i] * MathF.Cos((2 * j + 1) * k * MathF.PI / 16.0f);
                }
                temp[j * 8 + i] = sum * 0.5f;
            }
        }
        for (int j = 0; j < 8; j++)
        {
            for (int i = 0; i < 8; i++)
            {
                float sum = 0;
                for (int k = 0; k < 8; k++)
                {
                    float c = (k == 0) ? 0.70710678f : 1.0f;
                    sum += c * temp[j * 8 + k] * MathF.Cos((2 * i + 1) * k * MathF.PI / 16.0f);
                }
                block[j * 8 + i] = (int)MathF.Round(sum * 0.5f);
            }
        }
    }

    private static int Clamp(int val)
    {
        if (val < 0) return 0;
        if (val > 255) return 255;
        return val;
    }
}
