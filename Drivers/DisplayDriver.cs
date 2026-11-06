using System;
using System.Drawing;
using Cosmos.Kernel.System.Graphics;
using Cosmos.Kernel.System.Graphics.Fonts;
using NybuOS.GUI;

namespace NybuOS.Drivers;

/// <summary>
/// Driver gráfico VBE / SVGA do NybuOS RV (Revolution) com resolução Full HD REAL (1920x1080),
/// renderização de superfícies de vidro fosco (Frosted Glass), botões circulares e tipografia precisa.
/// </summary>
public class DisplayDriver
{
    public const int DefaultWidth = 1920;
    public const int DefaultHeight = 1080;

    private Canvas _canvas;
    private IconImage? _activeWallpaper = null;

    public int Width => _canvas?.Width ?? DefaultWidth;
    public int Height => _canvas?.Height ?? DefaultHeight;
    public IconImage? ActiveWallpaper => _activeWallpaper;

    public float BrightnessMultiplier { get; set; } = 1.0f;

    public DisplayDriver()
    {
    }

    public void Initialize()
    {
        try
        {
            // Resolução Full HD REAL 1920x1080 32bpp
            var mode = new Mode(DefaultWidth, DefaultHeight, ColorDepth.ColorDepth32);
            _canvas = Canvas.GetFullScreen(mode);
        }
        catch
        {
            try
            {
                // Fallback para HD 1280x720 se o hipervisor tiver restrição de VRAM
                var mode = new Mode(1280, 720, ColorDepth.ColorDepth32);
                _canvas = Canvas.GetFullScreen(mode);
            }
            catch
            {
                var mode = new Mode(800, 600, ColorDepth.ColorDepth32);
                _canvas = Canvas.GetFullScreen(mode);
            }
        }

        Clear(Color.Black);
        Display();
    }

    public void Clear(Color color)
    {
        _canvas?.Clear(ApplyBrightness(color));
    }

    public void Display()
    {
        _canvas?.Display();
    }

    public void Swap()
    {
        _canvas?.Display();
    }

    public Color ApplyBrightness(Color c)
    {
        if (Math.Abs(BrightnessMultiplier - 1.0f) < 0.05f)
            return c;

        int r = (int)(c.R * BrightnessMultiplier);
        int g = (int)(c.G * BrightnessMultiplier);
        int b = (int)(c.B * BrightnessMultiplier);

        if (r > 255) r = 255; else if (r < 0) r = 0;
        if (g > 255) g = 255; else if (g < 0) g = 0;
        if (b > 255) b = 255; else if (b < 0) b = 0;

        return Color.FromArgb(r, g, b);
    }

    public void DrawPixel(int x, int y, Color color)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        _canvas?.DrawPoint(ApplyBrightness(color), x, y);
    }

    public void DrawLine(int x1, int y1, int x2, int y2, Color color)
    {
        if (_canvas == null) return;
        _canvas.DrawLine(ApplyBrightness(color), x1, y1, x2, y2);
    }

    public void DrawRectangle(int x, int y, int w, int h, Color color)
    {
        if (_canvas == null || w <= 0 || h <= 0) return;
        _canvas.DrawRectangle(ApplyBrightness(color), x, y, w, h);
    }

    public void DrawFilledRectangle(int x, int y, int w, int h, Color color)
    {
        if (_canvas == null || w <= 0 || h <= 0) return;
        _canvas.DrawFilledRectangle(ApplyBrightness(color), x, y, w, h);
    }

    /// <summary>
    /// Renderiza um círculo preenchido suave com borda opcional (usado nos botões circulares e indicadores).
    /// </summary>
    public void DrawFilledCircle(int cx, int cy, int radius, Color color, Color? border = null)
    {
        if (_canvas == null || radius <= 0) return;

        Color col = ApplyBrightness(color);
        int r2 = radius * radius;

        for (int dy = -radius; dy <= radius; dy++)
        {
            int py = cy + dy;
            if (py < 0 || py >= Height) continue;

            int dxLimit = (int)Math.Sqrt(r2 - dy * dy);
            for (int dx = -dxLimit; dx <= dxLimit; dx++)
            {
                int px = cx + dx;
                if (px >= 0 && px < Width)
                {
                    _canvas.DrawPoint(col, px, py);
                }
            }
        }

        if (border.HasValue)
        {
            DrawCircle(cx, cy, radius, border.Value);
        }
    }

    /// <summary>
    /// Renderiza a borda de um círculo usando o algoritmo de Bresenham para círculos.
    /// </summary>
    public void DrawCircle(int cx, int cy, int radius, Color color)
    {
        if (_canvas == null || radius <= 0) return;

        Color col = ApplyBrightness(color);
        int x = 0;
        int y = radius;
        int d = 3 - 2 * radius;

        DrawCircleOctants(cx, cy, x, y, col);
        while (y >= x)
        {
            x++;
            if (d > 0)
            {
                y--;
                d = d + 4 * (x - y) + 10;
            }
            else
            {
                d = d + 4 * x + 6;
            }
            DrawCircleOctants(cx, cy, x, y, col);
        }
    }

    private void DrawCircleOctants(int cx, int cy, int x, int y, Color color)
    {
        DrawPixel(cx + x, cy + y, color);
        DrawPixel(cx - x, cy + y, color);
        DrawPixel(cx + x, cy - y, color);
        DrawPixel(cx - x, cy - y, color);
        DrawPixel(cx + y, cy + x, color);
        DrawPixel(cx - y, cy + x, color);
        DrawPixel(cx + y, cy - x, color);
        DrawPixel(cx - y, cy - x, color);
    }

    /// <summary>
    /// Renderiza um botão circular estilo macOS (Traffic Light) com símbolo sutil no hover.
    /// </summary>
    public void DrawTrafficLight(int cx, int cy, int radius, Color fill, Color border, bool isHover = false, char symbol = '\0')
    {
        DrawFilledCircle(cx, cy, radius, fill, border);

        if (isHover && symbol != '\0')
        {
            Color symCol = Color.FromArgb(130, 0, 0, 0);
            int s = radius >= 7 ? 4 : 3;
            if (symbol == 'x')
            {
                DrawLine(cx - s, cy - s, cx + s, cy + s, symCol);
                DrawLine(cx + s, cy - s, cx - s, cy + s, symCol);
            }
            else if (symbol == '-')
            {
                DrawLine(cx - s, cy, cx + s, cy, symCol);
            }
            else if (symbol == '+')
            {
                DrawLine(cx - s, cy, cx + s, cy, symCol);
                DrawLine(cx, cy - s, cx, cy + s, symCol);
            }
        }
    }

    /// <summary>
    /// Renderiza uma superfície de vidro fosco (Frosted Glass) com elevada opacidade,
    /// mesclando o tom desejado com os pixels reais do wallpaper de fundo.
    /// </summary>
    public void DrawFrostedGlass(int x, int y, int w, int h, Color tintColor, int alpha = 215, int cornerRadius = 12)
    {
        if (_canvas == null || w <= 0 || h <= 0) return;

        int r2 = cornerRadius * cornerRadius;
        int tR = tintColor.R;
        int tG = tintColor.G;
        int tB = tintColor.B;

        for (int row = 0; row < h; row++)
        {
            int py = y + row;
            if (py < 0 || py >= Height) continue;

            for (int col = 0; col < w; col++)
            {
                int px = x + col;
                if (px < 0 || px >= Width) continue;

                // Checagem de cantos arredondados
                if (cornerRadius > 0)
                {
                    if (col < cornerRadius && row < cornerRadius)
                    {
                        int dx = cornerRadius - col;
                        int dy = cornerRadius - row;
                        if (dx * dx + dy * dy > r2) continue;
                    }
                    else if (col >= w - cornerRadius && row < cornerRadius)
                    {
                        int dx = col - (w - cornerRadius - 1);
                        int dy = cornerRadius - row;
                        if (dx * dx + dy * dy > r2) continue;
                    }
                    else if (col < cornerRadius && row >= h - cornerRadius)
                    {
                        int dx = cornerRadius - col;
                        int dy = row - (h - cornerRadius - 1);
                        if (dx * dx + dy * dy > r2) continue;
                    }
                    else if (col >= w - cornerRadius && row >= h - cornerRadius)
                    {
                        int dx = col - (w - cornerRadius - 1);
                        int dy = row - (h - cornerRadius - 1);
                        if (dx * dx + dy * dy > r2) continue;
                    }
                }

                // Brilho specular sutil de vidro na borda superior
                if (row == 0 || (row == 1 && col >= cornerRadius && col < w - cornerRadius))
                {
                    _canvas.DrawPoint(Color.FromArgb(180, 255, 255, 255), px, py);
                    continue;
                }

                if (_activeWallpaper != null && px < _activeWallpaper.Width && py < _activeWallpaper.Height)
                {
                    uint wallPx = _activeWallpaper.Pixels[py * _activeWallpaper.Width + px];
                    int wr = (int)((wallPx >> 16) & 0xFF);
                    int wg = (int)((wallPx >> 8) & 0xFF);
                    int wb = (int)(wallPx & 0xFF);

                    int bR = (wr * (255 - alpha) + tR * alpha) / 255;
                    int bG = (wg * (255 - alpha) + tG * alpha) / 255;
                    int bB = (wb * (255 - alpha) + tB * alpha) / 255;

                    _canvas.DrawPoint(Color.FromArgb(bR, bG, bB), px, py);
                }
                else
                {
                    _canvas.DrawPoint(tintColor, px, py);
                }
            }
        }

        // Borda sutil de vidro translúcido
        DrawFrostedGlassBorder(x, y, w, h, cornerRadius, Color.FromArgb(70, 255, 255, 255));
    }

    public void DrawFrostedGlassBorder(int x, int y, int w, int h, int radius, Color color)
    {
        if (radius <= 0)
        {
            DrawRectangle(x, y, w, h, color);
            return;
        }

        DrawLine(x + radius, y, x + w - radius - 1, y, color);
        DrawLine(x + radius, y + h - 1, x + w - radius - 1, y + h - 1, color);
        DrawLine(x, y + radius, x, y + h - radius - 1, color);
        DrawLine(x + w - 1, y + radius, x + w - 1, y + h - radius - 1, color);
    }

    public void DrawTranslucentRectangle(int x, int y, int w, int h, Color tintColor)
    {
        DrawFrostedGlass(x, y, w, h, tintColor, alpha: 220, cornerRadius: 8);
    }

    /// <summary>
    /// Renderiza o Dock inferior profissional: cor cinza com gradiente branco/opacidade sem nenhuma transparência.
    /// Possui cantos arredondados de 20px, brilho metálico suave no topo e contorno nítido.
    /// </summary>
    /// <summary>
    /// Renderiza o Dock Flutuante do NybuOS RV (Tema Light 3D): formato quadrado/retangular sólido,
    /// APENAS COM AS VÉRTICES (CANTOS) ARREDONDADAS, fundo em cinza claro intenso com sutis degradês brancos.
    /// 100% opaco e sólido.
    /// </summary>
    public void DrawOpaqueGradientDock(int x, int y, int w, int h, int cornerRadius = 10)
    {
        if (_canvas == null || w <= 0 || h <= 0) return;

        int r2 = cornerRadius * cornerRadius;
        int innerR = Math.Max(0, cornerRadius - 1);
        int innerR2 = innerR * innerR;

        Color borderColor = Color.FromArgb(160, 165, 178);
        Color topHighlight = Color.FromArgb(255, 255, 255);

        for (int row = 0; row < h; row++)
        {
            int py = y + row;
            if (py < 0 || py >= Height) continue;

            float t = (float)row / (float)Math.Max(1, h - 1);
            // Cinza claro intenso: 242 -> 220 com sutis degradês brancos no topo
            int gr, gg, gb;
            if (t < 0.25f)
            {
                float lt = t / 0.25f;
                gr = (int)(250 - (lt * 12));
                gg = (int)(252 - (lt * 13));
                gb = (int)(255 - (lt * 14));
            }
            else
            {
                float lt = (t - 0.25f) / 0.75f;
                gr = (int)(238 - (lt * 16));
                gg = (int)(239 - (lt * 16));
                gb = (int)(241 - (lt * 15));
            }
            Color rowColor = Color.FromArgb(gr, gg, gb);

            for (int col = 0; col < w; col++)
            {
                int px = x + col;
                if (px < 0 || px >= Width) continue;

                bool isBorder = false;

                // Verificação estrita apenas das 4 vértices (cantos)
                if (col < cornerRadius && row < cornerRadius)
                {
                    int dx = cornerRadius - col;
                    int dy = cornerRadius - row;
                    int distSq = dx * dx + dy * dy;
                    if (distSq > r2) continue; // Fora do raio do vértice
                    if (distSq >= innerR2) isBorder = true;
                }
                else if (col >= w - cornerRadius && row < cornerRadius)
                {
                    int dx = col - (w - cornerRadius - 1);
                    int dy = cornerRadius - row;
                    int distSq = dx * dx + dy * dy;
                    if (distSq > r2) continue;
                    if (distSq >= innerR2) isBorder = true;
                }
                else if (col < cornerRadius && row >= h - cornerRadius)
                {
                    int dx = cornerRadius - col;
                    int dy = row - (h - cornerRadius - 1);
                    int distSq = dx * dx + dy * dy;
                    if (distSq > r2) continue;
                    if (distSq >= innerR2) isBorder = true;
                }
                else if (col >= w - cornerRadius && row >= h - cornerRadius)
                {
                    int dx = col - (w - cornerRadius - 1);
                    int dy = row - (h - cornerRadius - 1);
                    int distSq = dx * dx + dy * dy;
                    if (distSq > r2) continue;
                    if (distSq >= innerR2) isBorder = true;
                }
                else
                {
                    if (row == 0 || row == h - 1 || col == 0 || col == w - 1)
                    {
                        isBorder = true;
                    }
                }

                if (isBorder)
                {
                    _canvas.DrawPoint(ApplyBrightness(borderColor), px, py);
                }
                else if (row == 1 && col >= cornerRadius && col < w - cornerRadius)
                {
                    // Sutil linha de brilho branco superior
                    _canvas.DrawPoint(ApplyBrightness(topHighlight), px, py);
                }
                else
                {
                    // 100% Sólido e Opaco, cinza claro intenso
                    _canvas.DrawPoint(ApplyBrightness(rowColor), px, py);
                }
            }
        }
    }

    /// <summary>
    /// Renderiza a barra superior 3D Light: branca no topo com degradês cinzas profundos e relevo 3D.
    /// </summary>
    public void Draw3DLightTopBar(int screenW, int height)
    {
        if (_canvas == null || screenW <= 0 || height <= 0) return;

        Color bottomShadow = Color.FromArgb(135, 140, 154);

        for (int y = 0; y < height; y++)
        {
            float t = (float)y / (float)Math.Max(1, height - 1);
            int r, g, b;
            if (t < 0.55f)
            {
                float lt = t / 0.55f;
                r = (int)(255 - (lt * 18));
                g = (int)(255 - (lt * 18));
                b = (int)(255 - (lt * 14));
            }
            else
            {
                float lt = (t - 0.55f) / 0.45f;
                r = (int)(237 - (lt * 48));
                g = (int)(237 - (lt * 47));
                b = (int)(241 - (lt * 44));
            }
            Color rowColor = Color.FromArgb(r, g, b);

            for (int x = 0; x < screenW; x++)
            {
                _canvas.DrawPoint(ApplyBrightness(rowColor), x, y);
            }
        }

        // Borda inferior 3D em cinza profundo
        for (int x = 0; x < screenW; x++)
        {
            _canvas.DrawPoint(ApplyBrightness(bottomShadow), x, height - 1);
        }
    }

    /// <summary>
    /// Renderiza a moldura de janela no tema Light com cabeçalho idêntico ao dock (cinza claro intenso com sutis degradês brancos)
    /// e corpo claro em design moderno 3D.
    /// </summary>
    public void DrawWindowFrameLight(int x, int y, int w, int h, int titleH, int cornerRadius, bool isActive)
    {
        if (_canvas == null || w <= 0 || h <= 0) return;

        int r2 = cornerRadius * cornerRadius;
        int innerR = Math.Max(0, cornerRadius - 1);
        int innerR2 = innerR * innerR;

        Color borderColor = isActive ? Color.FromArgb(155, 160, 174) : Color.FromArgb(185, 190, 202);
        Color titleHighlight = Color.FromArgb(255, 255, 255);
        Color titleDivider = Color.FromArgb(170, 175, 188);
        Color bodyBg = Color.FromArgb(246, 247, 251);

        for (int row = 0; row < h; row++)
        {
            int py = y + row;
            if (py < 0 || py >= Height) continue;

            bool isTitle = row < titleH;
            Color rowColor;

            if (isTitle)
            {
                float t = (float)row / (float)Math.Max(1, titleH - 1);
                int gr, gg, gb;
                if (t < 0.25f)
                {
                    float lt = t / 0.25f;
                    gr = (int)(250 - (lt * 12));
                    gg = (int)(252 - (lt * 13));
                    gb = (int)(255 - (lt * 14));
                }
                else
                {
                    float lt = (t - 0.25f) / 0.75f;
                    gr = (int)(238 - (lt * 16));
                    gg = (int)(239 - (lt * 16));
                    gb = (int)(241 - (lt * 15));
                }
                rowColor = Color.FromArgb(gr, gg, gb);
            }
            else
            {
                rowColor = bodyBg;
            }

            for (int col = 0; col < w; col++)
            {
                int px = x + col;
                if (px < 0 || px >= Width) continue;

                bool isBorder = false;

                // Vértices superiores (no cabeçalho)
                if (col < cornerRadius && row < cornerRadius)
                {
                    int dx = cornerRadius - col;
                    int dy = cornerRadius - row;
                    int distSq = dx * dx + dy * dy;
                    if (distSq > r2) continue;
                    if (distSq >= innerR2) isBorder = true;
                }
                else if (col >= w - cornerRadius && row < cornerRadius)
                {
                    int dx = col - (w - cornerRadius - 1);
                    int dy = cornerRadius - row;
                    int distSq = dx * dx + dy * dy;
                    if (distSq > r2) continue;
                    if (distSq >= innerR2) isBorder = true;
                }
                // Vértices inferiores (no corpo da janela)
                else if (col < cornerRadius && row >= h - cornerRadius)
                {
                    int dx = cornerRadius - col;
                    int dy = row - (h - cornerRadius - 1);
                    int distSq = dx * dx + dy * dy;
                    if (distSq > r2) continue;
                    if (distSq >= innerR2) isBorder = true;
                }
                else if (col >= w - cornerRadius && row >= h - cornerRadius)
                {
                    int dx = col - (w - cornerRadius - 1);
                    int dy = row - (h - cornerRadius - 1);
                    int distSq = dx * dx + dy * dy;
                    if (distSq > r2) continue;
                    if (distSq >= innerR2) isBorder = true;
                }
                else
                {
                    if (row == 0 || row == h - 1 || col == 0 || col == w - 1)
                    {
                        isBorder = true;
                    }
                }

                if (isBorder)
                {
                    _canvas.DrawPoint(ApplyBrightness(borderColor), px, py);
                }
                else if (row == titleH - 1)
                {
                    // Divisória do cabeçalho com o corpo da janela
                    _canvas.DrawPoint(ApplyBrightness(titleDivider), px, py);
                }
                else if (row == 1 && col >= cornerRadius && col < w - cornerRadius)
                {
                    // Sutil brilho branco superior
                    _canvas.DrawPoint(ApplyBrightness(titleHighlight), px, py);
                }
                else
                {
                    _canvas.DrawPoint(ApplyBrightness(rowColor), px, py);
                }
            }
        }
    }

    /// <summary>
    /// Renderiza um retângulo preenchido com cantos arredondados e borda sólida.
    /// </summary>
    public void DrawFilledRoundedRectangle(int x, int y, int w, int h, int radius, Color fill, Color border)
    {
        if (_canvas == null || w <= 0 || h <= 0) return;

        int r2 = radius * radius;
        int innerR2 = (radius - 1) * (radius - 1);

        for (int row = 0; row < h; row++)
        {
            int py = y + row;
            if (py < 0 || py >= Height) continue;

            for (int col = 0; col < w; col++)
            {
                int px = x + col;
                if (px < 0 || px >= Width) continue;

                bool isBorder = false;

                if (col < radius && row < radius)
                {
                    int dx = radius - col;
                    int dy = radius - row;
                    int distSq = dx * dx + dy * dy;
                    if (distSq > r2) continue;
                    if (distSq >= innerR2) isBorder = true;
                }
                else if (col >= w - radius && row < radius)
                {
                    int dx = col - (w - radius - 1);
                    int dy = radius - row;
                    int distSq = dx * dx + dy * dy;
                    if (distSq > r2) continue;
                    if (distSq >= innerR2) isBorder = true;
                }
                else if (col < radius && row >= h - radius)
                {
                    int dx = radius - col;
                    int dy = row - (h - radius - 1);
                    int distSq = dx * dx + dy * dy;
                    if (distSq > r2) continue;
                    if (distSq >= innerR2) isBorder = true;
                }
                else if (col >= w - radius && row >= h - radius)
                {
                    int dx = col - (w - radius - 1);
                    int dy = row - (h - radius - 1);
                    int distSq = dx * dx + dy * dy;
                    if (distSq > r2) continue;
                    if (distSq >= innerR2) isBorder = true;
                }
                else
                {
                    if (row == 0 || row == h - 1 || col == 0 || col == w - 1) isBorder = true;
                }

                Color c = isBorder ? border : fill;
                _canvas.DrawPoint(ApplyBrightness(c), px, py);
            }
        }
    }

    /// <summary>
    /// Renderiza texto usando nossa própria fonte idêntica à do Cosmos OS (CosmosNativeFont),
    /// sem cortar letras nem sofrer bugs de alinhamento.
    /// </summary>
    public void DrawString(int x, int y, string text, Color color, Color? bg = null)
    {
        if (string.IsNullOrEmpty(text) || _canvas == null) return;
        if (x >= Width || y >= Height) return;

        Color finalCol = ApplyBrightness(color);

        int curX = x;
        int curY = y;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\n')
            {
                curX = x;
                curY += CosmosNativeFont.Height + 2;
                continue;
            }
            if (c == '\r') continue;

            DrawChar(curX, curY, c, finalCol, bg);
            curX += CosmosNativeFont.Width;
        }
    }

    /// <summary>
    /// Renderiza um caractere individual com precisão de pixel da fonte oficial.
    /// </summary>
    public void DrawChar(int x, int y, char c, Color color, Color? bg = null)
    {
        if (_canvas == null) return;
        if (x + CosmosNativeFont.Width <= 0 || x >= Width || y + CosmosNativeFont.Height <= 0 || y >= Height)
            return;

        int w = CosmosNativeFont.Width;
        int h = CosmosNativeFont.Height;

        for (int row = 0; row < h; row++)
        {
            int py = y + row;
            if (py < 0 || py >= Height) continue;

            for (int col = 0; col < w; col++)
            {
                int px = x + col;
                if (px < 0 || px >= Width) continue;

                if (CosmosNativeFont.GetPixel(c, col, row))
                {
                    _canvas.DrawPoint(color, px, py);
                }
                else if (bg.HasValue)
                {
                    _canvas.DrawPoint(bg.Value, px, py);
                }
            }
        }
    }

    /// <summary>
    /// Renderiza texto escalado (ex: 2x para Full HD 1080p) nítido e sem artefatos.
    /// </summary>
    public void DrawStringScale(int x, int y, string text, Color color, int scale = 2, Color? bg = null)
    {
        if (string.IsNullOrEmpty(text) || _canvas == null) return;
        if (scale <= 1)
        {
            DrawString(x, y, text, color, bg);
            return;
        }
        if (x >= Width || y >= Height) return;

        Color finalCol = ApplyBrightness(color);
        int curX = x;
        int curY = y;
        int charW = CosmosNativeFont.Width * scale;
        int charH = CosmosNativeFont.Height * scale;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\n')
            {
                curX = x;
                curY += charH + (2 * scale);
                continue;
            }
            if (c == '\r') continue;

            DrawCharScale(curX, curY, c, finalCol, scale, bg);
            curX += charW;
        }
    }

    public void DrawCharScale(int x, int y, char c, Color color, int scale = 2, Color? bg = null)
    {
        if (_canvas == null) return;
        if (scale <= 1)
        {
            DrawChar(x, y, c, color, bg);
            return;
        }

        int w = CosmosNativeFont.Width;
        int h = CosmosNativeFont.Height;

        for (int row = 0; row < h; row++)
        {
            int basePy = y + (row * scale);
            for (int col = 0; col < w; col++)
            {
                int basePx = x + (col * scale);
                bool hasPixel = CosmosNativeFont.GetPixel(c, col, row);

                for (int sy = 0; sy < scale; sy++)
                {
                    int py = basePy + sy;
                    if (py < 0 || py >= Height) continue;
                    for (int sx = 0; sx < scale; sx++)
                    {
                        int px = basePx + sx;
                        if (px < 0 || px >= Width) continue;

                        if (hasPixel)
                        {
                            _canvas.DrawPoint(color, px, py);
                        }
                        else if (bg.HasValue)
                        {
                            _canvas.DrawPoint(bg.Value, px, py);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Renderiza um ícone ou imagem (IconImage) com suporte a transparência e clipping seguro.
    /// </summary>
    public void DrawIcon(int x, int y, IconImage icon)
    {
        if (icon == null || _canvas == null) return;

        int iw = icon.Width;
        int ih = icon.Height;

        for (int row = 0; row < ih; row++)
        {
            int py = y + row;
            if (py < 0 || py >= Height) continue;

            int rowOffset = row * iw;

            for (int col = 0; col < iw; col++)
            {
                int px = x + col;
                if (px < 0 || px >= Width) continue;

                uint argb = icon.Pixels[rowOffset + col];
                byte alpha = (byte)((argb >> 24) & 0xFF);
                if (alpha == 0) continue;

                byte r = (byte)((argb >> 16) & 0xFF);
                byte g = (byte)((argb >> 8) & 0xFF);
                byte b = (byte)(argb & 0xFF);

                _canvas.DrawPoint(ApplyBrightness(Color.FromArgb(r, g, b)), px, py);
            }
        }
    }

    /// <summary>
    /// Renderiza o papel de parede HD oficial com alta performance.
    /// </summary>
    public void DrawWallpaper(IconImage wallpaper)
    {
        if (wallpaper == null || _canvas == null) return;
        _activeWallpaper = wallpaper;

        int ww = Math.Min(wallpaper.Width, Width);
        int wh = Math.Min(wallpaper.Height, Height);
        bool hasBrightness = Math.Abs(BrightnessMultiplier - 1.0f) >= 0.05f;

        for (int row = 0; row < wh; row++)
        {
            int rowOffset = row * wallpaper.Width;
            for (int col = 0; col < ww; col++)
            {
                uint argb = wallpaper.Pixels[rowOffset + col];
                byte r = (byte)((argb >> 16) & 0xFF);
                byte g = (byte)((argb >> 8) & 0xFF);
                byte b = (byte)(argb & 0xFF);

                Color c = Color.FromArgb(r, g, b);
                if (hasBrightness)
                {
                    c = ApplyBrightness(c);
                }

                _canvas.DrawPoint(c, col, row);
            }
        }
    }

    /// <summary>
    /// Renderiza a barra superior (Top Menu Bar) estilo macOS com vidro fosco e placeholders.
    /// </summary>
    public void DrawModernTaskbar(int y, int width, int height)
    {
        DrawFrostedGlass(0, y, width, height, Theme.TopBarBg, alpha: 210, cornerRadius: 0);
        DrawLine(0, y + height - 1, width - 1, y + height - 1, Theme.TopBarBorder);
    }

    public void DrawModernStartButton(int x, int y, int w, int h, bool isOpen, bool isHover = false)
    {
        Color bg = isOpen ? Theme.AccentColor : (isHover ? Theme.SurfaceHover : Color.Transparent);
        if (isOpen || isHover)
        {
            DrawFrostedGlass(x, y, w, h, bg, alpha: 180, cornerRadius: 6);
        }

        if (IconResources.StartButtonIcon != null)
        {
            DrawIcon(x + (w - IconResources.StartButtonIcon.Width) / 2, y + (h - IconResources.StartButtonIcon.Height) / 2, IconResources.StartButtonIcon);
        }
    }

    public void DrawModernButton(int x, int y, int w, int h, string text, bool isAccent = false, bool isHover = false)
    {
        Color bg = isAccent ? Theme.AccentColor : (isHover ? Theme.SurfaceHover : Theme.ButtonBg);
        Color border = isAccent ? Theme.AccentColor : Theme.ButtonBorder;
        Color fg = isAccent ? Color.White : Theme.TextPrimary;

        DrawFrostedGlass(x, y, w, h, bg, alpha: 220, cornerRadius: 6);
        DrawRectangle(x, y, w, h, border);

        if (!string.IsNullOrEmpty(text))
        {
            int textW = text.Length * CosmosNativeFont.Width;
            int textX = x + Math.Max(2, (w - textW) / 2);
            int textY = y + Math.Max(2, (h - CosmosNativeFont.Height) / 2);
            DrawString(textX, textY, text, fg);
        }
    }

    public void Draw3DBevel(int x, int y, int w, int h, bool sunken)
    {
        if (w <= 1 || h <= 1) return;
        Color border = sunken ? Color.FromArgb(20, 24, 32) : Theme.ButtonBorder;
        DrawRectangle(x, y, w, h, border);
    }

    public void DrawButton(int x, int y, int w, int h, string text, bool pressed = false)
    {
        DrawModernButton(x, y, w, h, text, pressed);
    }

    public void DrawModernInputBox(int x, int y, int w, int h, string text, bool hasFocus)
    {
        Color bg = Theme.InputBg;
        Color border = hasFocus ? Theme.AccentColor : Theme.ButtonBorder;

        DrawFrostedGlass(x, y, w, h, bg, alpha: 230, cornerRadius: 6);
        DrawRectangle(x, y, w, h, border);

        if (!string.IsNullOrEmpty(text))
        {
            DrawString(x + 6, y + (h - CosmosNativeFont.Height) / 2, text, Theme.TextPrimary);
        }
    }

    /// <summary>
    /// Renderiza o cursor do mouse estilo macOS moderno (seta preta elegante com contorno branco nítido).
    /// </summary>
    public void DrawCursor(int x, int y)
    {
        Color black = Color.FromArgb(20, 20, 25);
        Color white = Color.White;

        int[] rowLengths = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 7, 4, 4, 2 };

        for (int r = 0; r < rowLengths.Length; r++)
        {
            int py = y + r;
            if (py < 0 || py >= Height) continue;

            int len = rowLengths[r];
            for (int c = 0; c <= len; c++)
            {
                int px = x + c;
                if (px < 0 || px >= Width) continue;

                if (c == 0 || c == len || r == 0 || r == rowLengths.Length - 1)
                {
                    _canvas.DrawPoint(white, px, py);
                }
                else
                {
                    _canvas.DrawPoint(black, px, py);
                }
            }
        }
    }
}
