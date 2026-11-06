using System;
using System.IO;

namespace NybuOS.GUI;

/// <summary>
/// Decodificador JPEG (JPG) 100% nativo em C# gerenciado para o Cosmos OS.
/// Implementa especificação completa ITU-T T.81 / ISO 10918-1 para imagens Baseline DCT (SOF0)
/// com suporte a YCbCr 4:2:0, 4:2:2 e 4:4:4, tabelas de Huffman dinâmicas e amostragem bilinear,
/// sem qualquer dependência de bibliotecas externas do Windows ou Linux.
/// </summary>
public static class JpgDecoder
{
    public static IconImage? Decode(byte[]? data, int targetWidth = 0, int targetHeight = 0)
    {
        if (data == null || data.Length < 30) return null;

        // Marcador SOI: FF D8
        if (data[0] != 0xFF || data[1] != 0xD8) return null;

        try
        {
            var decoder = new JpegInternalDecoder(data);
            var image = decoder.Decode();
            if (image == null) return null;

            if (targetWidth > 0 && targetHeight > 0 && (image.Width != targetWidth || image.Height != targetHeight))
            {
                return PngDecoder.Resize(image, targetWidth, targetHeight);
            }

            return image;
        }
        catch
        {
            return null;
        }
    }

    private class JpegInternalDecoder
    {
        private readonly byte[] _data;
        private int _pos;

        private int _width;
        private int _height;
        private int _componentsCount;

        private readonly int[][] _qTables = new int[4][];
        private readonly HuffmanTable?[] _dcTables = new HuffmanTable?[4];
        private readonly HuffmanTable?[] _acTables = new HuffmanTable?[4];

        private class ComponentInfo
        {
            public int Id;
            public int HFactor;
            public int VFactor;
            public int QTableId;
            public int DcTableId;
            public int AcTableId;
            public int PreviousDc;
        }

        private ComponentInfo[] _components = Array.Empty<ComponentInfo>();
        private ComponentInfo[] _scanOrder = Array.Empty<ComponentInfo>();
        private int _restartInterval = 0;

        // Buffers reutilizáveis para zerar alocações de GC durante a decodificação de blocos
        private readonly int[] _blockCoeff = new int[64];
        private readonly float[] _idctTemp = new float[64];

        // Tabela estática pré-computada de cossenos para IDCT ultrarrápida
        private static readonly float[,] CosTable = new float[8, 8];

        static JpegInternalDecoder()
        {
            for (int u = 0; u < 8; u++)
            {
                float alpha = (u == 0) ? 0.35355339f : 0.5f;
                for (int x = 0; x < 8; x++)
                {
                    CosTable[u, x] = alpha * MathF.Cos(((2 * x + 1) * u * MathF.PI) / 16f);
                }
            }
        }

        public JpegInternalDecoder(byte[] data)
        {
            _data = data;
            _pos = 2; // Pula SOI
        }

        public IconImage? Decode()
        {
            while (_pos < _data.Length - 1)
            {
                if (_data[_pos] != 0xFF)
                {
                    _pos++;
                    continue;
                }

                while (_pos < _data.Length && _data[_pos] == 0xFF) _pos++;
                if (_pos >= _data.Length) break;

                byte marker = _data[_pos++];

                if (marker == 0xD9) // EOI
                {
                    break;
                }
                else if (marker == 0xDB) // DQT (Define Quantization Table)
                {
                    ParseDQT();
                }
                else if (marker == 0xC0) // SOF0 (Baseline DCT)
                {
                    ParseSOF0();
                }
                else if (marker == 0xC4) // DHT (Define Huffman Table)
                {
                    ParseDHT();
                }
                else if (marker == 0xDD) // DRI (Define Restart Interval)
                {
                    int len = ReadInt16();
                    _restartInterval = ReadInt16();
                }
                else if (marker == 0xDA) // SOS (Start of Scan)
                {
                    ParseSOS();
                    return DecodeScan();
                }
                else if (marker >= 0xE0 && marker <= 0xEF) // APPn
                {
                    int len = ReadInt16();
                    _pos += len - 2;
                }
                else if (marker == 0xFE) // COM
                {
                    int len = ReadInt16();
                    _pos += len - 2;
                }
                else
                {
                    // Marcador desconhecido com tamanho
                    if (_pos + 2 <= _data.Length)
                    {
                        int len = ReadInt16();
                        if (len >= 2) _pos += len - 2;
                    }
                }
            }

            return null;
        }

        private int ReadInt16()
        {
            int val = (_data[_pos] << 8) | _data[_pos + 1];
            _pos += 2;
            return val;
        }

        private void ParseDQT()
        {
            int len = ReadInt16();
            int end = _pos + len - 2;

            while (_pos < end)
            {
                byte info = _data[_pos++];
                int tableId = info & 0x0F;
                int precision = (info >> 4) & 0x0F;

                int[] table = new int[64];
                for (int i = 0; i < 64; i++)
                {
                    table[i] = (precision == 0) ? _data[_pos++] : ReadInt16();
                }
                if (tableId < 4)
                {
                    _qTables[tableId] = table;
                }
            }
        }

        private void ParseSOF0()
        {
            int len = ReadInt16();
            byte precision = _data[_pos++];
            _height = ReadInt16();
            _width = ReadInt16();
            _componentsCount = _data[_pos++];

            _components = new ComponentInfo[_componentsCount];
            for (int i = 0; i < _componentsCount; i++)
            {
                var comp = new ComponentInfo();
                comp.Id = _data[_pos++];
                byte factors = _data[_pos++];
                comp.HFactor = (factors >> 4) & 0x0F;
                comp.VFactor = factors & 0x0F;
                comp.QTableId = _data[_pos++];
                _components[i] = comp;
            }
        }

        private void ParseDHT()
        {
            int len = ReadInt16();
            int end = _pos + len - 2;

            while (_pos < end)
            {
                byte info = _data[_pos++];
                int tableClass = (info >> 4) & 0x0F; // 0 = DC, 1 = AC
                int tableId = info & 0x0F;

                byte[] counts = new byte[16];
                int totalSymbols = 0;
                for (int i = 0; i < 16; i++)
                {
                    counts[i] = _data[_pos++];
                    totalSymbols += counts[i];
                }

                byte[] symbols = new byte[totalSymbols];
                Buffer.BlockCopy(_data, _pos, symbols, 0, totalSymbols);
                _pos += totalSymbols;

                if (tableId < 4)
                {
                    var huff = new HuffmanTable(counts, symbols);
                    if (tableClass == 0)
                    {
                        _dcTables[tableId] = huff;
                    }
                    else
                    {
                        _acTables[tableId] = huff;
                    }
                }
            }
        }

        private void ParseSOS()
        {
            int len = ReadInt16();
            byte compCount = _data[_pos++];

            _scanOrder = new ComponentInfo[compCount];
            for (int i = 0; i < compCount; i++)
            {
                byte id = _data[_pos++];
                byte tableIds = _data[_pos++];

                for (int c = 0; c < _components.Length; c++)
                {
                    if (_components[c].Id == id)
                    {
                        _components[c].DcTableId = (tableIds >> 4) & 0x0F;
                        _components[c].AcTableId = tableIds & 0x0F;
                        _scanOrder[i] = _components[c];
                        break;
                    }
                }
            }

            _pos += 3; // Spectral selection start, end, approx
        }

        private IconImage DecodeScan()
        {
            int maxH = 1;
            int maxV = 1;
            for (int i = 0; i < _components.Length; i++)
            {
                if (_components[i].HFactor > maxH) maxH = _components[i].HFactor;
                if (_components[i].VFactor > maxV) maxV = _components[i].VFactor;
            }

            int mcuW = maxH * 8;
            int mcuH = maxV * 8;
            int mcusAcross = (_width + mcuW - 1) / mcuW;
            int mcusDown = (_height + mcuH - 1) / mcuH;

            var bitReader = new JpegBitReader(_data, _pos);
            uint[] pixels = new uint[_width * _height];

            int[][][] mcuBuffers = new int[_components.Length][][];
            for (int c = 0; c < _components.Length; c++)
            {
                int blocks = _components[c].HFactor * _components[c].VFactor;
                mcuBuffers[c] = new int[blocks][];
                for (int b = 0; b < blocks; b++)
                {
                    mcuBuffers[c][b] = new int[64];
                }
            }

            int mcuCount = 0;

            for (int my = 0; my < mcusDown; my++)
            {
                for (int mx = 0; mx < mcusAcross; mx++)
                {
                    if (_restartInterval > 0 && mcuCount > 0 && (mcuCount % _restartInterval) == 0)
                    {
                        bitReader.AlignToByte();
                        for (int c = 0; c < _components.Length; c++)
                        {
                            _components[c].PreviousDc = 0;
                        }
                    }

                    // Decodifica blocos de cada componente do scan na ordem correta
                    int scanLen = (_scanOrder.Length > 0) ? _scanOrder.Length : _components.Length;
                    for (int s = 0; s < scanLen; s++)
                    {
                        var comp = (_scanOrder.Length > 0) ? _scanOrder[s] : _components[s];
                        int cIdx = Array.IndexOf(_components, comp);
                        if (cIdx < 0) cIdx = s;

                        var dcTable = _dcTables[comp.DcTableId];
                        var acTable = _acTables[comp.AcTableId];
                        var qTable = _qTables[comp.QTableId];

                        if (dcTable == null || acTable == null || qTable == null) continue;

                        int blockCount = comp.HFactor * comp.VFactor;
                        for (int b = 0; b < blockCount; b++)
                        {
                            DecodeBlock(bitReader, dcTable, acTable, qTable, ref comp.PreviousDc, mcuBuffers[cIdx][b]);
                        }
                    }

                    // Renderiza o MCU nos pixels finais
                    RenderMCU(pixels, mcuBuffers, mx * mcuW, my * mcuH, mcuW, mcuH, maxH, maxV);
                    mcuCount++;
                }
            }

            return new IconImage(_width, _height, pixels);
        }

        // Tabela Zig-Zag oficial do padrão ITU-T T.81 / JPEG
        // Mapeia o índice linear da frequência k (0..63) para o índice espacial 8x8 (y * 8 + x)
        private static readonly int[] ZigZag = new int[]
        {
             0,  1,  8, 16,  9,  2,  3, 10,
            17, 24, 32, 25, 18, 11,  4,  5,
            12, 19, 26, 33, 40, 48, 41, 34,
            27, 20, 13,  6,  7, 14, 21, 28,
            35, 42, 49, 56, 57, 50, 43, 36,
            29, 22, 15, 23, 30, 37, 44, 51,
            58, 59, 52, 45, 38, 31, 39, 46,
            53, 60, 61, 54, 47, 55, 62, 63
        };

        private void DecodeBlock(JpegBitReader reader, HuffmanTable dcTable, HuffmanTable acTable, int[] qTable, ref int previousDc, int[] outputSpatial)
        {
            Array.Clear(_blockCoeff, 0, 64);

            // 1. Coeficiente DC
            int dcSize = dcTable.DecodeSymbol(reader);
            int dcDiff = (dcSize > 0) ? reader.ReadSignedBits(dcSize) : 0;
            previousDc += dcDiff;
            _blockCoeff[0] = previousDc * qTable[0];

            // 2. Coeficientes AC (1 a 63)
            int k = 1;
            while (k < 64)
            {
                int sym = acTable.DecodeSymbol(reader);
                if (sym == 0) // EOB (End of Block)
                {
                    break;
                }

                int run = (sym >> 4) & 0x0F;
                int size = sym & 0x0F;

                if (run > 0)
                {
                    k += run;
                    if (k >= 64) break;
                }

                if (size > 0)
                {
                    int val = reader.ReadSignedBits(size);
                    _blockCoeff[ZigZag[k]] = val * qTable[k];
                }

                k++;
            }

            // 3. IDCT rápida com tabela pré-calculada de cossenos
            FastIDCT(_blockCoeff, outputSpatial);
        }

        private void FastIDCT(int[] input, int[] output)
        {
            // Passada 1: Linhas
            for (int r = 0; r < 8; r++)
            {
                int r8 = r * 8;
                for (int x = 0; x < 8; x++)
                {
                    float sum = 0f;
                    for (int u = 0; u < 8; u++)
                    {
                        sum += input[r8 + u] * CosTable[u, x];
                    }
                    _idctTemp[r8 + x] = sum;
                }
            }

            // Passada 2: Colunas
            for (int c = 0; c < 8; c++)
            {
                for (int y = 0; y < 8; y++)
                {
                    float sum = 0f;
                    for (int v = 0; v < 8; v++)
                    {
                        sum += _idctTemp[v * 8 + c] * CosTable[v, y];
                    }
                    int val = (int)(sum + 128.5f);
                    if (val < 0) val = 0;
                    else if (val > 255) val = 255;
                    output[y * 8 + c] = val;
                }
            }
        }

        private void RenderMCU(uint[] pixels, int[][][] mcuBuffers, int startX, int startY, int mcuW, int mcuH, int maxH, int maxV)
        {
            bool isYCbCr = _components.Length >= 3;

            for (int y = 0; y < mcuH; y++)
            {
                int py = startY + y;
                if (py >= _height) break;
                int rowOffset = py * _width;

                for (int x = 0; x < mcuW; x++)
                {
                    int px = startX + x;
                    if (px >= _width) break;

                    int yVal = 0;
                    int cbVal = 128;
                    int crVal = 128;

                    // Amostragem de Y (Luminância)
                    int yBlockX = x / 8;
                    int yBlockY = y / 8;
                    int yBlockIdx = yBlockY * maxH + yBlockX;
                    if (yBlockIdx < mcuBuffers[0].Length)
                    {
                        int bx = x % 8;
                        int by = y % 8;
                        yVal = mcuBuffers[0][yBlockIdx][by * 8 + bx];
                    }

                    if (isYCbCr)
                    {
                        // Componentes Cb e Cr com suporte a sub-amostragem (4:2:0, 4:2:2, 4:4:4)
                        int cbX = (x * _components[1].HFactor) / maxH;
                        int cbY = (y * _components[1].VFactor) / maxV;
                        int cbBlockX = cbX / 8;
                        int cbBlockY = cbY / 8;
                        int cbBlockIdx = cbBlockY * _components[1].HFactor + cbBlockX;
                        if (cbBlockIdx < mcuBuffers[1].Length)
                        {
                            cbVal = mcuBuffers[1][cbBlockIdx][(cbY % 8) * 8 + (cbX % 8)];
                        }

                        int crX = (x * _components[2].HFactor) / maxH;
                        int crY = (y * _components[2].VFactor) / maxV;
                        int crBlockX = crX / 8;
                        int crBlockY = crY / 8;
                        int crBlockIdx = crBlockY * _components[2].HFactor + crBlockX;
                        if (crBlockIdx < mcuBuffers[2].Length)
                        {
                            crVal = mcuBuffers[2][crBlockIdx][(crY % 8) * 8 + (crX % 8)];
                        }
                    }

                    // Conversão oficial JFIF / ITU-T T.871 de YCbCr para RGB
                    int r = (int)(yVal + 1.402f * (crVal - 128));
                    int g = (int)(yVal - 0.344136f * (cbVal - 128) - 0.714136f * (crVal - 128));
                    int b = (int)(yVal + 1.772f * (cbVal - 128));

                    if (r < 0) r = 0; else if (r > 255) r = 255;
                    if (g < 0) g = 0; else if (g > 255) g = 255;
                    if (b < 0) b = 0; else if (b > 255) b = 255;

                    pixels[rowOffset + px] = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
                }
            }
        }
    }

    private class JpegBitReader
    {
        private readonly byte[] _data;
        private int _byteOffset;
        private int _bitBuffer;
        private int _bitsCount;

        public JpegBitReader(byte[] data, int startOffset)
        {
            _data = data;
            _byteOffset = startOffset;
            _bitBuffer = 0;
            _bitsCount = 0;
        }

        public int ReadBit()
        {
            if (_bitsCount == 0)
            {
                _bitBuffer = ReadNextByte();
                _bitsCount = 8;
            }

            _bitsCount--;
            return (_bitBuffer >> _bitsCount) & 1;
        }

        public int ReadBits(int count)
        {
            int result = 0;
            for (int i = 0; i < count; i++)
            {
                result = (result << 1) | ReadBit();
            }
            return result;
        }

        public int ReadSignedBits(int count)
        {
            int val = ReadBits(count);
            if (val < (1 << (count - 1)))
            {
                val -= (1 << count) - 1;
            }
            return val;
        }

        public void AlignToByte()
        {
            _bitsCount = 0;
            _bitBuffer = 0;
        }

        private byte ReadNextByte()
        {
            if (_byteOffset >= _data.Length) return 0;

            byte b = _data[_byteOffset++];
            if (b == 0xFF)
            {
                while (_byteOffset < _data.Length && _data[_byteOffset] == 0xFF)
                {
                    _byteOffset++;
                }

                if (_byteOffset < _data.Length)
                {
                    byte b2 = _data[_byteOffset++];
                    if (b2 == 0x00)
                    {
                        // Byte stuffing do JPEG (0xFF 0x00 representa o byte literal 0xFF)
                        return 0xFF;
                    }
                    else if (b2 >= 0xD0 && b2 <= 0xD7)
                    {
                        // Marcador de reinício (RST)
                        return 0;
                    }
                    else
                    {
                        // Marcador subsequente (ex: EOI 0xD9)
                        _byteOffset -= 2;
                        return 0;
                    }
                }
            }
            return b;
        }
    }

    /// <summary>
    /// Decodificador de Huffman em conformidade estrita com o padrão ITU-T T.81 / ISO 10918-1 (Anexo C e F).
    /// </summary>
    private class HuffmanTable
    {
        private readonly int[] _maxCode = new int[17];
        private readonly int[] _minCode = new int[17];
        private readonly int[] _valPtr = new int[17];
        private readonly int[] _symbols;

        public HuffmanTable(byte[] counts, byte[] symbols)
        {
            _symbols = new int[symbols.Length];
            for (int i = 0; i < symbols.Length; i++)
            {
                _symbols[i] = symbols[i];
            }

            // Geração da tabela de códigos canônicos HUFFCODE (Anexo C.2)
            int[] huffCode = new int[symbols.Length];
            int code = 0;
            int k = 0;
            for (int i = 1; i <= 16; i++)
            {
                int count = counts[i - 1];
                for (int j = 0; j < count; j++)
                {
                    huffCode[k++] = code++;
                }
                code <<= 1;
            }

            // Geração das tabelas do decodificador MAXCODE, MINCODE, VALPTR (Figura F.15)
            int jPtr = 0;
            for (int i = 1; i <= 16; i++)
            {
                int count = counts[i - 1];
                if (count == 0)
                {
                    _maxCode[i] = -1;
                }
                else
                {
                    _valPtr[i] = jPtr;
                    _minCode[i] = huffCode[jPtr];
                    jPtr += count;
                    _maxCode[i] = huffCode[jPtr - 1];
                }
            }
        }

        public int DecodeSymbol(JpegBitReader reader)
        {
            // Procedimento oficial DECODE (Figura F.16)
            int code = 0;
            for (int i = 1; i <= 16; i++)
            {
                code = (code << 1) | reader.ReadBit();
                if (code <= _maxCode[i])
                {
                    int index = _valPtr[i] + (code - _minCode[i]);
                    if (index >= 0 && index < _symbols.Length)
                    {
                        return _symbols[index];
                    }
                    return 0;
                }
            }
            return 0;
        }
    }
}
