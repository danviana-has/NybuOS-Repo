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

        try
        {
            TrueTypeRenderer.Initialize();
        }
        catch { }
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
    /// <summary>
    /// Renderiza uma superfície de vidro fosco (Frosted Glass) translúcida com desfoque (blur estilo macOS),
    /// difundindo e embaçando qualquer objeto ou elemento que passe atrás.
    /// </summary>
    public void DrawFrostedGlass(int x, int y, int w, int h, Color tintColor, int alpha = 165, int cornerRadius = 12)
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
                    _canvas.DrawPoint(Color.FromArgb(190, 255, 255, 255), px, py);
                    continue;
                }

                // Amostragem com filtro de desfoque (box blur) dos elementos que passam atrás
                int xL = px > 2 ? px - 2 : 0;
                int xR = px + 2 < Width ? px + 2 : Width - 1;
                int yU = py > 2 ? py - 2 : 0;
                int yD = py + 2 < Height ? py + 2 : Height - 1;

                Color c0 = _canvas.GetPointColor(px, py);
                Color cL = _canvas.GetPointColor(xL, py);
                Color cR = _canvas.GetPointColor(xR, py);
                Color cU = _canvas.GetPointColor(px, yU);
                Color cD = _canvas.GetPointColor(px, yD);

                int wr = (c0.R + cL.R + cR.R + cU.R + cD.R) / 5;
                int wg = (c0.G + cL.G + cR.G + cU.G + cD.G) / 5;
                int wb = (c0.B + cL.B + cR.B + cU.B + cD.B) / 5;

                // Fallback para o papel de parede se o canvas estiver preto
                if (wr == 0 && wg == 0 && wb == 0 && _activeWallpaper != null && px < _activeWallpaper.Width && py < _activeWallpaper.Height)
                {
                    uint wallPx = _activeWallpaper.Pixels[py * _activeWallpaper.Width + px];
                    wr = (int)((wallPx >> 16) & 0xFF);
                    wg = (int)((wallPx >> 8) & 0xFF);
                    wb = (int)(wallPx & 0xFF);
                }

                int bR = (wr * (255 - alpha) + tR * alpha) / 255;
                int bG = (wg * (255 - alpha) + tG * alpha) / 255;
                int bB = (wb * (255 - alpha) + tB * alpha) / 255;

                _canvas.DrawPoint(Color.FromArgb(bR, bG, bB), px, py);
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
        DrawFrostedGlass(x, y, w, h, tintColor, alpha: 165, cornerRadius: 8);
    }

    /// <summary>
    /// Renderiza o Dock Flutuante do NybuOS Purple estilo macOS:
    /// Borda grossa cinza (3px) com relevo 3D chanfrado (highlight no topo, shadow na base),
    /// e centro totalmente translúcido com efeito de desfoque (blur) dos objetos que passam atrás.
    /// </summary>
    public void DrawOpaqueGradientDock(int x, int y, int w, int h, int cornerRadius = 12)
    {
        DrawMacOSDock(x, y, w, h, cornerRadius);
    }

    public void DrawMacOSDock(int x, int y, int w, int h, int cornerRadius = 12)
    {
        if (_canvas == null || w <= 0 || h <= 0) return;

        int r2 = cornerRadius * cornerRadius;
        int innerR = Math.Max(0, cornerRadius - 3);
        int innerR2 = innerR * innerR;

        Color borderOuterHi = Theme.DockBorderOuterHighlight; // 3D highlight superior
        Color borderOuterSh = Theme.DockBorderOuterShadow;    // 3D shadow inferior
        Color borderMid = Theme.DockBorderMid;                // Borda cinza sólida média
        Color borderInner = Theme.DockBorderInner;            // Contorno interno
        Color tintColor = Theme.DockBg;
        int alpha = 20; // 8% de opacidade e 92% de transparência real (Dock quase totalmente transparente estilo macOS)

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

                // Distância das 4 vértices para cantos arredondados
                int distCornerSq = 0;
                bool isCorner = false;
                if (col < cornerRadius && row < cornerRadius)
                {
                    int dx = cornerRadius - col;
                    int dy = cornerRadius - row;
                    distCornerSq = dx * dx + dy * dy;
                    isCorner = true;
                }
                else if (col >= w - cornerRadius && row < cornerRadius)
                {
                    int dx = col - (w - cornerRadius - 1);
                    int dy = cornerRadius - row;
                    distCornerSq = dx * dx + dy * dy;
                    isCorner = true;
                }
                else if (col < cornerRadius && row >= h - cornerRadius)
                {
                    int dx = cornerRadius - col;
                    int dy = row - (h - cornerRadius - 1);
                    distCornerSq = dx * dx + dy * dy;
                    isCorner = true;
                }
                else if (col >= w - cornerRadius && row >= h - cornerRadius)
                {
                    int dx = col - (w - cornerRadius - 1);
                    int dy = row - (h - cornerRadius - 1);
                    distCornerSq = dx * dx + dy * dy;
                    isCorner = true;
                }

                if (isCorner && distCornerSq > r2)
                {
                    continue; // Fora do contorno do dock
                }

                // Determina se está na borda grossa (espessura de 3 pixels)
                bool isBorderRing0 = (row == 0 || row == h - 1 || col == 0 || col == w - 1) || (isCorner && distCornerSq >= (cornerRadius - 1) * (cornerRadius - 1));
                bool isBorderRing1 = !isBorderRing0 && ((row == 1 || row == h - 2 || col == 1 || col == w - 2) || (isCorner && distCornerSq >= (cornerRadius - 2) * (cornerRadius - 2)));
                bool isBorderRing2 = !isBorderRing0 && !isBorderRing1 && ((row == 2 || row == h - 3 || col == 2 || col == w - 3) || (isCorner && distCornerSq >= innerR2));

                if (isBorderRing0)
                {
                    // Anel externo com relevo 3D (claro no topo/esquerda, escuro na base/direita)
                    Color c = (row == 0 || (col == 0 && row < h / 2)) ? borderOuterHi : borderOuterSh;
                    _canvas.DrawPoint(ApplyBrightness(c), px, py);
                }
                else if (isBorderRing1)
                {
                    // Anel central da borda grossa cinza
                    _canvas.DrawPoint(ApplyBrightness(borderMid), px, py);
                }
                else if (isBorderRing2)
                {
                    // Anel interno da borda cinza 3D
                    _canvas.DrawPoint(ApplyBrightness(borderInner), px, py);
                }
                else
                {
                    // CENTRO TOTALMENTE TRANSLÚCIDO COM DESFOQUE DOS OBJETOS AO FUNDO (macOS Style)
                    if (row == 3 && col >= cornerRadius && col < w - cornerRadius)
                    {
                        // Sutil linha de reflexo de vidro (specular shine)
                        _canvas.DrawPoint(Color.FromArgb(210, 255, 255, 255), px, py);
                        continue;
                    }

                    int xL = px > 2 ? px - 2 : 0;
                    int xR = px + 2 < Width ? px + 2 : Width - 1;
                    int yU = py > 2 ? py - 2 : 0;
                    int yD = py + 2 < Height ? py + 2 : Height - 1;

                    Color c0 = _canvas.GetPointColor(px, py);
                    Color cL = _canvas.GetPointColor(xL, py);
                    Color cR = _canvas.GetPointColor(xR, py);
                    Color cU = _canvas.GetPointColor(px, yU);
                    Color cD = _canvas.GetPointColor(px, yD);

                    int wr = (c0.R + cL.R + cR.R + cU.R + cD.R) / 5;
                    int wg = (c0.G + cL.G + cR.G + cU.G + cD.G) / 5;
                    int wb = (c0.B + cL.B + cR.B + cU.B + cD.B) / 5;

                    if (wr == 0 && wg == 0 && wb == 0 && _activeWallpaper != null && px < _activeWallpaper.Width && py < _activeWallpaper.Height)
                    {
                        uint wallPx = _activeWallpaper.Pixels[py * _activeWallpaper.Width + px];
                        wr = (int)((wallPx >> 16) & 0xFF);
                        wg = (int)((wallPx >> 8) & 0xFF);
                        wb = (int)(wallPx & 0xFF);
                    }

                    int bR = (wr * (255 - alpha) + tR * alpha) / 255;
                    int bG = (wg * (255 - alpha) + tG * alpha) / 255;
                    int bB = (wb * (255 - alpha) + tB * alpha) / 255;

                    _canvas.DrawPoint(Color.FromArgb(bR, bG, bB), px, py);
                }
            }
        }
    }

    /// <summary>
    /// Renderiza a barra superior minimalista translúcida estilo macOS:
    /// Fundo em vidro fosco com desfoque e contorno sutil na base.
    /// </summary>
    public void Draw3DLightTopBar(int screenW, int height)
    {
        DrawMacOSTopBar(screenW, height);
    }

    public void DrawMacOSTopBar(int screenW, int height)
    {
        if (_canvas == null || screenW <= 0 || height <= 0) return;

        Color bottomBorder = Theme.TopBarBorder;
        Color tintColor = Theme.TopBarBg;
        int alpha = 160; // 62.7% opacidade (37.3% de transparência real estilo macOS)
        int tR = tintColor.R;
        int tG = tintColor.G;
        int tB = tintColor.B;

        for (int y = 0; y < height; y++)
        {
            if (y >= Height) break;

            if (y == height - 1)
            {
                // Borda inferior sutil estilo macOS
                for (int x = 0; x < screenW; x++)
                {
                    _canvas.DrawPoint(ApplyBrightness(bottomBorder), x, y);
                }
                continue;
            }

            for (int x = 0; x < screenW; x++)
            {
                if (x >= Width) break;

                if (y == 0)
                {
                    _canvas.DrawPoint(Color.FromArgb(200, 255, 255, 255), x, y);
                    continue;
                }

                // Amostragem com desfoque dos elementos ao fundo
                int xL = x > 2 ? x - 2 : 0;
                int xR = x + 2 < Width ? x + 2 : Width - 1;
                int yU = y > 2 ? y - 2 : 0;
                int yD = y + 2 < Height ? y + 2 : Height - 1;

                Color c0 = _canvas.GetPointColor(x, y);
                Color cL = _canvas.GetPointColor(xL, y);
                Color cR = _canvas.GetPointColor(xR, y);
                Color cU = _canvas.GetPointColor(x, yU);
                Color cD = _canvas.GetPointColor(x, yD);

                int wr = (c0.R + cL.R + cR.R + cU.R + cD.R) / 5;
                int wg = (c0.G + cL.G + cR.G + cU.G + cD.G) / 5;
                int wb = (c0.B + cL.B + cR.B + cU.B + cD.B) / 5;

                if (wr == 0 && wg == 0 && wb == 0 && _activeWallpaper != null && x < _activeWallpaper.Width && y < _activeWallpaper.Height)
                {
                    uint wallPx = _activeWallpaper.Pixels[y * _activeWallpaper.Width + x];
                    wr = (int)((wallPx >> 16) & 0xFF);
                    wg = (int)((wallPx >> 8) & 0xFF);
                    wb = (int)(wallPx & 0xFF);
                }

                int bR = (wr * (255 - alpha) + tR * alpha) / 255;
                int bG = (wg * (255 - alpha) + tG * alpha) / 255;
                int bB = (wb * (255 - alpha) + tB * alpha) / 255;

                _canvas.DrawPoint(Color.FromArgb(bR, bG, bB), x, y);
            }
        }
    }

    /// <summary>
    /// Renderiza a moldura de janela no tema minimalista macOS:
    /// Cabeçalho translúcido em vidro fosco com desfoque dos elementos que passam atrás,
    /// divisória nítida e corpo claro limpo.
    /// </summary>
    public void DrawWindowFrameLight(int x, int y, int w, int h, int titleH, int cornerRadius, bool isActive)
    {
        DrawMacOSWindowFrame(x, y, w, h, titleH, cornerRadius, isActive);
    }

    public void DrawMacOSWindowFrame(int x, int y, int w, int h, int titleH, int cornerRadius, bool isActive)
    {
        if (_canvas == null || w <= 0 || h <= 0) return;

        int r2 = cornerRadius * cornerRadius;
        int innerR = Math.Max(0, cornerRadius - 1);
        int innerR2 = innerR * innerR;

        Color borderColor = isActive ? Theme.WindowBorder : Theme.WindowBorderInactive;
        Color titleHighlight = Color.FromArgb(210, 255, 255, 255);
        Color titleDivider = Color.FromArgb(190, 195, 208);
        Color bodyBg = Theme.WindowBg;
        Color titleTint = Theme.WindowHeaderBg;
        int titleAlpha = isActive ? 160 : 175;
        int bodyAlpha = 168;

        int tR = titleTint.R;
        int tG = titleTint.G;
        int tB = titleTint.B;

        for (int row = 0; row < h; row++)
        {
            int py = y + row;
            if (py < 0 || py >= Height) continue;

            bool isTitle = row < titleH;

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
                    // Divisória sutil do cabeçalho com o corpo da janela
                    _canvas.DrawPoint(ApplyBrightness(titleDivider), px, py);
                }
                else if (row == 1 && col >= cornerRadius && col < w - cornerRadius)
                {
                    // Sutil brilho de vidro superior
                    _canvas.DrawPoint(titleHighlight, px, py);
                }
                else if (isTitle)
                {
                    // Cabeçalho Translúcido com efeito de desfoque dos elementos que passam atrás
                    int xL = px > 2 ? px - 2 : 0;
                    int xR = px + 2 < Width ? px + 2 : Width - 1;
                    int yU = py > 2 ? py - 2 : 0;
                    int yD = py + 2 < Height ? py + 2 : Height - 1;

                    Color c0 = _canvas.GetPointColor(px, py);
                    Color cL = _canvas.GetPointColor(xL, py);
                    Color cR = _canvas.GetPointColor(xR, py);
                    Color cU = _canvas.GetPointColor(px, yU);
                    Color cD = _canvas.GetPointColor(px, yD);

                    int wr = (c0.R + cL.R + cR.R + cU.R + cD.R) / 5;
                    int wg = (c0.G + cL.G + cR.G + cU.G + cD.G) / 5;
                    int wb = (c0.B + cL.B + cR.B + cU.B + cD.B) / 5;

                    if (wr == 0 && wg == 0 && wb == 0 && _activeWallpaper != null && px < _activeWallpaper.Width && py < _activeWallpaper.Height)
                    {
                        uint wallPx = _activeWallpaper.Pixels[py * _activeWallpaper.Width + px];
                        wr = (int)((wallPx >> 16) & 0xFF);
                        wg = (int)((wallPx >> 8) & 0xFF);
                        wb = (int)(wallPx & 0xFF);
                    }

                    int bR = (wr * (255 - titleAlpha) + tR * titleAlpha) / 255;
                    int bG = (wg * (255 - titleAlpha) + tG * titleAlpha) / 255;
                    int bB = (wb * (255 - titleAlpha) + tB * titleAlpha) / 255;

                    _canvas.DrawPoint(Color.FromArgb(bR, bG, bB), px, py);
                }
                else
                {
                    // Corpo da janela translúcido com efeito de desfoque dos elementos ao fundo (66% opacidade / 34% transparência estilo macOS)
                    int xL = px > 2 ? px - 2 : 0;
                    int xR = px + 2 < Width ? px + 2 : Width - 1;
                    int yU = py > 2 ? py - 2 : 0;
                    int yD = py + 2 < Height ? py + 2 : Height - 1;

                    Color c0 = _canvas.GetPointColor(px, py);
                    Color cL = _canvas.GetPointColor(xL, py);
                    Color cR = _canvas.GetPointColor(xR, py);
                    Color cU = _canvas.GetPointColor(px, yU);
                    Color cD = _canvas.GetPointColor(px, yD);

                    int wr = (c0.R + cL.R + cR.R + cU.R + cD.R) / 5;
                    int wg = (c0.G + cL.G + cR.G + cU.G + cD.G) / 5;
                    int wb = (c0.B + cL.B + cR.B + cU.B + cD.B) / 5;

                    if (wr == 0 && wg == 0 && wb == 0 && _activeWallpaper != null && px < _activeWallpaper.Width && py < _activeWallpaper.Height)
                    {
                        uint wallPx = _activeWallpaper.Pixels[py * _activeWallpaper.Width + px];
                        wr = (int)((wallPx >> 16) & 0xFF);
                        wg = (int)((wallPx >> 8) & 0xFF);
                        wb = (int)(wallPx & 0xFF);
                    }

                    int bR = (wr * (255 - bodyAlpha) + bodyBg.R * bodyAlpha) / 255;
                    int bG = (wg * (255 - bodyAlpha) + bodyBg.G * bodyAlpha) / 255;
                    int bB = (wb * (255 - bodyAlpha) + bodyBg.B * bodyAlpha) / 255;

                    _canvas.DrawPoint(Color.FromArgb(bR, bG, bB), px, py);
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
    /// Mede a largura em pixels de uma string, utilizando TrueType Arial se carregada
    /// ou fallback para a largura fixa de CosmosNativeFont.
    /// </summary>
    public int MeasureString(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        if (TrueTypeRenderer.IsLoaded)
        {
            return TrueTypeRenderer.MeasureString(text);
        }
        return text.Length * CosmosNativeFont.Width;
    }

    /// <summary>
    /// Renderiza texto usando fonte TrueType Arial com Alpha Blending avançado (0 a 255)
    /// com o fundo da tela para contornos suaves e arredondados.
    /// Fallback automático para CosmosNativeFont se a fonte TTF não estiver disponível.
    /// </summary>
    public void DrawString(int x, int y, string text, Color color, Color? bg = null)
    {
        if (string.IsNullOrEmpty(text) || _canvas == null) return;
        if (x >= Width || y >= Height) return;

        Color finalCol = ApplyBrightness(color);

        if (TrueTypeRenderer.IsLoaded)
        {
            TrueTypeRenderer.DrawString(_canvas, x, y, text, finalCol, bg, _activeWallpaper);
            return;
        }

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
            int textW = MeasureString(text);
            int textH = TrueTypeRenderer.IsLoaded ? TrueTypeRenderer.DefaultHeight : CosmosNativeFont.Height;
            int textX = x + Math.Max(2, (w - textW) / 2);
            int textY = y + Math.Max(2, (h - textH) / 2);
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
            int textH = TrueTypeRenderer.IsLoaded ? TrueTypeRenderer.DefaultHeight : CosmosNativeFont.Height;
            DrawString(x + 6, y + (h - textH) / 2, text, Theme.TextPrimary);
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
